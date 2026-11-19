import { behindModal, observeChildren } from './shared.js';

/**
 * The "Content" family: documentation components.
 *
 * <bx-content-toc>
 *   <nav>
 *     <button data-slot="trigger" aria-expanded="false" aria-controls="c">On this page</button>
 *     <div id="c" data-slot="content"><ul><li><a href="#intro" data-level="2">Intro</a></li>…</ul></div>
 *   </nav>
 * </bx-content-toc>
 *
 * Scroll-spy: an IntersectionObserver watches the headings the links point at and wakes the element when one crosses
 * the top quarter of the viewport; the heading being read is the last one above that line (or the first one in view,
 * before any is above it). Its link gets data-active and aria-current="location". Below the breakpoint the trigger
 * opens and closes the content (data-state, aria-expanded); the server renders neither attribute, so a render never
 * undoes them. Purely visual: never reported to .NET.
 */
export class BxContentTocElement extends HTMLElement {
  #observer = null;
  #children = null;
  #targets = [];
  #wiredCount = -1;
  #activeId = null;

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.#children = observeChildren(this, () => this.#wire());
    // Headings rendered after the table of contents (a page still being built) are found on load.
    window.addEventListener('load', this.#wire);
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    window.removeEventListener('load', this.#wire);
    this.#children?.disconnect();
    this.#observer?.disconnect();
    this.#observer = null;
    this.#targets = [];
    this.#wiredCount = -1;
  }

  /** After an enhanced navigation merged server HTML over the links: marks and the open state are gone. */
  refresh() {
    this.#wiredCount = -1;
    this.#activeId = null;
    this.#wire();
  }

  get #links() {
    return [...this.querySelectorAll('a[href*="#"]')].filter((a) => a.hash.length > 1);
  }

  get #trigger() {
    return this.querySelector('[data-slot="trigger"]');
  }

  get #content() {
    return this.querySelector('[data-slot="content"]');
  }

  #idOf(link) {
    try {
      return decodeURIComponent(link.hash.slice(1));
    } catch {
      return link.hash.slice(1);
    }
  }

  #wire = () => {
    const ids = this.#links.map((link) => this.#idOf(link));
    const targets = ids.map((id) => document.getElementById(id)).filter((el) => el !== null);
    // The elements themselves, not their ids: a render can replace a heading by another element with the same id.
    if (ids.length === this.#wiredCount && targets.length === this.#targets.length && targets.every((el) => this.#targets.includes(el))) return;
    this.#wiredCount = ids.length;
    this.#observer?.disconnect();
    // Document order, whatever the order of the links.
    this.#targets = targets.sort((a, b) => (a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1));
    if (this.#targets.length === 0) return;
    // The band is the top quarter of the viewport: a heading crossing it, in either direction, wakes #update.
    this.#observer = new IntersectionObserver(() => this.#update(), { rootMargin: '0px 0px -75% 0px', threshold: [0, 1] });
    for (const target of this.#targets) this.#observer.observe(target);
    this.#update();
  };

  #update() {
    const line = window.innerHeight * 0.25;
    let current = null;
    for (const target of this.#targets) {
      if (target.getBoundingClientRect().top <= line) current = target;
      else break;
    }
    // Before the first heading has reached the line, the first one in view is the one being read.
    current ??= this.#targets.find((target) => target.getBoundingClientRect().top < window.innerHeight) ?? null;
    this.#mark(current?.id ?? null);
  }

  #mark(id) {
    if (id === this.#activeId && this.#links.some((link) => link.hasAttribute('data-active'))) return;
    this.#activeId = id;
    for (const link of this.#links) {
      const on = id !== null && this.#idOf(link) === id;
      if (on) {
        link.setAttribute('data-active', '');
        link.setAttribute('aria-current', 'location');
      } else {
        link.removeAttribute('data-active');
        link.removeAttribute('aria-current');
      }
    }
  }

  #setOpen(open) {
    const trigger = this.#trigger;
    const content = this.#content;
    if (!trigger || !content) return;
    trigger.setAttribute('aria-expanded', String(open));
    content.dataset.state = open ? 'open' : 'closed';
  }

  #onClick = (event) => {
    const target = event.target instanceof Element ? event.target : null;
    if (!target) return;
    if (target.closest('[data-slot="trigger"]')) {
      this.#setOpen(this.#trigger.getAttribute('aria-expanded') !== 'true');
      return;
    }
    const link = target.closest('a[href*="#"]');
    if (!link || !this.contains(link) || link.hash.length < 2) return;
    // The click names the heading being read at once; the observer takes over as the page scrolls.
    this.#mark(this.#idOf(link));
    this.#setOpen(false);
  };
}

if (!customElements.get('bx-content-toc')) customElements.define('bx-content-toc', BxContentTocElement);

// BxContentSearchButton opens the BxContentSearch it names (the unnamed one by default) by clicking the hidden trigger
// of its modal, so the modal returns focus to the button when it closes.
document.addEventListener('click', (event) => {
  const button = event.target instanceof Element ? event.target.closest('[data-bx-content-search-open]') : null;
  if (!button || button.disabled) return;
  openSearch(button.getAttribute('data-bx-content-search-open') ?? '');
});

function openSearch(name) {
  const dialog = [...document.querySelectorAll('dialog[data-bx-content-search]')].find((d) => d.getAttribute('data-bx-content-search') === name);
  dialog?.closest('bx-modal')?.querySelector(':scope > [data-slot="trigger"] button')?.click();
}

// The shortcut of a BxContentSearch ("mod+k"). It is registered after js/hotkey.js, so a page that gives the same
// combination to its own data-bx-hotkey element has already handled the key (defaultPrevented) and wins.
const APPLE = /Mac|iPhone|iPad|iPod/.test(navigator.platform || navigator.userAgent);

function parse(combo) {
  const parts = combo.toLowerCase().split('+').map((p) => p.trim());
  const key = parts.pop();
  const wants = new Set(parts);
  const mod = wants.has('mod');
  return {
    key,
    ctrl: wants.has('ctrl') || (mod && !APPLE),
    meta: wants.has('meta') || (mod && APPLE),
    alt: wants.has('alt'),
    shift: wants.has('shift'),
  };
}

document.addEventListener('keydown', (event) => {
  if (event.defaultPrevented || event.repeat || event.isComposing || typeof event.key !== 'string') return;
  for (const element of document.querySelectorAll('[data-bx-content-hotkey]')) {
    const combo = parse(element.getAttribute('data-bx-content-hotkey'));
    const isTyping = event.target instanceof Element && event.target.closest('input, textarea, select, [contenteditable]:not([contenteditable="false"])') !== null;
    if (
      event.key.toLowerCase() !== combo.key ||
      event.ctrlKey !== combo.ctrl ||
      event.metaKey !== combo.meta ||
      event.altKey !== combo.alt ||
      event.shiftKey !== combo.shift
    ) {
      continue;
    }
    // A shortcut without ctrl or meta types a character: leave it to the text field.
    if (isTyping && !combo.ctrl && !combo.meta) continue;
    if (element.matches(':disabled') || element.closest('[inert]') || behindModal(element)) continue;
    event.preventDefault();
    element.click();
    return;
  }
});
