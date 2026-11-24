import { isHybrid } from './host.js';
import { appStorageKey } from './base-path.js';

/**
 * What the server decided about the culture, for a WebAssembly client that has no request (see
 * LuxCulture.ApplyServerCultureAsync, which parses and validates it). A three item array, [meta, cookie, lang],
 * where a missing value is null:
 * - the content of the lux-culture meta element the server rendered (LxCultureMeta), "c=en-GB|uic=en";
 * - the decoded value of the ASP.NET Core culture cookie ("c=fr|uic=fr", which LxLocaleSelect writes);
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
  const meta = document.head.querySelector('meta[name="lux-culture"]')?.getAttribute('content') || null;
  return [meta, cookie, document.documentElement.getAttribute('lang') || null];
}

/** The culture a Blazor Hybrid app remembered (LxLocalStorageCultureStore), or null. localStorage works in every web view. */
export function getStoredCulture(key = 'lux-culture') {
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
