import { Reporter } from './shared.js';

/** Separates the values of a list in attributes and in kly-change events, where they travel as one string. */
export const SEP = '\u001f';

export const parseList = (text) => (text ? text.split(SEP) : []);

/**
 * The selected values of a multi-valued element, shared by select-menu, listbox, toggle-group and input-tags.
 *
 * The rendered state is the host's data-values attribute. The model owns a list of hidden inputs in
 * <span data-slot="values" hidden>, which Blazor renders empty, so the values post in a plain HTML form.
 * Reports changes as kly-change, and ignores renders older than the last report (see Reporter). The rendered values are
 * taken when data-values or data-kly-seq change, never because children changed.
 */
export class ValueModel {
  #host;
  #paint;
  #values = [];
  #loaded = false;
  #cleared = false;
  reporter;

  constructor(host, paint) {
    this.#host = host;
    this.#paint = paint;
    this.reporter = new Reporter(host);
  }

  get values() {
    return this.#values;
  }

  /** Paints what is there. The rendered values are only taken the first time: a child change is not a new render. */
  ensure() {
    if (!this.#loaded) this.#take(true);
    this.render();
  }

  /** An observed attribute changed: data-kly-seq and data-values mean a render, anything else only repaints. */
  attributeChanged(name) {
    if (name === 'data-kly-seq') queueMicrotask(() => this.#apply(true));
    else if (name === 'data-values') this.#apply(false);
    else this.render();
  }

  /** After an enhanced load merged server HTML over this element. A bound element takes what the server rendered. */
  refresh() {
    this.reporter.adopt();
    this.#apply(this.reporter.bound);
  }

  #apply(force) {
    this.#take(force);
    this.render();
  }

  #take(force) {
    const text = this.reporter.take('data-values', force || !this.#loaded);
    if (text === undefined) return;
    this.#values = parseList(text);
    this.#cleared = false;
    this.#loaded = true;
  }

  render() {
    this.#writeInputs();
    this.#paint(this.#values);
  }

  set(values, report = true) {
    if (values.length === this.#values.length && values.every((v, i) => v === this.#values[i])) return;
    this.#values = values;
    this.#cleared = values.length === 0;
    this.render();
    if (report) {
      this.#host.dispatchEvent(new Event('change', { bubbles: true }));
      this.reporter.report('kly-change', { value: values.join(SEP) });
    }
  }

  #writeInputs() {
    const box = this.#host.querySelector(':scope > [data-slot="values"]');
    if (!box) return;
    const name = this.#host.getAttribute('name');
    let wanted = [];
    if (name && !this.#host.hasAttribute('disabled')) {
      // An empty selection posts nothing, so a form model keeps its default: post one empty value when the user emptied it
      // (and always for a single value, which has exactly one). The form mapper reads that as one empty entry.
      const marker = this.#host.hasAttribute('data-post-empty') || this.#cleared;
      wanted = this.#values.length === 0 && marker ? [''] : this.#values;
    }
    const current = [...box.children];
    if (current.length === wanted.length && current.every((i, k) => i.value === wanted[k] && i.name === name)) return;
    box.replaceChildren(
      ...wanted.map((value) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        return input;
      }),
    );
  }
}

/** Options a user can land on: not hidden by a filter, not disabled. */
export const navigable = (options) => options.filter((o) => !o.hidden && o.getAttribute('aria-disabled') !== 'true');

/**
 * Shows the icon of the selected option in the trigger: a copy of its leading svg in the empty span Blazor renders for it
 * (no Blazor children, so nothing for Blazor to diff). Writes only when the markup differs, because a write wakes
 * the observers of the children that call this.
 */
export function syncSelectedIcon(root, option) {
  const slot = root.querySelector('[data-slot="selectedIcon"]');
  if (!slot) return;
  const icon = option?.hasAttribute('data-has-icon') ? option.querySelector(':scope > svg') : null;
  if ((slot.firstElementChild?.outerHTML ?? '') === (icon?.outerHTML ?? '')) return;
  slot.replaceChildren(...(icon ? [icon.cloneNode(true)] : []));
}

/** A group whose options are all hidden by a filter hides with them, so no heading is left alone. A group is not an option: navigation never lands on it. */
export function syncGroups(root) {
  for (const group of root.querySelectorAll('[role="group"]')) {
    group.hidden = group.querySelector('[role="option"]:not([hidden])') === null;
  }
}

/** First-letter typeahead over option labels, with a short-lived buffer for multi-letter searches. */
export class Typeahead {
  #buffer = '';
  #timer = 0;

  find(key, items, current) {
    clearTimeout(this.#timer);
    this.#timer = setTimeout(() => (this.#buffer = ''), 600);
    this.#buffer += key.toLowerCase();
    const label = (o) => (o.dataset.label ?? o.textContent).trim().toLowerCase();
    const start = items.indexOf(current);
    const cycling = this.#buffer.split('').every((c) => c === this.#buffer[0]);
    const search = cycling ? this.#buffer[0] : this.#buffer;
    const from = cycling ? start + 1 : Math.max(start, 0);
    return [...items.slice(from), ...items.slice(0, from)].find((o) => label(o).startsWith(search)) ?? null;
  }
}
