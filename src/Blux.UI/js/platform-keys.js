import { observeChildren } from './shared.js';

/**
 * <bx-mod-keys><kbd data-bx-mod data-label="Ctrl"></kbd><kbd>K</kbd></bx-mod-keys>: the hint of the "mod" key of a
 * shortcut. Blazor renders the kbd empty with a label in an attribute, shown with before:content-[attr(data-label)]; this
 * element sets the label of the platform ("⌘" on Apple devices, which is what mod+k means there, "Ctrl" elsewhere) on the
 * kbd it hosts. Only an attribute is touched, never a text node, so a render of Blazor cannot fight it. Each element
 * looks only at its own children: nothing watches the document. An enhanced navigation that merges the server markup
 * calls refresh().
 */
const APPLE = /Mac|iPhone|iPad|iPod/.test(navigator.platform || navigator.userAgent);
const LABEL = APPLE ? '⌘' : 'Ctrl';

export class BxModKeysElement extends HTMLElement {
  #children = null;

  connectedCallback() {
    this.#children = observeChildren(this, () => this.refresh());
  }

  disconnectedCallback() {
    this.#children?.disconnect();
    this.#children = null;
  }

  refresh() {
    for (const kbd of this.querySelectorAll('kbd[data-bx-mod]')) {
      if (kbd.getAttribute('data-label') !== LABEL) kbd.setAttribute('data-label', LABEL);
    }
  }
}

if (!customElements.get('bx-mod-keys')) customElements.define('bx-mod-keys', BxModKeysElement);
