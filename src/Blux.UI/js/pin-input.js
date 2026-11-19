import { Reporter } from './shared.js';

/**
 * One box per character. Typing moves to the next box, Backspace to the previous one, pasting fills every box.
 * A hidden input carries the joined value, so the pin posts in a plain HTML form.
 *
 * <bx-pin-input value="1234" data-type="numeric|alphanumeric">
 *   <input type="hidden" data-slot="value" name="…" value="1234">
 *   <input data-slot="cell" maxlength="1"> …
 * </bx-pin-input>
 */
export class BxPinInputElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'value'];

  #reporter = new Reporter(this);
  #last = null;

  connectedCallback() {
    this.addEventListener('input', this.#onInput);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('paste', this.#onPaste);
    this.addEventListener('focusin', this.#onFocusIn);
    this.#last ??= this.#read();
  }

  attributeChangedCallback(name) {
    if (!this.isConnected) return;
    if (name === 'data-bx-seq') queueMicrotask(() => this.#applyRendered());
    else this.#applyRendered();
  }

  refresh() {
    this.#applyRendered();
  }

  #cells() {
    return [...this.querySelectorAll('[data-slot="cell"]')];
  }

  #read() {
    return this.#cells().map((c) => c.value).join('');
  }

  #accepts(char) {
    return this.getAttribute('data-type') === 'alphanumeric' ? /^[a-zA-Z0-9]$/.test(char) : /^[0-9]$/.test(char);
  }

  #clean(text) {
    return [...text].filter((c) => this.#accepts(c));
  }

  #applyRendered() {
    if (this.#reporter.isStale()) return;
    const value = this.getAttribute('value') ?? '';
    if (value === this.#read()) return;
    this.#fill(this.#clean(value), 0);
    this.#last = this.#read();
  }

  #fill(chars, from) {
    const cells = this.#cells();
    cells.forEach((cell, i) => {
      if (i >= from) cell.value = chars[i - from] ?? '';
    });
    this.#sync();
  }

  #sync() {
    const value = this.#read();
    const hidden = this.querySelector('input[data-slot="value"]');
    if (hidden) hidden.value = value;
    return value;
  }

  #report() {
    const value = this.#sync();
    if (value === this.#last) return;
    this.#last = value;
    this.dispatchEvent(new Event('change', { bubbles: true }));
    this.#reporter.report('bx-change', { value });
  }

  #onInput = (event) => {
    const cell = event.target;
    if (!cell.matches?.('[data-slot="cell"]')) return;
    const cells = this.#cells();
    const index = cells.indexOf(cell);
    const chars = this.#clean(cell.value);
    // More than one character in a box (autofill, a one-time code suggestion): spread it from here.
    if (chars.length > 1) {
      this.#fill(chars.slice(0, cells.length - index), index);
      cells[Math.min(index + chars.length, cells.length - 1)].focus();
    } else {
      cell.value = chars[0] ?? '';
      if (chars.length === 1) cells[index + 1]?.focus();
    }
    this.#report();
  };

  #onKeyDown = (event) => {
    const cell = event.target;
    if (!cell.matches?.('[data-slot="cell"]')) return;
    const cells = this.#cells();
    const index = cells.indexOf(cell);
    switch (event.key) {
      case 'Backspace':
        if (cell.value === '' && index > 0) {
          event.preventDefault();
          cells[index - 1].value = '';
          cells[index - 1].focus();
          this.#report();
        }
        break;
      case 'ArrowLeft': event.preventDefault(); cells[index - 1]?.focus(); break;
      case 'ArrowRight': event.preventDefault(); cells[index + 1]?.focus(); break;
      case 'Home': event.preventDefault(); cells[0].focus(); break;
      case 'End': event.preventDefault(); cells.at(-1).focus(); break;
    }
  };

  #onPaste = (event) => {
    if (!event.target.matches?.('[data-slot="cell"]')) return;
    event.preventDefault();
    const cells = this.#cells();
    const chars = this.#clean(event.clipboardData?.getData('text') ?? '').slice(0, cells.length);
    if (chars.length === 0) return;
    this.#fill(chars, 0);
    cells[Math.min(chars.length, cells.length - 1)].focus();
    this.#report();
  };

  #onFocusIn = (event) => {
    if (event.target.matches?.('[data-slot="cell"]')) event.target.select();
  };
}

if (!customElements.get('bx-pin-input')) customElements.define('bx-pin-input', BxPinInputElement);
