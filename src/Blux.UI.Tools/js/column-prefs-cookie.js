import { basePathFor, cookiePathFor, scopedCookieName } from '../../Blux.UI/js/base-path.js';

// The column preferences of every data list on the app (visibility, order, pinning, widths), grouped into one cookie per path base so the server can read them at the first paint
// without growing the Cookie header with one cookie per list. Named like the other Blux cookies (BxPathScope.CookieName): "blux-dl-cols" at the root, "blux-dl-cols.{slug}~~"
// below a path base, whose path the cookie is scoped to. SameSite=Lax, a year, Secure on https. In a Blazor Hybrid web view (MAUI) there is no server to read a cookie and cookies
// are unreliable there (the tracking prevention of WebKit), so the same grouped value is kept in localStorage under the same name.
//
// Entries are stored as "<key>.<v1!...>" separated by "!!", most recently updated first (LRU). Each list entry is capped at 600 chars, at most 32 lists are kept, and the whole
// grouped cookie is capped at 3000 chars (~3 KB), evicting the oldest entries when full. The value holds letters, digits and . _ - ! ~ * ' only, never %, + , ; or a space.
const COOKIE_NAME = 'blux-dl-cols';
const MAX_ENTRY_LENGTH = 600;
const MAX_COOKIE_LENGTH = 3000;
const MAX_ENTRIES = 32;
const SAFE_KEY = /^[A-Za-z0-9_-]{1,64}$/;
const SAFE_VALUE = /^v1!(?:[A-Za-z0-9._~*'-]+!)*[A-Za-z0-9._~*'-]+$/;
const SAFE_COOKIE = /^[A-Za-z0-9._~!*'-]{1,3000}$/;
const ONE_YEAR = 365 * 24 * 60 * 60;

function nameOf(doc) {
  const hasBase = !!doc.querySelector?.('base[href]');
  return {
    name: scopedCookieName(basePathFor(doc.baseURI, hasBase), COOKIE_NAME),
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

function readRawGroup(doc, info, storage) {
  if (webView(doc)) {
    try {
      return storageOf(storage)?.getItem(info.name) ?? null;
    } catch {
      return null;
    }
  }
  if (!doc.cookie) return null;
  const prefix = info.name + '=';
  const entry = doc.cookie.split('; ').find((part) => part.startsWith(prefix));
  return entry ? entry.slice(prefix.length) : null;
}

function parseEntries(raw) {
  if (!raw || !SAFE_COOKIE.test(raw)) return [];
  const entries = [];
  const seen = new Set();
  for (const part of raw.split('!!')) {
    if (!part) continue;
    const dot = part.indexOf('.');
    if (dot <= 0) continue;
    const k = part.slice(0, dot);
    const v = part.slice(dot + 1);
    if (!SAFE_KEY.test(k) || seen.has(k)) continue;
    if (!v || v.length > MAX_ENTRY_LENGTH || !SAFE_VALUE.test(v)) continue;
    seen.add(k);
    entries.push([k, v]);
    if (entries.length >= MAX_ENTRIES) break;
  }
  return entries;
}

function serializeEntries(entries) {
  return entries.map(([k, v]) => `${k}.${v}`).join('!!');
}

/**
 * Reads the column preferences of a list key from the grouped cookie, or null when absent or not a value this module writes.
 * @param {{ cookie: string, baseURI?: string, querySelector?: (selector: string) => unknown, documentElement?: { getAttribute?: (name: string) => string | null } }} doc
 * @param {string | null | undefined} key
 * @param {{ getItem: (name: string) => string | null } | null} [storage] localStorage, for a web view
 * @returns {string | null}
 */
export function readColumnPrefs(doc, key, storage) {
  if (!key || !SAFE_KEY.test(key)) return null;
  const info = nameOf(doc);
  const raw = readRawGroup(doc, info, storage);
  const found = parseEntries(raw).find(([k]) => k === key);
  return found ? found[1] : null;
}

/**
 * Writes (or removes when value is empty) the column preferences of a list key in the grouped cookie, promoting the key to the front (LRU)
 * and evicting the oldest entries if the grouped value would pass 3000 characters or 32 lists.
 * @param {{ cookie: string, baseURI?: string, querySelector?: (selector: string) => unknown, documentElement?: { getAttribute?: (name: string) => string | null } }} doc
 * @param {string | null | undefined} key
 * @param {string | null | undefined} value
 * @param {string} [protocol] "https:" adds Secure
 * @param {{ getItem?: (name: string) => string | null, setItem: (name: string, value: string) => void, removeItem: (name: string) => void } | null} [storage] localStorage, for a web view
 * @returns {boolean} whether the value was written (or removed)
 */
export function writeColumnPrefs(doc, key, value, protocol = 'http:', storage) {
  if (!key || !SAFE_KEY.test(key)) return false;
  if (value && (value.length > MAX_ENTRY_LENGTH || !SAFE_VALUE.test(value))) return false;
  const info = nameOf(doc);
  const entries = parseEntries(readRawGroup(doc, info, storage)).filter(([k]) => k !== key);
  if (value) {
    entries.unshift([key, value]);
    while (entries.length > MAX_ENTRIES) entries.pop();
    while (entries.length > 1 && serializeEntries(entries).length > MAX_COOKIE_LENGTH) entries.pop();
  }
  const serialized = serializeEntries(entries);
  if (serialized.length > MAX_COOKIE_LENGTH) return false;
  if (webView(doc)) {
    try {
      const store = storageOf(storage);
      if (!store) return false;
      if (serialized) store.setItem(info.name, serialized);
      else store.removeItem(info.name);
      return true;
    } catch {
      return false;
    }
  }
  const secure = protocol === 'https:' ? '; Secure' : '';
  if (!serialized) {
    doc.cookie = `${info.name}=; path=${info.path}; max-age=0; SameSite=Lax${secure}`;
    return true;
  }
  doc.cookie = `${info.name}=${serialized}; path=${info.path}; max-age=${ONE_YEAR}; SameSite=Lax${secure}`;
  return true;
}
