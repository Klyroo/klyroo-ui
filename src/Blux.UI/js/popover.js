import { FOCUSABLE, Reporter, ensureId, float, isTvMode, observeChildren, rovingKey } from './shared.js';

const MENU_ITEMS = '[role="menuitem"]:not([aria-disabled="true"])';
const SUBMENU = '[data-slot="submenu"]';
// The pointer must rest on an item this long before its submenu opens, and on another item this long before the submenu closes, so a diagonal move toward the submenu keeps it.
const HOVER_OPEN_MS = 100;
const HOVER_CLOSE_MS = 250;

/**
 * <bx-popover open placement="bottom" offset="8">
 *   <span data-slot="trigger"><button>…</button></span>
 *   <div popover="auto" data-slot="content">…</div>
 * </bx-popover>
 *
 * The "open" attribute is the intent rendered by Blazor; the native popover is the truth. Every change of the native
 * state (including Escape and light dismiss) is reported with a bubbling "bx-toggle" event.
 * When the content has role="menu", it behaves as a menu button: arrow keys and typeahead move between items, and
 * activating an item closes the menu.
 * A menu item with aria-haspopup="menu" and popovertarget opens a submenu: a sibling <div popover role="menu" data-slot="submenu">
 * of the content (aria-labelledby names the item). ArrowRight (ArrowLeft in right-to-left text), Enter, Space and a click open it
 * and focus its first item; ArrowLeft and Escape close it and give focus back to the item; the pointer opens it after a short pause
 * on the item and closes it after a pause on another item of the menu.
 */
