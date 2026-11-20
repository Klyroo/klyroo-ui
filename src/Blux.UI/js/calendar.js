import { Reporter, observeChildren } from './shared.js';

const DAY = 864e5;
let counter = 0;

const parse = (text) => {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(text ?? '')) return null;
  const date = new Date(`${text}T00:00:00Z`);
  return Number.isNaN(date.getTime()) || date.toISOString().slice(0, 10) !== text ? null : date;
};
const fmt = (date) => date.toISOString().slice(0, 10);
const addDays = (date, n) => new Date(date.getTime() + n * DAY);
const addMonths = (date, n) => {
  const y = date.getUTCFullYear();
  const m = date.getUTCMonth() + n;
  const last = new Date(Date.UTC(y, m + 1, 0)).getUTCDate();
  return new Date(Date.UTC(y, m, Math.min(date.getUTCDate(), last)));
};
const firstOfMonth = (date) => new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), 1));
const sameMonth = (a, b) => a.getUTCFullYear() === b.getUTCFullYear() && a.getUTCMonth() === b.getUTCMonth();

const CHEVRON = {
  prev: 'm15 18-6-6 6-6',
  next: 'm9 18 6-6-6-6',
};

function icon(path, className) {
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.setAttribute('viewBox', '0 0 24 24');
  svg.setAttribute('fill', 'none');
  svg.setAttribute('stroke', 'currentColor');
  svg.setAttribute('stroke-width', '2');
  svg.setAttribute('stroke-linecap', 'round');
  svg.setAttribute('stroke-linejoin', 'round');
  svg.setAttribute('aria-hidden', 'true');
  svg.setAttribute('class', className);
  const p = document.createElementNS('http://www.w3.org/2000/svg', 'path');
  p.setAttribute('d', path);
  svg.append(p);
  return svg;
}

/**
 * Month calendar following the WAI-ARIA date picker grid pattern. Selection, month navigation and keyboard
 * handling run here; the grid is built into the empty [data-slot=body] element Blazor renders, so Blazor and
 * this script never touch the same nodes. Classes of the generated elements come from data-cls-* attributes.
 *
 * <bx-calendar mode="single|multiple|range" value="2026-11-19" min max disabled-dates="a,b" week-start="1"
 *              months="January|…" weekdays="Su|…" weekdays-long="Sunday|…" data-locale="en-US">
 *   <input type="hidden" data-slot="value" name="…" value="2026-11-19">
 *   <div data-slot="body"></div>
 * </bx-calendar>
 *
 * Values are ISO dates. Multiple joins them with ",", range is "start/end" (end empty while pending).
 * Keys on a day: arrows move by day or week, Home/End to the week's edges, PageUp/PageDown by month (Shift: year).
 */
