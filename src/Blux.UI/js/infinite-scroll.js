import { Reporter, observeChildren } from './shared.js';

/**
 * "Load more" sentinel. An IntersectionObserver watches a 1px strip; when it comes into view, the element reports a
 * bx-change (value "more") to .NET, once, and does not report again until the load is over. Only an element rendered
 * by a .NET component that listens (it renders data-bx-seq) does anything: a static page keeps a plain link.
 *
 * <bx-infinite-scroll data-bx-seq data-has-more data-loading data-loads="3" data-root="#list" data-root-margin="0px 0px 200px 0px">
 *   <div data-slot="sentinel"></div>
 *   <button data-slot="trigger">Load more</button>   (or a link: a click is taken over when bound)
 *   <div role="status" data-slot="status" tabindex="-1"></div>
 * </bx-infinite-scroll>
 *
 * "A load is over" is one of: data-loading goes away (the Loading parameter or the running OnLoadMore callback ended), or
 * data-loads changes (every finished callback bumps it, so a handler that finishes at once and never sets Loading
 * is covered). Each of them observes the sentinel again, so a sentinel that is still in view after the new items
 * loads the next ones: the observer only reports changes, and an observation made again reports the current state.
 */
export class BxInfiniteScrollElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'data-has-more', 'data-loading', 'data-loads', 'data-root', 'data-root-margin'];

  #reporter = new Reporter(this);
  #observer = null;
  #watching = null;
  #scroller = null;
  #key = '';
  #visible = false;
  #fired = false;
  #restoreFocus = false;
  #children = null;

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.#children = observeChildren(this, () => this.#wire());
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    this.#children?.disconnect();
    this.#stop();
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (!this.isConnected) return;
    if (name === 'data-loads' || (name === 'data-loading' && newValue === null && oldValue !== null)) {
      // A load is over: the next visibility may report again.
      this.#fired = false;
      this.#look();
    } else if (name === 'data-has-more' && newValue !== null) {
      this.#look();
    } else if (name === 'data-has-more' && newValue === null) {
      // Nothing left: the button is about to go away. A render changes the attributes of an element before its children, so
      // the focus is still on it now, and that is the only moment to know it was there (browsers differ on blur at removal).
      const active = document.activeElement;
      this.#restoreFocus = active instanceof Element && this.contains(active) && active.closest('[data-slot="trigger"]') !== null;
    } else if (name === 'data-bx-seq' && newValue !== null && oldValue === null) {
      // The component became interactive after a prerender.
      this.#look();
    }
    this.#wire();
  }

  /** Re-applies the state after an enhanced navigation merged server HTML over the element. */
  refresh() {
    this.#fired = false;
    this.#stop();
    this.#wire();
  }

  get #sentinel() {
    return this.querySelector(':scope > [data-slot="sentinel"]');
  }

  #canLoad() {
    return this.#reporter.bound && this.hasAttribute('data-has-more') && !this.hasAttribute('data-loading');
  }

  /** The element the sentinel scrolls in: the selector in data-root, else the closest scrollable ancestor, else the viewport (null). */
  #root() {
    const selector = this.getAttribute('data-root');
    if (selector) {
      try {
        const named = document.querySelector(selector);
        if (named) return named;
      } catch {
        // Not a valid selector: fall back to the ancestors.
      }
    }
    for (let node = this.parentElement; node && node !== document.documentElement; node = node.parentElement) {
      const { overflowY } = getComputedStyle(node);
      if (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') return node;
    }
    return null;
  }

  #wire() {
    const sentinel = this.#sentinel;
    // The last "Load more" went away while it had the focus: the focus would fall to the top of the page. The announcement gets it.
    if (this.#restoreFocus && !this.querySelector('[data-slot="trigger"]')) {
      this.#restoreFocus = false;
      this.querySelector('[data-slot="status"]')?.focus({ preventScroll: true });
    }

    const wanted = sentinel && this.#reporter.bound && this.hasAttribute('data-has-more') && typeof IntersectionObserver === 'function';
    if (!wanted) {
      this.#stop();
      return;
    }
    const root = this.#root();
    const margin = this.getAttribute('data-root-margin') ?? '0px';
    const key = `${margin}|${root ? 'r' : 'v'}`;
    if (this.#watching === sentinel && this.#key === key && this.#scroller === root) return;
    this.#stop();
    let observer;
    try {
      observer = new IntersectionObserver(this.#onIntersect, { root, rootMargin: margin });
    } catch {
      // A margin the browser rejects: watch with none.
      observer = new IntersectionObserver(this.#onIntersect, { root });
    }
    this.#observer = observer;
    this.#watching = sentinel;
    this.#key = key;
    this.#scroller = root;
    observer.observe(sentinel);
  }

  #stop() {
    this.#observer?.disconnect();
    this.#observer = null;
    this.#watching = null;
    this.#scroller = null;
    this.#key = '';
    this.#visible = false;
  }

  /** Observes the sentinel again: the observer then reports where it is now, even when nothing changed. */
  #look() {
    if (!this.#observer || !this.#watching) return;
    this.#observer.unobserve(this.#watching);
    this.#observer.observe(this.#watching);
  }

  #onIntersect = (entries) => {
    const latest = entries.at(-1);
    if (!latest) return;
    this.#visible = latest.isIntersecting;
    this.#maybeLoad();
  };

  #maybeLoad() {
    if (!this.#visible || this.#fired || !this.#canLoad()) return;
    this.#fired = true;
    this.#reporter.report('bx-change', { value: 'more' });
  }

  #onClick = (event) => {
    const trigger = event.target instanceof Element ? event.target.closest('[data-slot="trigger"]') : null;
    if (!trigger || !this.contains(trigger) || !this.#reporter.bound) return;
    // A link is the fallback of a page without .NET: with it, the click loads in place instead of leaving the page.
    event.preventDefault();
    if (!this.#canLoad()) return;
    this.#fired = true;
    this.#reporter.report('bx-change', { value: 'more' });
  };
}

if (!customElements.get('bx-infinite-scroll')) customElements.define('bx-infinite-scroll', BxInfiniteScrollElement);
