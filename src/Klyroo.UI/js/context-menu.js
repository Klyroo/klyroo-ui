import { Reporter, float, observeChildren, rovingKey } from './shared.js';

const MENU_ITEMS = '[role="menuitem"]:not([aria-disabled="true"])';
const LONG_PRESS_MS = 500;
const LONG_PRESS_SLOP = 10;

/**
 * <kly-context-menu open disabled>
 *   <div data-slot="trigger" tabindex="0">…area…</div>
 *   <div popover="manual" role="menu" data-slot="content">…menuitems…</div>
 * </kly-context-menu>
 *
 * Opens the menu at the pointer on right click, on a touch long press, and on Shift+F10 or the ContextMenu key
 * (at the focused element). The menu is positioned with Floating UI on a virtual point. It is a manual popover
 * because a menu opened by the press that is still going on must not be light-dismissed by its release: this element
 * dismisses it on an outside press, Escape, Tab, scroll and resize. Every change is reported with "kly-toggle".
 */
export class KlyContextMenuElement extends HTMLElement {
  static observedAttributes = ['data-kly-seq', 'open'];

  #observer = null;
  #stopFloating = null;
  #reporter = new Reporter(this);
  #point = { x: 0, y: 0 };
  #returnFocus = null;
  #pressTimer = 0;
  #pressOrigin = null;

