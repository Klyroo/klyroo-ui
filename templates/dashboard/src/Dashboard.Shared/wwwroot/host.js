// Small helpers of the layout, imported by the shared components.

/**
 * Calls back with true when the page is dark, now and every time the color mode changes (Blux dispatches bx-color-mode).
 * Returns an object whose dispose() removes the listener: call it when the component goes away.
 */
export function watchDark(dotnet) {
  const send = () => dotnet.invokeMethodAsync('OnMode', document.documentElement.classList.contains('dark')).catch(() => {});
  document.addEventListener('bx-color-mode', send);
  send();
  return { dispose: () => document.removeEventListener('bx-color-mode', send) };
}

/** The IANA time zone of the browser ("Europe/Paris"), or null when it cannot be read. */
export function timeZone() {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone ?? null;
  } catch {
    return null;
  }
}
