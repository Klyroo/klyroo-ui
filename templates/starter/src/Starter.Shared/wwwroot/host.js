// Small helpers of the layout, imported by the shared components.

/**
 * Calls back with true when the page is dark, now and every time the color mode changes (Lux dispatches lx-color-mode).
 * Returns an object whose dispose() removes the listener: call it when the component goes away.
 */
export function watchDark(dotnet) {
  const send = () => dotnet.invokeMethodAsync('OnMode', document.documentElement.classList.contains('dark')).catch(() => {});
  document.addEventListener('lx-color-mode', send);
  send();
  return { dispose: () => document.removeEventListener('lx-color-mode', send) };
}
