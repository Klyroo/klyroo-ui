// Blazor JS initializer of Blux.UI.Tools, a second one next to the one of Blux.UI: Blazor loads the initializer of every Razor class
// library of the app (Blazor Web App, standalone WebAssembly or Server hosting, and Blazor Hybrid web views) from _content/<package>/<package>.lib.module.js.
// The custom events (bx-change, bx-toggle) are registered by Blux.UI: this file registers none, and the elements of Tools report through them.
//
// A helper of Blux.UI can be imported from source and is bundled here, for example:
//   import { Reporter, ensureId } from '../../Blux.UI/js/shared.js';

const tools = { version: 1 };

// Merged into the same global as Blux.UI: either initializer may run first.
globalThis.Blux = Object.assign(globalThis.Blux ?? {}, { tools });

/** What an enhanced navigation merge resets: every element of Tools re-applies it (the elements are named bx-data-list-*). */
function refreshElements() {
  for (const el of document.querySelectorAll('*')) {
    if (el.localName.startsWith('bx-data-list') && typeof el.refresh === 'function') el.refresh();
  }
}

// Blazor Web App.
export function afterWebStarted(blazor) {
  blazor.addEventListener('enhancedload', refreshElements);
}

// Standalone WebAssembly or Server hosting, and Blazor Hybrid: no enhanced navigation, nothing to merge.
export function beforeStart() {}
export function afterStarted() {}
