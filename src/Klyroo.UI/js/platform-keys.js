import { observeChildren } from './shared.js';

/**
 * <kly-mod-keys><kbd data-kly-mod data-label="Ctrl"></kbd><kbd>K</kbd></kly-mod-keys>: the hint of the "mod" key of a
 * shortcut. Blazor renders the kbd empty with a label in an attribute, shown with before:content-[attr(data-label)]; this
 * element sets the label of the platform ("⌘" on Apple devices, which is what mod+k means there, "Ctrl" elsewhere) on the
 * kbd it hosts. Elsewhere the label Blazor rendered (the localized "Ctrl") stays. Only an attribute is touched, never a text node, so a render of Blazor cannot fight it. Each element
 * looks only at its own children: nothing watches the document. An enhanced navigation that merges the server markup
 * calls refresh().
 */
const APPLE = /Mac|iPhone|iPad|iPod/.test(navigator.platform || navigator.userAgent);
const APPLE_LABEL = '⌘';

export class KlyModKeysElement extends HTMLElement {
  #children = null;

  connectedCallback() {
    this.#children = observeChildren(this, () => this.refresh());
  }

  disconnectedCallback() {
    this.#children?.disconnect();
    this.#children = null;
  }

  refresh() {
    if (!APPLE) return;
    for (const kbd of this.querySelectorAll('kbd[data-kly-mod]')) {
      if (kbd.getAttribute('data-label') !== APPLE_LABEL) kbd.setAttribute('data-label', APPLE_LABEL);
    }
  }
}

if (!customElements.get('kly-mod-keys')) customElements.define('kly-mod-keys', KlyModKeysElement);
