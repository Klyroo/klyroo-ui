import { Reporter } from './shared.js';

/**
 * A pressed/unpressed button that works without .NET: the element flips aria-pressed itself.
 *
 * <lx-toggle pressed>
 *   <input type="hidden" data-slot="value" name="…" value="true" disabled>   (posts only while pressed)
 *   <button type="button" aria-pressed="true">…</button>
 * </lx-toggle>
 */
export class LxToggleElement extends HTMLElement {
  static observedAttributes = ['data-lx-seq', 'pressed'];

  #reporter = new Reporter(this);

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
  }

  attributeChangedCallback(name) {
    if (!this.isConnected) return;
    if (name === 'data-lx-seq') queueMicrotask(() => this.#apply());
    else this.#apply();
  }

  refresh() {
    this.#reporter.adopt();
    this.#apply();
  }

  get #button() {
    return this.querySelector('button');
  }

  // The rendered state is the pressed attribute; a render older than the last report is ignored.
  #apply() {
    if (this.#reporter.isStale()) return;
    this.#show(this.hasAttribute('pressed'));
  }

  #show(pressed) {
    const button = this.#button;
    button?.setAttribute('aria-pressed', String(pressed));
    if (button) button.dataset.state = pressed ? 'on' : 'off';
    const input = this.querySelector('input[data-slot="value"]');
    if (input) input.disabled = !pressed || this.hasAttribute('disabled');
  }

  #onClick = (event) => {
    const button = this.#button;
    if (!button || !button.contains(event.target) || button.disabled) return;
    const pressed = button.getAttribute('aria-pressed') !== 'true';
    this.#show(pressed);
    this.toggleAttribute('pressed', pressed);
    this.dispatchEvent(new Event('change', { bubbles: true }));
    this.#reporter.report('lx-change', { value: String(pressed) });
  };
}

if (!customElements.get('lx-toggle')) customElements.define('lx-toggle', LxToggleElement);
