import { appStorageKey } from './base-path.js';

const STORAGE_PREFIX = 'blux:banner:';

/**
 * <bx-banner data-bx-banner-id="launch">
 *   <div>… <button data-slot="close">…</button></div>
 * </bx-banner>
 *
 * The close button hides the element (the "hidden" attribute, which Blazor never renders) and remembers the id in
 * localStorage, so the banner stays hidden on later visits, on static pages too. Purely browser side: never reported
 * to .NET. Storage can be unavailable (private windows): then a dismissal lasts until the page is reloaded.
 */
export class BxBannerElement extends HTMLElement {
  static observedAttributes = ['data-bx-banner-id'];

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.refresh();
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
  }

  attributeChangedCallback() {
    this.refresh();
  }

  /** Re-applies a remembered dismissal, e.g. after enhanced navigation merged server HTML over the element. */
  refresh() {
    if (this.#dismissed()) this.hidden = true;
  }

  #key() {
    const id = this.getAttribute('data-bx-banner-id');
    return id ? appStorageKey(STORAGE_PREFIX + id) : null;
  }

  #dismissed() {
    const key = this.#key();
    if (!key) return false;
    try {
      return localStorage.getItem(key) === '1';
    } catch {
      return false;
    }
  }

  #onClick = (event) => {
    const close = event.target instanceof Element ? event.target.closest('[data-slot="close"]') : null;
    if (!close || !this.contains(close)) return;
    this.hidden = true;
    const key = this.#key();
    if (!key) return;
    try {
      localStorage.setItem(key, '1');
    } catch {
      // Storage unavailable: the banner stays hidden for this page view only.
    }
  };
}

if (!customElements.get('bx-banner')) customElements.define('bx-banner', BxBannerElement);
