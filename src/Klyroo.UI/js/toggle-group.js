import { observeChildren } from './shared.js';
import { ValueModel } from './form2-shared.js';

/**
 * Group of toggle buttons, one or several pressed (data-multiple). Roving tabindex: one item is in the tab order,
 * arrow keys move focus between items, Space and Enter press.
 *
 * <kly-toggle-group role="group" name="…" data-values="a" data-multiple data-orientation="horizontal">
 *   <span data-slot="values" hidden></span>
 *   <button type="button" data-value="a" aria-pressed="true">…</button> …
 * </kly-toggle-group>
 */
export class KlyToggleGroupElement extends HTMLElement {
  static observedAttributes = ['data-kly-seq', 'data-values', 'name', 'disabled'];

  #observer = null;
  #stop = null;
  #model = new ValueModel(this, (values) => this.#paint(values));

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('focusin', this.#onFocusIn);
    this.#observer = observeChildren(this, () => this.#model.ensure());
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

  #items() {
    return [...this.querySelectorAll('button[data-value]')];
  }

  #enabled() {
    return this.#items().filter((b) => !b.disabled);
  }

  #paint(values) {
    const items = this.#items();
    for (const item of items) {
      const pressed = values.includes(item.dataset.value);
      item.setAttribute('aria-pressed', String(pressed));
      item.dataset.state = pressed ? 'on' : 'off';
    }
    // The item that is the tab stop stays it when a press changes what is pressed; only a first paint picks one.
    const stop = items.includes(this.#stop) && !this.#stop.disabled ? this.#stop : null;
    this.#roving(stop ?? items.find((b) => !b.disabled && b.getAttribute('aria-pressed') === 'true') ?? this.#enabled()[0]);
  }

  #roving(current) {
    this.#stop = current;
    for (const item of this.#items()) item.tabIndex = item === current ? 0 : -1;
  }

  #press(item) {
    const value = item.dataset.value;
    const values = this.#model.values;
    if (this.hasAttribute('data-multiple')) this.#model.set(values.includes(value) ? values.filter((v) => v !== value) : [...values, value]);
    else this.#model.set(values.includes(value) ? [] : [value]);
  }

  #onClick = (event) => {
    const item = event.target.closest?.('button[data-value]');
    if (item && this.contains(item) && !item.disabled) this.#press(item);
  };

  #onFocusIn = (event) => {
    const item = event.target.closest?.('button[data-value]');
    if (item) this.#roving(item);
  };

  #onKeyDown = (event) => {
    const item = event.target.closest?.('button[data-value]');
    if (!item) return;
    const items = this.#enabled();
    const index = items.indexOf(item);
    const vertical = this.dataset.orientation === 'vertical';
    const [back, forward] = vertical ? ['ArrowUp', 'ArrowDown'] : ['ArrowLeft', 'ArrowRight'];
    let next = null;
    if (event.key === forward) next = items[(index + 1) % items.length];
    else if (event.key === back) next = items[(index - 1 + items.length) % items.length];
    else if (event.key === 'Home') next = items[0];
    else if (event.key === 'End') next = items.at(-1);
    if (next) {
      event.preventDefault();
      next.focus();
    }
  };
}

if (!customElements.get('kly-toggle-group')) customElements.define('kly-toggle-group', KlyToggleGroupElement);
