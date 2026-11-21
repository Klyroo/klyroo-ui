import { basePathFor, cookiePathFor, scopedCookieName } from '../../Blux.UI/js/base-path.js';

// The cookie that tells the server the time zone of the browser, so that a static or prerendered data list cuts its days where the user lives.
// Named like the other Blux cookies (BxPathScope.CookieName): "blux-timezone" at the root, "blux-timezone.{slug}~~" below a path base, whose path it is scoped to.
export const TIME_ZONE_COOKIE = 'blux-timezone';

const SAFE_ID = /^[A-Za-z0-9_/+-]{1,64}$/;
const ONE_YEAR_AND_MORE = 400 * 24 * 60 * 60;

/** The IANA id of the time zone of the browser ("Europe/Paris"), or null. */
export function timeZone() {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || null;
  } catch {
    return null;
  }
}

/**
 * Writes the cookie when it does not already hold this zone (SameSite=Lax, path of the app, about 13 months). A zone that would not be safe in a cookie value is not written.
 * @param {{ cookie: string, baseURI?: string, querySelector?: (selector: string) => unknown }} doc the document
 * @param {string | null} zone
 * @param {string} protocol "https:" adds Secure
 * @returns {boolean} whether the cookie was written
 */
export function storeTimeZone(doc, zone, protocol = 'http:') {
  if (!zone || !SAFE_ID.test(zone)) return false;
  const hasBase = !!doc.querySelector?.('base[href]');
  const name = scopedCookieName(basePathFor(doc.baseURI, hasBase), TIME_ZONE_COOKIE);
  const current = doc.cookie.split('; ').find((part) => part.startsWith(name + '='));
  if (current === `${name}=${zone}`) return false;
  const secure = protocol === 'https:' ? '; Secure' : '';
  doc.cookie = `${name}=${zone}; path=${cookiePathFor(doc.baseURI, hasBase)}; max-age=${ONE_YEAR_AND_MORE}; SameSite=Lax${secure}`;
  return true;
}
