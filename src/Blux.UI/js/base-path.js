/**
 * The path the app lives under, free of the DOM so Node can test it. A Blazor app deployed below a path base
 * (UsePathBase("/x") with <base href="/x/">) shares its origin with other apps, and a cookie written with path=/ is
 * read and overwritten by all of them.
 */

/**
 * The cookie path for an app whose document base is baseUri (document.baseURI, which follows <base href>): the path of
 * the base without its trailing slash, "/" for an app at the root. "/x" matches /x and everything below it, but not /xy.
 * @param {string | null | undefined} baseUri
 * @returns {string}
 */
export function cookiePathFor(baseUri) {
  if (!baseUri) return '/';
  let pathname;
  try {
    pathname = new URL(baseUri).pathname;
  } catch {
    return '/';
  }
  // A path with ";" or a control character would end the attribute early: do not scope to it.
  if (/[;\u0000-\u001f\u007f]/.test(pathname)) return '/';
  const trimmed = pathname.replace(/\/+$/, '');
  return trimmed.startsWith('/') ? trimmed : '/';
}

/** The cookie path of the current document. */
export function appCookiePath() {
  return cookiePathFor(typeof document === 'undefined' ? null : document.baseURI);
}
