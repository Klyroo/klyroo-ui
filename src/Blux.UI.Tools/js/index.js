// Blazor JS initializer of Blux.UI.Tools, a second one next to the one of Blux.UI: Blazor loads the initializer of every Razor class
// library of the app (Blazor Web App, standalone WebAssembly or Server hosting, and Blazor Hybrid web views) from _content/<package>/<package>.lib.module.js.
// The custom events (bx-change, bx-toggle) are registered by Blux.UI: this file registers none, and the elements of Tools report through them.
//
// A helper of Blux.UI can be imported from source and is bundled here, for example:
//   import { Reporter, ensureId } from '../../Blux.UI/js/shared.js';
import { timeZone, storeTimeZone } from './time-zone-cookie.js';

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

// Puts the focus in the control with this id (the search field after its "clear" action removed the button that had it).
function focusId(id) {
  document.getElementById(id)?.focus();
}

const tools = { version: 1, timeZone, focusPage, focusId };

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
