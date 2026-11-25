import { applyColorMode, getColorMode, setColorMode } from './color-mode.js';

/**
 * Behavior of the color mode controls, wired by attribute so that it works on static pages, before and without a .NET
 * runtime, and for any number of instances on a page:
 *   [data-kly-color-mode="toggle"]  button: switches between light and dark
 *   [data-kly-color-mode="switch"]  checkbox: checked in dark mode
 *   [data-kly-color-mode="select"]  select: system, light or dark
 * The visuals that only depend on light or dark (icons, images, the switch thumb) use the "dark" class in CSS.
 * What CSS cannot know (the checked state, the select value, aria-pressed) is written here every time the mode
 * changes, whoever changed it: a control, Klyroo.setColorMode, the system preference, another tab or enhanced navigation.
 */
const SELECTOR = '[data-kly-color-mode]';

const isDark = () => document.documentElement.classList.contains('dark');

function syncControl(control) {
  switch (control.dataset.klyColorMode) {
    case 'toggle':
      control.setAttribute('aria-pressed', String(isDark()));
      break;
    case 'switch':
      if (control.checked !== isDark()) control.checked = isDark();
      break;
    case 'select': {
      const mode = getColorMode();
      if (control.value !== mode) control.value = mode;
      break;
    }
  }
}

function syncWithin(root) {
  if (root.matches?.(SELECTOR)) syncControl(root);
  for (const control of root.querySelectorAll?.(SELECTOR) ?? []) syncControl(control);
}

const syncAll = () => syncWithin(document);

document.addEventListener('click', (event) => {
  if (!event.target.closest?.('[data-kly-color-mode="toggle"]')) return;
  setColorMode(isDark() ? 'light' : 'dark');
});

document.addEventListener('change', (event) => {
  const control = event.target;
  if (!(control instanceof Element) || !control.matches(SELECTOR)) return;
  if (control.dataset.klyColorMode === 'switch') setColorMode(control.checked ? 'dark' : 'light');
  else if (control.dataset.klyColorMode === 'select') setColorMode(control.value);
});

document.addEventListener('kly-color-mode', syncAll);
// localStorage changed in another tab: apply the stored mode here too, which then syncs the controls.
window.addEventListener('storage', (event) => {
  if (event.key !== 'klyroo-color-mode') return;
  applyColorMode();
});

// Controls rendered later (Blazor inserts them after the script ran).
new MutationObserver((mutations) => {
  for (const mutation of mutations) {
    for (const node of mutation.addedNodes) if (node.nodeType === Node.ELEMENT_NODE) syncWithin(node);
  }
}).observe(document.documentElement, { childList: true, subtree: true });

syncAll();
