// Blazor JS initializer of Blux.UI.Tools, a second one next to the one of Blux.UI: Blazor loads the initializer of every Razor class
// library of the app (Blazor Web App, standalone WebAssembly or Server hosting, and Blazor Hybrid web views) from _content/<package>/<package>.lib.module.js.
// The custom events (bx-change, bx-toggle) are registered by Blux.UI: this file registers none, and the elements of Tools report through them.
//
// A helper of Blux.UI can be imported from source and is bundled here, for example:
//   import { Reporter, ensureId } from '../../Blux.UI/js/shared.js';
import { timeZone, storeTimeZone } from './time-zone-cookie.js';
import { pruneBlank } from './prune-blank.js';
import { readColumnPrefs as readColumnPrefsCookie, writeColumnPrefs as writeColumnPrefsCookie } from './column-prefs-cookie.js';
import { attachColumnResize } from './column-resize.js';
import { attachColumnPanelDrag } from './column-panel-drag.js';
import { keepVirtualFocus } from './virtual-focus.js';

// The time zone of the browser goes to the server in a cookie, which the next static or prerendered render reads (the first visit has none and cuts days in UTC).
function rememberTimeZone() {
  if (typeof document === 'undefined') return;
  try {
    storeTimeZone(document, timeZone(), location.protocol);
  } catch {
    // Cookies refused: the server keeps cutting days in UTC until the interactive render knows the zone.
  }
}

rememberTimeZone();

// After a page change the control that was pressed can be gone or disabled (the "next" button on the last page): focus goes to the current page.
// A disabled button keeps document.activeElement until the browser fixes focus up, so a disabled one does not count as focused.
function focusPage(root) {
  if (!root) return;
  const active = document.activeElement;
  const usable = active && root.contains(active) && !active.disabled && active.getAttribute('aria-disabled') !== 'true';
  if (usable) return;
  root.querySelector('[aria-current="page"]')?.focus();
}

// The control that was pressed is going away (the "back to start" of a list that was entered mid-way): the table scrolls back to its top and takes the focus as it starts again from its first page.
function focusTable(part) {
  const table = part?.closest('[data-slot=root]')?.querySelector('[data-slot=table-root]');
  if (!table) return;
  table.scrollTop = 0;
  table.focus();
}

// Puts the focus in the control with this id (the search field after its "clear" action removed the button that had it).
function focusId(id) {
  document.getElementById(id)?.focus();
}

// After an action emptied the selection its button is gone with the bar, and the focus fell to the page: it goes to the select-all box of the list (the start of its table).
// Only when the focus is lost: a button the user moved to meanwhile keeps it.
function focusSelectAll(part) {
  const root = part?.closest('[data-slot=root]');
  const lost = () => {
    const active = document.activeElement;
    return !active || active === document.body || !active.isConnected || active.getClientRects().length === 0;
  };
  const give = () => (root?.querySelector('input[data-select=all]:not(:disabled)') ?? root?.querySelector('[data-slot=table-root]'))?.focus();

  // Later than the render that removed the bar and than the close event of a confirmation dialog, which gives the focus back to the button that opened it.
  requestAnimationFrame(() => setTimeout(() => {
    if (lost()) return give();
    if (!root) return;
    // The focus is still on a control, usually the button of the bar: the render that removes it can reach the browser after this call (a render batch waits for the
    // one before it to be acknowledged, a call does not). The control going away is what to wait for, unless the user moves the focus or acts first.
    const from = document.activeElement;
    const stop = () => {
      observer.disconnect();
      document.removeEventListener('pointerdown', stop, true);
      document.removeEventListener('keydown', stop, true);
    };
    const observer = new MutationObserver(() => {
      if (!from.isConnected) {
        stop();
        if (lost()) give();
      } else if (document.activeElement !== from) {
        stop();
      }
    });
    observer.observe(root, { childList: true, subtree: true });
    document.addEventListener('pointerdown', stop, true);
    document.addEventListener('keydown', stop, true);
  }, 0));
}

// A row action may remove its row, and the control that had the focus with it. The part calls watchRowRemoval before the action runs, while the row is in the table, and the browser
// does the rest on its own: once the row really leaves the table, and only when the focus was lost with it, the focus goes to the actions of the next row, else to the table.
// The component that started the action can be gone before the action returns (the page renders its rows again), so nothing here waits for it. The watch ends when the row has
// left, when the user presses a key or a pointer (they moved on), or when stopRowRemovalWatch says the row stays (the action failed).
const rowWatches = new WeakMap();

