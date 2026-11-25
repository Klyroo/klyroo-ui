import { Reporter, observeChildren } from './shared.js';
import { appStorageKey } from './base-path.js';

const PREFIX = 'klyroo:dashboard:';

function read(key) {
  try {
    return localStorage.getItem(appStorageKey(key));
  } catch {
    return null;
  }
}

function write(key, value) {
  try {
    localStorage.setItem(appStorageKey(key), value);
  } catch {
    // Storage unavailable (private window): the sizes last until the page is reloaded.
  }
}

function parse(text, count) {
  if (!text) return null;
  const sizes = text.split(',').filter(Boolean).map(Number);
  return sizes.length === count && sizes.every((s) => Number.isFinite(s) && s >= 0) ? sizes : null;
}

/**
 * <kly-dashboard-group data-kly-dashboard-id="app" sizes="30,70">
 *   <span hidden data-kly-dashboard-sink @onklychange></span>
 *   <kly-sidebar>…</kly-sidebar>
 *   <div data-kly-dash-panel data-size="30" data-min="20" data-max="60">… <div role="separator" data-kly-dash-handle></div></div>
 *   <div data-kly-dash-panel>…</div>
 * </kly-dashboard-group>
 *
 * Owns the sizes of the panels, as flex weights in percent (the same model as <kly-splitter>): a resize handle moves the
 * boundary between the panel it sits in (or follows) and the next one. Sizes are remembered in localStorage under the
 * id, except that bound sizes (data-kly-seq) are never read from storage: .NET owns them. The collapsed state and the
 * width of the sidebar belong to <kly-sidebar>, which is also told when controls are added around it.
 * Reports "kly-change" (value: sizes joined by commas) when a resize gesture ends, on the sink element (see KlyDashboardGroup).
 */
export class KlyDashboardGroupElement extends HTMLElement {
  static observedAttributes = ['data-kly-seq', 'sizes', 'data-kly-dashboard-id'];

  #reporter = new Reporter(this);
  #observer = null;
  #sizes = [];
  // The id the sizes belong to: sizes computed for another dashboard must not be applied to this one.
  #sizesKey = null;
  #drag = null;
  #ready = false;

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('pointerdown', this.#onPointerDown);
    this.#ready = true;
    this.#observer = observeChildren(this, () => this.#sync());
  }

  disconnectedCallback() {
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('pointerdown', this.#onPointerDown);
    this.#observer?.disconnect();
    this.#ready = false;
    this.#sizes = [];
    this.#sizesKey = null;
  }

  attributeChangedCallback(name) {
    if (!this.#ready) return;
    // data-kly-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-kly-seq') queueMicrotask(() => this.#applyRendered(true));
    else if (name === 'data-kly-dashboard-id') {
      // An enhanced navigation can turn this element into another dashboard: its panels are merged after the attribute.
      this.#sizesKey = undefined;
      queueMicrotask(() => this.#reinit());
    } else this.#applyRendered(false);
  }

  /** Re-applies the sizes an enhanced navigation merge reset to what the server rendered. */
  refresh() {
    if (!this.#ready) return;
    this.#reporter.adopt();
    if (this.#sizesKey !== this.#key) this.#reinit();
    this.#layout();
    this.#syncSidebars();
  }

  /** Computes the sizes again, as for a dashboard that has just appeared (stored sizes of its id, else what it rendered). */
  #reinit() {
    if (!this.#ready) return;
    const panels = this.#panels();
    this.#sizes = panels.length === 0 ? [] : this.#initial(panels);
    this.#sizesKey = this.#key;
    this.#layout();
  }

  get #key() {
    const id = this.getAttribute('data-kly-dashboard-id');
    return id ? `${PREFIX}${id}:panels` : null;
  }

  #panels() {
    return [...this.querySelectorAll('[data-kly-dash-panel]')].filter((p) => p.closest('kly-dashboard-group') === this);
  }

  #handles() {
    return [...this.querySelectorAll('[data-kly-dash-handle]')].filter((h) => h.closest('kly-dashboard-group') === this);
  }

  /** The panel before a handle: the one it sits in, or the closest panel sibling before it. */
  #before(handle) {
    const own = handle.parentElement?.closest('[data-kly-dash-panel]');
    if (own && own.closest('kly-dashboard-group') === this) return own;
    for (let el = handle.previousElementSibling; el; el = el.previousElementSibling) {
      if (el.hasAttribute('data-kly-dash-panel')) return el;
    }
    return null;
  }

  #syncSidebars() {
    for (const sidebar of this.querySelectorAll('kly-sidebar')) sidebar.syncControls?.();
  }

