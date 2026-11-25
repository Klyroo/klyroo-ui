import './calendar.js';
import './popover.js';

/**
 * Glue between the native date input and the calendar popover of KlyInputDate.
 *
 * <kly-date-field>
 *   <input type="date" name="…">
 *   <span><kly-popover>…<kly-calendar>…</kly-calendar></kly-popover></span>
 * </kly-date-field>
 *
 * Picking a day writes its ISO date into the input and fires input and change on it, so a bound .NET handler, a
 * validator or a plain form post all see the new value; typing in the input moves the calendar's selection.
 * The input keeps the locale display and the keyboard segments of the browser; its value is always yyyy-MM-dd.
 */
export class KlyDateFieldElement extends HTMLElement {
  connectedCallback() {
    this.addEventListener('kly-change', this.#onPicked);
    this.addEventListener('input', this.#onTyped);
    this.addEventListener('change', this.#onTyped);
    this.addEventListener('toggle', this.#onToggle, true);
  }

  disconnectedCallback() {
    this.removeEventListener('kly-change', this.#onPicked);
    this.removeEventListener('input', this.#onTyped);
    this.removeEventListener('change', this.#onTyped);
    this.removeEventListener('toggle', this.#onToggle, true);
  }

  get #input() {
    return this.querySelector(':scope > input');
  }

  get #calendar() {
    return this.querySelector('kly-calendar');
  }

  #onPicked = (event) => {
    const calendar = this.#calendar;
    if (!calendar || !calendar.contains(event.target) && event.target !== calendar) return;
    event.stopPropagation();
    const input = this.#input;
    if (!input) return;
    input.value = event.detail.value ?? '';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    input.dispatchEvent(new Event('change', { bubbles: true }));
    this.querySelector('[popover]')?.hidePopover();
  };

  #onTyped = (event) => {
    if (event.target !== this.#input) return;
    this.#calendar?.selectValue(this.#input.value);
  };

  #onToggle = (event) => {
    if (event.newState !== 'open' || !event.target.matches?.('[popover]')) return;
    this.#calendar?.selectValue(this.#input?.value ?? '');
    // kly-popover focuses the first control of the panel when it opens: move on to the day afterwards.
    setTimeout(() => this.#calendar?.focusDay(), 0);
  };
}

if (!customElements.get('kly-date-field')) customElements.define('kly-date-field', KlyDateFieldElement);