function watchRowRemoval(part) {
  const row = part?.closest('tr, [data-slot=grid-card]');
  const region = part?.closest('[data-slot=table-root], [data-slot=grid-root]');
  if (!row || !region) return;
  rowWatches.get(row)?.();
  const visible = (el) => el.getClientRects().length > 0 && !el.disabled;
  // The button that opens the actions of the row (the menu on a wide screen, the sheet on a phone), else the first control of its actions.
  const anchor = (from) => [...(from?.querySelectorAll('[data-slot=row-actions-trigger], [data-slot=row-actions-phone]') ?? [])].find(visible)
    ?? [...(from?.querySelectorAll('[data-row-action-anchor]') ?? [])].find(visible);
  const next = row.nextElementSibling;
  const lost = () => {
    const active = document.activeElement;
    return !active || active === document.body || !active.isConnected || active.getClientRects().length === 0;
  };
  const stop = () => {
    observer.disconnect();
    document.removeEventListener('pointerdown', stop, true);
    document.removeEventListener('keydown', stop, true);
    if (rowWatches.get(row) === stop) rowWatches.delete(row);
  };
  const observer = new MutationObserver(() => {
    if (row.isConnected) return;
    stop();
    if (!lost()) return;
    // The next row, found now: it is the one that took the place of this one, and it is still in the table when the rows are keyed.
    const target = next?.isConnected ? anchor(next) : null;
    (target ?? region).focus();
  });
  observer.observe(region, { childList: true, subtree: true });
  document.addEventListener('pointerdown', stop, true);
  document.addEventListener('keydown', stop, true);
  rowWatches.set(row, stop);
}

function stopRowRemovalWatch(part) {
  const row = part?.closest('tr, [data-slot=grid-card]');
  if (row) rowWatches.get(row)?.();
}

// Runs before the handler of Blazor (the capture phase of the document), so an enhanced submit and a plain one both leave the blank marked fields out. The fields come back on once the form data was taken.
if (typeof document !== 'undefined') {
  document.addEventListener(
    'submit',
    (event) => {
      if (event.target instanceof HTMLFormElement && event.target.method === 'get') setTimeout(pruneBlank(event.target), 0);
    },
    true,
  );
  attachColumnResize(document);
  attachColumnPanelDrag(document);
}

function readColumnPrefs(key) {
  if (typeof document === 'undefined') return null;
  try {
    return readColumnPrefsCookie(document, key);
  } catch {
    return null;
  }
}

function writeColumnPrefs(key, value) {
  if (typeof document === 'undefined') return false;
  try {
    return writeColumnPrefsCookie(document, key, value, location.protocol);
  } catch {
    return false;
  }
}

// Puts the focus on the control of the column panel that was just used: the render that moved or pinned a column can leave the focus on another column's button, or on one that is now disabled.
// Tries the selectors in order, the first that finds an enabled control wins.
function focusIn(root, ...selectors) {
  if (!root) return;
  for (const selector of selectors) {
    const target = root.querySelector(selector);
    if (target && !target.disabled) {
      target.focus({ preventScroll: false });
      return;
    }
  }
}

// A column pinned to the end sticks next to the row actions column, which is as wide as its buttons: the table root gets that width as --bx-actions-w, and keeps it up to date.
const watched = new WeakSet();
function watchActionsWidth(root) {
  if (!root || watched.has(root) || typeof ResizeObserver === 'undefined') return;
  const cell = root.querySelector('th[data-key=actions]');
  if (!cell) return;
  watched.add(root);
  const update = () => root.style.setProperty('--bx-actions-w', `${Math.round(cell.getBoundingClientRect().width)}px`);
  new ResizeObserver(update).observe(cell);
  update();
}

const tools = { version: 1, timeZone, focusPage, focusId, focusTable, focusSelectAll, watchRowRemoval, stopRowRemovalWatch, keepVirtualFocus, readColumnPrefs, writeColumnPrefs, focusIn, watchActionsWidth };

// Merged into the same global as Blux.UI: either initializer may run first.
globalThis.Blux = Object.assign(globalThis.Blux ?? {}, { tools });

// The custom elements of Tools (none yet): an enhanced navigation merge resets what they set, so each re-applies it. Add the tag names here.
const ELEMENTS = [];

function refreshElements() {
  if (ELEMENTS.length === 0) return;
  for (const el of document.querySelectorAll(ELEMENTS.join(','))) {
    if (typeof el.refresh === 'function') el.refresh();
  }
}

// Blazor Web App.
export function afterWebStarted(blazor) {
  blazor.addEventListener('enhancedload', refreshElements);
  rememberTimeZone();
}

// Standalone WebAssembly or Server hosting, and Blazor Hybrid: no enhanced navigation, nothing to merge.
export function beforeStart() {}
export function afterStarted() {}