export class BxCalendarElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'value', 'mode', 'min', 'max', 'disabled-dates', 'disabled', 'week-start', 'data-locale', 'name'];

  #observer = null;
  #reporter = new Reporter(this);
  #selection = [];
  #view = null;
  #focus = null;
  #body = null;
  #scheduled = false;
  #forced = false;
  #headingId = `bx-calendar-${++counter}`;

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('keydown', this.#onKeyDown);
    this.#observer = observeChildren(this, () => this.#init());
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.#observer?.disconnect();
    this.#body = null;
  }

  attributeChangedCallback(name) {
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-bx-seq') this.#forced = true;
    if (this.#scheduled) return;
    this.#scheduled = true;
    queueMicrotask(() => {
      this.#scheduled = false;
      const forced = this.#forced;
      this.#forced = false;
      if (this.#body) this.#sync(forced);
    });
  }

  /** Re-applies the wiring, e.g. after enhanced navigation merged server HTML over attributes set here. */
  refresh() {
    this.#reporter.adopt();
    this.#init();
    if (this.#body) this.#sync(this.#reporter.bound);
  }

  /** Selects a value without reporting it, for a host that keeps the value elsewhere (the date field). */
  selectValue(text) {
    this.#applyValue(text ?? '');
    this.#render(null);
  }

  /** Moves focus to the day that is the grid's tab stop. */
  focusDay() {
    this.#body?.querySelector('button[data-date][tabindex="0"]')?.focus({ preventScroll: true });
  }

  get #mode() {
    const mode = this.getAttribute('mode');
    return mode === 'multiple' || mode === 'range' ? mode : 'single';
  }

  get #min() {
    return parse(this.getAttribute('min')) ? this.getAttribute('min') : null;
  }

  get #max() {
    return parse(this.getAttribute('max')) ? this.getAttribute('max') : null;
  }

  get #weekStart() {
    return Number(this.getAttribute('week-start') ?? 0) % 7;
  }

  #cls(slot) {
    return this.getAttribute(`data-cls-${slot}`) ?? '';
  }

  #init() {
    const body = this.querySelector(':scope > [data-slot="body"]');
    if (!body) return;
    if (body !== this.#body) {
      this.#body = body;
      this.#sync(true);
    } else if (body.childElementCount === 0) {
      // An enhanced navigation or form post merged the server's empty body over the grid: draw it again.
      this.#render(null);
    }
  }

  /** Takes the rendered value when a render changed it (or force: a bound render arrived), then draws. */
  #sync(force) {
    const text = this.#reporter.take('value', force || !this.#view);
    if (text !== undefined) this.#applyValue(text ?? '');
    else if (!this.#view) this.#applyValue('');
    else this.#writeInput(this.#serialize());
    this.#render(null);
  }

  #parseValue(text) {
    if (!text) return [];
    switch (this.#mode) {
      case 'range': {
        const [start, end] = text.split('/');
        return parse(start) ? [start, parse(end) ? end : null] : [];
      }
      case 'multiple':
        return text.split(',').filter((d) => parse(d));
      default:
        return parse(text) ? [text] : [];
    }
  }

  #serialize() {
    const sel = this.#selection;
    switch (this.#mode) {
      case 'range': return sel.length ? `${sel[0]}/${sel[1] ?? ''}` : '';
      case 'multiple': return sel.join(',');
      default: return sel[0] ?? '';
    }
  }

  #today() {
    const now = new Date();
    return new Date(Date.UTC(now.getFullYear(), now.getMonth(), now.getDate()));
  }

  #clamp(date) {
    const iso = fmt(date);
    if (this.#min && iso < this.#min) return parse(this.#min);
    if (this.#max && iso > this.#max) return parse(this.#max);
    return date;
  }

  #applyValue(text) {
    const next = this.#parseValue(text);
    const changed = next.join('/') !== this.#selection.join('/') || !this.#view;
    this.#selection = next;
    if (changed) {
      const anchor = next.length ? parse(next[0]) : this.#clamp(this.#today());
      this.#view = firstOfMonth(anchor);
      this.#focus = fmt(anchor);
    }
    this.#writeInput(this.#serialize());
  }

  #writeInput(value) {
    const input = this.querySelector(':scope > input[data-slot="value"]');
    if (input && input.value !== value) {
      input.value = value;
      input.setAttribute('value', value);
    }
    this.#writeEntries();
  }

  /**
   * Multiple and range post one entry per date, so a form binds them to a list or a range: the dates under "name",
   * or "name.Start" and "name.End". The inputs live in the empty <span data-slot="values">, which Blazor renders empty.
   */
  #writeEntries() {
    const box = this.querySelector(':scope > [data-slot="values"]');
    if (!box) return;
    const name = this.getAttribute('name');
    const sel = this.#selection;
    let wanted = [];
    if (name && !this.hasAttribute('disabled')) {
      if (this.#mode === 'multiple') wanted = sel.map((date) => [name, date]);
      else if (this.#mode === 'range' && sel[0]) wanted = [[`${name}.Start`, sel[0]], ...(sel[1] ? [[`${name}.End`, sel[1]]] : [])];
    }
    const current = [...box.children];
    if (current.length === wanted.length && current.every((input, i) => input.name === wanted[i][0] && input.value === wanted[i][1])) return;
    box.replaceChildren(
      ...wanted.map(([entry, date]) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = entry;
        input.value = date;
        return input;
      }),
    );
  }

  #isDisabled(iso) {
    if (this.hasAttribute('disabled')) return true;
    if ((this.#min && iso < this.#min) || (this.#max && iso > this.#max)) return true;
    return (this.getAttribute('disabled-dates') ?? '').split(',').includes(iso);
  }

  #label(date) {
    const locale = this.getAttribute('data-locale') || undefined;
    try {
      return new Intl.DateTimeFormat(locale, { dateStyle: 'full', timeZone: 'UTC' }).format(date);
    } catch {
      return fmt(date);
    }
  }

  // The month and the year in the order and with the words of the locale ("2026年11月", "novembre 2026"), which a
  // "{month} {year}" template cannot give. The month names of the markup are the fallback when Intl rejects the locale.
  #monthYear(date, monthName, year) {
    const locale = this.getAttribute('data-locale') || undefined;
    try {
      return new Intl.DateTimeFormat(locale, { year: 'numeric', month: 'long', calendar: 'gregory', timeZone: 'UTC' }).format(date);
    } catch {
      return `${monthName} ${year}`;
    }
  }

  #render(focusTarget) {
    const body = this.#body;
    if (!body || !this.#view) return;
    const view = this.#view;
    const year = view.getUTCFullYear();
    const month = view.getUTCMonth();
    const months = (this.getAttribute('months') ?? '').split('|');
    const weekdays = (this.getAttribute('weekdays') ?? '').split('|');
    const weekdaysLong = (this.getAttribute('weekdays-long') ?? '').split('|');
    const start = this.#weekStart;
    const today = fmt(this.#today());
    const sel = this.#selection;
    const mode = this.#mode;

    // The grid's tab stop must be a day of the shown month.
    let focus = parse(this.#focus);
    if (!focus || !sameMonth(focus, view)) {
      const first = sel.map(parse).find((d) => d && sameMonth(d, view));
      const now = this.#today();
      focus = first ?? (sameMonth(now, view) ? now : view);
      this.#focus = fmt(focus);
    }

    const header = document.createElement('div');
    header.className = this.#cls('header');
    const prev = this.#navButton('prev', addDays(view, -1));
    const next = this.#navButton('next', new Date(Date.UTC(year, month + 1, 1)));
    const heading = document.createElement('div');
    heading.id = this.#headingId;
    heading.className = this.#cls('heading');
    heading.setAttribute('aria-live', 'polite');
    heading.textContent = this.#monthYear(view, months[month] ?? String(month + 1), year);
    header.append(prev, heading, next);

    const table = document.createElement('table');
    table.setAttribute('role', 'grid');
    table.setAttribute('aria-labelledby', this.#headingId);
    if (mode !== 'single') table.setAttribute('aria-multiselectable', 'true');
    table.className = this.#cls('grid');
    const head = table.createTHead().insertRow();
    for (let i = 0; i < 7; i++) {
      const index = (start + i) % 7;
      const th = document.createElement('th');
      th.scope = 'col';
      th.className = this.#cls('weekday');
      th.abbr = weekdaysLong[index] ?? '';
      th.textContent = weekdays[index] ?? '';
      head.append(th);
    }
    const bodyEl = table.createTBody();
    const offset = (view.getUTCDay() - start + 7) % 7;
    const days = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
    let row = bodyEl.insertRow();
    for (let i = 0; i < offset; i++) this.#cell(row, null);
    for (let day = 1; day <= days; day++) {
      if (row.cells.length === 7) row = bodyEl.insertRow();
      const date = new Date(Date.UTC(year, month, day));
      const iso = fmt(date);
      const selected = sel.includes(iso);
      const inRange = mode === 'range' && sel.length === 2 && sel[1] && iso > sel[0] && iso < sel[1];
      const td = this.#cell(row, iso);
      td.setAttribute('aria-selected', String(selected));
      const button = document.createElement('button');
      button.type = 'button';
      button.className = this.#cls('day');
      button.textContent = String(day);
      button.dataset.date = iso;
      button.tabIndex = iso === this.#focus ? 0 : -1;
      button.setAttribute('aria-label', this.#label(date));
      if (selected) button.setAttribute('data-selected', '');
      if (inRange) button.setAttribute('data-in-range', '');
      if (iso === today) {
        button.setAttribute('data-today', '');
        button.setAttribute('aria-current', 'date');
      }
      if (this.#isDisabled(iso)) button.setAttribute('aria-disabled', 'true');
      td.append(button);
    }
    while (row.cells.length < 7) this.#cell(row, null);

    const hadFocus = body.contains(document.activeElement);
    body.replaceChildren(header, table);
    if (focusTarget === 'day' || (focusTarget === null && hadFocus && document.activeElement === document.body)) this.focusDay();
    else if (focusTarget === 'prev') prev.focus({ preventScroll: true });
    else if (focusTarget === 'next') next.focus({ preventScroll: true });
  }

  #cell(row, iso) {
    const td = row.insertCell();
    td.setAttribute('role', 'gridcell');
    td.className = this.#cls('cell');
    if (!iso) td.setAttribute('aria-hidden', 'true');
    return td;
  }

  #navButton(direction, edge) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = this.#cls('nav');
    button.dataset.nav = direction;
    button.setAttribute('aria-label', this.getAttribute(`label-${direction}`) ?? direction);
    button.append(icon(CHEVRON[direction], 'size-4'));
    const iso = fmt(edge);
    const blocked = direction === 'prev' ? this.#min && iso < this.#min : this.#max && iso > this.#max;
    if (blocked || this.hasAttribute('disabled')) button.setAttribute('aria-disabled', 'true');
    return button;
  }

  #pick(iso) {
    const before = this.#serialize();
    const sel = this.#selection;
    switch (this.#mode) {
      case 'multiple':
        this.#selection = sel.includes(iso) ? sel.filter((d) => d !== iso) : [...sel, iso].sort();
        break;
      case 'range':
        if (sel.length === 0 || sel[1]) this.#selection = [iso, null];
        else this.#selection = iso < sel[0] ? [iso, sel[0]] : [sel[0], iso];
        break;
      default:
        this.#selection = [iso];
    }
    this.#focus = iso;
    this.#render('day');
    const value = this.#serialize();
    this.#writeInput(value);
    if (value !== before) this.#report(value);
  }

  #report(value) {
    (this.querySelector(':scope > input[data-slot="value"]') ?? this).dispatchEvent(new Event('change', { bubbles: true }));
    this.#reporter.report('bx-change', { value });
  }

  #showMonth(date, focusTarget) {
    this.#view = firstOfMonth(date);
    this.#render(focusTarget);
  }

  #moveFocus(date) {
    const target = this.#clamp(date);
    this.#focus = fmt(target);
    if (!sameMonth(target, this.#view)) {
      this.#showMonth(target, 'day');
      return;
    }
    const buttons = this.#body.querySelectorAll('button[data-date]');
    for (const button of buttons) {
      const here = button.dataset.date === this.#focus;
      button.tabIndex = here ? 0 : -1;
      if (here) button.focus({ preventScroll: true });
    }
  }

  #onClick = (event) => {
    const day = event.target.closest?.('button[data-date]');
    if (day && this.#body?.contains(day)) {
      if (day.getAttribute('aria-disabled') !== 'true') this.#pick(day.dataset.date);
      return;
    }
    const nav = event.target.closest?.('button[data-nav]');
    if (nav && this.#body?.contains(nav) && nav.getAttribute('aria-disabled') !== 'true') {
      this.#showMonth(addMonths(this.#view, nav.dataset.nav === 'next' ? 1 : -1), nav.dataset.nav);
    }
  };

  #onKeyDown = (event) => {
    const day = event.target.closest?.('button[data-date]');
    if (!day || !this.#body?.contains(day) || event.altKey || event.ctrlKey || event.metaKey) return;
    const date = parse(day.dataset.date);
    const rtl = getComputedStyle(this).direction === 'rtl';
    const offset = (date.getUTCDay() - this.#weekStart + 7) % 7;
    let target = null;
    switch (event.key) {
      case 'ArrowLeft': target = addDays(date, rtl ? 1 : -1); break;
      case 'ArrowRight': target = addDays(date, rtl ? -1 : 1); break;
      case 'ArrowUp': target = addDays(date, -7); break;
      case 'ArrowDown': target = addDays(date, 7); break;
      case 'Home': target = addDays(date, -offset); break;
      case 'End': target = addDays(date, 6 - offset); break;
      case 'PageUp': target = addMonths(date, event.shiftKey ? -12 : -1); break;
      case 'PageDown': target = addMonths(date, event.shiftKey ? 12 : 1); break;
      default: return;
    }
    event.preventDefault();
    this.#moveFocus(target);
  };
}

if (!customElements.get('bx-calendar')) customElements.define('bx-calendar', BxCalendarElement);