  connectedCallback() {
    this.addEventListener('contextmenu', this.#onContextMenu);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('pointerdown', this.#onPointerDown);
    this.addEventListener('pointermove', this.#onPointerMove);
    this.addEventListener('pointerup', this.#cancelPress);
    this.addEventListener('pointercancel', this.#cancelPress);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('toggle', this.#onToggle, true);
    document.addEventListener('pointerdown', this.#onOutsidePress, true);
    document.addEventListener('scroll', this.#onScroll, true);
    window.addEventListener('resize', this.#hide);
    this.#observer = observeChildren(this, () => this.#sync());
  }

  disconnectedCallback() {
    this.removeEventListener('contextmenu', this.#onContextMenu);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('pointerdown', this.#onPointerDown);
    this.removeEventListener('pointermove', this.#onPointerMove);
    this.removeEventListener('pointerup', this.#cancelPress);
    this.removeEventListener('pointercancel', this.#cancelPress);
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('toggle', this.#onToggle, true);
    document.removeEventListener('pointerdown', this.#onOutsidePress, true);
    document.removeEventListener('scroll', this.#onScroll, true);
    window.removeEventListener('resize', this.#hide);
    this.#observer?.disconnect();
    this.#cancelPress();
    this.#stopFloating?.();
    this.#stopFloating = null;
  }

  attributeChangedCallback(name) {
    // data-kly-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-kly-seq') queueMicrotask(() => this.#apply());
    else if (name === 'open') this.#apply();
  }

  refresh() {
    this.#sync();
  }

  get #area() {
    return this.querySelector(':scope > [data-slot="trigger"]');
  }

  get #menu() {
    return this.querySelector(':scope > [popover]');
  }

  get #isOpen() {
    return this.#menu?.matches(':popover-open') ?? false;
  }

  #known = null;

  // Only a new menu element is opened from the attribute: a menu closed by the user must not reopen on the next mutation.
  #sync() {
    const menu = this.#menu;
    if (menu === this.#known) return;
    this.#known = menu;
    if (menu && this.hasAttribute('open') && !this.#isOpen) this.#openAtCenter();
  }

  #apply() {
    if (!this.#menu || this.#reporter.isStale()) return;
    // The render answering an open at the pointer must not move the menu to the centre.
    if (!this.hasAttribute('open')) this.#hide();
    else if (!this.#isOpen) this.#openAtCenter();
  }

  #openAtCenter() {
    const rect = (this.#area ?? this).getBoundingClientRect();
    this.#openAt(rect.left + rect.width / 2, rect.top + rect.height / 2);
  }

  #items() {
    return [...(this.#menu?.querySelectorAll(MENU_ITEMS) ?? [])].filter((item) => item.closest('kly-context-menu') === this);
  }

  #openAt(x, y) {
    const menu = this.#menu;
    if (!menu || this.hasAttribute('disabled')) return;
    this.#point = { x, y };
    const wasOpen = this.#isOpen;
    if (!wasOpen) {
      this.#returnFocus = document.activeElement;
      menu.showPopover();
    }
    // A virtual element at the pointer: Floating UI only needs its rectangle.
    this.#stopFloating?.();
    const reference = {
      getBoundingClientRect: () => new DOMRect(this.#point.x, this.#point.y, 0, 0),
      contextElement: this.#area ?? this,
    };
    this.#stopFloating = float(reference, menu, { placement: 'bottom-start', distance: 2 });
    (this.#items()[0] ?? menu).focus({ preventScroll: true });
  }

  #hide = () => {
    const menu = this.#menu;
    if (menu?.matches(':popover-open')) menu.hidePopover();
  };

  #owns(event) {
    const area = this.#area;
    return !!area && area.contains(event.target) && event.target.closest?.('kly-context-menu') === this;
  }

  #onContextMenu = (event) => {
    if (!this.#owns(event) || this.hasAttribute('disabled') || !this.#menu) return;
    event.preventDefault();
    // A keyboard-initiated contextmenu event has no pointer position: its coordinates are those of the element.
    this.#openAt(event.clientX, event.clientY);
  };

  #onKeyDown = (event) => {
    const menu = this.#menu;
    if (!menu) return;
    if (menu.contains(event.target)) {
      if (event.key === 'Escape') {
        event.preventDefault();
        event.stopPropagation();
        this.#hide();
      } else if (event.key === 'Tab') {
        this.#hide();
      } else {
        rovingKey(event, this.#items(), document.activeElement);
      }
      return;
    }
    const isMenuKey = event.key === 'ContextMenu' || (event.key === 'F10' && event.shiftKey);
    if (!isMenuKey || !this.#owns(event) || this.hasAttribute('disabled')) return;
    event.preventDefault();
    const rect = event.target.getBoundingClientRect();
    if (event.target === this.#area) this.#openAt(rect.left + Math.min(rect.width / 2, 48), rect.top + Math.min(rect.height / 2, 24));
    else this.#openAt(rect.left, rect.bottom);
  };

  // Touch has no right button: a press that stays in place opens the menu, as on mobile platforms.
  #onPointerDown = (event) => {
    if (event.pointerType === 'mouse' || !this.#owns(event) || this.hasAttribute('disabled')) return;
    this.#cancelPress();
    this.#pressOrigin = { x: event.clientX, y: event.clientY };
    this.#pressTimer = setTimeout(() => {
      this.#pressTimer = 0;
      this.#openAt(this.#pressOrigin.x, this.#pressOrigin.y);
    }, LONG_PRESS_MS);
  };

  #onPointerMove = (event) => {
    if (!this.#pressTimer) return;
    if (Math.hypot(event.clientX - this.#pressOrigin.x, event.clientY - this.#pressOrigin.y) > LONG_PRESS_SLOP) this.#cancelPress();
  };

  #cancelPress = () => {
    clearTimeout(this.#pressTimer);
    this.#pressTimer = 0;
  };

  #onClick = (event) => {
    const item = event.target.closest?.('[role="menuitem"]');
    if (!item || !this.#menu?.contains(item) || item.getAttribute('aria-disabled') === 'true') return;
    // The click keeps propagating, so a .NET handler on the item still runs.
    this.#hide();
  };

  #onOutsidePress = (event) => {
    if (this.#isOpen && !this.#menu.contains(event.target)) this.#hide();
  };

  #onScroll = (event) => {
    if (this.#isOpen && !this.#menu.contains(event.target)) this.#hide();
  };

  #onToggle = (event) => {
    const menu = this.#menu;
    if (!menu || event.target !== menu) return;
    const open = event.newState === 'open';
    this.dataset.state = menu.dataset.state = open ? 'open' : 'closed';
    if (!open) {
      this.#stopFloating?.();
      this.#stopFloating = null;
      const target = this.#returnFocus?.isConnected ? this.#returnFocus : this.#area;
      this.#returnFocus = null;
      const active = document.activeElement;
      if (!active || active === document.body || menu.contains(active)) target?.focus({ preventScroll: true });
    }
    this.#reporter.report('kly-toggle', { open });
  };
}

if (!customElements.get('kly-context-menu')) customElements.define('kly-context-menu', KlyContextMenuElement);
