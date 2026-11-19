import { Reporter, observeChildren } from './shared.js';

const ITEM = '[role="treeitem"]';

/**
 * <bx-tree value="a">
 *   <ul role="tree">
 *     <li role="none">
 *       <div role="treeitem" data-slot="row" aria-expanded="true" aria-owns="g" aria-selected="false" data-value="a">…</div>
 *       <ul id="g" role="group">…<li role="none">…</li></ul>
 *     </li>
 *   </ul>
 * </bx-tree>
 *
 * WAI-ARIA tree view with a roving tabindex: Up/Down move through visible items, Right expands or enters a node,
 * Left collapses or goes to the parent, Home/End jump, Enter selects (and follows a link, and toggles a node),
 * Space selects, "*" expands the siblings, letters jump by first letter.
 * The item is its own row and mirrors the state (data-selected, data-expanded); its group is a sibling inside the
 * same list item (so the item's name never has to cover its children) and is tied to it with aria-owns.
 * Selection is reported with "bx-change".
 */
export class BxTreeElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'value'];

  #observer = null;
  #reporter = new Reporter(this);

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('focusin', this.#onFocusIn);
    this.#observer = observeChildren(this, () => this.#syncTabIndex());
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('focusin', this.#onFocusIn);
    this.#observer?.disconnect();
  }

  attributeChangedCallback(name) {
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-bx-seq') queueMicrotask(() => this.#apply());
    else this.#apply();
  }

  #apply() {
    if (this.#reporter.isStale()) return;
    const value = this.getAttribute('value');
    const target = value === null ? null : this.#items().find((item) => item.dataset.value === value) ?? null;
    this.#mark(target);
  }

  #items() {
    return [...this.querySelectorAll(ITEM)].filter((item) => item.closest('bx-tree') === this);
  }

  #visible() {
    return this.#items().filter((item) => item.getAttribute('aria-disabled') !== 'true' && !item.parentElement.closest('[role="group"][hidden]'));
  }

  #row(item) {
    return item;
  }

  #group(item) {
    return item.parentElement.querySelector(':scope > [role="group"]');
  }

  #parent(item) {
    const parent = item.parentElement.parentElement.closest('[role="group"]')?.parentElement.querySelector(':scope > ' + ITEM);
    return parent && parent.closest('bx-tree') === this ? parent : null;
  }

  #syncTabIndex() {
    const items = this.#items();
    if (items.length === 0) return;
    const visible = this.#visible();
    const current =
      visible.find((item) => item.getAttribute('tabindex') === '0') ??
      visible.find((item) => item.getAttribute('aria-selected') === 'true') ??
      visible[0];
    for (const item of items) item.tabIndex = item === current ? 0 : -1;
  }

  #mark(selected) {
    for (const item of this.#items()) {
      const on = item === selected;
      item.setAttribute('aria-selected', String(on));
      this.#row(item)?.setAttribute('data-selected', String(on));
    }
  }

  #select(item) {
    const changed = item.getAttribute('aria-selected') !== 'true';
    this.#mark(item);
    if (changed) this.#reporter.report('bx-change', { value: item.dataset.value });
  }

  #setExpanded(item, expanded) {
    const group = this.#group(item);
    if (!group) return;
    item.setAttribute('aria-expanded', String(expanded));
    group.hidden = !expanded;
    this.#row(item)?.setAttribute('data-expanded', String(expanded));
    // A collapsed subtree cannot hold focus.
    if (!expanded && group.contains(document.activeElement)) item.focus();
    this.#syncTabIndex();
  }

  #focus(item) {
    if (!item) return;
    item.focus();
  }

  #onFocusIn = (event) => {
    const item = event.target.closest?.(ITEM);
    if (!item || item.closest('bx-tree') !== this) return;
    for (const other of this.#items()) other.tabIndex = other === item ? 0 : -1;
  };

  #onClick = (event) => {
    const item = event.target.closest?.(ITEM);
    if (!item || item.closest('bx-tree') !== this || item.getAttribute('aria-disabled') === 'true') return;
    this.#select(item);
    if (this.#group(item)) this.#setExpanded(item, item.getAttribute('aria-expanded') !== 'true');
  };

  #onKeyDown = (event) => {
    if (event.altKey || event.ctrlKey || event.metaKey) return;
    const item = event.target.closest?.(ITEM);
    if (!item || item.closest('bx-tree') !== this) return;
    const visible = this.#visible();
    const index = visible.indexOf(item);
    const hasChildren = !!this.#group(item);
    const expanded = item.getAttribute('aria-expanded') === 'true';
    let handled = true;
    switch (event.key) {
      case 'ArrowDown':
        this.#focus(visible[index + 1]);
        break;
      case 'ArrowUp':
        this.#focus(visible[index - 1]);
        break;
      case 'ArrowRight':
        if (!hasChildren) handled = false;
        else if (!expanded) this.#setExpanded(item, true);
        else this.#focus(this.#visible().find((child) => this.#parent(child) === item));
        break;
      case 'ArrowLeft': {
        const parent = this.#parent(item);
        if (hasChildren && expanded) this.#setExpanded(item, false);
        else if (parent) this.#focus(parent);
        else handled = false;
        break;
      }
      case 'Home':
        this.#focus(visible[0]);
        break;
      case 'End':
        this.#focus(visible[visible.length - 1]);
        break;
      case 'Enter':
        if (item.getAttribute('aria-disabled') === 'true') break;
        this.#select(item);
        if (hasChildren) this.#setExpanded(item, !expanded);
        // The link is not focusable (tabindex -1), so the item forwards Enter to it.
        if (event.target === item) this.#row(item)?.querySelector('a')?.click();
        break;
      case ' ':
        if (item.getAttribute('aria-disabled') !== 'true') this.#select(item);
        break;
      case '*': {
        const parent = this.#parent(item);
        const siblings = this.#items().filter((other) => this.#parent(other) === parent);
        for (const sibling of siblings) if (this.#group(sibling)) this.#setExpanded(sibling, true);
        break;
      }
      default:
        if (event.key.length === 1 && /\S/.test(event.key)) {
          const letter = event.key.toLowerCase();
          const ordered = [...visible.slice(index + 1), ...visible.slice(0, index + 1)];
          const match = ordered.find((other) => this.#row(other)?.textContent.trim().toLowerCase().startsWith(letter));
          if (match) this.#focus(match);
          else handled = false;
        } else {
          handled = false;
        }
    }
    if (handled) event.preventDefault();
  };
}

if (!customElements.get('bx-tree')) customElements.define('bx-tree', BxTreeElement);
