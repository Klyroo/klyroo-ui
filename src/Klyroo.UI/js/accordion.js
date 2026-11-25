import { Reporter } from './shared.js';

/**
 * <kly-accordion type="single|multiple" collapsible>
 *   <div data-slot="item" data-value="a">
 *     <h3><button data-kly-accordion-trigger aria-expanded="false" aria-controls="…">…</button></h3>
 *     <div role="region" data-kly-accordion-content hidden="until-found">…</div>
 *   </div>
 * </kly-accordion>
 *
 * WAI-ARIA accordion. hidden="until-found" keeps closed panels searchable with find-in-page, which opens them.
 * Changes are reported with "kly-change" (value: open item values joined by commas).
 */
export class KlyAccordionElement extends HTMLElement {
  static observedAttributes = ['data-kly-seq', 'value'];

  #reporter = new Reporter(this);

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('beforematch', this.#onBeforeMatch, true);
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('beforematch', this.#onBeforeMatch, true);
  }

  attributeChangedCallback(name) {
    // data-kly-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-kly-seq') queueMicrotask(() => this.#apply());
    else this.#apply();
  }

  #apply() {
    if (this.#reporter.isStale()) return;
    const value = this.getAttribute('value');
    const open = new Set((value ?? '').split(',').filter(Boolean));
    for (const item of this.#items()) this.#set(item, open.has(item.dataset.value));
  }

  #items() {
    return [...this.querySelectorAll('[data-slot="item"]')].filter((item) => item.closest('kly-accordion') === this);
  }

  #set(item, open) {
    const trigger = item.querySelector('[data-kly-accordion-trigger]');
    const content = item.querySelector('[data-kly-accordion-content]');
    trigger?.setAttribute('aria-expanded', String(open));
    item.dataset.state = open ? 'open' : 'closed';
    if (content) {
      if (open) content.removeAttribute('hidden');
      else content.setAttribute('hidden', 'until-found');
    }
  }

  #isOpen(item) {
    return item.querySelector('[data-kly-accordion-trigger]')?.getAttribute('aria-expanded') === 'true';
  }

  #toggle(item, open) {
    const single = this.getAttribute('type') !== 'multiple';
    if (!open && single && !this.hasAttribute('collapsible')) return;
    if (open && single) for (const other of this.#items()) if (other !== item) this.#set(other, false);
    this.#set(item, open);
    const value = this.#items().filter((i) => this.#isOpen(i)).map((i) => i.dataset.value).join(',');
    this.#reporter.report('kly-change', { value });
  }

  #onClick = (event) => {
    // Only an item's own header button: buttons of components inside a panel (menus, selects) must not toggle it.
    const trigger = event.target.closest?.('[data-kly-accordion-trigger]');
    const item = trigger?.closest('[data-slot="item"]');
    if (!item || trigger.disabled || item.closest('kly-accordion') !== this) return;
    this.#toggle(item, !this.#isOpen(item));
  };

  #onBeforeMatch = (event) => {
    const item = event.target.closest?.('[data-slot="item"]');
    if (item && item.closest('kly-accordion') === this && !this.#isOpen(item)) this.#toggle(item, true);
  };
}

if (!customElements.get('kly-accordion')) customElements.define('kly-accordion', KlyAccordionElement);
