import { Reporter, ensureId } from './shared.js';
import { isHybrid } from './host.js';
import { appBasePath, appCookiePath, sidebarCookiePrefix } from './base-path.js';

// The state is remembered in cookies, so the server can render it on the next request (the app reads them and passes
// Collapsed): blux-sidebar-{id} is "1" when collapsed, blux-sidebar-{id}-width the width in pixels.
// In a Blazor Hybrid web view there is no server to read them and cookies are unreliable there (WebKit's tracking
// prevention on iOS and macOS), so the same names are kept in localStorage.
// At the root the names are blux-sidebar-{id}; below a path base the base is part of the name (see base-path.js and BxPathScope), because
// a server that reads the cookie keeps the last of two with the same name, and the cookie of another app on the origin can come after.
const ONE_YEAR = 60 * 60 * 24 * 365;

function read(name) {
  if (!name) return null;
  if (isHybrid()) {
    try {
      return localStorage.getItem(name);
    } catch {
      return null;
    }
  }
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
  if (isHybrid()) {
    try {
      localStorage.setItem(name, String(value));
    } catch {
      // Storage blocked: the state lasts until the page is reloaded.
    }
    return;
  }
  try {
    const secure = location.protocol === 'https:' ? '; secure' : '';
    document.cookie = `${name}=${encodeURIComponent(value)}; path=${appCookiePath()}; max-age=${ONE_YEAR}; samesite=lax${secure}`;
  } catch {
    // Cookies blocked: the state lasts until the page is reloaded.
  }
}

/**
 * <bx-sidebar data-bx-sidebar-id="app" collapsed resizable data-bx-width="256" data-bx-min-width="192" data-bx-max-width="448" style="--bx-sidebar-width:256px">
 *   <aside data-slot="root" data-state="expanded">
 *     <button data-bx-sidebar-toggle aria-pressed="false">…</button> … <div role="separator" data-slot="handle"></div>
 *   </aside>
 *   <div>…the button that opens the panel…</div>
 * </bx-sidebar>
 *
 * Owns the collapsed state (data-state on the aside, aria-pressed on the toggles) and the width (the --bx-sidebar-width
 * variable on this element). Both are remembered in cookies under the id, except that a bound collapsed state
 * (data-bx-seq) is never read from storage: .NET owns it. A change of the collapsed state is reported with "bx-toggle",
 * where open means expanded.
 *
 * Below the lg breakpoint the aside is the off-canvas panel: a popover="auto" element that a button with popovertarget opens.
 * The popover alone is not a modal, so while it is open this element makes it one: role dialog and aria-modal on the inner panel element
 * (an aside may not be a dialog), the rest of the page inert, Tab kept inside, the focus moved in and, on close, back to where it was.
 * A form submitted inside the panel, or a navigation to another path, closes it; a modal opened from inside it whose dialog is outside the
 * aside closes it (the browser does, as for any popover; and the focus goes back to the button when that modal closes); a modal declared inside the aside works as it is. Escape and a click outside
 * (or on the backdrop) close it natively; a screen that grows past lg closes it, and the collapsed state only applies from lg.
 */
const WIDE = '(min-width: 64rem)';
const TABBABLE = 'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), summary, [contenteditable=""], [contenteditable="true"], [tabindex]:not([tabindex="-1"])';

