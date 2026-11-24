import { observeChildren, shortcutState } from './shared.js';

/**
 * The "Content" family: documentation components.
 *
 * <lx-content-toc>
 *   <nav>
 *     <button data-slot="trigger" aria-expanded="false" aria-controls="c">On this page</button>
 *     <div id="c" data-slot="content"><ul><li><a href="#intro" data-level="2">Intro</a></li>…</ul></div>
 *   </nav>
 * </lx-content-toc>
 *
 * Scroll-spy: an IntersectionObserver watches the headings the links point at and wakes the element when one crosses
 * the top quarter of the viewport; the heading being read is the last one above that line (or the first one in view,
 * before any is above it). Its link gets data-active and aria-current="location". Below the breakpoint the trigger
 * opens and closes the content (data-state, aria-expanded); the server renders neither attribute, so a render never
 * undoes them. Purely visual: never reported to .NET.
 */
export class LxContentTocElement extends HTMLElement {
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

if (!customElements.get('lx-content-toc')) customElements.define('lx-content-toc', LxContentTocElement);

// LxContentSearchButton opens the LxContentSearch it names (the unnamed one by default) by clicking the hidden trigger
// of its modal, so the modal returns focus to the button when it closes.
document.addEventListener('click', (event) => {
  const button = event.target instanceof Element ? event.target.closest('[data-lx-content-search-open]') : null;
  if (!button || button.disabled) return;
  openSearch(button.getAttribute('data-lx-content-search-open') ?? '');
});

function openSearch(name) {
  const dialog = [...document.querySelectorAll('dialog[data-lx-content-search]')].find((d) => d.getAttribute('data-lx-content-search') === name);
  dialog?.closest('lx-modal')?.querySelector(':scope > [data-slot="trigger"] button')?.click();
}

// The shortcut of a LxContentSearch ("mod+k"). It is registered after js/hotkey.js, so a page that gives the same
// combination to its own data-lx-hotkey element has already handled the key (defaultPrevented) and wins.
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
  for (const element of document.querySelectorAll('[data-lx-content-hotkey]')) {
    const combo = parse(element.getAttribute('data-lx-content-hotkey'));
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
    const state = shortcutState(element);
    if (element.matches(':disabled') || element.closest('[inert]') || state === 'skip') continue;
    event.preventDefault();
    if (state === 'run') element.click();
    return;
  }
});

// A LxContentSearch with an IndexUrl does not carry its pages: the first time its dialog opens, the JSON file is fetched and the options are built
// from the markup of the hidden element that the component rendered (so they have the classes of the palette). A page keeps the result: opening the
// search again, or a second search that names the same file, does not fetch again. A failed fetch says so in a live region and offers a retry.
const indexRequests = new Map();
let groupCounter = 0;

function indexAddress(dialog) {
  const url = new URL(dialog.getAttribute('data-lx-content-search-index'), document.baseURI);
  // Same origin only: the file is the page's own data, and the options it makes are links the visitor will follow.
  if (url.origin !== location.origin) throw new Error('The search index must be on the same origin as the page.');
  return url.href;
}

function fetchIndex(address) {
  let request = indexRequests.get(address);
  if (!request) {
    request = fetch(address, { headers: { Accept: 'application/json' }, credentials: 'same-origin' })
      .then((response) => {
        if (!response.ok) throw new Error(`The search index answered ${response.status}.`);
        return response.json();
      })
      .then((pages) => {
        if (!Array.isArray(pages)) throw new Error('The search index is not an array of pages.');
        return pages;
      });
    // A failure is not kept: the retry fetches again.
    request.catch(() => indexRequests.delete(address));
    indexRequests.set(address, request);
  }
  return request;
}

const text = (value) => (typeof value === 'string' ? value : '');

// A link of the index is relative to the page or http(s): a script or data address is never made into an option.
function isSafeHref(href) {
  const cleaned = href.replace(/[\u0000- ]/g, '');
  const scheme = /^([A-Za-z][A-Za-z0-9+.-]*):/.exec(cleaned);
  return !scheme || /^https?$/i.test(scheme[1]);
}

