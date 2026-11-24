/**
 * Where the page runs. In a Blazor Hybrid app (BlazorWebView: .NET MAUI, WPF, WinForms) the page is a local web view,
 * not a document served by ASP.NET Core: there is no server to read cookies, prerender a head or enhance navigations.
 *
 * blazor.webview.js talks to the host through window.external.sendMessage / receiveMessage, which every platform's
 * web view provides and no browser does, so that pair tells a web view from a browser. The result is written once as
 * data-lx-host="webview" on <html>, which CSS can also use.
 */
export function detectWebView() {
  try {
    const bridge = globalThis.external;
    return typeof bridge?.sendMessage === 'function' && typeof bridge?.receiveMessage === 'function';
  } catch {
    return false;
  }
}

/** Marks <html> when the page runs in a web view. Idempotent. */
export function markHost() {
  if (detectWebView()) document.documentElement.setAttribute('data-lx-host', 'webview');
}

/** True in a BlazorWebView. */
export function isHybrid() {
  return document.documentElement.getAttribute('data-lx-host') === 'webview';
}
