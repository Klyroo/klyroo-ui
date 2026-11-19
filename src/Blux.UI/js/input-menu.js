import { ensureId, float, observeChildren, Reporter } from './shared.js';
import { navigable } from './form2-shared.js';

/**
 * Text field with a list of suggestions (WAI-ARIA APG combobox with list autocomplete): focus stays in the field,
 * the active suggestion is announced through aria-activedescendant. Free text is the value, unless data-strict is
 * set: then only a suggestion is a value, and the text snaps back to it when the field loses focus.
 *
 * <bx-input-menu value="…" data-strict>
 *   <input type="hidden" data-slot="value" name="…" value="…">
 *   <input role="combobox" data-slot="input" aria-autocomplete="list" aria-expanded="false">
 *   <div popover="manual" data-slot="content"><div role="listbox"><div role="option" data-value data-label>…</div></div><p data-slot="empty" hidden>…</p></div>
 * </bx-input-menu>
 */
export class BxInputMenuElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'value'];

  #observer = null;
  #stopFloating = null;
  #active = null;
  #reporter = new Reporter(this);
  #value = '';
  #loaded = false;

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('input', this.#onInput);
    this.addEventListener('change', this.#onChange);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('mousedown', this.#onMouseDown);
    this.addEventListener('pointermove', this.#onPointerMove);
    this.addEventListener('focusout', this.#onFocusOut);
    this.#observer = observeChildren(this, () => this.#wire());
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
    this.#stopFloating?.();
  }

  attributeChangedCallback(name) {
    if (!this.isConnected) return;
    if (name === 'data-bx-seq') queueMicrotask(() => this.#applyRendered());
    else this.#applyRendered();
  }

  refresh() {
    this.#applyRendered();
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
    return this.#options().find((o) => o.dataset.value === value)?.dataset.label ?? null;
  }

  #wire() {
    const listbox = this.querySelector('[role="listbox"]');
    if (!listbox) return;
    this.#input?.setAttribute('aria-controls', ensureId(listbox, 'listbox'));
    for (const option of this.#options()) ensureId(option, 'option');
    if (!this.#loaded) {
      this.#loaded = true;
      this.#applyRendered(true);
    }
  }

  #applyRendered(force = false) {
    if (!force && this.#reporter.isStale()) return;
    const value = this.getAttribute('value') ?? '';
    if (!force && value === this.#value) return;
    this.#value = value;
    this.#writeHidden();
    const input = this.#input;
    if (input && document.activeElement !== input) input.value = this.#strict ? (this.#labelOf(value) ?? '') : value;
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
    if (!content || !input || input.disabled || input.readOnly) return;
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
    this.#input?.setAttribute('aria-expanded', 'false');
    this.dataset.state = 'closed';
    this.#highlight(null);
  }

  #filter(text) {
    const query = text.trim().toLowerCase();
    let shown = 0;
    for (const option of this.#options()) {
      const match = !query || (option.dataset.label ?? '').toLowerCase().includes(query);
      option.hidden = !match;
      if (match) shown++;
    }
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
    if (report) {
      this.dispatchEvent(new Event('change', { bubbles: true }));
      this.#reporter.report('bx-change', { value });
    }
  }

  #choose(option) {
    if (!option || option.getAttribute('aria-disabled') === 'true') return;
    const input = this.#input;
    input.value = option.dataset.label ?? '';
    this.#commit(this.#strict ? option.dataset.value : input.value, true);
    this.#close();
  }

  #onInput = (event) => {
    const input = this.#input;
    if (event.target !== input) return;
    this.#filter(input.value);
    this.#open();
    this.#highlight(this.#strict ? (navigable(this.#options())[0] ?? null) : null);
    // Free text is the value as it is typed (the hidden field posts it); .NET hears about it on change.
    if (!this.#strict) {
      this.#value = input.value;
      this.#writeHidden();
    }
  };

  // The native change event of the field: free text commits when the field loses focus or Enter is pressed.
  #onChange = (event) => {
    if (event.target !== this.#input || this.#strict) return;
    event.stopPropagation();
    this.#value = this.#input.value;
    this.#writeHidden();
    this.dispatchEvent(new Event('change', { bubbles: true }));
    this.#reporter.report('bx-change', { value: this.#value });
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
          this.#filter(this.#strict ? '' : input.value);
          this.#open();
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
      this.#filter(this.#strict ? '' : this.#input.value);
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

if (!customElements.get('bx-input-menu')) customElements.define('bx-input-menu', BxInputMenuElement);