function buildIndex(dialog, pages) {
  const listbox = dialog.querySelector('lx-command-palette [role="listbox"]');
  const template = dialog.querySelector('[data-lx-content-search-template]');
  if (!listbox || !template) return;
  const pageOption = template.querySelector('[data-template="page"]');
  const headingOption = template.querySelector('[data-template="heading"]');
  const groupTemplate = template.querySelector('[role="group"]');

  // Pages without a group come first, then the groups in the order of their first page, as LxContentSearch does for Pages.
  const ungrouped = [];
  const groups = new Map();
  for (const page of pages) {
    const group = text(page?.group);
    if (!group) ungrouped.push(page);
    else if (groups.has(group)) groups.get(group).push(page);
    else groups.set(group, [page]);
  }

  const option = (source, label, description, href, search) => {
    const element = source.cloneNode(true);
    element.removeAttribute('data-template');
    element.setAttribute('href', href);
    element.setAttribute('data-value', href);
    element.setAttribute('data-search', search);
    element.querySelector('[data-slot="label"]').textContent = label;
    const descriptionElement = element.querySelector('[data-slot="description"]');
    if (description) descriptionElement.textContent = description;
    else descriptionElement.remove();
    return element;
  };

  const addPages = (container, list, groupLabel) => {
    for (const page of list) {
      const title = text(page?.title);
      const href = text(page?.href);
      if (!title || !href || !isSafeHref(href)) continue;
      const description = text(page.description);
      container.append(option(pageOption, title, description, href, [title, description, text(page.keywords), groupLabel].filter(Boolean).join(' ')));
      for (const heading of Array.isArray(page.headings) ? page.headings : []) {
        const label = text(heading?.text);
        const id = text(heading?.id);
        if (!label || !id) continue;
        // A heading is found by its own words and the title of its page: the text of the page is not repeated for each of them.
        container.append(option(headingOption, label, title, `${href}#${id}`, [label, title, groupLabel].filter(Boolean).join(' ')));
      }
    }
  };

  const fragment = document.createDocumentFragment();
  addPages(fragment, ungrouped, '');
  for (const [label, list] of groups) {
    const group = groupTemplate.cloneNode(true);
    const labelElement = group.firstElementChild;
    labelElement.textContent = label;
    labelElement.id = `lx-content-search-group-${++groupCounter}`;
    group.setAttribute('aria-labelledby', labelElement.id);
    addPages(group, list, label);
    fragment.append(group);
  }
  listbox.append(fragment);
}

async function loadIndex(dialog) {
  const state = dialog.dataset.bxIndexState;
  if (state === 'loading' || state === 'loaded') return;
  dialog.dataset.bxIndexState = 'loading';
  const box = dialog.querySelector('[data-lx-content-search-state]');
  const message = box?.querySelector('[data-slot="message"]');
  const retry = box?.querySelector('[data-slot="retry"]');
  const texts = box?.querySelector('[data-loading-text]');
  const show = (value, alert) => {
    if (!box || !message) return;
    box.hidden = !value;
    message.setAttribute('role', alert ? 'alert' : 'status');
    message.textContent = value;
  };
  if (retry) retry.hidden = true;
  show(texts?.getAttribute('data-loading-text') ?? '', false);
  try {
    buildIndex(dialog, await fetchIndex(indexAddress(dialog)));
    dialog.dataset.bxIndexState = 'loaded';
    show('', false);
  } catch {
    dialog.dataset.bxIndexState = 'error';
    show(texts?.getAttribute('data-error-text') ?? '', true);
    if (retry) {
      retry.hidden = false;
      retry.onclick = () => loadIndex(dialog);
    }
  }
}

// The dialog is a native <dialog>: it opens when it gets the open attribute, whoever opens it (the button, the shortcut, a script).
new MutationObserver((records) => {
  for (const record of records) {
    const dialog = record.target;
    if (dialog instanceof HTMLDialogElement && dialog.open && dialog.hasAttribute('data-lx-content-search-index')) loadIndex(dialog);
  }
}).observe(document, { subtree: true, attributes: true, attributeFilter: ['open'] });
