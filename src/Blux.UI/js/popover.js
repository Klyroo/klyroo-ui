import { FOCUSABLE, Reporter, ensureId, float, observeChildren, rovingKey } from './shared.js';

const MENU_ITEMS = '[role="menuitem"]:not([aria-disabled="true"])';

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
 */
export class BxPopoverElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'open', 'placement', 'offset'];

  #content = null;
  #stopFloating = null;
  #observer = null;
  #warned = false;
  #reporter = new Reporter(this);

  connectedCallback() {
    // "toggle" does not bubble: listen in the capture phase so a replaced panel needs no re-binding.
    this.addEventListener('toggle', this.#onToggle, true);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('click', this.#onClick);
    this.#observer = observeChildren(this, () => this.#sync());
  }

  disconnectedCallback() {
    this.removeEventListener('toggle', this.#onToggle, true);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('click', this.#onClick);
    this.#observer?.disconnect();
    this.#stopFloating?.();
    this.#stopFloating = null;
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

  #menuItems() {
    return [...this.#content.querySelectorAll(MENU_ITEMS)].filter((item) => item.closest('bx-popover') === this);
  }

  #onKeyDown = (event) => {
    const content = this.#content;
    if (!content || !this.#isMenu) return;
    const trigger = this.#trigger;
    // Menu button: arrow keys on the closed trigger open the menu on the first or last item.
    if (event.target === trigger && (event.key === 'ArrowDown' || event.key === 'ArrowUp')) {
      event.preventDefault();
      this.#focusLast = event.key === 'ArrowUp';
      this.#show();
      return;
    }
    if (content.contains(event.target)) {
      if (event.key === 'Tab') {
        this.#hide();
        return;
      }
      rovingKey(event, this.#menuItems(), document.activeElement);
    }
  };

  #focusLast = false;

  #onClick = (event) => {
    if (!this.#isMenu) return;
    const item = event.target.closest?.('[role="menuitem"]');
    if (!item || !this.#content.contains(item) || item.getAttribute('aria-disabled') === 'true') return;
    // The click keeps propagating, so a .NET handler on the item still runs.
    this.#hide();
  };

  #onToggle = (event) => {
    const content = this.#content;
    if (!content || event.target !== content) return;
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
      // Escape or a programmatic close leaves focus nowhere: give it back to the trigger.
      const active = document.activeElement;
      if (!active || active === document.body || content.contains(active)) trigger?.focus({ preventScroll: true });
    }

    this.#reporter.report('bx-toggle', { open });
  };
}

if (!customElements.get('bx-popover')) customElements.define('bx-popover', BxPopoverElement);
