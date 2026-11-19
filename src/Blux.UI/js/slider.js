import { Reporter } from './shared.js';
import { SEP } from './form2-shared.js';

/**
 * One or two native range inputs over a drawn track. The native inputs keep the keyboard behaviour, the ARIA
 * slider role and form posting; the element clamps the two thumbs of a range against each other and positions
 * the filled part of the track through --bx-lo and --bx-hi.
 *
 * <bx-slider data-immediate>
 *   <div data-slot="track"><div data-slot="range"></div></div>
 *   <input type="range" data-slot="thumb" name="…"> [<input type="range" data-slot="thumb" name="…">]
 * </bx-slider>
 */
export class BxSliderElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'data-values'];

  #reporter = new Reporter(this);
  #last = null;

  connectedCallback() {
    this.addEventListener('input', this.#onInput);
    this.addEventListener('change', this.#onChange);
    this.#last = this.#read();
    this.#update();
  }

  attributeChangedCallback() {
    if (!this.isConnected) return;
    // The inputs are updated after the host's attributes: look once the whole render is in place.
    queueMicrotask(() => {
      if (this.#reporter.isStale() && this.#last) this.#inputs().forEach((input, i) => (input.value = this.#last[i]));
      else this.#last = this.#read();
      this.#update();
    });
  }

  refresh() {
    this.#update();
  }

  #inputs() {
    return [...this.querySelectorAll('input[data-slot="thumb"]')];
  }

  #read() {
    return this.#inputs().map((i) => i.value);
  }

  #update() {
    const inputs = this.#inputs();
    if (inputs.length === 0) return;
    const percent = (input) => {
      const min = Number(input.min || 0);
      const max = Number(input.max || 100);
      return max === min ? 0 : ((Number(input.value) - min) / (max - min)) * 100;
    };
    const position = (p) => `calc(${p}% + ${0.5 - p / 100} * var(--bx-thumb, 1rem))`;
    const range = inputs.length > 1;
    this.style.setProperty('--bx-lo', range ? position(percent(inputs[0])) : '0px');
    this.style.setProperty('--bx-hi', position(percent(inputs.at(-1))));
    if (range) {
      // When both thumbs sit at the top end, the low one must stay reachable.
      const middle = (Number(inputs[0].min || 0) + Number(inputs[0].max || 100)) / 2;
      inputs[0].style.zIndex = Number(inputs[0].value) >= middle ? '2' : '1';
      inputs[1].style.zIndex = Number(inputs[0].value) >= middle ? '1' : '2';
    }
  }

  #onInput = (event) => {
    const inputs = this.#inputs();
    if (inputs.length > 1) {
      const [lo, hi] = inputs;
      if (event.target === lo && Number(lo.value) > Number(hi.value)) lo.value = hi.value;
      if (event.target === hi && Number(hi.value) < Number(lo.value)) hi.value = lo.value;
    }
    this.#update();
    if (this.hasAttribute('data-immediate')) this.#report();
  };

  #onChange = (event) => {
    if (event.target === this) return;
    this.#report();
  };

  #report() {
    const values = this.#read();
    if (this.#last && values.every((v, i) => v === this.#last[i])) return;
    this.#last = values;
    this.#reporter.report('bx-change', { value: values.join(SEP) });
  }
}

if (!customElements.get('bx-slider')) customElements.define('bx-slider', BxSliderElement);
