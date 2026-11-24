import { Reporter } from './shared.js';

/**
 * One box per character. Typing moves to the next box, Backspace to the previous one, pasting fills every box.
 * A hidden input carries the joined value, so the pin posts in a plain HTML form.
 *
 * <lx-pin-input value="1234" data-type="numeric|alphanumeric">
 *   <input type="hidden" data-slot="value" name="…" value="1234">
 *   <input data-slot="cell" maxlength="1"> …
 * </lx-pin-input>
 */
export class LxPinInputElement extends HTMLElement {
  static observedAttributes = ['data-lx-seq', 'value'];

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
    if (name === 'data-lx-seq') queueMicrotask(() => this.#applyRendered(true));
    else this.#applyRendered();
  }

  refresh() {
    this.#reporter.adopt();
    this.#applyRendered(this.#reporter.bound);
    this.#sync();
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

  /** Shows the rendered value when a render changed it (force: a bound render arrived). */
  #applyRendered(force = false) {
    const text = this.#reporter.take('value', force);
    if (text === undefined) return;
    const value = text ?? '';
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
    // The value is the boxes joined, so the boxes show exactly that: no digit sits after an empty box.
    const value = this.#read();
    this.#cells().forEach((cell, i) => {
      cell.value = value[i] ?? '';
    });
    const hidden = this.querySelector('input[data-slot="value"]');
    if (hidden) hidden.value = value;
    return value;
  }

  #report() {
    const value = this.#sync();
    if (value === this.#last) return;
    this.#last = value;
    this.dispatchEvent(new Event('change', { bubbles: true }));
    this.#reporter.report('lx-change', { value });
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
    if (!event.target.matches?.('[data-slot="cell"]')) return;
    // Typing continues after the last digit: a box past the first empty one hands focus to it.
    const cells = this.#cells();
    const empty = cells.findIndex((c) => c.value === '');
    if (empty !== -1 && cells.indexOf(event.target) > empty) {
      cells[empty].focus();
      return;
    }
    event.target.select();
  };
}

if (!customElements.get('lx-pin-input')) customElements.define('lx-pin-input', LxPinInputElement);
