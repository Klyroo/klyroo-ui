import { Reporter } from './shared.js';

/** Separates the values of a list in attributes and in bx-change events, where they travel as one string. */
export const SEP = '\u001f';

export const parseList = (text) => (text ? text.split(SEP) : []);

/**
 * The selected values of a multi-valued element, shared by select-menu, listbox, toggle-group and input-tags.
 *
 * The rendered state is the host's data-values attribute. The model owns a list of hidden inputs in
 * <span data-slot="values" hidden>, which Blazor renders empty, so the values post in a plain HTML form.
 * Reports changes as bx-change, and ignores renders older than the last report (see Reporter).
 */
export class ValueModel {
  #host;
  #paint;
  #values = [];
  #loaded = false;
  reporter;

  constructor(host, paint) {
    this.#host = host;
    this.#paint = paint;
    this.reporter = new Reporter(host);
  }

  get values() {
    return this.#values;
  }

  /** Takes the rendered state, unless it comes from a render older than the last report, then paints. */
  load() {
    if (!this.#loaded || !this.reporter.isStale()) this.#values = parseList(this.#host.getAttribute('data-values'));
    this.#loaded = true;
    this.render();
  }

  render() {
    this.#writeInputs();
    this.#paint(this.#values);
  }

  set(values, report = true) {
    if (values.length === this.#values.length && values.every((v, i) => v === this.#values[i])) return;
    this.#values = values;
    this.render();
    if (report) {
      this.#host.dispatchEvent(new Event('change', { bubbles: true }));
      this.reporter.report('bx-change', { value: values.join(SEP) });
    }
  }

  #writeInputs() {
    const box = this.#host.querySelector(':scope > [data-slot="values"]');
    if (!box) return;
    const name = this.#host.getAttribute('name');
    let wanted = [];
    if (name && !this.#host.hasAttribute('disabled')) {
      wanted = this.#values.length === 0 && this.#host.hasAttribute('data-post-empty') ? [''] : this.#values;
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
