import { observeChildren } from './shared.js';

/**
 * <kly-scroll-area><div data-kly-scroll-viewport>…</div></kly-scroll-area>
 *
 * A scroll container that can only be scrolled with a pointer is out of reach for keyboard users: while the viewport
 * overflows, it is made focusable (tabindex 0) so arrow keys and Page Up/Down scroll it.
 */
export class KlyScrollAreaElement extends HTMLElement {
  #resize = null;
  #observer = null;

  connectedCallback() {
    this.#resize = new ResizeObserver(() => this.#update());
    this.#observer = observeChildren(this, () => this.#watch());
  }

  disconnectedCallback() {
    this.#resize?.disconnect();
    this.#observer?.disconnect();
  }

  /** Re-applies the tab stop, e.g. after enhanced navigation merged server HTML over it. */
  refresh() {
    this.#update();
  }

  get #viewport() {
    return this.querySelector(':scope > [data-kly-scroll-viewport]');
  }

  #watch() {
    const viewport = this.#viewport;
    this.#resize.disconnect();
    if (!viewport) return;
    this.#resize.observe(viewport);
    for (const child of viewport.children) this.#resize.observe(child);
    this.#update();
  }

  #update() {
    const viewport = this.#viewport;
    if (!viewport) return;
    const overflows = viewport.scrollHeight > viewport.clientHeight + 1 || viewport.scrollWidth > viewport.clientWidth + 1;
    if (overflows) viewport.setAttribute('tabindex', '0');
    else viewport.removeAttribute('tabindex');
  }
}

if (!customElements.get('kly-scroll-area')) customElements.define('kly-scroll-area', KlyScrollAreaElement);
