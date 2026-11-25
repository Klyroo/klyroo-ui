import { Reporter, ensureId, observeChildren } from './shared.js';

/**
 * <kly-command-palette data-results-text="{0} results available">
 *   <div><input role="combobox" aria-controls="list" …></div>
 *   <div role="listbox" id="list"> [<div role="group">…] <div role="option" data-value data-search>…</div> </div>
 *   <div data-slot="empty">No results</div>
 *   <div role="status" data-slot="status"></div>
 * </kly-command-palette>
 *
 * Filtering, highlighting and selection happen here, so typing never goes through .NET. Options that do not match
 * get data-filtered (the theme hides them), the element gets data-empty when none match, and the highlighted option
 * is announced through aria-activedescendant while focus stays in the input. Activating an option reports
 * "kly-change" with its value; links navigate natively.
 */
export class KlyCommandPaletteElement extends HTMLElement {
  #observer = null;
  #active = null;
  #query = null;
  #reporter = new Reporter(this);

  connectedCallback() {
    this.addEventListener('input', this.#onInput);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('pointermove', this.#onPointerMove);
    this.#observer = observeChildren(this, () => this.#refilter(false));
  }

  disconnectedCallback() {
    this.removeEventListener('input', this.#onInput);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('pointermove', this.#onPointerMove);
    this.#observer?.disconnect();
  }

  refresh() {
    this.#refilter(false);
  }

  get #input() {
    return this.querySelector('input[role="combobox"]');
  }

  get #listbox() {
    return this.querySelector('[role="listbox"]');
  }

  #options() {
    return [...(this.#listbox?.querySelectorAll('[role="option"]') ?? [])];
  }

  #selectable() {
    return this.#options().filter((o) => !o.hasAttribute('data-filtered') && o.getAttribute('aria-disabled') !== 'true');
  }

  // Cheap enough to run on every keystroke: a few hundred commands is a few hundred string searches.
  #refilter(reset) {
    const input = this.#input;
    const listbox = this.#listbox;
    if (!input || !listbox) return;
    const query = input.value.trim().toLowerCase();
    const tokens = query.split(/\s+/).filter(Boolean);
    const changed = query !== this.#query;
    this.#query = query;
    let visible = 0;
    for (const option of this.#options()) {
      ensureId(option, 'command');
      const text = (option.dataset.search ?? option.textContent).toLowerCase();
      const match = tokens.every((token) => text.includes(token));
      if (option.hasAttribute('data-filtered') === match) option.toggleAttribute('data-filtered', !match);
      if (match) visible++;
    }
    for (const group of listbox.querySelectorAll('[role="group"]')) {
      const any = group.querySelector('[role="option"]:not([data-filtered])') !== null;
      if (group.hasAttribute('data-filtered') === any) group.toggleAttribute('data-filtered', !any);
    }
    if (this.hasAttribute('data-empty') !== (visible === 0)) this.toggleAttribute('data-empty', visible === 0);

    const keep = !reset && !changed && this.#active && this.#selectable().includes(this.#active);
    this.#highlight(keep ? this.#active : (this.#selectable()[0] ?? null), reset);
    if (reset) this.#announce(visible);
  }

  #announce(count) {
    const status = this.querySelector('[data-slot="status"]');
    if (!status) return;
    const text = count === 0
      ? (this.querySelector('[data-slot="empty"]')?.textContent ?? '')
      : (this.dataset.resultsText ?? '{0}').replace('{0}', String(count));
    // Writing the same text again would replace the node and re-trigger the child observer.
    if (status.textContent !== text) status.textContent = text;
  }

  // Scrolls the listbox only, never the page: scrollIntoView would also scroll the window when the palette is inline.
  #reveal(option) {
    const list = this.#listbox;
    if (!list) return;
    const box = option.getBoundingClientRect();
    const bounds = list.getBoundingClientRect();
    if (box.top < bounds.top) list.scrollTop -= bounds.top - box.top;
    else if (box.bottom > bounds.bottom) list.scrollTop += box.bottom - bounds.bottom;
  }

  /** Highlights an option; only a keyboard move or a new search scrolls it into view. */
  #highlight(option, reveal = false) {
    if (this.#active && this.#active !== option) {
      this.#active.removeAttribute('data-highlighted');
      this.#active.setAttribute('aria-selected', 'false');
    }
    this.#active = option;
    const input = this.#input;
    if (option) {
      option.setAttribute('data-highlighted', '');
      option.setAttribute('aria-selected', 'true');
      if (reveal) this.#reveal(option);
      input?.setAttribute('aria-activedescendant', option.id);
    } else {
      input?.removeAttribute('aria-activedescendant');
    }
  }

  #onInput = (event) => {
    if (event.target === this.#input) this.#refilter(true);
  };

  #onKeyDown = (event) => {
    const input = this.#input;
    // Enter that confirms an IME composition must not run a command.
    if (event.target !== input || event.isComposing || event.keyCode === 229) return;
    const options = this.#selectable();
    const index = options.indexOf(this.#active);
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        if (options.length) this.#highlight(options[(index + 1) % options.length], true);
        break;
      case 'ArrowUp':
        event.preventDefault();
        if (options.length) this.#highlight(options[(index - 1 + options.length) % options.length], true);
        break;
      case 'Enter':
        if (this.#active) {
          event.preventDefault();
          this.#active.click();
        }
        break;
      case 'Escape':
        // A first Escape clears the search; once it is empty Escape reaches the enclosing modal.
        if (input.value) {
          event.preventDefault();
          event.stopPropagation();
          input.value = '';
          this.#refilter(true);
        }
        break;
    }
  };

  #onClick = (event) => {
    const option = event.target.closest?.('[role="option"]');
    if (!option || !this.contains(option) || option.getAttribute('aria-disabled') === 'true') return;
    this.#reporter.report('kly-change', { value: option.dataset.value ?? '' });
    this.#input?.focus({ preventScroll: true });
  };

  #onPointerMove = (event) => {
    const option = event.target.closest?.('[role="option"]');
    if (option && option !== this.#active && option.getAttribute('aria-disabled') !== 'true') this.#highlight(option);
  };
}

if (!customElements.get('kly-command-palette')) customElements.define('kly-command-palette', KlyCommandPaletteElement);
