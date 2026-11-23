import { basePathFor, cookiePathFor, scopedCookieName } from '../../Blux.UI/js/base-path.js';

// Column preferences cookie: stores column visibility, order, pinning and widths so the very first SSR / prerender paint matches the user's setup.
// Named "blux-dl-cols-<key>" at the root, scoped below a path base via scopedCookieName.
const PREFIX = 'blux-dl-cols-';
const SAFE_KEY = /^[A-Za-z0-9_-]{1,64}$/;
const SAFE_VALUE = /^[A-Za-z0-9._~!*'()%-]{1,2048}$/;
const ONE_YEAR = 365 * 24 * 60 * 60;

function cookieName(doc, key) {
  if (!key || !SAFE_KEY.test(key)) return null;
  const hasBase = !!doc.querySelector?.('base[href]');
  return {
    name: scopedCookieName(basePathFor(doc.baseURI, hasBase), PREFIX + key),
    path: cookiePathFor(doc.baseURI, hasBase),
  };
}

/**
 * Reads the column preferences cookie for a list key, or null when absent or invalid.
 * @param {{ cookie: string, baseURI?: string, querySelector?: (selector: string) => unknown }} doc
 * @param {string | null | undefined} key
 * @returns {string | null}
 */
export function readColumnPrefs(doc, key) {
  const info = cookieName(doc, key);
  if (!info || !doc.cookie) return null;
  const prefix = info.name + '=';
  const entry = doc.cookie.split('; ').find((part) => part.startsWith(prefix));
  if (!entry) return null;
  const value = entry.slice(prefix.length);
  return SAFE_VALUE.test(value) ? value : null;
}

/**
 * Writes (or removes when value is empty) the column preferences cookie (SameSite=Lax, 365 days, scoped to path base).
 * @param {{ cookie: string, baseURI?: string, querySelector?: (selector: string) => unknown }} doc
 * @param {string | null | undefined} key
 * @param {string | null | undefined} value
 * @param {string} protocol
 * @returns {boolean}
 */
export function writeColumnPrefs(doc, key, value, protocol = 'http:') {
  const info = cookieName(doc, key);
  if (!info) return false;
  const secure = protocol === 'https:' ? '; Secure' : '';
  if (!value) {
    doc.cookie = `${info.name}=; path=${info.path}; max-age=0; SameSite=Lax${secure}`;
    return true;
  }
  if (!SAFE_VALUE.test(value)) return false;
  doc.cookie = `${info.name}=${value}; path=${info.path}; max-age=${ONE_YEAR}; SameSite=Lax${secure}`;
  return true;
}
