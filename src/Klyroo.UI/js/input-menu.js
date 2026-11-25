import { ensureId, float, observeChildren, Reporter } from './shared.js';
import { SEP, navigable, syncGroups } from './form2-shared.js';
import { createDebouncer } from './debounce.js';

/**
 * Text field with a list of suggestions (WAI-ARIA APG combobox with list autocomplete): focus stays in the field,
 * the active suggestion is announced through aria-activedescendant. Free text is the value, unless data-strict is
 * set: then only a suggestion is a value, and the text snaps back to it when the field loses focus.
 *
 * <kly-input-menu value="…" data-strict>
 *   <input type="hidden" data-slot="value" name="…" value="…">
 *   <input role="combobox" data-slot="input" aria-autocomplete="list" aria-expanded="false">
 *   <div popover="manual" data-slot="content"><div role="listbox"><div role="option" data-value data-label>…</div></div><p data-slot="empty" hidden>…</p></div>
 * </kly-input-menu>
 *
 * With data-remote (an interactive .NET component that loads the suggestions from a source) the browser stops filtering: the list is
 * whatever .NET renders. The text is reported as kly-search (value = the text) when the list opens (at once) and after data-debounce
 * milliseconds of quiet while typing. .NET renders data-generation on the listbox, which changes each time the suggestions are replaced
 * (not when a page is added): the list goes back to the top and, for a strict field, the first one is highlighted. The label of a strict
 * value is kept here (a search can drop its option), seeded by data-known (value, label). data-inert-list (a remote field that is not
 * interactive yet) never opens the list: there is nothing to list.
 */
export class KlyInputMenuElement extends HTMLElement {
  static observedAttributes = ['data-kly-seq', 'value', 'data-known', 'data-epoch'];

  #observer = null;
  #stopFloating = null;
  #active = null;
  #reporter = new Reporter(this);
  #value = '';
  #loaded = false;
  #labels = new Map();
  #generation = null;
  #awaiting = false;
  #reported = null;
  #generations = null;
  #searchSequence = 0;
  #debouncer = createDebouncer(() => this.#reportSearch(this.#input?.value ?? ''));

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('input', this.#onInput);
    this.addEventListener('change', this.#onChange);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('mousedown', this.#onMouseDown);
    this.addEventListener('pointermove', this.#onPointerMove);
    this.addEventListener('focusout', this.#onFocusOut);
    this.#observer = observeChildren(this, () => this.#wire());
    // A new generation with the same suggestions changes no child: the highlight still has to follow.
    this.#generations = new MutationObserver(() => this.#wire());
    this.#generations.observe(this, { subtree: true, attributes: true, attributeFilter: ['data-generation'] });
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
    this.#generations?.disconnect();
    this.#stopFloating?.();
    this.#debouncer.cancel();
  }

  attributeChangedCallback(name, oldValue) {
    if (!this.isConnected) return;
    if (name === 'data-epoch' && oldValue !== null) {
      // The source changed (a new OptionsKey): what was kept about the old one is wrong.
      this.#labels.clear();
      this.#generation = null;
      this.#awaiting = false;
      this.#reported = null;
      this.#debouncer.cancel();
    }
    if (name === 'data-known' || name === 'data-epoch') {
      this.#seedLabels();
      return;
    }
    if (name === 'data-kly-seq') queueMicrotask(() => this.#applyRendered(true));
    else this.#applyRendered();
  }

  refresh() {
    this.#reporter.adopt();
    this.#applyRendered(this.#reporter.bound);
    this.#writeHidden();
    this.#paintSelected();
  }

  get #remote() {
    return this.hasAttribute('data-remote');
  }

  get #strict() {
    return this.hasAttribute('data-strict');
  }

  get #input() {
    return this.querySelector('[data-slot="input"]');
  }

  get #content() {
    return this.querySelector(':scope > [popover]');
  }

