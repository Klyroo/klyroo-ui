import { observeChildren } from './shared.js';
import { ValueModel } from './form2-shared.js';

const CLOSE = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M18 6 6 18M6 6l12 12"/></svg>';

/**
 * Text field that turns typed values into removable tags. Enter or a comma adds, Backspace on an empty field
 * removes the last tag. The tags are drawn into the empty <span data-slot="tags">, which Blazor renders empty.
 *
 * <bx-input-tags name="…" data-values="a␟b" data-max="5" data-tag-class data-remove-class data-remove-label="Remove {0}">
 *   <span data-slot="values" hidden></span>
 *   <span data-slot="tags" class="contents"></span>
 *   <input data-slot="input">
 * </bx-input-tags>
 */
export class BxInputTagsElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'data-values', 'name', 'disabled'];

  #observer = null;
  #model = new ValueModel(this, (values) => this.#paint(values));

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('paste', this.#onPaste);
    this.#observer = observeChildren(this, () => this.#model.load());
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
  }

  attributeChangedCallback(name) {
    if (!this.isConnected) return;
    if (name === 'data-bx-seq') queueMicrotask(() => this.#model.load());
    else this.#model.load();
  }

  refresh() {
    this.#model.load();
  }

  get #input() {
    return this.querySelector('[data-slot="input"]');
  }

  get #max() {
    const max = Number(this.getAttribute('data-max'));
    return max > 0 ? max : Infinity;
  }

  #paint(values) {
    const box = this.querySelector(':scope > [data-slot="tags"]');
    if (box) {
      const current = [...box.children].map((c) => c.dataset.tag);
      if (current.length !== values.length || current.some((v, i) => v !== values[i])) {
        box.replaceChildren(...values.map((value) => this.#chip(value)));
      }
    }
    this.toggleAttribute('data-full', values.length >= this.#max);
  }

  #chip(value) {
    const chip = document.createElement('span');
    chip.dataset.tag = value;
    chip.className = this.dataset.tagClass ?? '';
    const text = document.createElement('span');
    text.textContent = value;
    const remove = document.createElement('button');
    remove.type = 'button';
    remove.tabIndex = -1;
    remove.dataset.remove = value;
    remove.className = this.dataset.removeClass ?? '';
    remove.setAttribute('aria-label', (this.dataset.removeLabel ?? 'Remove {0}').replace('{0}', value));
    remove.innerHTML = CLOSE;
    remove.disabled = this.hasAttribute('disabled');
    chip.append(text, remove);
    return chip;
  }

  #add(texts) {
    let values = [...this.#model.values];
    for (const raw of texts) {
      const text = raw.trim();
      if (text && !values.includes(text) && values.length < this.#max) values.push(text);
    }
    this.#model.set(values);
  }

  #onKeyDown = (event) => {
    const input = this.#input;
    if (event.target !== input) return;
    if (event.key === 'Enter' || event.key === ',') {
      if (input.value.trim() === '') {
        // An empty Enter still submits the form; an empty comma types nothing.
        if (event.key === ',') event.preventDefault();
        return;
      }
      event.preventDefault();
      this.#add([input.value]);
      input.value = '';
    } else if (event.key === 'Backspace' && input.value === '' && this.#model.values.length > 0) {
      this.#model.set(this.#model.values.slice(0, -1));
    }
  };

  #onPaste = (event) => {
    if (event.target !== this.#input) return;
    const text = event.clipboardData?.getData('text') ?? '';
    if (!/[,\n]/.test(text)) return;
    event.preventDefault();
    this.#add(text.split(/[,\n]/));
  };

  #onClick = (event) => {
    const remove = event.target.closest?.('button[data-remove]');
    if (remove && this.contains(remove)) {
      this.#model.set(this.#model.values.filter((v) => v !== remove.dataset.remove));
      this.#input?.focus();
    } else if (event.target === this || event.target.dataset?.slot === 'tags') {
      this.#input?.focus();
    }
  };
}

if (!customElements.get('bx-input-tags')) customElements.define('bx-input-tags', BxInputTagsElement);
