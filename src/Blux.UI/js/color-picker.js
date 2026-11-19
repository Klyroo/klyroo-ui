import { Reporter, observeChildren } from './shared.js';

const clamp = (n, min, max) => Math.min(max, Math.max(min, n));

function hsvToHex(h, s, v) {
  const sat = s / 100;
  const val = v / 100;
  const f = (n) => {
    const k = (n + h / 60) % 6;
    return val - val * sat * Math.max(0, Math.min(k, 4 - k, 1));
  };
  const hex = (x) => Math.round(x * 255).toString(16).padStart(2, '0');
  return `#${hex(f(5))}${hex(f(3))}${hex(f(1))}`;
}

function hexToHsv(hex, previousHue) {
  const r = parseInt(hex.slice(1, 3), 16) / 255;
  const g = parseInt(hex.slice(3, 5), 16) / 255;
  const b = parseInt(hex.slice(5, 7), 16) / 255;
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const d = max - min;
  let h = previousHue;
  if (d !== 0) {
    if (max === r) h = ((g - b) / d + 6) % 6;
    else if (max === g) h = (b - r) / d + 2;
    else h = (r - g) / d + 4;
    h *= 60;
  }
  return { h: Math.round(h) % 360, s: Math.round(max === 0 ? 0 : (d / max) * 100), v: Math.round(max * 100) };
}

/** "#abc", "abc", "#aabbcc" or "AABBCC" to "#aabbcc"; null when it is not a hex color. */
function normalize(text) {
  const match = /^#?([0-9a-f]{3}|[0-9a-f]{6})$/i.exec((text ?? '').trim());
  if (!match) return null;
  const digits = match[1].length === 3 ? [...match[1]].map((c) => c + c).join('') : match[1];
  return `#${digits.toLowerCase()}`;
}

/**
 * Color picker: a saturation/brightness area, a hue slider and a hex field. The area holds two native range inputs
 * (saturation, brightness) hidden visually, so the keyboard and assistive technology get real sliders; pointer
 * input on the area drives the same inputs. The value is "#rrggbb", carried by the hidden input and reported with
 * "bx-change".
 *
 * <bx-color-picker value="#ffd23f">
 *   <input type="hidden" data-slot="value" name="…" value="#ffd23f">
 *   <div data-slot="area"><input type="range" data-slot="saturation"> <input type="range" data-slot="brightness"> <span data-slot="thumb"></span></div>
 *   <input type="range" data-slot="hue"> <input type="text" data-slot="hex">
 * </bx-color-picker>
 *
 * Positions, ranges and the hex text are set here (they derive from the value and change while dragging), so Blazor
 * renders none of them. The host gets the custom properties --bx-hue and --bx-swatch for the stylesheet.
 */