  #options() {
    return [...this.querySelectorAll('[role="option"]')];
  }

  #labelOf(value) {
    // The label kept here (a search may have dropped the option; .NET names values in data-known in every render mode), and else, for a
    // remote field, the raw value (what an unresolved value shows, also before the page is interactive).
    return this.#options().find((o) => o.dataset.value === value)?.dataset.label ?? this.#labels.get(value) ?? (this.#remote || this.hasAttribute('data-inert-list') ? value : null);
  }

  /** The labels .NET knows (data-known: value, label, ...), in every render mode: a static render must not lose a label to the raw value. */
  #seedLabels() {
    const known = (this.getAttribute('data-known') ?? '').split(SEP);
    for (let i = 0; i + 1 < known.length; i += 2) {
      if (!this.#labels.has(known[i])) this.#labels.set(known[i], known[i + 1]);
    }
  }

  #remember(option) {
    if (option.dataset.value !== undefined) this.#labels.set(option.dataset.value, option.dataset.label ?? option.dataset.value);
  }

  /** What a render of the suggestions changes in remote mode: the labels to remember, and the highlight when they were replaced. */
  #syncRemote(listbox) {
    for (const option of this.#options()) this.#remember(option);
    const generation = listbox.dataset.generation ?? null;
    if (generation !== this.#generation) {
      this.#generation = generation;
      this.#awaiting = false;
      const scroller = this.querySelector('[data-slot="scroller"]');
      if (scroller) scroller.scrollTop = 0;
      if (this.#isOpen()) this.#highlight(this.#strict ? (navigable(this.#options())[0] ?? null) : null);
    } else if (this.#active && !this.#active.isConnected) {
      this.#active = null;
      this.#input?.removeAttribute('aria-activedescendant');
    }
    this.#paintSelected();
  }

  #reportSearch(term) {
    this.#reported = term.trim();
    this.#awaiting = true;
    this.dispatchEvent(new CustomEvent('kly-search', { bubbles: true, detail: { value: term, sequence: ++this.#searchSequence } }));
  }

  #wire() {
    const listbox = this.querySelector('[role="listbox"]');
    if (!listbox) return;
    this.#input?.setAttribute('aria-controls', ensureId(listbox, 'listbox'));
    for (const option of this.#options()) ensureId(option, 'option');
    this.#seedLabels();
    if (this.#remote) this.#syncRemote(listbox);
    if (!this.#loaded) {
      this.#loaded = true;
      this.#applyRendered(true);
    }
  }

  /** Shows the rendered value when a render changed it (force: a bound render arrived, or the first one). */
  #applyRendered(force = false) {
    const text = this.#reporter.take('value', force);
    if (text === undefined) return;
    const value = text ?? '';
    this.#value = value;
    this.#writeHidden();
    this.#paintSelected();
    const input = this.#input;
    if (input && document.activeElement !== input) input.value = this.#strict ? (this.#labelOf(value) ?? '') : value;
  }

  /** Strict: the option that is the value is selected. Blazor renders it once; after that it is the script's to keep. */
  #paintSelected() {
    for (const option of this.#options()) option.setAttribute('aria-selected', String(this.#strict && option.dataset.value === this.#value));
  }

  #writeHidden() {
    const hidden = this.querySelector('input[data-slot="value"]');
    if (hidden) hidden.value = this.#value;
  }

  #isOpen() {
    return this.#content?.matches(':popover-open') ?? false;
  }

  #open() {
    const content = this.#content;
    const input = this.#input;
    if (!content || !input || input.disabled || input.readOnly || this.hasAttribute('data-inert-list')) return;
    if (!this.#isOpen()) {
      content.showPopover();
      this.#stopFloating = float(input, content, { placement: 'bottom-start', distance: 4, matchWidth: true });
    }
    input.setAttribute('aria-expanded', 'true');
    this.dataset.state = 'open';
  }

  #close() {
    const content = this.#content;
    if (this.#isOpen()) content.hidePopover();
    this.#stopFloating?.();
    this.#stopFloating = null;
    this.#debouncer.cancel();
    this.#input?.setAttribute('aria-expanded', 'false');
    this.dataset.state = 'closed';
    this.#highlight(null);
  }

  /**
   * Remote: the suggestions are not filtered here, .NET renders the answer. The text is reported (at once when the list opens, after the
   * quiet time while typing); until the answer replaces the suggestions nothing is highlighted, so Enter cannot choose one of the previous text.
   */
  #filterRemote(text, immediate) {
    // The text .NET already answered (a trailing space, a letter typed and deleted within the pause): nothing is asked, so no answer will come to
    // highlight a suggestion again. An error is not an answer: asking again is how it is retried.
    const settled = text.trim() === this.#reported && !this.#awaiting && !this.querySelector('[data-slot="error"]');
    if (settled) {
      this.#debouncer.cancel();
      if (!this.#active?.isConnected) this.#highlight(this.#strict ? (navigable(this.#options())[0] ?? null) : null);
      return 0;
    }
    // The list shown is the one of another text, until the answer replaces it: nothing is highlighted, so Enter cannot choose from it.
    this.#highlight(null);
    if (immediate) {
      this.#debouncer.cancel();
      this.#reportSearch(text);
      return 0;
    }
    const delay = Number(this.getAttribute('data-debounce'));
    this.#debouncer.schedule(Number.isFinite(delay) && delay >= 0 ? delay : 250);
    return 0;
  }

  #filter(text, { immediate = false } = {}) {
    if (this.#remote) return this.#filterRemote(text, immediate);
    const query = text.trim().toLowerCase();
    let shown = 0;
    for (const option of this.#options()) {
      const match = !query || (option.dataset.label ?? '').toLowerCase().includes(query);
      option.hidden = !match;
      if (match) shown++;
    }
    syncGroups(this);
    const empty = this.querySelector('[data-slot="empty"]');
    if (empty) empty.hidden = shown > 0;
    return shown;
  }

  #highlight(option) {
    this.#active?.removeAttribute('data-highlighted');
    this.#active = option;
    const input = this.#input;
    if (option) {
      option.setAttribute('data-highlighted', '');
      option.scrollIntoView({ block: 'nearest' });
      input?.setAttribute('aria-activedescendant', option.id);
    } else {
      input?.removeAttribute('aria-activedescendant');
    }
  }

  #commit(value, report) {
    if (value === this.#value) return;
    this.#value = value;
    this.#writeHidden();
    this.#paintSelected();
    if (report) {
      this.dispatchEvent(new Event('change', { bubbles: true }));
      this.#reporter.report('kly-change', { value });
    }
  }

  #choose(option) {
    if (!option || option.getAttribute('aria-disabled') === 'true') return;
    const input = this.#input;
    if (this.#remote) this.#remember(option);
    input.value = option.dataset.label ?? '';
    this.#commit(this.#strict ? option.dataset.value : input.value, true);
    this.#close();
  }

  #onInput = (event) => {
    const input = this.#input;
    if (event.target !== input) return;
    this.#filter(input.value);
    this.#open();
    if (!this.#remote) this.#highlight(this.#strict ? (navigable(this.#options())[0] ?? null) : null);
    // Emptying a strict field clears the value: no suggestion is left to be it.
    if (this.#strict) {
      if (input.value === '') this.#commit('', true);
      return;
    }
    // Free text is the value as it is typed (the hidden field posts it); .NET hears about it on change.
    this.#value = input.value;
    this.#writeHidden();
  };

  // The native change event of the field: free text commits when the field loses focus or Enter is pressed.
  #onChange = (event) => {
    if (event.target !== this.#input || this.#strict) return;
    event.stopPropagation();
    this.#value = this.#input.value;
    this.#writeHidden();
    this.dispatchEvent(new Event('change', { bubbles: true }));
    this.#reporter.report('kly-change', { value: this.#value });
  };

  #onKeyDown = (event) => {
    const input = this.#input;
    if (event.target !== input) return;
    const options = navigable(this.#options());
    const index = options.indexOf(this.#active);
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowUp': {
        event.preventDefault();
        if (!this.#isOpen()) {
          this.#filter(this.#strict ? '' : input.value, { immediate: true });
          this.#open();
          // Remote: the list shown is that of another text until the answer comes; it highlights then.
          if (this.#remote && this.#awaiting) break;
          const current = navigable(this.#options());
          this.#highlight(current.find((o) => o.dataset.value === this.#value) ?? (event.key === 'ArrowDown' ? current[0] : current.at(-1)) ?? null);
          break;
        }
        const next = event.key === 'ArrowDown' ? options[Math.min(index + 1, options.length - 1)] : options[Math.max(index - 1, 0)];
        if (next) this.#highlight(next);
        break;
      }
      case 'Enter':
        if (this.#isOpen() && this.#active) {
          event.preventDefault();
          this.#choose(this.#active);
        } else if (this.#isOpen()) {
          // Nothing to choose while the list failed to load: Enter asks again.
          const retry = this.querySelector('[data-slot="retry"]');
          if (retry) {
            event.preventDefault();
            retry.click();
          }
        }
        break;
      case 'Escape':
        if (this.#isOpen()) {
          event.preventDefault();
          this.#close();
        }
        break;
      case 'Tab':
        this.#close();
        break;
    }
  };

  #onClick = (event) => {
    const option = event.target.closest?.('[role="option"]');
    if (option && this.contains(option)) {
      this.#choose(option);
      return;
    }
    if (event.target === this.#input) {
      this.#filter(this.#strict ? '' : this.#input.value, { immediate: true });
      this.#open();
    }
  };

  // Keep focus in the field while the pointer works the list.
  #onMouseDown = (event) => {
    if (this.#content?.contains(event.target)) event.preventDefault();
  };

  #onPointerMove = (event) => {
    const option = event.target.closest?.('[role="option"]');
    if (option && option !== this.#active && option.getAttribute('aria-disabled') !== 'true') this.#highlight(option);
  };

  #onFocusOut = (event) => {
    if (event.relatedTarget && this.contains(event.relatedTarget)) return;
    this.#close();
    const input = this.#input;
    if (this.#strict && input) input.value = this.#labelOf(this.#value) ?? '';
  };
}

if (!customElements.get('kly-input-menu')) customElements.define('kly-input-menu', KlyInputMenuElement);
