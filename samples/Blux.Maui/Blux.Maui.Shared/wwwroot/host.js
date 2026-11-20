// Small helpers of the sample, imported by the shared components.

/** The user agent of the web view: its Chrome version tells what the page can use (the popover API needs 114 or later). */
export function userAgent() {
  return navigator.userAgent;
}

/** Calls back with true when the page is dark, now and every time the color mode changes (Blux dispatches bx-color-mode). */
export function watchDark(dotnet) {
  const send = () => dotnet.invokeMethodAsync('OnMode', document.documentElement.classList.contains('dark'));
  document.addEventListener('bx-color-mode', send);
  send();
}
