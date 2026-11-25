import { Reporter, observeChildren } from './shared.js';

/**
 * "Load more" sentinel. An IntersectionObserver watches a 1px strip; when it comes into view, the element reports a
 * kly-change (value "more") to .NET, once, and does not report again until the load is over. Only an element rendered
 * by a .NET component that listens (it renders data-kly-seq) does anything: a static page keeps a plain link.
 *
 * <kly-infinite-scroll data-kly-seq data-has-more data-loading data-loads="3" data-root="#list" data-root-margin="0px 0px 200px 0px">
 *   <div data-slot="sentinel"></div>
 *   <button data-slot="trigger">Load more</button>   (or a link: a click is taken over when bound)
 *   <div role="status" data-slot="status" tabindex="-1"></div>
 * </kly-infinite-scroll>
 *
 * "A load is over" is one of: data-loading goes away (the Loading parameter or the running OnLoadMore callback ended), or
 * data-loads changes (every finished callback bumps it, so a handler that finishes at once and never sets Loading
 * is covered). The next frame the scrolled content is measured: when it grew, the sentinel is observed again, so a sentinel
 * still in view after the new items loads the next ones (the observer only reports changes, and an observation made again
 * reports the current state). When it did not grow (the load added nothing, or failed) nothing is asked again until the
 * sentinel has left the view and come back, or the button is used: a sentinel that stays in view would loop forever.
 */
export class KlyInfiniteScrollElement extends HTMLElement {
  static observedAttributes = ['data-kly-seq', 'data-has-more', 'data-loading', 'data-loads', 'data-root', 'data-root-margin'];

  #reporter = new Reporter(this);
  #observer = null;
  #watching = null;
  #scroller = null;
  #key = '';
  #visible = false;
  #fired = false;
  #restoreFocus = false;
  #before = null;
  #settling = false;
  #stalled = false;
  #warned = '';
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
      this.#loadOver();
    } else if (name === 'data-has-more' && newValue !== null) {
      this.#look();
    } else if (name === 'data-has-more' && newValue === null) {
      // Nothing left: the button is about to go away. A render changes the attributes of an element before its children, so
      // the focus is still on it now, and that is the only moment to know it was there (browsers differ on blur at removal).
      const active = document.activeElement;
      this.#restoreFocus = active instanceof Element && this.contains(active) && active.closest('[data-slot="trigger"]') !== null;
    } else if (name === 'data-kly-seq' && newValue !== null && oldValue === null) {
      // The component became interactive after a prerender.
      this.#look();
    }
    this.#wire();
  }

  /**
   * What a load changes when it adds items: the height of what scrolls (items after the sentinel, or before it in a list that
   * loads upward) and where the sentinel is inside it (items before it). A fixed height box that the new items still fit in
   * keeps its scroll height, which is why the place of the sentinel is part of it.
   */
  #measure() {
    const scroller = this.#scroller ?? document.documentElement;
    const rect = this.#watching?.getBoundingClientRect();
    const origin = this.#scroller ? this.#scroller.getBoundingClientRect().top - this.#scroller.scrollTop : -window.scrollY;
    return `${scroller.scrollHeight}|${rect ? Math.round(rect.top - origin) : 0}`;
  }

  #loadOver() {
    if (this.#settling) return;
    this.#settling = true;
    const before = this.#before;
    this.#before = null;
    // After the frame: the render that ends the load has put its items in the document and laid them out.
    requestAnimationFrame(() => {
      this.#settling = false;
      if (!this.isConnected) return;
      if (before !== null && this.#measure() === before) {
        this.#stalled = true;
        return;
      }
      this.#fired = false;
      this.#look();
    });
  }

  #fire() {
    this.#fired = true;
    this.#stalled = false;
    this.#before = this.#measure();
    this.#reporter.report('kly-change', { value: 'more' });
  }

  /** Re-applies the state after an enhanced navigation merged server HTML over the element. */
  refresh() {
    this.#fired = false;
    this.#stalled = false;
    this.#before = null;
    this.#stop();
    this.#wire();
  }

  get #sentinel() {
    return this.querySelector(':scope > [data-slot="sentinel"]');
  }

  #canLoad() {
    return this.#reporter.bound && this.hasAttribute('data-has-more') && !this.hasAttribute('data-loading');
  }

  /**
   * The element the sentinel scrolls in: the selector in data-root, else the closest ancestor that really scrolls, else the
   * viewport (null). An ancestor with overflow-y auto is not enough: setting overflow-x (an overflow-x-hidden wrapper, a table
   * wrapper) makes the computed overflow-y auto too, though nothing scrolls there, and a sentinel watched against it is always in view.
   */
  #root() {
    const selector = this.getAttribute('data-root');
    if (selector) {
      let named = null;
      try {
        named = document.querySelector(selector);
      } catch {
        // Not a valid selector: reported below.
      }
      if (named) return named;
      if (this.#warned !== selector) {
        this.#warned = selector;
        console.warn(`kly-infinite-scroll: Root "${selector}" matches no element, watching the viewport.`);
      }
      return null;
    }
    for (let node = this.parentElement; node && node !== document.documentElement && node !== document.body; node = node.parentElement) {
      const { overflowY } = getComputedStyle(node);
      if ((overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') && node.scrollHeight > node.clientHeight) return node;
    }
    return null;
  }

  #wire() {
    const sentinel = this.#sentinel;
    // The last "Load more" went away while it had the focus: the focus would fall to the top of the page. A plain element that is not the live region gets it (the status announces by itself).
    if (this.#restoreFocus && !this.querySelector('[data-slot="trigger"]')) {
      this.#restoreFocus = false;
      this.querySelector('[data-slot="marker"]')?.focus({ preventScroll: true });
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
    // Gone from the view after a load that added nothing: coming back may ask again.
    if (!this.#visible && this.#stalled) {
      this.#stalled = false;
      this.#fired = false;
    }
    this.#maybeLoad();
  };

  #maybeLoad() {
    if (!this.#visible || this.#fired || !this.#canLoad()) return;
    this.#fire();
  }

  #onClick = (event) => {
    const trigger = event.target instanceof Element ? event.target.closest('[data-slot="trigger"]') : null;
    if (!trigger || !this.contains(trigger) || !this.#reporter.bound) return;
    // A link is the fallback of a page without .NET: with it, the click loads in place instead of leaving the page.
    event.preventDefault();
    if (!this.#canLoad()) return;
    this.#fire();
  };
}

if (!customElements.get('kly-infinite-scroll')) customElements.define('kly-infinite-scroll', KlyInfiniteScrollElement);
