import { Reporter } from './shared.js';

/**
 * <kly-collapsible open>
 *   <button data-kly-collapsible-trigger aria-expanded="false" aria-controls="…">…</button>
 *   <div data-kly-collapsible-content hidden="until-found">…</div>
 * </kly-collapsible>
 *
 * Disclosure pattern. hidden="until-found" keeps a closed region searchable with find-in-page, which opens it.
 * Changes are reported with "kly-toggle".
 *
 * A static page has no .NET to hold the open state: the state lives here, and an enhanced navigation (a GET form, a link) that merges the
 * server markup over this element, which resets aria-expanded, data-state and hidden to what was rendered, gets it back in refresh().
 * What the server renders (the open attribute) wins only when it changed since the last render: a section the user opened or closed by hand
 * stays as it is while the server still says what it said before.
 */
export class KlyCollapsibleElement extends HTMLElement {
  static observedAttributes = ['data-kly-seq', 'open'];

  #reporter = new Reporter(this);
  #open = false;
  #rendered = false;

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

  /** After an enhanced navigation or form post merged server HTML over this element. */
  refresh() {
    // A bound collapsible takes its state from the renders of its component, which no merge replaces.
    if (this.#reporter.bound) return;
    const rendered = this.hasAttribute('open');
    if (rendered !== this.#rendered) this.#apply();
    else this.#set(this.#open);
  }

  // Only direct children: a collapsible nested in the content has its own trigger and region.
  get #trigger() {
    return this.querySelector(':scope > [data-kly-collapsible-trigger]');
  }

  get #content() {
    return this.querySelector(':scope > [data-kly-collapsible-content]');
  }

  #apply() {
    if (this.#reporter.isStale()) return;
    this.#rendered = this.hasAttribute('open');
    this.#set(this.#rendered);
  }

  #isOpen() {
    return this.#trigger?.getAttribute('aria-expanded') === 'true';
  }

  #set(open) {
    this.#open = open;
    this.#trigger?.setAttribute('aria-expanded', String(open));
    this.dataset.state = open ? 'open' : 'closed';
    const content = this.#content;
    if (content) {
      if (open) content.removeAttribute('hidden');
      else content.setAttribute('hidden', 'until-found');
    }
  }

  #toggle(open) {
    this.#set(open);
    this.#reporter.report('kly-toggle', { open });
  }

  #onClick = (event) => {
    const trigger = event.target.closest?.('[data-kly-collapsible-trigger]');
    if (!trigger || trigger.disabled || trigger !== this.#trigger) return;
    this.#toggle(!this.#isOpen());
  };

  #onBeforeMatch = (event) => {
    if (event.target === this.#content && !this.#isOpen()) this.#toggle(true);
  };
}

if (!customElements.get('kly-collapsible')) customElements.define('kly-collapsible', KlyCollapsibleElement);
