import { isHybrid } from './host.js';
import { appStorageKey } from './base-path.js';

/**
 * What the server decided about the culture, for a WebAssembly client that has no request (see
 * BluxCulture.ApplyServerCultureAsync, which parses and validates it). A three item array, [meta, cookie, lang],
 * where a missing value is null:
 * - the content of the blux-culture meta element the server rendered (BxCultureMeta), "c=en-GB|uic=en";
 * - the decoded value of the ASP.NET Core culture cookie ("c=fr|uic=fr", which BxLocaleSelect writes);
 * - the lang attribute the server wrote on the html element (the locale it rendered).
 */
export function serverCulture(cookieName = '.AspNetCore.Culture') {
  // A Blazor Hybrid web view has no server that decided anything: its lang attribute is the one of a static index.html.
  if (isHybrid()) return [null, null, null];
  let cookie = null;
  try {
    for (const part of document.cookie.split(';')) {
      const [name, ...rest] = part.trim().split('=');
      if (name !== cookieName) continue;
      cookie = decodeURIComponent(rest.join('='));
      break;
    }
  } catch {
    // Cookies blocked or malformed: the other sources still tell.
  }
  const meta = document.head.querySelector('meta[name="blux-culture"]')?.getAttribute('content') || null;
  return [meta, cookie, document.documentElement.getAttribute('lang') || null];
}

/** The culture a Blazor Hybrid app remembered (BxLocalStorageCultureStore), or null. localStorage works in every web view. */
export function getStoredCulture(key = 'blux-culture') {
  try {
    return localStorage.getItem(appStorageKey(key));
  } catch {
    return null;
  }
}

export function setStoredCulture(key, value) {
  try {
    localStorage.setItem(appStorageKey(key), value);
  } catch {
    // Storage blocked: the choice lasts until the app restarts.
  }
}
