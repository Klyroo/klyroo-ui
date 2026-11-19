import { Reporter, ensureId } from './shared.js';

// The state is remembered in cookies, so the server can render it on the next request (the app reads them and passes
// Collapsed): blux-sidebar-{id} is "1" when collapsed, blux-sidebar-{id}-width the width in pixels.
const SIDEBAR_PREFIX = 'blux-sidebar-';
const ONE_YEAR = 60 * 60 * 24 * 365;

function read(name) {
  if (!name) return null;
  try {
    for (const part of document.cookie.split('; ')) {
      const at = part.indexOf('=');
      if (at > 0 && part.slice(0, at) === name) return decodeURIComponent(part.slice(at + 1));
    }
  } catch {
    // Cookies blocked.
  }
  return null;
}

function write(name, value) {
  if (!name) return;
  try {
    const secure = location.protocol === 'https:' ? '; secure' : '';
    document.cookie = `${name}=${encodeURIComponent(value)}; path=/; max-age=${ONE_YEAR}; samesite=lax${secure}`;
  } catch {
    // Cookies blocked: the state lasts until the page is reloaded.
  }
}

/**
 * <bx-sidebar data-bx-sidebar-id="app" collapsed resizable data-bx-width="256" data-bx-min-width="192" data-bx-max-width="448" style="--bx-sidebar-width:256px">
 *   <aside data-slot="root" data-state="expanded">
 *     <button data-bx-sidebar-toggle aria-pressed="false">…</button> … <div role="separator" data-slot="handle"></div>
 *   </aside>
 *   <div>…the mobile panel…</div>
 * </bx-sidebar>
 *
 * Owns the collapsed state (data-state on the aside, aria-pressed on the toggles) and the width (the --bx-sidebar-width
 * variable on this element). Both are remembered in cookies under the id, except that a bound collapsed state
 * (data-bx-seq) is never read from storage: .NET owns it. A change of the collapsed state is reported with "bx-toggle",
 * where open means expanded.
 */