export class BxPopoverElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'open', 'placement', 'offset'];

  #content = null;
  #stopFloating = null;
  #observer = null;
  #warned = false;
  #reporter = new Reporter(this);
  #floating = new Map();
  #focusSub = null;
  #hovering = false;
  #hoverTimer = 0;

  connectedCallback() {
    // "toggle" does not bubble: listen in the capture phase so a replaced panel needs no re-binding.
    this.addEventListener('toggle', this.#onToggle, true);
    this.addEventListener('beforetoggle', this.#onBeforeToggle, true);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('pointerover', this.#onPointerOver);
    this.#observer = observeChildren(this, () => this.#sync());
  }

  disconnectedCallback() {
    this.removeEventListener('toggle', this.#onToggle, true);
    this.removeEventListener('beforetoggle', this.#onBeforeToggle, true);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('pointerover', this.#onPointerOver);
    this.#observer?.disconnect();
    clearTimeout(this.#hoverTimer);
    this.#stopFloating?.();
    this.#stopFloating = null;
    for (const stop of this.#floating.values()) stop();
    this.#floating.clear();
    this.#content = null;
  }

  attributeChangedCallback(name) {
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-bx-seq') queueMicrotask(() => this.#apply());
    else if (name === 'open') this.#apply();
  }

  #apply() {
    if (!this.#content || this.#reporter.isStale()) return;
    this.hasAttribute('open') ? this.#show() : this.#hide();
  }

  /** Re-applies the wiring, e.g. after enhanced navigation merged server HTML over attributes set here. */
  refresh() {
    this.#sync();
  }

  get #isMenu() {
    return this.#content?.getAttribute('role') === 'menu';
  }

  // Only a button is a valid trigger: it is keyboard reachable and, as the native invoker of the popover,
  // it is excluded from light dismiss, so clicking it closes an open panel instead of reopening it.
  get #trigger() {
    return this.querySelector(':scope > [data-slot="trigger"] button');
  }

  #sync() {
    const content = this.querySelector(':scope > [popover]');
    if (content !== this.#content) {
      this.#content = content;
      if (content && this.hasAttribute('open')) this.#show();
    }
    this.#wireTrigger();
    this.#wireSubmenus();
  }

  #submenus() {
    return [...this.querySelectorAll(`:scope > ${SUBMENU}`)];
  }

  #submenuTrigger(sub) {
    const id = sub.getAttribute('aria-labelledby');
    return id ? this.querySelector(`[id="${CSS.escape(id)}"]`) : null;
  }

  // The invoker link is rendered as attributes (it works before this element does); here it is made a property again after a merge, with the state of the item.
  #wireSubmenus() {
    for (const sub of this.#submenus()) {
      const trigger = this.#submenuTrigger(sub);
      if (!trigger || trigger.getAttribute('aria-disabled') === 'true') continue;
      ensureId(sub, 'submenu');
      if (trigger.popoverTargetElement !== sub) {
        trigger.popoverTargetElement = sub;
        trigger.popoverTargetAction = 'show';
      }
      trigger.setAttribute('aria-controls', sub.id);
      trigger.setAttribute('aria-expanded', String(sub.matches(':popover-open')));
    }
  }

  #wireTrigger() {
    const content = this.#content;
    if (!content) return;
    const trigger = this.#trigger;
    if (!trigger) {
      if (!this.#warned) console.warn('<bx-popover>: the trigger must contain a <button>.', this);
      this.#warned = true;
      return;
    }
    ensureId(content, 'popover');
    ensureId(trigger, 'trigger');
    if (trigger.popoverTargetElement !== content) {
      trigger.popoverTargetElement = content;
      trigger.popoverTargetAction = 'toggle';
    }
    // A panel without a role is a disclosure (navigation menu): expanded and controls say it all.
    const role = content.getAttribute('role');
    if (role) trigger.setAttribute('aria-haspopup', role === 'menu' ? 'menu' : 'dialog');
    else trigger.removeAttribute('aria-haspopup');
    trigger.setAttribute('aria-controls', content.id);
    trigger.setAttribute('aria-expanded', String(content.matches(':popover-open')));
    // Name the panel after its trigger, unless the component was given an explicit label.
    if (!role) {
      // A role-less element cannot carry a name.
    } else if (content.hasAttribute('aria-label')) {
      if (content.hasAttribute('data-bx-labelledby')) {
        content.removeAttribute('aria-labelledby');
        content.removeAttribute('data-bx-labelledby');
      }
    } else if (!content.hasAttribute('aria-labelledby')) {
      content.setAttribute('aria-labelledby', trigger.id);
      content.setAttribute('data-bx-labelledby', '');
    }
  }

  #show() {
    if (!this.#content.matches(':popover-open')) this.#content.showPopover();
  }

  #hide() {
    if (this.#content.matches(':popover-open')) this.#content.hidePopover();
  }

  // The items a menu owns: its own, not those of a submenu or of a menu nested in an item.
  #menuItems(menu = this.#content) {
    return [...menu.querySelectorAll(MENU_ITEMS)].filter((item) => item.closest('[role="menu"]') === menu && item.closest('bx-popover') === this);
  }

  #openSubmenus(menu) {
    return this.#submenus().filter((sub) => sub.matches(':popover-open') && (!menu || this.#submenuTrigger(sub)?.closest('[role="menu"]') === menu));
  }

  #onKeyDown = (event) => {
    const content = this.#content;
    if (!content || !this.#isMenu) return;
    const trigger = this.#trigger;
    // Menu button: arrow keys on the closed trigger open the menu on the first or last item.
    if (event.target === trigger && !isTvMode() && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
      event.preventDefault();
      this.#focusLast = event.key === 'ArrowUp';
      this.#show();
      return;
    }
    const menu = event.target.closest?.('[role="menu"]');
    if (!menu || (menu !== content && !this.#submenus().includes(menu))) return;
    if (event.key === 'Tab') {
      this.#hide();
      return;
    }
    const rtl = getComputedStyle(menu).direction === 'rtl';
    const item = event.target.closest('[role="menuitem"]');
    if (item && event.key === (rtl ? 'ArrowLeft' : 'ArrowRight') && item.hasAttribute('aria-haspopup') && item.getAttribute('aria-disabled') !== 'true') {
      event.preventDefault();
      this.#openSubmenu(item, false);
      return;
    }
    if (menu !== content && event.key === (rtl ? 'ArrowRight' : 'ArrowLeft')) {
      event.preventDefault();
      menu.hidePopover();
      return;
    }
    rovingKey(event, this.#menuItems(menu), document.activeElement);
  };

  // A click on the item (also the one Enter and Space make) opens the submenu natively, as its invoker; this makes the same click for the arrow key and the pointer.
  #openSubmenu(item, byPointer) {
    const sub = item.popoverTargetElement;
    if (!sub) return;
    if (sub.matches(':popover-open')) {
      if (!byPointer) (this.#menuItems(sub)[0] ?? sub).focus({ preventScroll: true });
      return;
    }
    this.#hovering = byPointer;
    item.click();
    this.#hovering = false;
  }

  #onPointerOver = (event) => {
    if (event.pointerType !== 'mouse' || !this.#isMenu) return;
    const menu = event.target.closest?.('[role="menu"]');
    if (!menu || (menu !== this.#content && !this.#submenus().includes(menu))) return;
    clearTimeout(this.#hoverTimer);
    const item = event.target.closest('[role="menuitem"]');
    if (!item || item.getAttribute('aria-disabled') === 'true' || item.closest('[role="menu"]') !== menu) return;
    if (item !== document.activeElement) item.focus({ preventScroll: true });
    const target = item.hasAttribute('aria-haspopup') ? item.popoverTargetElement : null;
    const others = this.#openSubmenus(menu).filter((sub) => sub !== target);
    if (target && !target.matches(':popover-open')) {
      this.#hoverTimer = setTimeout(() => {
        for (const sub of others) sub.hidePopover();
        if (item.isConnected) this.#openSubmenu(item, true);
      }, HOVER_OPEN_MS);
    } else if (others.length > 0) {
      this.#hoverTimer = setTimeout(() => {
        for (const sub of others) if (sub.matches(':popover-open')) sub.hidePopover();
      }, HOVER_CLOSE_MS);
    }
  };

  #onSubmenuToggle(sub, open) {
    const trigger = this.#submenuTrigger(sub);
    sub.dataset.state = open ? 'open' : 'closed';
    trigger?.setAttribute('aria-expanded', String(open));
    this.#floating.get(sub)?.();
    this.#floating.delete(sub);
    if (open) {
      if (trigger) {
        const rtl = getComputedStyle(trigger).direction === 'rtl';
        this.#floating.set(sub, float(trigger, sub, { placement: rtl ? 'left-start' : 'right-start', distance: 0 }));
      }
      if (this.#focusSub === sub) (this.#menuItems(sub)[0] ?? sub).focus({ preventScroll: true });
      this.#focusSub = null;
    } else {
      // Escape, the arrow key or a close of the parent leaves focus in the closed submenu or nowhere: give it back to the item.
      const active = document.activeElement;
      if (!active || active === document.body || sub.contains(active)) trigger?.focus({ preventScroll: true });
    }
  }

  #focusLast = false;

  #onClick = (event) => {
    if (!this.#isMenu) return;
    const item = event.target.closest?.('[role="menuitem"]');
    const menu = item?.closest('[role="menu"]');
    if (!item || !menu || item.getAttribute('aria-disabled') === 'true') return;
    if (menu !== this.#content && !this.#submenus().includes(menu)) return;
    if (item.hasAttribute('aria-haspopup')) {
      // An item that opens a submenu keeps the menu open; focus goes into the submenu unless the pointer only rested on the item.
      if (!this.#hovering) this.#focusSub = item.popoverTargetElement;
      return;
    }
    // The click keeps propagating, so a .NET handler on the item still runs.
    this.#hide();
  };

  // The native state changes now (Escape, light dismiss, the trigger) and "toggle" follows in a later task: a render made before the change, that lands in between,
  // must not put the old state back, so the change counts from here.
  #onBeforeToggle = (event) => {
    if (event.target === this.#content) this.#reporter.reserve();
  };

  #onToggle = (event) => {
    const content = this.#content;
    if (!content) return;
    if (event.target !== content) {
      if (event.target.matches?.(SUBMENU) && event.target.parentElement === this) this.#onSubmenuToggle(event.target, event.newState === 'open');
      return;
    }
    const open = event.newState === 'open';
    const trigger = this.#trigger;
    this.dataset.state = content.dataset.state = open ? 'open' : 'closed';
    trigger?.setAttribute('aria-expanded', String(open));

    if (open) {
      this.#stopFloating?.();
      if (trigger) {
        this.#stopFloating = float(trigger, content, {
          placement: this.getAttribute('placement') || 'bottom',
          distance: Number(this.getAttribute('offset') ?? 8),
        });
      }
      let target;
      if (this.#isMenu) {
        const items = this.#menuItems();
        target = (this.#focusLast ? items.at(-1) : items[0]) ?? content;
      } else {
        target = content.querySelector('[autofocus]') ?? content.querySelector(FOCUSABLE) ?? content;
      }
      this.#focusLast = false;
      target.focus({ preventScroll: true });
    } else {
      this.#stopFloating?.();
      this.#stopFloating = null;
      clearTimeout(this.#hoverTimer);
      // Escape or a programmatic close leaves focus nowhere: give it back to the trigger.
      const active = document.activeElement;
      if (!active || active === document.body || content.contains(active) || this.#submenus().some((sub) => sub.contains(active))) trigger?.focus({ preventScroll: true });
    }

    this.#reporter.report('bx-toggle', { open });
  };
}

if (!customElements.get('bx-popover')) customElements.define('bx-popover', BxPopoverElement);
