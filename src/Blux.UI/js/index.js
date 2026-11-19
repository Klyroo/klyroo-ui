// Blazor JS initializer of Blux.UI: loaded automatically by blazor.web.js (Blazor Web App)
// or blazor.webassembly.js / blazor.server.js (standalone hosting).
import './popover.js';
import './modal.js';
import './tooltip.js';
import './tabs.js';
import './accordion.js';
import './select.js';
import './collapsible.js';
import './tree.js';
import './splitter.js';
import './scroll-area.js';
import './context-menu.js';
import './command-palette.js';
import './color-mode-controls.js';
import './hotkey.js';
import './banner.js';
import './calendar.js';
import './date-field.js';
import './input-time.js';
import './color-picker.js';
import './file-upload.js';
import './carousel.js';
import './form2.js';
import './layout.js';
import './dashboard.js';
import './marketing.js';
import './chat.js';
import './editor.js';
import './chart.js';
import './content.js';
import { toast } from './toaster.js';
import { applyColorMode, getColorMode, setColorMode } from './color-mode.js';

globalThis.Blux = Object.assign(globalThis.Blux ?? {}, { setColorMode, getColorMode, toast });

// Avatar images that fail to load are hidden so the initials underneath show. "error" does not bubble: capture it.
// Images that failed before this script loaded are caught by the scan.
function hideBrokenAvatars() {
  for (const img of document.querySelectorAll('img[data-bx-avatar-image]')) {
    if (img.complete && img.naturalWidth === 0) img.hidden = true;
  }
}
document.addEventListener(
  'load',
  (event) => {
    if (event.target instanceof HTMLImageElement && event.target.hasAttribute('data-bx-avatar-image')) event.target.hidden = false;
  },
  true,
);
document.addEventListener(
  'error',
  (event) => {
    if (event.target instanceof HTMLImageElement && event.target.hasAttribute('data-bx-avatar-image')) event.target.hidden = true;
  },
  true,
);
hideBrokenAvatars();
// Blazor sets src before inserting the image; a cached failure can fire "error" while the image is still detached,
// out of reach of the document listener. So check images as they are inserted too.
new MutationObserver((mutations) => {
  for (const mutation of mutations) {
    for (const node of mutation.addedNodes) {
      if (node.nodeType !== Node.ELEMENT_NODE) continue;
      const images = node.matches('img[data-bx-avatar-image]') ? [node] : node.querySelectorAll('img[data-bx-avatar-image]');
      for (const img of images) if (img.complete && img.naturalWidth === 0) img.hidden = true;
    }
  }
}).observe(document.documentElement, { childList: true, subtree: true });

let eventsRegistered = false;

function registerEvents(blazor) {
  if (eventsRegistered) return;
  eventsRegistered = true;
  blazor.registerCustomEventType('bxtoggle', {
    browserEventName: 'bx-toggle',
    createEventArgs: (event) => ({ open: event.detail.open, sequence: event.detail.sequence }),
  });
  blazor.registerCustomEventType('bxchange', {
    browserEventName: 'bx-change',
    createEventArgs: (event) => ({ value: event.detail.value, sequence: event.detail.sequence }),
  });
}

// Blazor Web App.
export function afterWebStarted(blazor) {
  registerEvents(blazor);
  blazor.addEventListener('enhancedload', () => {
    applyColorMode();
    hideBrokenAvatars();
    // Enhanced navigation merges server HTML over attributes the elements set: let them re-apply.
    // Every Blux element re-applies what the merge reset; no list to keep in sync with the components.
    for (const el of document.querySelectorAll('*')) {
      if (el.localName.startsWith('bx-') && typeof el.refresh === 'function') el.refresh();
    }
  });
}

// Standalone WebAssembly or Server hosting.
export function afterStarted(blazor) {
  registerEvents(blazor);
}
