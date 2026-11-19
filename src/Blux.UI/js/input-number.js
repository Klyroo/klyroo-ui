import { Reporter, observeChildren } from './shared.js';

/**
 * Numeric field with increment and decrement buttons. The visible field shows the number formatted for the
 * element's locale; a hidden input carries the invariant value, so the number posts the same in every culture.
 *
 * <bx-input-number value="12.5" min max step="0.5" locale="fr-FR">
 *   <input type="hidden" data-slot="value" name="…" value="12.5">
 *   <button data-slot="decrement">…</button>
 *   <input data-slot="input" role="spinbutton">
 *   <button data-slot="increment">…</button>
 * </bx-input-number>
 */
export class BxInputNumberElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'value', 'min', 'max', 'step', 'locale'];

  #reporter = new Reporter(this);
  #value = null;
  #format = null;
  #decimal = '.';
  #observer = null;

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('change', this.#onChange);
    this.#observer?.disconnect();
    // Blazor inserts the element before its children and attributes: take the rendered state once they are there.
    this.#observer = observeChildren(this, () => this.#applyRendered());
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
    this.#observer = null;
  }

  attributeChangedCallback(name) {
    if (!this.isConnected) return;
    if (name === 'data-bx-seq') {
      queueMicrotask(() => this.#applyRendered(true));
    } else {
      if (name === 'locale') this.#format = null;
      this.#applyRendered();
    }
  }

  refresh() {
    this.#reporter.adopt();
    this.#applyRendered(this.#reporter.bound);
  }

  get #input() {
    return this.querySelector('[data-slot="input"]');
  }

  #num(name) {
    const raw = this.getAttribute(name);
    const n = raw === null || raw === '' ? NaN : Number(raw);
    return Number.isFinite(n) ? n : null;
  }

  get #step() {
    return this.#num('step') ?? 1;
  }

  #rendered() {
    return this.#num('value');
  }

  /** Takes the rendered value when a render changed it (force: a bound render arrived), then paints. */
  #applyRendered(force = false) {
    if (this.#reporter.take('value', force) !== undefined) this.#value = this.#rendered();
    this.#show();
  }

  #decimals(n) {
    const text = String(n);
    const e = /e-(\d+)$/.exec(text);
    if (e) return Number(e[1]);
    const dot = text.indexOf('.');
    return dot < 0 ? 0 : text.length - dot - 1;
  }

  #formatter() {
    if (!this.#format) {
      const locale = this.getAttribute('locale') || undefined;
      try {
        this.#format = new Intl.NumberFormat(locale, { maximumFractionDigits: 10 });
      } catch {
        this.#format = new Intl.NumberFormat(undefined, { maximumFractionDigits: 10 });
      }
      this.#decimal = this.#format.formatToParts(1.5).find((p) => p.type === 'decimal')?.value ?? '.';
      this.groupSeparator = this.#format.formatToParts(1000000).find((p) => p.type === 'group')?.value ?? '';
    }
    return this.#format;
  }

  #parse(text) {
    this.#formatter();
    let t = text.replace(/[\s  ]/g, '');
    if (this.groupSeparator) t = t.split(this.groupSeparator.replace(/[\s  ]/g, '')).join('');
    t = t.replace(this.#decimal, '.').replace('−', '-');
    if (t === '' || !/^[-+]?(\d+\.?\d*|\.\d+)$/.test(t)) return null;
    return Number(t);
  }

  #clamp(n) {
    const min = this.#num('min');
    const max = this.#num('max');
    if (min !== null && n < min) n = min;
    if (max !== null && n > max) n = max;
    return n;
  }

  #show() {
    const input = this.#input;
    if (!input) return;
    const value = this.#value;
    input.value = value === null ? '' : this.#formatter().format(value);
    input.setAttribute('aria-valuenow', value === null ? '' : String(value));
    if (value === null) input.removeAttribute('aria-valuenow');
    for (const name of ['min', 'max']) {
      const n = this.#num(name);
      if (n === null) input.removeAttribute(`aria-value${name}`);
      else input.setAttribute(`aria-value${name}`, String(n));
    }
    if (value === null) input.removeAttribute('aria-valuetext');
    else input.setAttribute('aria-valuetext', input.value);
    const hidden = this.querySelector('input[data-slot="value"]');
    if (hidden) hidden.value = value === null ? '' : String(value);
    const min = this.#num('min');
    const max = this.#num('max');
    const dec = this.querySelector('[data-slot="decrement"]');
    const inc = this.querySelector('[data-slot="increment"]');
    if (dec && !this.hasAttribute('disabled')) dec.disabled = value !== null && min !== null && value <= min;
    if (inc && !this.hasAttribute('disabled')) inc.disabled = value !== null && max !== null && value >= max;
  }

  #commit(value) {
    const next = value === null ? null : this.#clamp(value);
    if (next === this.#value) {
      this.#show();
      return;
    }
    this.#value = next;
    this.#show();
    this.dispatchEvent(new Event('change', { bubbles: true }));
    this.#reporter.report('bx-change', { value: next === null ? '' : String(next) });
  }

  #nudge(direction, multiplier = 1) {
    const step = this.#step * multiplier;
    const base = this.#value ?? this.#clamp(0);
    const digits = Math.max(this.#decimals(step), this.#decimals(base));
    this.#commit(Number((base + direction * step).toFixed(Math.min(digits, 10))));
  }

  #onClick = (event) => {
    const button = event.target.closest?.('button');
    if (!button || !this.contains(button) || button.disabled) return;
    if (button.dataset.slot === 'increment') this.#nudge(1);
    else if (button.dataset.slot === 'decrement') this.#nudge(-1);
    else return;
    this.#input?.focus();
  };

  #onKeyDown = (event) => {
    const input = this.#input;
    if (event.target !== input) return;
    switch (event.key) {
      case 'ArrowUp': event.preventDefault(); this.#commitText(); this.#nudge(1); break;
      case 'ArrowDown': event.preventDefault(); this.#commitText(); this.#nudge(-1); break;
      case 'PageUp': event.preventDefault(); this.#commitText(); this.#nudge(1, 10); break;
      case 'PageDown': event.preventDefault(); this.#commitText(); this.#nudge(-1, 10); break;
      case 'Home': {
        const min = this.#num('min');
        if (min !== null) { event.preventDefault(); this.#commit(min); }
        break;
      }
      case 'End': {
        const max = this.#num('max');
        if (max !== null) { event.preventDefault(); this.#commit(max); }
        break;
      }
      case 'Enter': this.#commitText(); break;
    }
  };

  // Typed text commits when the field loses focus or Enter is pressed (the native change event of the visible field).
  #onChange = (event) => {
    if (event.target === this.#input) {
      event.stopPropagation();
      this.#commitText();
    }
  };

  #commitText() {
    const input = this.#input;
    if (!input) return;
    const text = input.value.trim();
    if (text === '') this.#commit(null);
    else {
      const parsed = this.#parse(text);
      if (parsed === null) this.#show();
      else this.#commit(parsed);
    }
  }
}

if (!customElements.get('bx-input-number')) customElements.define('bx-input-number', BxInputNumberElement);
