import { Reporter } from './shared.js';
import { createDebouncer, parseDelay, holdsTypedText } from './debounce.js';

/**
 * Browser-side commit of a BxInput that asks for it (data-bx-debounce and/or data-bx-clear-on-escape on the native
 * <input>; a plain BxInput has neither and none of this runs):
 *
 * - data-bx-debounce="400": the value is reported once, 400 ms after the last keystroke ("0": on every input). Without
 *   the attribute the value is reported when it is committed (blur, Enter), like a plain input.
 * - Enter reports at once. Blur does too. Neither happens while an IME composition is in progress: the report waits for
 *   its end.
 * - data-bx-clear-on-escape: Escape empties a non-empty field and reports the empty value; on an empty field it is left to
 *   the page (a modal still closes).
 *
 * The report is the bx-change event of every other Blux element, dispatched on the input with an increasing sequence;
 * .NET renders the last one back in data-bx-seq. Blazor writes the rendered Value into the input's `value` property, so
 * the property of each input in use is guarded: a write that would undo typed text is ignored (see holdsTypedText).
 */
const SELECTOR = 'input[data-bx-debounce], input[data-bx-clear-on-escape]';
const valueProperty = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value');
const states = new WeakMap();

const read = (input) => valueProperty.get.call(input);

function stateOf(input) {
  let state = states.get(input);
  if (state) return state;
  state = { dirty: false, composing: false, silent: false, reporter: new Reporter(input), debouncer: null };
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
  state.reporter.report('bx-change', { value: read(input) });
}

const target = (event) =>
  event.target instanceof HTMLInputElement && event.target.matches(SELECTOR) && !event.target.disabled ? event.target : null;

/** Starts the wait for the quiet period after the text changed (or reports at once for a delay of 0). */
function typed(input, state) {
  const delay = parseDelay(input.getAttribute('data-bx-debounce'));
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
  if (state.dirty) typed(input, state);
});

// The native change event: blur, or Enter. Whatever is still waiting is reported now.
document.addEventListener('change', (event) => {
  const input = target(event);
  if (input && !stateOf(input).composing) commit(input);
});

document.addEventListener('keydown', (event) => {
  const input = target(event);
  if (!input || event.isComposing) return;
  const state = stateOf(input);
  if (state.composing) return;
  if (event.key === 'Enter') {
    commit(input);
  } else if (event.key === 'Escape' && input.hasAttribute('data-bx-clear-on-escape')) {
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
    // The Escape was spent on the text: it does not also close the modal or popover around the field.
    event.preventDefault();
    event.stopPropagation();
  }
});
