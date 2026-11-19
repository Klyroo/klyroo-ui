import { ensureId, observeChildren } from './shared.js';
import { Typeahead, ValueModel, navigable } from './form2-shared.js';

/**
 * Always-visible listbox (WAI-ARIA APG): the listbox element holds focus and announces the active option through
 * aria-activedescendant. Space and Enter select; letters jump to a matching option.
 *
 * <bx-listbox name="…" data-values="a" data-multiple>
 *   <span data-slot="values" hidden></span>
 *   <div role="listbox" tabindex="0"><div role="option" data-value data-label aria-selected>…</div></div>
 * </bx-listbox>
 */
export class BxListboxElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'data-values', 'name', 'disabled'];

  #observer = null;
  #active = null;
  #typeahead = new Typeahead();
  #model = new ValueModel(this, (values) => this.#paint(values));

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('focusin', this.#onFocusIn);
    this.addEventListener('focusout', this.#onFocusOut);
    this.#observer = observeChildren(this, () => this.#wire());
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
  }

  attributeChangedCallback(name) {
    if (!this.isConnected) return;
    this.#model.attributeChanged(name);
  }

  refresh() {
    this.#model.refresh();
  }

  get #listbox() {
    return this.querySelector(':scope > [role="listbox"]');
  }

  get #multiple() {
    return this.hasAttribute('data-multiple');
  }

  #options() {
    return [...this.querySelectorAll('[role="option"]')];
  }

  #wire() {
    for (const option of this.#options()) ensureId(option, 'option');
    this.#model.ensure();
  }

  #paint(values) {
    for (const option of this.#options()) option.setAttribute('aria-selected', String(values.includes(option.dataset.value)));
  }

  #highlight(option) {
    this.#active?.removeAttribute('data-highlighted');
    this.#active = option;
    const listbox = this.#listbox;
    if (option) {
      option.setAttribute('data-highlighted', '');
      option.scrollIntoView({ block: 'nearest' });
      listbox.setAttribute('aria-activedescendant', option.id);
    } else {
      listbox.removeAttribute('aria-activedescendant');
    }
  }

  #select(option) {
    if (!option || option.getAttribute('aria-disabled') === 'true') return;
    const value = option.dataset.value;
    const values = this.#model.values;
    if (this.#multiple) this.#model.set(values.includes(value) ? values.filter((v) => v !== value) : [...values, value]);
    else this.#model.set([value]);
  }

  #onFocusIn = (event) => {
    if (event.target !== this.#listbox || this.#active) return;
    const options = navigable(this.#options());
    this.#highlight(options.find((o) => o.getAttribute('aria-selected') === 'true') ?? options[0] ?? null);
  };

  #onFocusOut = (event) => {
    if (event.target === this.#listbox) this.#highlight(null);
  };

  #onKeyDown = (event) => {
    if (event.target !== this.#listbox || event.ctrlKey || event.metaKey || event.altKey) return;
    const options = navigable(this.#options());
    const index = options.indexOf(this.#active);
    let next = null;
    switch (event.key) {
      case 'ArrowDown': next = options[Math.min(index + 1, options.length - 1)]; break;
      case 'ArrowUp': next = options[Math.max(index - 1, 0)]; break;
      case 'Home': next = options[0]; break;
      case 'End': next = options.at(-1); break;
      case 'Enter':
      case ' ':
        event.preventDefault();
        this.#select(this.#active);
        return;
      default:
        if (event.key.length === 1 && /\S/.test(event.key)) next = this.#typeahead.find(event.key, options, this.#active);
    }
    if (next) {
      event.preventDefault();
      this.#highlight(next);
    }
  };

  #onClick = (event) => {
    const option = event.target.closest?.('[role="option"]');
    if (!option || !this.contains(option)) return;
    this.#listbox.focus({ preventScroll: true });
    this.#highlight(option);
    this.#select(option);
  };
}

if (!customElements.get('bx-listbox')) customElements.define('bx-listbox', BxListboxElement);
