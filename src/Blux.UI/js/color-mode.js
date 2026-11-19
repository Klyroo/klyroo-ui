const STORAGE_KEY = 'blux-color-mode';
const media = matchMedia('(prefers-color-scheme: dark)');

function storedMode() {
  try {
    return localStorage.getItem(STORAGE_KEY) || 'system';
  } catch {
    return 'system';
  }
}

/** Applies the stored mode to <html>. Also called after enhanced navigation, which may rewrite <html>. */
export function applyColorMode() {
  const mode = storedMode();
  const dark = mode === 'dark' || (mode === 'system' && media.matches);
  document.documentElement.classList.toggle('dark', dark);
  document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
  // Controls that show the mode (switch, select) listen to this to stay in sync.
  document.dispatchEvent(new CustomEvent('bx-color-mode', { detail: { mode, dark } }));
}

/** @param {'light' | 'dark' | 'system'} mode */
export function setColorMode(mode) {
  try {
    localStorage.setItem(STORAGE_KEY, mode);
  } catch {
    // Storage blocked: the mode still applies to this page.
  }
  applyColorMode();
}

export function getColorMode() {
  return storedMode();
}

media.addEventListener('change', () => {
  if (storedMode() === 'system') applyColorMode();
});
