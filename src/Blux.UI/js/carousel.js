import { Reporter, observeChildren } from './shared.js';

/**
 * Carousel (WAI-ARIA APG) over a scroll-snap viewport.
 *
 * <bx-carousel index="0" loop autoplay="5000" role="region" aria-roledescription="carousel">
 *   <div data-slot="controls"> <button data-slot="toggle"> <button data-slot="prev"> <button data-slot="dot" data-index="0"> … <button data-slot="next"> </div>
 *   <div data-slot="viewport" tabindex="0"> <div role="group" aria-roledescription="slide" data-index="0">…</div> … </div>
 * </bx-carousel>
 *
 * The browser's scrolling does the swiping; this script keeps the current slide in step (buttons, keys, swipes),
 * makes the other slides inert so their content is out of the tab order and the accessibility tree, and runs autoplay.
 * Autoplay is paused while the pointer or focus is inside, while the tab is hidden and by the rotation button, and is off
 * from the start for visitors who prefer reduced motion (WCAG 2.2.2). While it is not rotating, the slides are a polite live region.
 * Changes of slide are reported with "bx-change" (the index).
 */
export class BxCarouselElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'index', 'autoplay', 'loop'];

  #index = 0;
  #userPaused = false;
  #hovered = false;
  #focused = false;
  #timer = null;
  #scrollTimer = null;
  #movingTo = null;
  #observer = null;
  #resize = null;
  #wired = false;
  #scheduled = false;
  #reporter = new Reporter(this);
  #reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('pointerenter', this.#onPointerEnter);
    this.addEventListener('pointerleave', this.#onPointerLeave);
    this.addEventListener('focusin', this.#onFocusIn);
    this.addEventListener('focusout', this.#onFocusOut);
    document.addEventListener('visibilitychange', this.#restartTimer);
    this.#observer = observeChildren(this, () => this.#wire());
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('pointerenter', this.#onPointerEnter);
    this.removeEventListener('pointerleave', this.#onPointerLeave);
    this.removeEventListener('focusin', this.#onFocusIn);
    this.removeEventListener('focusout', this.#onFocusOut);
    document.removeEventListener('visibilitychange', this.#restartTimer);
    this.#viewport?.removeEventListener('scroll', this.#onScroll);
    this.#observer?.disconnect();
    this.#resize?.disconnect();
    clearInterval(this.#timer);
    clearTimeout(this.#scrollTimer);
    this.#wired = false;
  }

  attributeChangedCallback() {
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (this.#scheduled) return;
    this.#scheduled = true;
    queueMicrotask(() => {
      this.#scheduled = false;
      if (!this.#wired) return;
      if (!this.#reporter.isStale()) this.#goTo(Number(this.getAttribute('index')) || 0, { report: false, smooth: true });
      this.#restartTimer();
    });
  }

  refresh() {
    this.#wired = false;
    this.#wire();
  }

  get #viewport() {
    return this.querySelector(':scope > [data-slot="viewport"]');
  }

  #slides() {
    return [...(this.#viewport?.querySelectorAll(':scope > [data-index]') ?? [])];
  }

  #button(slot) {
    return this.querySelector(`:scope > [data-slot="controls"] [data-slot="${slot}"]`);
  }

  get #loop() {
    return this.hasAttribute('loop');
  }

  get #interval() {
    return Number(this.getAttribute('autoplay')) || 0;
  }

  #wire() {
    const viewport = this.#viewport;
    if (!viewport || this.#wired || this.#slides().length === 0) return;
    this.#wired = true;
    viewport.addEventListener('scroll', this.#onScroll, { passive: true });
    this.#resize = new ResizeObserver(() => this.#align());
    this.#resize.observe(viewport);
    this.#userPaused = this.#interval > 0 && this.#reducedMotion.matches;
    this.#index = this.#clamp(Number(this.getAttribute('index')) || 0);
    this.#align();
    this.#update();
    this.#restartTimer();
  }

  #clamp(index) {
    return Math.min(Math.max(index, 0), Math.max(this.#slides().length - 1, 0));
  }

  #align() {
    const viewport = this.#viewport;
    if (viewport) viewport.scrollTo({ left: this.#index * viewport.clientWidth * (getComputedStyle(viewport).direction === 'rtl' ? -1 : 1), behavior: 'instant' });
  }

  #goTo(target, { report = true, smooth = true } = {}) {
    const count = this.#slides().length;
    if (count === 0) return;
    const index = this.#loop ? ((target % count) + count) % count : this.#clamp(target);
    const viewport = this.#viewport;
    const changed = index !== this.#index;
    this.#index = index;
    if (viewport) {
      const direction = getComputedStyle(viewport).direction === 'rtl' ? -1 : 1;
      const left = index * viewport.clientWidth * direction;
      const animate = smooth && !this.#reducedMotion.matches && changed;
      if (animate) {
        this.#movingTo = index;
        clearTimeout(this.#scrollTimer);
        this.#scrollTimer = setTimeout(() => { this.#movingTo = null; }, 700);
      }
      viewport.scrollTo({ left, behavior: animate ? 'smooth' : 'instant' });
    }
    this.#update();
    if (changed && report) this.#reporter.report('bx-change', { value: String(index) });
  }

  #update() {
    const slides = this.#slides();
    slides.forEach((slide, i) => slide.toggleAttribute('inert', i !== this.#index));
    this.querySelectorAll(':scope > [data-slot="controls"] [data-slot="dot"]').forEach((dot) => {
      if (Number(dot.dataset.index) === this.#index) dot.setAttribute('aria-current', 'true');
      else dot.removeAttribute('aria-current');
    });
    const atStart = !this.#loop && this.#index === 0;
    const atEnd = !this.#loop && this.#index === slides.length - 1;
    for (const [slot, disabled] of [['prev', atStart], ['next', atEnd]]) {
      const button = this.#button(slot);
      if (!button) continue;
      if (disabled) button.setAttribute('aria-disabled', 'true');
      else button.removeAttribute('aria-disabled');
    }
    this.#paintRotation();
  }

  #running() {
    return this.#interval > 0 && !this.#userPaused && !this.#hovered && !this.#focused && !document.hidden;
  }

  #paintRotation() {
    this.toggleAttribute('data-paused', this.#userPaused);
    const toggle = this.#button('toggle');
    if (toggle) toggle.setAttribute('aria-label', (this.#userPaused ? this.getAttribute('data-label-play') : this.getAttribute('data-label-pause')) ?? '');
    // APG: announce slide changes politely, but never while the slides rotate on their own.
    this.#viewport?.setAttribute('aria-live', this.#interval > 0 && !this.#userPaused ? 'off' : 'polite');
  }

  #restartTimer = () => {
    clearInterval(this.#timer);
    this.#timer = null;
    if (this.#running()) this.#timer = setInterval(() => this.#goTo(this.#index + 1), this.#interval);
  };

  #next() {
    if (this.#loop || this.#index < this.#slides().length - 1) this.#goTo(this.#index + 1);
  }

  #prev() {
    if (this.#loop || this.#index > 0) this.#goTo(this.#index - 1);
  }

  #onScroll = () => {
    if (this.#movingTo !== null) {
      // A scripted smooth scroll is on its way: wait for it to land.
      clearTimeout(this.#scrollTimer);
      this.#scrollTimer = setTimeout(() => {
        this.#movingTo = null;
      }, 120);
      return;
    }
    clearTimeout(this.#scrollTimer);
    this.#scrollTimer = setTimeout(() => {
      const viewport = this.#viewport;
      if (!viewport || viewport.clientWidth === 0) return;
      const index = this.#clamp(Math.round(Math.abs(viewport.scrollLeft) / viewport.clientWidth));
      if (index === this.#index) return;
      this.#index = index;
      this.#update();
      this.#reporter.report('bx-change', { value: String(index) });
    }, 100);
  };

  #onClick = (event) => {
    const button = event.target.closest?.('button[data-slot]');
    if (!button || button.closest('bx-carousel') !== this) return;
    if (button.getAttribute('aria-disabled') === 'true') return;
    switch (button.dataset.slot) {
      case 'prev': this.#prev(); break;
      case 'next': this.#next(); break;
      case 'dot': this.#goTo(Number(button.dataset.index)); break;
      case 'toggle':
        this.#userPaused = !this.#userPaused;
        this.#paintRotation();
        this.#restartTimer();
        break;
      default: return;
    }
    // A manual choice restarts the countdown instead of racing it.
    if (button.dataset.slot !== 'toggle') this.#restartTimer();
  };

  #onKeyDown = (event) => {
    if (event.target !== this.#viewport || event.altKey || event.ctrlKey || event.metaKey) return;
    const rtl = getComputedStyle(this).direction === 'rtl';
    switch (event.key) {
      case 'ArrowLeft': rtl ? this.#next() : this.#prev(); break;
      case 'ArrowRight': rtl ? this.#prev() : this.#next(); break;
      case 'Home': this.#goTo(0); break;
      case 'End': this.#goTo(this.#slides().length - 1); break;
      default: return;
    }
    event.preventDefault();
    this.#restartTimer();
  };

  #onPointerEnter = () => {
    this.#hovered = true;
    this.#restartTimer();
  };

  #onPointerLeave = () => {
    this.#hovered = false;
    this.#restartTimer();
  };

  #onFocusIn = () => {
    this.#focused = true;
    this.#restartTimer();
  };

  #onFocusOut = (event) => {
    if (this.contains(event.relatedTarget)) return;
    this.#focused = false;
    this.#restartTimer();
  };
}

if (!customElements.get('bx-carousel')) customElements.define('bx-carousel', BxCarouselElement);