export class BxColorPickerElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'value'];

  #h = 0;
  #s = 0;
  #v = 0;
  #has = false;
  #committed = '';
  #observer = null;
  #scheduled = false;
  #reporter = new Reporter(this);

  connectedCallback() {
    this.addEventListener('input', this.#onInput);
    this.addEventListener('change', this.#onChange);
    this.addEventListener('pointerdown', this.#onPointerDown);
    this.addEventListener('pointermove', this.#onPointerMove);
    this.#observer = observeChildren(this, () => this.#sync());
  }

  disconnectedCallback() {
    this.removeEventListener('input', this.#onInput);
    this.removeEventListener('change', this.#onChange);
    this.removeEventListener('pointerdown', this.#onPointerDown);
    this.removeEventListener('pointermove', this.#onPointerMove);
    this.#observer?.disconnect();
  }

  attributeChangedCallback() {
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (this.#scheduled) return;
    this.#scheduled = true;
    queueMicrotask(() => {
      this.#scheduled = false;
      this.#sync();
    });
  }

  refresh() {
    this.#sync();
  }

  #slot(name) {
    return this.querySelector(`[data-slot="${name}"]`);
  }

  get #disabled() {
    return this.hasAttribute('data-disabled');
  }

  #hex() {
    return hsvToHex(this.#h, this.#s, this.#v);
  }

  #sync() {
    if (!this.#slot('hue')) return;
    if (!this.#reporter.isStale()) {
      const hex = normalize(this.getAttribute('value'));
      if (hex === null) {
        if (this.#has) {
          this.#has = false;
          this.#s = this.#v = 0;
        }
      } else if (!this.#has || hex !== this.#hex()) {
        this.#set(hexToHsv(hex, this.#h));
      }
      this.#committed = this.#has ? this.#hex() : '';
    }
    this.#paint(null);
  }

  #set({ h, s, v }) {
    this.#h = clamp(h, 0, 359);
    this.#s = clamp(s, 0, 100);
    this.#v = clamp(v, 0, 100);
    this.#has = true;
  }

  /** Writes the state into every part. `source` is the part the user is editing, left alone so typing is not disturbed. */
  #paint(source) {
    const hex = this.#hex();
    this.style.setProperty('--bx-hue', String(this.#h));
    this.style.setProperty('--bx-swatch', this.#has ? hex : 'transparent');
    const ranges = { saturation: this.#s, brightness: this.#v, hue: this.#h };
    for (const [name, value] of Object.entries(ranges)) {
      const range = this.#slot(name);
      if (!range) continue;
      if (source !== name) range.value = String(value);
      range.setAttribute('aria-valuetext', name === 'hue' ? `${value}°` : `${value}%`);
    }
    const thumb = this.#slot('thumb');
    if (thumb) {
      thumb.style.left = `${this.#s}%`;
      thumb.style.top = `${100 - this.#v}%`;
      thumb.style.backgroundColor = hex;
    }
    const field = this.#slot('hex');
    if (field && source !== 'hex') {
      field.value = this.#has ? hex : '';
      field.removeAttribute('aria-invalid');
    }
  }

  #commit(source) {
    this.#paint(source);
    const value = this.#has ? this.#hex() : '';
    if (value === this.#committed) return;
    this.#committed = value;
    const input = this.querySelector(':scope > input[data-slot="value"]');
    if (input) {
      input.value = value;
      input.setAttribute('value', value);
      input.dispatchEvent(new Event('change', { bubbles: true }));
    }
    this.#reporter.report('bx-change', { value });
  }

  #onInput = (event) => {
    const target = event.target;
    if (this.#disabled || !(target instanceof HTMLInputElement)) return;
    const slot = target.dataset.slot;
    if (slot === 'saturation') this.#set({ h: this.#h, s: Number(target.value), v: this.#v });
    else if (slot === 'brightness') this.#set({ h: this.#h, s: this.#s, v: Number(target.value) });
    else if (slot === 'hue') this.#set({ h: Number(target.value), s: this.#s, v: this.#v });
    else if (slot === 'hex') {
      const hex = normalize(target.value);
      target.toggleAttribute('aria-invalid', false);
      if (hex === null) return;
      this.#set(hexToHsv(hex, this.#h));
      this.#commit('hex');
      return;
    } else return;
    this.#commit(slot);
  };

  // Leaving the hex field with text that is not a color puts the current color back.
  #onChange = (event) => {
    const target = event.target;
    if (target instanceof HTMLInputElement && target.dataset.slot === 'hex') this.#paint(null);
  };

  #fromPointer(event) {
    const area = this.#slot('area');
    const rect = area.getBoundingClientRect();
    const s = clamp(((event.clientX - rect.left) / rect.width) * 100, 0, 100);
    const v = clamp(100 - ((event.clientY - rect.top) / rect.height) * 100, 0, 100);
    this.#set({ h: this.#h, s: Math.round(s), v: Math.round(v) });
    this.#commit(null);
  }

  #onPointerDown = (event) => {
    const area = this.#slot('area');
    if (this.#disabled || event.button !== 0 || !area?.contains(event.target)) return;
    event.preventDefault();
    area.setPointerCapture(event.pointerId);
    this.#slot('saturation')?.focus({ preventScroll: true });
    this.#fromPointer(event);
  };

  #onPointerMove = (event) => {
    const area = this.#slot('area');
    if (area?.hasPointerCapture(event.pointerId)) this.#fromPointer(event);
  };
}

if (!customElements.get('bx-color-picker')) customElements.define('bx-color-picker', BxColorPickerElement);