export class BxSidebarElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'collapsed'];

  #reporter = new Reporter(this);
  #collapsed = false;
  #width = null;
  #ready = false;
  #drag = null;
  #settling = null;
  #wide = matchMedia(WIDE);
  #opener = null;
  #inerted = [];

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('pointerdown', this.#onPointerDown);
    this.addEventListener('keydown', this.#onKeyDown);
    // beforetoggle and toggle do not bubble, but a capture listener on an ancestor still sees them.
    this.addEventListener('beforetoggle', this.#onBeforeToggle, true);
    this.addEventListener('toggle', this.#onToggle, true);
    this.#wide.addEventListener('change', this.#onWideChange);
    // Blazor inserts the element before its attributes and children: read them once the whole batch is applied.
    queueMicrotask(() => this.#init());
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('pointerdown', this.#onPointerDown);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('beforetoggle', this.#onBeforeToggle, true);
    this.removeEventListener('toggle', this.#onToggle, true);
    this.#wide.removeEventListener('change', this.#onWideChange);
    this.#release();
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
    // An enhanced navigation merge resets the attributes this element set while the panel was open: put the modal state back, or close a panel that should not stay.
    const aside = this.#aside;
    if (aside?.matches(':popover-open')) {
      if (this.#wide.matches) aside.hidePopover();
      else this.#makeModal(aside);
    }
  }

  get #key() {
    const id = this.getAttribute('data-bx-sidebar-id');
    return id ? sidebarCookiePrefix(appBasePath()) + encodeURIComponent(id) : null;
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
    // Collapsed is a state of the column: the panel below lg is always expanded.
    const state = this.#collapsed && this.#wide.matches ? 'collapsed' : 'expanded';
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
    for (const button of document.querySelectorAll('[data-bx-sidebar-collapse]')) {
      if (sidebarFor(button) !== this || !aside) continue;
      button.setAttribute('aria-controls', ensureId(aside, 'sidebar'));
      button.setAttribute('aria-expanded', String(!this.#collapsed));
    }
    for (const button of document.querySelectorAll('[data-bx-sidebar-open]')) {
      if (sidebarFor(button) !== this || !aside) continue;
      // The native invoker: the browser toggles the popover, with no script in the way.
      if (button.popoverTargetElement !== aside) {
        button.popoverTargetElement = aside;
        button.popoverTargetAction = 'toggle';
      }
      button.setAttribute('aria-controls', ensureId(aside, 'sidebar'));
      button.setAttribute('aria-expanded', String(aside.matches(':popover-open')));
    }
  }

  toggleCollapsed() {
    if (this.#ready) this.#setCollapsed(!this.#collapsed);
  }

  /** Opens the off-canvas panel, as its own button would. Does nothing from lg up, where the sidebar is a column. */
  openPanel() {
    const aside = this.#aside;
    if (aside && !this.#wide.matches && !aside.matches(':popover-open')) aside.showPopover();
  }

  /** Opens the panel or closes it. */
  togglePanel() {
    const aside = this.#aside;
    if (!aside) return;
    if (aside.matches(':popover-open')) aside.hidePopover();
    else this.openPanel();
  }

  #onBeforeToggle = (event) => {
    const aside = this.#aside;
    if (!aside || event.target !== aside) return;
    if (event.newState === 'open') {
      // A column has nothing to open.
      if (this.#wide.matches) {
        event.preventDefault();
        return;
      }
      this.#opener = document.activeElement;
      this.#makeModal(aside);
    } else {
      // Before the popover goes: its invoker must be able to take the focus back.
      this.#release();
    }
  };

  #onToggle = (event) => {
    const aside = this.#aside;
    if (!aside || event.target !== aside) return;
    if (event.newState === 'open') {
      const first = this.#tabbables(aside)[0];
      (first ?? aside).focus({ preventScroll: true });
    } else {
      const opener = this.#opener;
      this.#opener = null;
      const active = document.activeElement;
      const modal = [...document.querySelectorAll('dialog:modal')].find((dialog) => !aside.contains(dialog));
      if (modal) this.#handOver(modal, opener);
      else if (opener?.isConnected && (!active || active === document.body || aside.contains(active))) opener.focus({ preventScroll: true });
    }
    this.syncControls();
  };

  // The panel closes for what leaves the page it belongs to: a form submitted in it, or a navigation to another path (a change of the query alone stays).
  #onNavigate = (event) => {
    const aside = this.#aside;
    if (!aside?.matches(':popover-open')) return;
    let path = null;
    try {
      path = new URL(event.destination.url).pathname;
    } catch {
      return;
    }
    if (path !== location.pathname) aside.hidePopover();
  };

  // A modal dialog opened outside the aside while the panel is open (a modal of the layout, opened from a menu of the panel, or Ctrl+K): showModal closes
  // the popovers that are not its ancestors, so the panel is already closed and its inert is gone. What is left is the focus: the dialog may have failed to take it,
  // and when it closes the focus goes back to the button that opened the panel.
  #handOver(dialog, opener) {
    if (!dialog.contains(document.activeElement)) (this.#tabbables(dialog)[0] ?? dialog).focus({ preventScroll: true });
    dialog.addEventListener('close', () => {
      // After the browser's own focus fix-up: the focus is still in the closed dialog or nowhere, unless the modal sent it somewhere on purpose.
      setTimeout(() => {
        const active = document.activeElement;
        if (opener?.isConnected && (!active || active === document.body || dialog.contains(active))) opener.focus({ preventScroll: true });
      });
    }, { once: true });
  }

  #onWideChange = () => {
    const aside = this.#aside;
    if (this.#wide.matches && aside?.matches(':popover-open')) aside.hidePopover();
    if (this.#ready) this.#render();
  };

  #tabbables(root) {
    return [...root.querySelectorAll(TABBABLE)].filter((el) => !el.closest('[inert]') && el.getClientRects().length > 0 && getComputedStyle(el).visibility !== 'hidden');
  }

  /** A popover is not modal: name the aside a modal dialog and make everything around it inert until it closes. */
  #makeModal(aside) {
    this.#release();
    // The dialog semantics go on the inner panel: an aside has no dialog role, and it stays the complementary landmark.
    const panel = aside.querySelector(':scope > [data-slot="panel"]');
    const title = panel?.querySelector(':scope > [data-slot="mobile-header"] > h2');
    if (panel) {
      panel.setAttribute('role', 'dialog');
      panel.setAttribute('aria-modal', 'true');
      if (title?.id) panel.setAttribute('aria-labelledby', title.id);
    }
    window.navigation?.addEventListener('navigate', this.#onNavigate);
    for (let node = aside; node.parentElement; node = node.parentElement) {
      for (const sibling of node.parentElement.children) {
        if (sibling === node || sibling.inert || sibling.localName === 'head') continue;
        sibling.inert = true;
        this.#inerted.push(sibling);
      }
    }
  }

  #release() {
    for (const element of this.#inerted) element.inert = false;
    this.#inerted = [];
    window.navigation?.removeEventListener('navigate', this.#onNavigate);
    const panel = this.#aside?.querySelector(':scope > [data-slot="panel"]');
    if (panel?.getAttribute('role') === 'dialog') {
      panel.removeAttribute('role');
      panel.removeAttribute('aria-modal');
      panel.removeAttribute('aria-labelledby');
    }
  }

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
    // A click on the backdrop reaches the aside itself, from outside its box.
    const aside = this.#aside;
    if (aside && event.target === aside && aside.matches(':popover-open')) {
      const box = aside.getBoundingClientRect();
      if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) aside.hidePopover();
      return;
    }
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
    const panel = this.#aside;
    // A modal dialog declared inside the panel keeps Tab to itself (the rest of the panel is inert while it is open).
    if (event.key === 'Tab' && panel?.matches(':popover-open') && event.target instanceof Element && panel.contains(event.target) && !event.target.closest('dialog:modal')) {
      // Keep Tab inside the open panel.
      const items = this.#tabbables(panel);
      if (items.length === 0) {
        event.preventDefault();
      } else if (event.shiftKey && (event.target === items[0] || event.target === panel)) {
        event.preventDefault();
        items.at(-1).focus();
      } else if (!event.shiftKey && event.target === items.at(-1)) {
        event.preventDefault();
        items[0].focus();
      }
      return;
    }
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
  const own = button.closest('bx-sidebar');
  if (own) return own;
  const id = button.getAttribute('data-bx-sidebar-for');
  const selector = id ? `bx-sidebar[data-bx-sidebar-id="${CSS.escape(id)}"]` : 'bx-sidebar';
  return button.closest('bx-dashboard-group')?.querySelector(selector) ?? document.querySelector(selector);
}

document.addEventListener('click', (event) => {
  const button = event.target instanceof Element ? event.target.closest('[data-bx-sidebar-collapse], [data-bx-sidebar-open]') : null;
  if (!button || button.disabled) return;
  const sidebar = sidebarFor(button);
  if (!sidebar) return;
  if (button.hasAttribute('data-bx-sidebar-collapse')) {
    sidebar.toggleCollapsed();
  } else if (!button.popoverTargetElement) {
    // A button rendered after the sidebar was set up: not wired yet, so do what the native invoker would. It is wired once the panel toggles.
    sidebar.togglePanel();
  }
});

// Following a link closes the sidebar panel: the page may stay (enhanced navigation) and the panel would keep it inert.
document.addEventListener('click', (event) => {
  const link = event.target instanceof Element ? event.target.closest('a[href]') : null;
  const panel = link?.closest('bx-sidebar > [popover]:popover-open');
  if (panel) panel.hidePopover();
});

// So does a form submitted in it: the page behind is about to change (an enhanced submit would leave the panel open over it).
document.addEventListener('submit', (event) => {
  const panel = event.target instanceof Element ? event.target.closest('bx-sidebar > [popover]:popover-open') : null;
  if (panel) panel.hidePopover();
}, true);

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
  // In a Blazor Hybrid app there is no ASP.NET Core to read the cookie: BxLocaleSelect keeps the choice in an IBxCultureStore (.NET side).
  if (isHybrid()) return;
  const name = host.getAttribute('data-bx-cookie') || '.AspNetCore.Culture';
  // One culture for the whole origin, so the default path is "/": a language chosen in one app applies to the others.
  const path = host.getAttribute('data-bx-cookie-path') || '/';
  const secure = location.protocol === 'https:' ? '; secure' : '';
  try {
    document.cookie = `${name}=${encodeURIComponent(`c=${culture}|uic=${culture}`)}; path=${path}; max-age=31536000; samesite=lax${secure}`;
  } catch {
    // Cookies blocked: nothing to remember.
  }
  if (host.hasAttribute('data-bx-reload')) location.reload();
});
