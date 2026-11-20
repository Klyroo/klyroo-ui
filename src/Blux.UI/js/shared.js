import { autoUpdate, computePosition, flip, offset, shift, size } from '@floating-ui/dom';

export const FOCUSABLE =
  'button:not([disabled]), [href], input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

let counter = 0;

/** Gives an element an id if it has none, and returns it. */
export function ensureId(element, prefix) {
  if (!element.id) element.id = `bx-${prefix}-${++counter}`;
  return element.id;
}

/**
 * Keeps a floating element next to its anchor while it is shown. Returns the cleanup function.
 * matchWidth makes the floating element at least as wide as the anchor (select lists).
 */
export function float(anchor, floating, { placement = 'bottom', distance = 8, matchWidth = false } = {}) {
  const middleware = [offset(distance), flip({ padding: 8 }), shift({ padding: 8 })];
  if (matchWidth) {
    middleware.push(size({ apply: ({ rects }) => { floating.style.minWidth = `${rects.reference.width}px`; } }));
  }
  return autoUpdate(anchor, floating, async () => {
    const result = await computePosition(anchor, floating, { strategy: 'fixed', placement, middleware });
    floating.style.left = `${result.x}px`;
    floating.style.top = `${result.y}px`;
    floating.dataset.side = result.placement.split('-')[0];
  });
}

/**
 * Change reporting shared by every element bound from .NET. Each report carries an increasing sequence; a bound
 * component renders back the last one it has seen in data-bx-seq, so a render computed before .NET received the
 * latest report can be recognized and ignored instead of undoing what the user just did.
 */
export class Reporter {
  #host;
  #sequence = 0;
  #taken = new Map();

  constructor(host) {
    this.#host = host;
  }

  /** Dispatches the event on the host, or on `target` when the .NET listener sits on another element (a childless sink). */
  report(type, detail, target = this.#host) {
    target.dispatchEvent(new CustomEvent(type, { bubbles: true, detail: { ...detail, sequence: ++this.#sequence } }));
  }

  /** True when the attribute change being processed comes from a render older than the last report. */
  isStale() {
    const seen = this.#host.getAttribute('data-bx-seq');
    return seen !== null && Number(seen) < this.#sequence;
  }

  /** True when a .NET component renders this element and listens to it (it renders data-bx-seq). */
  get bound() {
    return this.#host.hasAttribute('data-bx-seq');
  }

  /**
   * The rendered value of an attribute, when the element should show it: it differs from the one taken last time (or
   * the caller forces it, because a bound render arrived) and the render is not older than the last report.
   * Otherwise undefined. An element that owns its state reads the rendered value this way, never because some other
   * attribute or a child changed: a value the user chose would be undone by a render that did not touch it.
   */
  take(name, force = false) {
    if (this.isStale()) return undefined;
    const value = this.#host.getAttribute(name);
    if (!force && this.#taken.has(name) && this.#taken.get(name) === value) return undefined;
    this.#taken.set(name, value);
    return value;
  }

  /**
   * After an enhanced navigation or form post the server has rendered the page again. A static page has no .NET
   * runtime to receive reports, so the sequence the server rendered (always the initial one) becomes the baseline:
   * without this, every later server render would look older than the element's own reports and be ignored.
   */
  adopt() {
    this.#sequence = Number(this.#host.getAttribute('data-bx-seq') ?? 0);
  }
}

/**
 * Calls callback now and whenever the element's children change (Blazor inserts children after the element).
 * Changes inside an element matching `ignore` do not count: it is DOM another library owns (the editor surface), which
 * changes on every keystroke and holds nothing for the element to wire.
 */
export function observeChildren(host, callback, { ignore = null } = {}) {
  const inside = (node) => (node instanceof Element ? node : node.parentElement)?.closest(ignore) != null;
  const observer = new MutationObserver((records, self) => {
    if (ignore && records.every((record) => inside(record.target))) return;
    callback(records, self);
  });
  observer.observe(host, { childList: true, subtree: true });
  callback();
  return observer;
}

/**
 * True when a modal dialog is open and does not contain the element: the page behind it is not available, though it
 * is not marked inert (a modal dialog blocks it without the attribute).
 */
export function behindModal(element) {
  for (const dialog of document.querySelectorAll('dialog:modal')) {
    if (!dialog.contains(element)) return true;
  }
  return false;
}

/** Moves focus within items with arrow keys, Home, End and first-letter typeahead. Returns true when handled. */
export function rovingKey(event, items, current, { vertical = true } = {}) {
  if (items.length === 0) return false;
  const index = items.indexOf(current);
  const next = vertical ? 'ArrowDown' : 'ArrowRight';
  const previous = vertical ? 'ArrowUp' : 'ArrowLeft';
  let target = null;
  if (event.key === next) target = items[(index + 1) % items.length];
  else if (event.key === previous) target = items[(index - 1 + items.length) % items.length];
  else if (event.key === 'Home') target = items[0];
  else if (event.key === 'End') target = items[items.length - 1];
  else if (event.key.length === 1 && /\S/.test(event.key)) {
    const letter = event.key.toLowerCase();
    const ordered = [...items.slice(index + 1), ...items.slice(0, index + 1)];
    target = ordered.find((item) => item.textContent.trim().toLowerCase().startsWith(letter)) ?? null;
  }
  if (!target) return false;
  event.preventDefault();
  target.focus();
  return true;
}
