/**
 * Pure scheduling pieces of the text field's browser-side commit (input-commit.js), kept free of the DOM so they can be
 * tested with fake timers.
 */

/**
 * A trailing debounce: schedule(ms) restarts the quiet period, and callback runs once when it ends. flush() runs the
 * callback now if a run is pending (and returns true), cancel() drops it. `timers` is replaceable for tests.
 */
export function createDebouncer(callback, timers = { set: (fn, ms) => setTimeout(fn, ms), clear: (id) => clearTimeout(id) }) {
  let handle = null;
  const fire = () => {
    handle = null;
    callback();
  };
  return {
    schedule(ms) {
      if (handle !== null) timers.clear(handle);
      handle = timers.set(fire, ms);
    },
    cancel() {
      if (handle === null) return;
      timers.clear(handle);
      handle = null;
    },
    flush() {
      if (handle === null) return false;
      timers.clear(handle);
      fire();
      return true;
    },
    get pending() {
      return handle !== null;
    },
  };
}

/**
 * The delay a field uses, from its data-lx-debounce attribute: a number of milliseconds (0 reports on every input), or
 * null when the attribute is missing or not a non-negative number, which means "report when the value is committed".
 */
export function parseDelay(raw) {
  if (raw === null || raw === undefined || String(raw).trim() === '') return null;
  const n = Number(raw);
  return Number.isFinite(n) && n >= 0 ? n : null;
}

/**
 * Whether a value arriving from a render must be ignored because it would undo what the user typed: text not reported
 * yet (`dirty`, which covers a pending quiet period), an IME composition in progress, or a render computed before .NET
 * received the latest report (`stale`).
 */
export function holdsTypedText({ dirty, composing, stale }) {
  return dirty || composing || stale;
}
