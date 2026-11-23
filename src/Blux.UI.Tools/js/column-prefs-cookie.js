import { basePathFor, cookiePathFor, scopedCookieName } from '../../Blux.UI/js/base-path.js';

// The column preferences of a data list (visibility, order, pinning, widths), kept where the server can read them at the first paint.
// Named like the other Blux cookies (BxPathScope.CookieName): "blux-dl-cols-<key>" at the root, "blux-dl-cols-<key>.{slug}~~" below a path base, whose path the cookie is scoped to.
// SameSite=Lax, a year, Secure on https. In a Blazor Hybrid web view (MAUI) there is no server to read a cookie and cookies are unreliable there (the tracking prevention of WebKit),
// so the same name is kept in localStorage, like the sidebar and the layout of a dashboard do.
//
// The value is the one DataListColumnPreferences writes: letters, digits and . _ - ! ~ * ' only, never %, + , ; or a space, which a server would change on the way in. Anything else
// is refused on the way out and ignored on the way in, so a hand-edited cookie cannot put another character into the page or into a request.
const PREFIX = 'blux-dl-cols-';
const SAFE_KEY = /^[A-Za-z0-9_-]{1,64}$/;
const SAFE_VALUE = /^[A-Za-z0-9._~!*'-]{1,2000}$/;
const ONE_YEAR = 365 * 24 * 60 * 60;

function nameOf(doc, key) {
  if (!key || !SAFE_KEY.test(key)) return null;
  const hasBase = !!doc.querySelector?.('base[href]');
  return {
    name: scopedCookieName(basePathFor(doc.baseURI, hasBase), PREFIX + key),
    path: cookiePathFor(doc.baseURI, hasBase),
  };
}

function webView(doc) {
  try {
    return doc.documentElement?.getAttribute?.('data-bx-host') === 'webview';
  } catch {
    return false;
  }
}

function storageOf(storage) {
  try {
    return storage ?? globalThis.localStorage ?? null;
  } catch {
    return null;
  }
}

/**
 * Reads the column preferences of a list key, or null when absent or not a value this module writes.
 * @param {{ cookie: string, baseURI?: string, querySelector?: (selector: string) => unknown, documentElement?: { getAttribute?: (name: string) => string | null } }} doc
 * @param {string | null | undefined} key
 * @param {{ getItem: (name: string) => string | null } | null} [storage] localStorage, for a web view
 * @returns {string | null}
 */
export function readColumnPrefs(doc, key, storage) {
  const info = nameOf(doc, key);
  if (!info) return null;
  let value = null;
  if (webView(doc)) {
    try {
      value = storageOf(storage)?.getItem(info.name) ?? null;
    } catch {
      value = null;
    }
  } else if (doc.cookie) {
    const prefix = info.name + '=';
    const entry = doc.cookie.split('; ').find((part) => part.startsWith(prefix));
    value = entry ? entry.slice(prefix.length) : null;
  }
  return value !== null && SAFE_VALUE.test(value) ? value : null;
}

/**
 * Writes (or removes when value is empty) the column preferences.
 * @param {{ cookie: string, baseURI?: string, querySelector?: (selector: string) => unknown, documentElement?: { getAttribute?: (name: string) => string | null } }} doc
 * @param {string | null | undefined} key
 * @param {string | null | undefined} value
 * @param {string} [protocol] "https:" adds Secure
 * @param {{ setItem: (name: string, value: string) => void, removeItem: (name: string) => void } | null} [storage] localStorage, for a web view
 * @returns {boolean} whether the value was written (or removed)
 */
export function writeColumnPrefs(doc, key, value, protocol = 'http:', storage) {
  const info = nameOf(doc, key);
  if (!info) return false;
  if (value && !SAFE_VALUE.test(value)) return false;
  if (webView(doc)) {
    try {
      const store = storageOf(storage);
      if (!store) return false;
      if (value) store.setItem(info.name, value);
      else store.removeItem(info.name);
      return true;
    } catch {
      return false;
    }
  }
  const secure = protocol === 'https:' ? '; Secure' : '';
  if (!value) {
    doc.cookie = `${info.name}=; path=${info.path}; max-age=0; SameSite=Lax${secure}`;
    return true;
  }
  doc.cookie = `${info.name}=${value}; path=${info.path}; max-age=${ONE_YEAR}; SameSite=Lax${secure}`;
  return true;
}
