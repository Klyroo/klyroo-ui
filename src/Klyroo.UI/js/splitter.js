import { Reporter, observeChildren } from './shared.js';

/**
 * <kly-splitter orientation="horizontal" sizes="30,70" step="5">
 *   <div data-kly-panel data-min="0" data-max="100">…</div>
 *   <div role="separator" tabindex="0" aria-valuenow="30" …></div>
 *   <div data-kly-panel>…</div>
 * </kly-splitter>
 *
 * WAI-ARIA window splitter. aria-valuenow of a separator is the size, in percent, of the panel before it.
 * Arrow keys (left/right for side by side panels, up/down for stacked ones), Home and End resize; so does dragging.
 * The new sizes are reported with "kly-change" (value: sizes joined by commas) when a gesture ends.
 */
export class KlySplitterElement extends HTMLElement {
  static observedAttributes = ['data-kly-seq', 'sizes'];

  #sizes = [];
  #observer = null;
  #reporter = new Reporter(this);
  #drag = null;

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('pointerdown', this.#onPointerDown);
    this.addEventListener('pointermove', this.#onPointerMove);
    this.addEventListener('pointerup', this.#onPointerUp);
    this.addEventListener('pointercancel', this.#onPointerUp);
    this.#observer = observeChildren(this, () => this.#layout());
  }

  disconnectedCallback() {
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('pointerdown', this.#onPointerDown);
    this.removeEventListener('pointermove', this.#onPointerMove);
    this.removeEventListener('pointerup', this.#onPointerUp);
    this.removeEventListener('pointercancel', this.#onPointerUp);
    this.#observer?.disconnect();
  }

  /** Re-applies the layout, e.g. after enhanced navigation merged server HTML over the panel styles. */
  refresh() {
    this.#layout();
  }

  attributeChangedCallback(name) {
    // data-kly-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-kly-seq') queueMicrotask(() => this.#apply());
    else this.#apply();
  }

  #apply() {
    if (this.#reporter.isStale() || this.#drag) return;
    this.#sizes = (this.getAttribute('sizes') ?? '').split(',').filter(Boolean).map(Number);
    this.#layout();
  }

  #panels() {
    return [...this.querySelectorAll(':scope > [data-kly-panel]')];
  }

  #handles() {
    return [...this.querySelectorAll(':scope > [role="separator"]')];
  }

  #limit(panel, name, fallback) {
    const value = Number(panel?.dataset[name]);
    return Number.isFinite(value) && panel?.dataset[name] !== undefined ? value : fallback;
  }

  /** Allowed range of the size of panel index, given its neighbour. */
  #range(index, sizes = this.#sizes) {
    const panels = this.#panels();
    const pair = sizes[index] + sizes[index + 1];
    const a = panels[index];
    const b = panels[index + 1];
    return [
      Math.max(this.#limit(a, 'min', 0), pair - this.#limit(b, 'max', 100)),
      Math.min(this.#limit(a, 'max', 100), pair - this.#limit(b, 'min', 0)),
    ];
  }

  #layout() {
    const panels = this.#panels();
    if (panels.length === 0 || this.#sizes.length !== panels.length) return;
    panels.forEach((panel, i) => {
      panel.style.flex = `${this.#sizes[i]} 1 0%`;
    });
    this.#handles().forEach((handle, i) => {
      if (i >= panels.length - 1) return;
      const [min, max] = this.#range(i);
      handle.setAttribute('aria-valuenow', String(Math.round(this.#sizes[i])));
      handle.setAttribute('aria-valuemin', String(Math.round(min)));
      handle.setAttribute('aria-valuemax', String(Math.round(max)));
    });
  }

  /** Sets the size of panel index (its neighbour takes the difference). Returns true when something changed. */
  #resize(index, value, from = this.#sizes) {
    const [min, max] = this.#range(index, from);
    const size = Math.min(Math.max(value, min), max);
    const pair = from[index] + from[index + 1];
    const next = [...from];
    next[index] = size;
    next[index + 1] = pair - size;
    const changed = Math.abs(next[index] - this.#sizes[index]) > 1e-6;
    this.#sizes = next;
    this.#layout();
    return changed;
  }

  #report() {
    this.#reporter.report('kly-change', { value: this.#sizes.map((s) => +s.toFixed(2)).join(',') });
  }

  #onKeyDown = (event) => {
    const handle = event.target.closest?.('[role="separator"]');
    if (!handle || handle.parentElement !== this || event.altKey || event.ctrlKey || event.metaKey) return;
    const index = this.#handles().indexOf(handle);
    const horizontal = this.getAttribute('orientation') !== 'vertical';
    const step = Number(this.getAttribute('step')) || 5;
    const rtl = horizontal && getComputedStyle(this).direction === 'rtl';
    const current = this.#sizes[index];
    let target = null;
    if (event.key === (horizontal ? 'ArrowRight' : 'ArrowDown')) target = current + (rtl ? -step : step);
    else if (event.key === (horizontal ? 'ArrowLeft' : 'ArrowUp')) target = current - (rtl ? -step : step);
    else if (event.key === 'Home') target = -Infinity;
    else if (event.key === 'End') target = Infinity;
    if (target === null) return;
    event.preventDefault();
    if (this.#resize(index, target)) this.#report();
  };

  #onPointerDown = (event) => {
    const handle = event.target.closest?.('[role="separator"]');
    if (!handle || handle.parentElement !== this || event.button !== 0) return;
    const index = this.#handles().indexOf(handle);
    const horizontal = this.getAttribute('orientation') !== 'vertical';
    const available = this.#panels().reduce((sum, p) => sum + (horizontal ? p.getBoundingClientRect().width : p.getBoundingClientRect().height), 0);
    handle.setPointerCapture(event.pointerId);
    handle.dataset.dragging = 'true';
    handle.focus();
    this.#drag = { index, handle, horizontal, available, start: [...this.#sizes], origin: horizontal ? event.clientX : event.clientY, moved: false };
    event.preventDefault();
  };

  #onPointerMove = (event) => {
    const drag = this.#drag;
    if (!drag) return;
    const delta = (horizontalPos(event, drag) - drag.origin) / (drag.available || 1);
    const total = drag.start.reduce((a, b) => a + b, 0);
    const rtl = drag.horizontal && getComputedStyle(this).direction === 'rtl';
    this.#resize(drag.index, drag.start[drag.index] + (rtl ? -delta : delta) * total, drag.start);
    drag.moved = true;
  };

  #onPointerUp = () => {
    const drag = this.#drag;
    if (!drag) return;
    this.#drag = null;
    delete drag.handle.dataset.dragging;
    if (drag.moved && drag.start.some((s, i) => Math.abs(s - this.#sizes[i]) > 1e-6)) this.#report();
  };
}

function horizontalPos(event, drag) {
  return drag.horizontal ? event.clientX : event.clientY;
}

if (!customElements.get('kly-splitter')) customElements.define('kly-splitter', KlySplitterElement);
