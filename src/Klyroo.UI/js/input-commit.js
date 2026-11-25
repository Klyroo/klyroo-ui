import { Reporter, isTvMode } from './shared.js';
import { createDebouncer, parseDelay, holdsTypedText } from './debounce.js';

/**
 * Browser-side commit of a KlyInput that asks for it (data-kly-debounce and/or data-kly-clear-on-escape on the native
 * <input>; a plain KlyInput has neither and none of this runs):
 *
 * - data-kly-debounce="400": the value is reported once, 400 ms after the last keystroke ("0": on every input). Without
 *   the attribute the value is reported when it is committed (blur, Enter), like a plain input.
 * - Enter reports at once. Blur does too. Neither happens while an IME composition is in progress: the report waits for
 *   its end.
 * - data-kly-clear-on-escape: Escape empties a non-empty field and reports the empty value; on an empty field it is left to
 *   the page (a modal still closes).
 *
 * - SetValueAsync (KlyInput, from .NET) renders a new data-kly-force with the value in data-kly-set: whatever is pending is
 *   dropped, the text is replaced, and the sequence restarts from the one .NET rendered.
 *
 * The report is the kly-change event of every other Klyroo element, dispatched on the input with an increasing sequence;
 * .NET renders the last one back in data-kly-seq. Blazor writes the rendered Value into the input's `value` property, so
 * the property of each input in use is guarded: a write that would undo typed text is ignored (see holdsTypedText).
 */
const SELECTOR = 'input[data-kly-debounce], input[data-kly-clear-on-escape]';
const valueProperty = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value');
const states = new WeakMap();

const read = (input) => valueProperty.get.call(input);

function stateOf(input) {
  let state = states.get(input);
  if (state) return state;
  state = { dirty: false, composing: false, silent: false, reporter: new Reporter(input), debouncer: null };
  // A new <input> can appear while the component lives (its sequence is already N): continue from the rendered one.
  state.reporter.adopt();
  state.debouncer = createDebouncer(() => commit(input));
  states.set(input, state);
  Object.defineProperty(input, 'value', {
    configurable: true,
    get() {
      return valueProperty.get.call(this);
    },
    set(value) {
      if (holdsTypedText({ dirty: state.dirty, composing: state.composing, stale: state.reporter.isStale() })) return;
      valueProperty.set.call(this, value);
    },
  });
  return state;
}

/** Reports the current text if it has not been reported yet. */
function commit(input) {
  const state = states.get(input);
  if (!state) return;
  state.debouncer.cancel();
  if (!state.dirty) return;
  state.dirty = false;
  if (!input.isConnected) return;
  state.reporter.report('kly-change', { value: read(input) });
}

const target = (event) =>
  event.target instanceof HTMLInputElement && event.target.matches(SELECTOR) && !event.target.disabled ? event.target : null;

/** Starts the wait for the quiet period after the text changed (or reports at once for a delay of 0). */
function typed(input, state) {
  const delay = parseDelay(input.getAttribute('data-kly-debounce'));
  if (delay === null) return;
  if (delay === 0) commit(input);
  else state.debouncer.schedule(delay);
}

document.addEventListener('focusin', (event) => {
  const input = target(event);
  if (input) stateOf(input);
});

document.addEventListener('input', (event) => {
  const input = target(event);
  if (!input) return;
  const state = stateOf(input);
  if (state.silent) return;
  state.dirty = true;
  if (event.isComposing || state.composing) return;
  typed(input, state);
});

document.addEventListener('compositionstart', (event) => {
  const input = target(event);
  if (input) stateOf(input).composing = true;
});

document.addEventListener('compositionend', (event) => {
  const input = target(event);
  if (!input) return;
  const state = stateOf(input);
  state.composing = false;
  if (!state.dirty) return;
  // The field was left in the middle of the composition: nothing will report it later.
  if (document.activeElement !== input) commit(input);
  else typed(input, state);
});

// Leaving the field reports what is left, including text that is back to the value it started with (which fires no
// native change event). A composition in progress is reported when it ends.
document.addEventListener('focusout', (event) => {
  const input = target(event);
  if (!input) return;
  const state = stateOf(input);
  if (state.dirty && !state.composing) commit(input);
});

// SetValueAsync: a render with a new data-kly-force replaces the text, even text still waiting to be reported.
new MutationObserver((records) => {
  for (const record of records) {
    const input = record.target;
    if (!(input instanceof HTMLInputElement) || !input.matches(SELECTOR)) continue;
    const state = stateOf(input);
    state.debouncer.cancel();
    state.dirty = false;
    state.composing = false;
    state.reporter.adopt();
    valueProperty.set.call(input, input.getAttribute('data-kly-set') ?? '');
  }
}).observe(document.documentElement, { subtree: true, attributes: true, attributeFilter: ['data-kly-force'] });

// The native change event: blur, or Enter. Whatever is still waiting is reported now.
document.addEventListener('change', (event) => {
  const input = target(event);
  if (input && !stateOf(input).composing) commit(input);
});

document.addEventListener('keydown', (event) => {
  const input = target(event);
  if (!input || event.isComposing || event.key !== 'Enter') return;
  const state = stateOf(input);
  if (!state.composing) commit(input);
});

// Escape is handled in the capture phase of the window, before every other listener: the Escape that clears text is
// spent on it, so Blazor's own handlers (it listens on the document) and the page never see it. On an empty field it
// passes through and closes the modal or popover around it. In TV mode, while the field is being edited, Escape/Back
// leaves the editing mode first (spatial-nav.js), so it is left alone there.
window.addEventListener(
  'keydown',
  (event) => {
    if (event.key !== 'Escape' || event.isComposing) return;
    const input = target(event);
    if (!input || !input.hasAttribute('data-kly-clear-on-escape') || input.readOnly) return;
    if (isTvMode() && input.hasAttribute('data-kly-editing')) return;
    const state = stateOf(input);
    if (state.composing) return;
    if (read(input) === '') {
      commit(input);
      return;
    }
    valueProperty.set.call(input, '');
    state.dirty = true;
    // Other listeners learn about the change; this one must not start a quiet period for it.
    state.silent = true;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    state.silent = false;
    commit(input);
    event.preventDefault();
    event.stopImmediatePropagation();
  },
  true,
);
