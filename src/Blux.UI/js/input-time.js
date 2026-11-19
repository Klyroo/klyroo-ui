import { Reporter, observeChildren } from './shared.js';

const pad = (n) => String(n).padStart(2, '0');

/**
 * Time field made of spinbutton segments (hour, minute and, in 12-hour mode, AM/PM). Typing digits, the arrow keys,
 * Home/End and Backspace edit the focused segment; Left/Right move between segments. The value is HH:mm, carried by
 * the hidden input so the field posts in a plain form, and reported with "bx-change" when it becomes complete or empty.
 *
 * <bx-input-time value="14:30" hour12 step="5" data-label-am="AM" data-label-pm="PM">
 *   <input type="hidden" data-slot="value" name="…" value="14:30">
 *   <span role="spinbutton" data-segment="hour" data-label="02" tabindex="0" aria-label="Hours"></span> …
 * </bx-input-time>
 *
 * Segment text is CSS generated content from data-label, so the script never replaces nodes Blazor owns.
 */
export class BxInputTimeElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'value', 'hour12', 'step'];

  #hour = null; // 0-23
  #minute = null; // 0-59
  #pm = false; // AM/PM chosen while the hour is still empty
  #buffer = '';
  #committed = '';
  #observer = null;
  #scheduled = false;
  #reporter = new Reporter(this);

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('focusout', this.#onFocusOut);
    this.addEventListener('click', this.#onClick);
    this.#observer = observeChildren(this, () => this.#sync());
  }

  disconnectedCallback() {
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('focusout', this.#onFocusOut);
    this.removeEventListener('click', this.#onClick);
    this.#observer?.disconnect();
  }

  attributeChangedCallback() {
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (this.#scheduled) return;
    this.#scheduled = true;
    queueMicrotask(() => {
      this.#scheduled = false;
      this.#sync();
    });
  }

  refresh() {
    this.#sync();
  }

  get #hour12() {
    return this.hasAttribute('hour12');
  }

  get #step() {
    return Math.min(30, Math.max(1, Number(this.getAttribute('step')) || 1));
  }

  get #disabled() {
    return this.hasAttribute('data-disabled');
  }

  #segment(name) {
    return this.querySelector(`:scope > [data-segment="${name}"]`);
  }

  #segments() {
    return [...this.querySelectorAll(':scope > [data-segment]')];
  }

  #sync() {
    if (!this.#reporter.isStale()) {
      const value = this.getAttribute('value') ?? '';
      if (value !== this.#current()) this.#parse(value);
      this.#committed = this.#current();
    }
    this.#paint();
  }

  #parse(value) {
    const match = /^(\d{2}):(\d{2})/.exec(value);
    if (match && Number(match[1]) < 24 && Number(match[2]) < 60) {
      this.#hour = Number(match[1]);
      this.#minute = Number(match[2]);
      this.#pm = this.#hour >= 12;
    } else {
      this.#hour = null;
      this.#minute = null;
    }
  }

  #current() {
    return this.#hour === null || this.#minute === null ? '' : `${pad(this.#hour)}:${pad(this.#minute)}`;
  }

  #displayHour() {
    if (this.#hour === null) return null;
    return this.#hour12 ? this.#hour % 12 || 12 : this.#hour;
  }

  #paint() {
    const dh = this.#displayHour();
    const set = (name, label, now, min, max, text) => {
      const el = this.#segment(name);
      if (!el) return;
      el.dataset.label = label;
      if (now === null) {
        el.setAttribute('data-placeholder', '');
        el.removeAttribute('aria-valuenow');
        el.removeAttribute('aria-valuetext');
      } else {
        el.removeAttribute('data-placeholder');
        el.setAttribute('aria-valuenow', String(now));
        el.setAttribute('aria-valuetext', text ?? label);
      }
      el.setAttribute('aria-valuemin', String(min));
      el.setAttribute('aria-valuemax', String(max));
    };
    set('hour', dh === null ? '--' : pad(dh), dh, this.#hour12 ? 1 : 0, this.#hour12 ? 12 : 23);
    set('minute', this.#minute === null ? '--' : pad(this.#minute), this.#minute, 0, 59);
    const pm = this.#hour === null ? this.#pm : this.#hour >= 12;
    const am = this.getAttribute('data-label-am') || 'AM';
    const pmLabel = this.getAttribute('data-label-pm') || 'PM';
    set('period', pm ? pmLabel : am, pm ? 1 : 0, 0, 1);
  }

  #commit() {
    this.#paint();
    const value = this.#current();
    if (value === this.#committed) return;
    this.#committed = value;
    const input = this.querySelector(':scope > input[data-slot="value"]');
    if (input) {
      input.value = value;
      input.setAttribute('value', value);
      input.dispatchEvent(new Event('change', { bubbles: true }));
    }
    this.#reporter.report('bx-change', { value });
  }

  #setHour(displayHour) {
    if (displayHour === null) {
      this.#hour = null;
      return;
    }
    if (this.#hour12) {
      const pm = this.#hour === null ? this.#pm : this.#hour >= 12;
      this.#hour = (displayHour % 12) + (pm ? 12 : 0);
    } else {
      this.#hour = displayHour;
    }
  }

  #setPeriod(pm) {
    this.#pm = pm;
    if (this.#hour !== null) this.#hour = (this.#hour % 12) + (pm ? 12 : 0);
  }

  #adjust(name, direction) {
    this.#buffer = '';
    if (name === 'period') {
      const pm = this.#hour === null ? this.#pm : this.#hour >= 12;
      this.#setPeriod(!pm);
    } else if (name === 'hour') {
      const min = this.#hour12 ? 1 : 0;
      const max = this.#hour12 ? 12 : 23;
      const dh = this.#displayHour();
      let next = dh === null ? (direction > 0 ? min : max) : dh + direction;
      if (next > max) next = min;
      if (next < min) next = max;
      this.#setHour(next);
    } else {
      const step = this.#step;
      let next;
      if (this.#minute === null) next = direction > 0 ? 0 : 60 - step;
      else if (direction > 0) next = (Math.floor(this.#minute / step) + 1) * step;
      else next = (Math.ceil(this.#minute / step) - 1) * step;
      if (next >= 60) next = 0;
      if (next < 0) next = 60 - step;
      this.#minute = next;
    }
    this.#commit();
  }

  #edge(name, toMax) {
    this.#buffer = '';
    if (name === 'hour') this.#setHour(toMax ? (this.#hour12 ? 12 : 23) : this.#hour12 ? 1 : 0);
    else if (name === 'minute') this.#minute = toMax ? 59 : 0;
    else this.#setPeriod(toMax);
    this.#commit();
  }

  #type(name, key) {
    const segments = this.#segments();
    const index = segments.findIndex((s) => s.dataset.segment === name);
    if (name === 'period') {
      const lower = key.toLowerCase();
      const am = (this.getAttribute('data-label-am') || 'AM').toLowerCase();
      const pm = (this.getAttribute('data-label-pm') || 'PM').toLowerCase();
      if (lower === 'a' || lower === am[0]) this.#setPeriod(false);
      else if (lower === 'p' || lower === pm[0]) this.#setPeriod(true);
      else return;
      this.#commit();
      return;
    }
    if (!/^\d$/.test(key)) return;
    const hour = name === 'hour';
    const min = hour && this.#hour12 ? 1 : 0;
    const max = hour ? (this.#hour12 ? 12 : 23) : 59;
    let text = this.#buffer + key;
    if (Number(text) > max) text = key;
    const n = Number(text);
    this.#buffer = text;
    if (n >= min && n <= max) {
      if (hour) this.#setHour(n);
      else this.#minute = n;
      this.#commit();
    }
    if (text.length >= 2 || n * 10 > max) {
      this.#buffer = '';
      segments[index + 1]?.focus();
    }
  }

  #clear(name) {
    this.#buffer = '';
    if (name === 'hour') this.#hour = null;
    else if (name === 'minute') this.#minute = null;
    else return;
    this.#commit();
  }

  #onKeyDown = (event) => {
    const el = event.target.closest?.('[data-segment]');
    if (!el || el.parentElement !== this || this.#disabled || event.altKey || event.ctrlKey || event.metaKey) return;
    const name = el.dataset.segment;
    const segments = this.#segments();
    const index = segments.indexOf(el);
    const rtl = getComputedStyle(this).direction === 'rtl';
    switch (event.key) {
      case 'ArrowUp': this.#adjust(name, 1); break;
      case 'ArrowDown': this.#adjust(name, -1); break;
      case 'ArrowLeft': segments[index + (rtl ? 1 : -1)]?.focus(); break;
      case 'ArrowRight': segments[index + (rtl ? -1 : 1)]?.focus(); break;
      case 'Home': this.#edge(name, false); break;
      case 'End': this.#edge(name, true); break;
      case 'Backspace':
      case 'Delete': this.#clear(name); break;
      case 'Tab': return;
      default:
        if (event.key.length !== 1) return;
        this.#type(name, event.key);
    }
    event.preventDefault();
  };

  #onFocusOut = () => {
    this.#buffer = '';
  };

  #onClick = (event) => {
    const el = event.target.closest?.('[data-segment]');
    if (el && el.parentElement === this && !this.#disabled) el.focus();
  };
}

if (!customElements.get('bx-input-time')) customElements.define('bx-input-time', BxInputTimeElement);
