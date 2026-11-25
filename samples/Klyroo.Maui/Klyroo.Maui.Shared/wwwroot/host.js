// Small helpers of the sample, imported by the shared components.

/** The user agent of the web view: its Chrome version tells what the page can use (the popover API needs 114 or later). */
export function userAgent() {
  return navigator.userAgent;
}

/**
 * Calls back with true when the page is dark, now and every time the color mode changes (Klyroo dispatches kly-color-mode).
 * Returns an object whose dispose() removes the listener: call it when the component goes away.
 */
export function watchDark(dotnet) {
  const send = () => dotnet.invokeMethodAsync('OnMode', document.documentElement.classList.contains('dark')).catch(() => {});
  document.addEventListener('kly-color-mode', send);
  send();
  return { dispose: () => document.removeEventListener('kly-color-mode', send) };
}
