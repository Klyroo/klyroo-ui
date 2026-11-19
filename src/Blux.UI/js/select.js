import { Reporter, ensureId, float, observeChildren } from './shared.js';

/**
 * Select-only combobox (WAI-ARIA APG): focus stays on the button, the active option is announced through
 * aria-activedescendant. A hidden input carries the value, so the select posts in a plain HTML form.
 *
 * <bx-select value="b">
 *   <input type="hidden" data-slot="value" name="…" value="b">
 *   <button type="button" role="combobox" data-slot="trigger"><span data-slot="label" data-label="Option B"></span>…</button>
 *   <div popover="auto" data-slot="content"><div role="listbox">
 *     <div role="option" data-value="a" data-label="Option A" aria-selected="false">…</div> …
 *   </div></div>
 * </bx-select>
 *
 * The visible label is CSS generated content from data-label, so the script never replaces nodes Blazor owns.
 */
export class BxSelectElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'value'];

  #observer = null;
  #stopFloating = null;
  #active = null;
  #reporter = new Reporter(this);

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('pointermove', this.#onPointerMove);
    this.addEventListener('toggle', this.#onToggle, true);
    this.#observer = observeChildren(this, () => this.#wire());
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
    this.#stopFloating?.();
  }

  attributeChangedCallback(name) {
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-bx-seq') queueMicrotask(() => this.#applyValue());
    else this.#applyValue();
  }

  #applyValue() {
    if (this.#reporter.isStale()) return;
    const value = this.getAttribute('value');
    const option = this.#options().find((o) => o.dataset.value === (value ?? ''));
    this.#apply(option ?? null, false);
  }

  refresh() {
    this.#wire();
  }

  get #trigger() {
    return this.querySelector(':scope > [data-slot="trigger"]');
  }

  get #content() {
    return this.querySelector(':scope > [popover]');
  }

  #options() {
    return [...this.querySelectorAll('[role="option"]')];
  }

  #enabled() {
    return this.#options().filter((o) => o.getAttribute('aria-disabled') !== 'true');
  }

  #wire() {
    const trigger = this.#trigger;
    const content = this.#content;
    const listbox = content?.querySelector('[role="listbox"]');
    if (!trigger || !content || !listbox) return;
    if (trigger.popoverTargetElement !== content) trigger.popoverTargetElement = content;
    trigger.setAttribute('aria-controls', ensureId(listbox, 'listbox'));
    trigger.setAttribute('aria-expanded', String(content.matches(':popover-open')));
    for (const option of this.#options()) ensureId(option, 'option');
  }

  #isOpen() {
    return this.#content?.matches(':popover-open') ?? false;
  }

  #highlight(option) {
    this.#active?.removeAttribute('data-highlighted');
    this.#active = option;
    const trigger = this.#trigger;
    if (option) {
      option.setAttribute('data-highlighted', '');
      option.scrollIntoView({ block: 'nearest' });
      trigger.setAttribute('aria-activedescendant', option.id);
    } else {
      trigger.removeAttribute('aria-activedescendant');
    }
  }

  #apply(option, report) {
    const value = option?.dataset.value ?? '';
    const input = this.querySelector(':scope > input[data-slot="value"]');
    const label = this.querySelector('[data-slot="label"]');
    const changed = input ? input.value !== value : true;
    if (input) {
      input.value = value;
      input.setAttribute('value', value);
    }
    if (label) {
      if (option) {
        label.dataset.label = option.dataset.label ?? option.textContent.trim();
        label.removeAttribute('data-placeholder');
      } else {
        label.dataset.label = label.dataset.placeholderText ?? '';
        label.setAttribute('data-placeholder', '');
      }
    }
    for (const o of this.#options()) o.setAttribute('aria-selected', String(o === option));
    if (changed && report) {
      input?.dispatchEvent(new Event('change', { bubbles: true }));
      this.#reporter.report('bx-change', { value });
    }
  }

  #choose(option) {
    if (!option || option.getAttribute('aria-disabled') === 'true') return;
    this.#apply(option, true);
    this.#content.hidePopover();
  }

  // "toggle" fires asynchronously: keys typed right after opening are handled before it. So opening highlights the
  // current option synchronously in #opened(), and the toggle handler only fills in what is still missing.
  #opened() {
    const trigger = this.#trigger;
    const content = this.#content;
    trigger.setAttribute('aria-expanded', 'true');
    this.dataset.state = content.dataset.state = 'open';
    if (!this.#stopFloating) {
      this.#stopFloating = float(trigger, content, { placement: 'bottom-start', distance: 4, matchWidth: true });
    }
    if (!this.#active) {
      const selected = this.#enabled().find((o) => o.getAttribute('aria-selected') === 'true');
      this.#highlight(selected ?? this.#enabled()[0] ?? null);
    }
  }

  #onToggle = (event) => {
    const content = this.#content;
    if (event.target !== content) return;
    if (event.newState === 'open') {
      this.#opened();
      this.#trigger.focus({ preventScroll: true });
      return;
    }
    this.#trigger.setAttribute('aria-expanded', 'false');
    this.dataset.state = content.dataset.state = 'closed';
    this.#stopFloating?.();
    this.#stopFloating = null;
    this.#highlight(null);
  };

  #onKeyDown = (event) => {
    const trigger = this.#trigger;
    if (event.target !== trigger) return;
    const options = this.#enabled();
    if (!this.#isOpen()) {
      if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(event.key)) {
        event.preventDefault();
        this.#content.showPopover();
        this.#opened();
      }
      return;
    }
    const index = options.indexOf(this.#active);
    let next = null;
    switch (event.key) {
      case 'ArrowDown': next = options[Math.min(index + 1, options.length - 1)]; break;
      case 'ArrowUp': next = options[Math.max(index - 1, 0)]; break;
      case 'Home': next = options[0]; break;
      case 'End': next = options.at(-1); break;
      case 'Enter':
      case ' ':
        event.preventDefault();
        this.#choose(this.#active);
        return;
      case 'Tab':
        this.#content.hidePopover();
        return;
      default:
        if (event.key.length === 1 && /\S/.test(event.key)) {
          const letter = event.key.toLowerCase();
          const ordered = [...options.slice(index + 1), ...options.slice(0, index + 1)];
          next = ordered.find((o) => (o.dataset.label ?? o.textContent).trim().toLowerCase().startsWith(letter)) ?? null;
        }
    }
    if (next) {
      event.preventDefault();
      this.#highlight(next);
    }
  };

  #onClick = (event) => {
    const option = event.target.closest?.('[role="option"]');
    if (option && this.contains(option)) this.#choose(option);
  };

  #onPointerMove = (event) => {
    const option = event.target.closest?.('[role="option"]');
    if (option && option !== this.#active && option.getAttribute('aria-disabled') !== 'true') this.#highlight(option);
  };
}

if (!customElements.get('bx-select')) customElements.define('bx-select', BxSelectElement);
