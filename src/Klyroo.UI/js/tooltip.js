import { FOCUSABLE, ensureId, float, observeChildren } from './shared.js';

/**
 * <kly-tooltip delay="300" placement="top">
 *   <span data-slot="trigger">…focusable element…</span>
 *   <div popover="manual" role="tooltip">…</div>
 * </kly-tooltip>
 *
 * Shows on hover (after a delay) and on keyboard focus, stays while the pointer is over the tooltip,
 * hides on Escape, blur or leave (WCAG 1.4.13). Purely visual: never reported to .NET.
 */
const LONG_PRESS = 500;
const TOUCH_LINGER = 2500;

export class KlyTooltipElement extends HTMLElement {
  #content = null;
  #timer = 0;
  #pressTimer = 0;
  #pressed = false;
  #stopFloating = null;
  #observer = null;

  connectedCallback() {
    this.addEventListener('pointerover', this.#onPointerOver);
    this.addEventListener('pointerout', this.#onPointerOut);
    this.addEventListener('pointerdown', this.#onPointerDown);
    this.addEventListener('pointerup', this.#onPointerEnd);
    this.addEventListener('pointercancel', this.#onPointerEnd);
    this.addEventListener('focusin', this.#onFocusIn);
    this.addEventListener('focusout', this.#onFocusOut);
    this.#observer = observeChildren(this, () => this.#sync());
  }

  disconnectedCallback() {
    clearTimeout(this.#pressTimer);
    this.#observer?.disconnect();
    this.#hide();
  }

  refresh() {
    this.#sync();
  }

  /** Hides the tooltip now (TV Back uses it: a tooltip is not a layer Back closes, but it may go away with it). */
  hide() {
    this.#hide();
  }

  // The element that receives focus (an input inside a wrapper, say), else the first child.
  get #trigger() {
    const wrapper = this.querySelector(':scope > [data-slot="trigger"]');
    return wrapper?.querySelector(FOCUSABLE) ?? wrapper?.firstElementChild ?? null;
  }

  #sync() {
    this.#content = this.querySelector(':scope > [popover]');
    const trigger = this.#trigger;
    if (!this.#content || !trigger) return;
    // Added to existing descriptions (a form field's hint, say), not in place of them.
    const id = ensureId(this.#content, 'tooltip');
    const ids = (trigger.getAttribute('aria-describedby') ?? '').split(' ').filter(Boolean);
    if (!ids.includes(id)) trigger.setAttribute('aria-describedby', [...ids, id].join(' '));
  }

  #schedule(show) {
    clearTimeout(this.#timer);
    if (!show) {
      this.#timer = setTimeout(() => this.#hide(), 100);
      return;
    }
    this.#timer = setTimeout(() => this.#show(), Number(this.getAttribute('delay') ?? 300));
  }

  #show() {
    const content = this.#content;
    const trigger = this.#trigger;
    if (!content || !trigger || content.matches(':popover-open')) return;
    content.showPopover();
    this.dataset.state = content.dataset.state = 'open';
    this.#stopFloating = float(trigger, content, { placement: this.getAttribute('placement') || 'top', distance: 6 });
    document.addEventListener('keydown', this.#onEscape);
  }

  #hide() {
    clearTimeout(this.#timer);
    document.removeEventListener('keydown', this.#onEscape);
    this.#stopFloating?.();
    this.#stopFloating = null;
    if (this.#content?.matches(':popover-open')) this.#content.hidePopover();
    if (this.#content) this.dataset.state = this.#content.dataset.state = 'closed';
  }

  // A finger has no hover: the pointerover of a tap must not open the tooltip (it would cover the control just tapped and
  // stay until the next tap). Touch opens it on a long press instead, and it closes shortly after the finger lifts.
  #onPointerOver = (event) => {
    if (event.pointerType === 'touch') return;
    this.#schedule(true);
  };

  #onPointerOut = (event) => {
    if (event.pointerType === 'touch') return;
    if (!this.contains(event.relatedTarget)) this.#schedule(false);
  };

  #onPointerDown = (event) => {
    if (event.pointerType !== 'touch') return;
    clearTimeout(this.#pressTimer);
    this.#pressTimer = setTimeout(() => {
      this.#pressed = true;
      this.#show();
    }, LONG_PRESS);
  };

  #onPointerEnd = (event) => {
    if (event.pointerType !== 'touch') return;
    clearTimeout(this.#pressTimer);
    if (this.#pressed) {
      this.#pressed = false;
      clearTimeout(this.#timer);
      this.#timer = setTimeout(() => this.#hide(), TOUCH_LINGER);
    }
  };

  #onFocusIn = (event) => {
    if (this.#trigger?.contains(event.target) && event.target.matches(':focus-visible')) {
      clearTimeout(this.#timer);
      this.#show();
    }
  };

  #onFocusOut = () => this.#hide();

  #onEscape = (event) => {
    if (event.key === 'Escape') this.#hide();
  };
}

if (!customElements.get('kly-tooltip')) customElements.define('kly-tooltip', KlyTooltipElement);