  #sync() {
    this.#syncSidebars();
    const panels = this.#panels();
    if (panels.length === 0) return;
    if (this.#sizes.length !== panels.length) {
      this.#sizes = this.#initial(panels);
      this.#sizesKey = this.#key;
    }
    this.#layout();
  }

  #initial(panels) {
    const rendered = this.#reporter.take('sizes', true);
    const given = parse(rendered, panels.length) ?? (this.#reporter.bound ? null : parse(read(this.#key) ?? '', panels.length));
    if (given) return given;
    const declared = panels.map((p) => (p.dataset.size !== undefined && p.dataset.size !== '' ? Number(p.dataset.size) : null));
    const specified = declared.reduce((sum, s) => sum + (s ?? 0), 0);
    const free = declared.filter((s) => s === null).length;
    const each = free === 0 ? 0 : Math.max(0, 100 - specified) / free;
    return declared.map((s) => s ?? each);
  }

  #applyRendered(force) {
    if (this.#drag) return;
    const value = this.#reporter.take('sizes', force);
    const panels = this.#panels();
    const sizes = value ? parse(value, panels.length) : null;
    if (!sizes) return;
    this.#sizes = sizes;
    this.#sizesKey = this.#key;
    this.#layout();
  }

  #limit(panel, name, fallback) {
    const raw = panel?.dataset[name];
    const value = Number(raw);
    return raw !== undefined && raw !== '' && Number.isFinite(value) ? value : fallback;
  }

  /** Allowed size of panel index, given its neighbour. */
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
      const flex = `${+this.#sizes[i].toFixed(2)} 1 0%`;
      if (panel.style.flex !== flex) panel.style.flex = flex;
    });
    for (const handle of this.#handles()) {
      const index = panels.indexOf(this.#before(handle));
      if (index < 0 || index >= panels.length - 1) {
        // An inline style: the theme gives the handle a display of its own, which would win over the hidden attribute.
        if (handle.style.display !== 'none') handle.style.display = 'none';
        continue;
      }
      if (handle.style.display) handle.style.removeProperty('display');
      const [min, max] = this.#range(index);
      handle.setAttribute('aria-valuenow', String(Math.round(this.#sizes[index])));
      handle.setAttribute('aria-valuemin', String(Math.round(min)));
      handle.setAttribute('aria-valuemax', String(Math.round(max)));
    }
  }

  /** Sets the size of panel index; its neighbour takes the difference. True when something changed. */
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

  #commit() {
    const value = this.#sizes.map((s) => +s.toFixed(2)).join(',');
    if (!this.#reporter.bound && this.#key) write(this.#key, value);
    // The .NET listener sits on a childless sink, so a kly-change of a component inside the group is never read as sizes.
    this.#reporter.report('kly-change', { value }, this.querySelector(':scope > [data-kly-dashboard-sink]') ?? this);
  }

  get #rtl() {
    return getComputedStyle(this).direction === 'rtl';
  }

  #onKeyDown = (event) => {
    const handle = event.target instanceof Element ? event.target.closest('[data-kly-dash-handle]') : null;
    if (!handle || event.altKey || event.ctrlKey || event.metaKey || handle.closest('kly-dashboard-group') !== this) return;
    const index = this.#panels().indexOf(this.#before(handle));
    if (index < 0 || index >= this.#sizes.length - 1) return;
    const step = Number(handle.dataset.step) || 5;
    const sign = this.#rtl ? -1 : 1;
    const current = this.#sizes[index];
    let target = null;
    if (event.key === 'ArrowRight') target = current + step * sign;
    else if (event.key === 'ArrowLeft') target = current - step * sign;
    else if (event.key === 'Home') target = -Infinity;
    else if (event.key === 'End') target = Infinity;
    if (target === null) return;
    event.preventDefault();
    if (this.#resize(index, target)) this.#commit();
  };

  #onPointerDown = (event) => {
    const handle = event.target instanceof Element ? event.target.closest('[data-kly-dash-handle]') : null;
    if (!handle || event.button !== 0 || handle.closest('kly-dashboard-group') !== this) return;
    const panels = this.#panels();
    const index = panels.indexOf(this.#before(handle));
    if (index < 0 || index >= panels.length - 1) return;
    const available = panels.reduce((sum, p) => sum + p.getBoundingClientRect().width, 0);
    handle.setPointerCapture(event.pointerId);
    handle.dataset.dragging = '';
    handle.focus();
    this.#drag = { index, handle, available, start: [...this.#sizes], origin: event.clientX, moved: false };
    handle.addEventListener('pointermove', this.#onPointerMove);
    handle.addEventListener('pointerup', this.#onPointerEnd);
    handle.addEventListener('pointercancel', this.#onPointerEnd);
    event.preventDefault();
  };

  #onPointerMove = (event) => {
    const drag = this.#drag;
    if (!drag) return;
    const total = drag.start.reduce((a, b) => a + b, 0);
    const delta = ((event.clientX - drag.origin) / (drag.available || 1)) * total * (this.#rtl ? -1 : 1);
    this.#resize(drag.index, drag.start[drag.index] + delta, drag.start);
    drag.moved = true;
  };

  #onPointerEnd = () => {
    const drag = this.#drag;
    if (!drag) return;
    this.#drag = null;
    const { handle } = drag;
    handle.removeEventListener('pointermove', this.#onPointerMove);
    handle.removeEventListener('pointerup', this.#onPointerEnd);
    handle.removeEventListener('pointercancel', this.#onPointerEnd);
    delete handle.dataset.dragging;
    if (drag.moved && drag.start.some((s, i) => Math.abs(s - this.#sizes[i]) > 1e-6)) this.#commit();
  };
}

if (!customElements.get('kly-dashboard-group')) customElements.define('kly-dashboard-group', KlyDashboardGroupElement);

// KlyDashboardSearchButton opens the KlyDashboardSearch of its dashboard (or the first one of the page) by clicking the
// hidden trigger of its modal, so the modal returns focus to the button when it closes.
document.addEventListener('click', (event) => {
  const button = event.target instanceof Element ? event.target.closest('[data-kly-search-open]') : null;
  if (!button || button.disabled) return;
  const selector = 'dialog[data-kly-dashboard-search]';
  const dialog = button.closest('kly-dashboard-group')?.querySelector(selector) ?? document.querySelector(selector);
  dialog?.closest('kly-modal')?.querySelector(':scope > [data-slot="trigger"] button')?.click();
});