export class BxSidebarElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'collapsed'];

  #reporter = new Reporter(this);
  #collapsed = false;
  #width = null;
  #ready = false;
  #drag = null;
  #settling = null;

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('pointerdown', this.#onPointerDown);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('bx-toggle', this.#onPanelToggle);
    // Blazor inserts the element before its attributes and children: read them once the whole batch is applied.
    queueMicrotask(() => this.#init());
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('pointerdown', this.#onPointerDown);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('bx-toggle', this.#onPanelToggle);
    this.#ready = false;
  }

  attributeChangedCallback(name) {
    if (!this.#ready) return;
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-bx-seq') queueMicrotask(() => this.#applyRendered(true));
    else this.#applyRendered(false);
  }

  /** Re-applies the state an enhanced navigation merge reset to what the server rendered. */
  refresh() {
    if (!this.#ready) return;
    this.#reporter.adopt();
    this.#reporter.take('collapsed', true);
    this.#settle();
  }

  get #key() {
    const id = this.getAttribute('data-bx-sidebar-id');
    return id ? SIDEBAR_PREFIX + encodeURIComponent(id) : null;
  }

  get #aside() {
    return this.querySelector(':scope > [data-slot="root"]');
  }

  get #handle() {
    return this.#aside?.querySelector(':scope > [data-slot="handle"]') ?? null;
  }

  #number(name, fallback) {
    const value = Number(this.getAttribute(name));
    return Number.isFinite(value) && value > 0 ? value : fallback;
  }

  get #min() {
    return this.#number('data-bx-min-width', 192);
  }

  get #max() {
    return this.#number('data-bx-max-width', 448);
  }

  get #default() {
    return this.#number('data-bx-width', 256);
  }

  #clamp(width) {
    return Math.min(this.#max, Math.max(this.#min, Math.round(width)));
  }

  #init() {
    if (!this.isConnected || this.#ready) return;
    this.#reporter.take('collapsed', true);
    const stored = this.#reporter.bound ? null : read(this.#key);
    this.#collapsed = stored === null ? this.hasAttribute('collapsed') : stored === '1';
    const width = this.hasAttribute('resizable') ? Number(read(this.#key && this.#key + '-width')) : 0;
    this.#width = width > 0 ? this.#clamp(width) : null;
    this.#ready = true;
    this.#settle();
  }

  /**
   * Applies the state without the width transition, then allows it: the state of a page load or of an enhanced
   * navigation is not something the user did, so it must not animate in.
   */
  #settle() {
    const token = (this.#settling = {});
    this.#aside?.removeAttribute('data-ready');
    this.#render();
    requestAnimationFrame(() => requestAnimationFrame(() => {
      if (this.#settling === token && this.#ready) this.#aside?.setAttribute('data-ready', '');
    }));
  }

  #applyRendered(force) {
    const value = this.#reporter.take('collapsed', force);
    if (value === undefined) return;
    this.#collapsed = value !== null;
    this.#render();
  }

  #render() {
    const state = this.#collapsed ? 'collapsed' : 'expanded';
    const aside = this.#aside;
    if (aside && aside.dataset.state !== state) aside.dataset.state = state;
    for (const toggle of this.querySelectorAll('[data-bx-sidebar-toggle]')) {
      if (toggle.closest('bx-sidebar') === this) toggle.setAttribute('aria-pressed', String(this.#collapsed));
    }
    this.#renderWidth();
    this.syncControls();
  }

  /**
   * Collapse and open buttons placed outside the sidebar (BxDashboardSidebarCollapse, BxDashboardSidebarToggle) carry
   * aria-controls and aria-expanded set here, since the state lives in the browser.
   */
  syncControls() {
    if (!this.#ready) return;
    const aside = this.#aside;
    const dialog = this.querySelector('dialog');
    for (const button of document.querySelectorAll('[data-bx-sidebar-collapse]')) {
      if (sidebarFor(button) !== this || !aside) continue;
      button.setAttribute('aria-controls', ensureId(aside, 'sidebar'));
      button.setAttribute('aria-expanded', String(!this.#collapsed));
    }
    for (const button of document.querySelectorAll('[data-bx-sidebar-open]')) {
      if (sidebarFor(button) !== this || !dialog) continue;
      button.setAttribute('aria-controls', ensureId(dialog, 'sidebar-panel'));
      button.setAttribute('aria-expanded', String(dialog.open));
    }
  }

  toggleCollapsed() {
    if (this.#ready) this.#setCollapsed(!this.#collapsed);
  }

  /** Opens the off-canvas panel, as its own button would. */
  openPanel() {
    this.querySelector('bx-modal > [data-slot="trigger"] button')?.click();
  }

  // The panel is a bx-modal inside: its toggles bubble up here; the sidebar's own report has this element as target.
  #onPanelToggle = (event) => {
    if (event.target === this) return;
    queueMicrotask(() => this.syncControls());
  };

  #renderWidth() {
    const width = this.#width ?? this.#default;
    this.style.setProperty('--bx-sidebar-width', `${width}px`);
    const handle = this.#handle;
    if (handle) {
      handle.setAttribute('aria-valuemin', String(this.#min));
      handle.setAttribute('aria-valuemax', String(this.#max));
      handle.setAttribute('aria-valuenow', String(width));
    }
  }

  #setCollapsed(collapsed) {
    this.#collapsed = collapsed;
    this.#render();
    if (!this.#reporter.bound) write(this.#key, collapsed ? '1' : '0');
    this.#reporter.report('bx-toggle', { open: !collapsed });
  }

  #setWidth(width, persist) {
    this.#width = this.#clamp(width);
    this.#renderWidth();
    if (persist) write(this.#key && this.#key + '-width', String(this.#width));
  }

  #onClick = (event) => {
    const toggle = event.target instanceof Element ? event.target.closest('[data-bx-sidebar-toggle]') : null;
    if (!toggle || toggle.closest('bx-sidebar') !== this) return;
    this.#setCollapsed(!this.#collapsed);
  };

  get #direction() {
    return getComputedStyle(this).direction === 'rtl' ? -1 : 1;
  }

  #onPointerDown = (event) => {
    const handle = event.target instanceof Element ? event.target.closest('[data-slot="handle"]') : null;
    if (!handle || handle !== this.#handle || event.button !== 0) return;
    const aside = this.#aside;
    event.preventDefault();
    handle.setPointerCapture(event.pointerId);
    this.#drag = { startX: event.clientX, startWidth: aside.getBoundingClientRect().width, direction: this.#direction };
    aside.dataset.dragging = '';
    handle.dataset.dragging = '';
    handle.addEventListener('pointermove', this.#onPointerMove);
    handle.addEventListener('pointerup', this.#onPointerEnd);
    handle.addEventListener('pointercancel', this.#onPointerEnd);
  };

  #onPointerMove = (event) => {
    const drag = this.#drag;
    if (!drag) return;
    this.#setWidth(drag.startWidth + drag.direction * (event.clientX - drag.startX), false);
  };

  #onPointerEnd = (event) => {
    const handle = event.currentTarget;
    handle.removeEventListener('pointermove', this.#onPointerMove);
    handle.removeEventListener('pointerup', this.#onPointerEnd);
    handle.removeEventListener('pointercancel', this.#onPointerEnd);
    this.#drag = null;
    delete this.#aside?.dataset.dragging;
    delete handle.dataset.dragging;
    if (this.#width !== null) write(this.#key && this.#key + '-width', String(this.#width));
  };

  #onKeyDown = (event) => {
    const handle = event.target instanceof Element ? event.target.closest('[data-slot="handle"]') : null;
    if (!handle || handle !== this.#handle) return;
    const current = this.#width ?? this.#default;
    let next = null;
    if (event.key === 'ArrowRight') next = current + 16 * this.#direction;
    else if (event.key === 'ArrowLeft') next = current - 16 * this.#direction;
    else if (event.key === 'Home') next = this.#min;
    else if (event.key === 'End') next = this.#max;
    if (next === null) return;
    event.preventDefault();
    this.#setWidth(next, true);
  };
}

if (!customElements.get('bx-sidebar')) customElements.define('bx-sidebar', BxSidebarElement);

/** The sidebar a control outside it belongs to: data-bx-sidebar-for names it, else the one of the same dashboard. */
function sidebarFor(button) {
  const id = button.getAttribute('data-bx-sidebar-for');
  const selector = id ? `bx-sidebar[data-bx-sidebar-id="${CSS.escape(id)}"]` : 'bx-sidebar';
  return button.closest('bx-dashboard-group')?.querySelector(selector) ?? document.querySelector(selector);
}

document.addEventListener('click', (event) => {
  const button = event.target instanceof Element ? event.target.closest('[data-bx-sidebar-collapse], [data-bx-sidebar-open]') : null;
  if (!button || button.disabled) return;
  const sidebar = sidebarFor(button);
  if (!sidebar) return;
  if (button.hasAttribute('data-bx-sidebar-collapse')) sidebar.toggleCollapsed();
  else sidebar.openPanel();
});

// ---- Mobile panels of the header and the sidebar: real modal dialogs marked data-bx-layout-menu.
const MENU = 'dialog[data-bx-layout-menu]';

// Following a link closes the panel: the page may stay (enhanced navigation) and a modal dialog would keep it inert.
document.addEventListener('click', (event) => {
  const link = event.target instanceof Element ? event.target.closest('a[href]') : null;
  const dialog = link?.closest(MENU);
  if (dialog?.open) dialog.close();
});

// The panels belong to small screens: close them when the screen grows past the lg breakpoint, where their trigger is hidden.
matchMedia('(min-width: 64rem)').addEventListener('change', (event) => {
  if (!event.matches) return;
  for (const dialog of document.querySelectorAll(`${MENU}[open]`)) dialog.close();
});

// ---- BxLocaleSelect: writes the ASP.NET Core culture cookie, then reloads so the server renders the new culture.
document.addEventListener('bx-change', (event) => {
  const host = event.target instanceof Element ? event.target.closest('[data-bx-locale-select]') : null;
  const culture = event.detail?.value;
  if (!host || !culture) return;
  const name = host.getAttribute('data-bx-cookie') || '.AspNetCore.Culture';
  const path = host.getAttribute('data-bx-cookie-path') || '/';
  const secure = location.protocol === 'https:' ? '; secure' : '';
  try {
    document.cookie = `${name}=${encodeURIComponent(`c=${culture}|uic=${culture}`)}; path=${path}; max-age=31536000; samesite=lax${secure}`;
  } catch {
    // Cookies blocked: nothing to remember.
  }
  if (host.hasAttribute('data-bx-reload')) location.reload();
});
