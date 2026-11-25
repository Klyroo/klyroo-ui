// Small helpers of the layout, imported by the shared components.

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

/** The value of a cookie of the page, or null. The names the app uses are plain (letters, dashes, dots and tildes), so they are compared as they are. */
export function getCookie(name) {
  const prefix = name + '=';
  const found = document.cookie.split('; ').find((c) => c.startsWith(prefix));
  return found ? decodeURIComponent(found.slice(prefix.length)) : null;
}

/**
 * Writes a cookie for the path of the app: SameSite=Lax, Secure on https, 400 days. The server reads it while it renders, which is why it is a cookie and not local storage.
 * The value is a palette name; it is checked against the offered names when it is read.
 */
export function setCookie(name, value, path) {
  const secure = location.protocol === 'https:' ? '; secure' : '';
  document.cookie = `${name}=${encodeURIComponent(value)}; path=${path}; max-age=34560000; samesite=lax${secure}`;
}

/** The IANA time zone of the browser ("Europe/Paris"), or null when it cannot be read. */
export function timeZone() {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone ?? null;
  } catch {
    return null;
  }
}
