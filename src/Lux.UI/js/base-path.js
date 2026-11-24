/**
 * The path the app lives under, free of the DOM so Node can test it. A Blazor app deployed below a path base
 * (UsePathBase("/x") with <base href="/x/">) shares its origin with other apps. Cookies and localStorage are shared with them
 * too, so what an app remembers is scoped to its base path: a cookie by its path and its name (a server keeps the last of two
 * cookies of the same name), a storage key by a prefix. An app at the root keeps the plain names and keys.
 * The C# side of the same rules is BxPathScope.
 */

/**
 * The path of the app, without a trailing slash: "" at the root. Only an explicit <base href> makes a base: without one,
 * document.baseURI is the URL of the page and a page at /dashboard/settings would not be an app below that path.
 * @param {string | null | undefined} baseUri document.baseURI
 * @param {boolean} hasBase whether the document has a <base href> element
 * @returns {string}
 */
export function basePathFor(baseUri, hasBase = true) {
  if (!hasBase || !baseUri) return '';
  let pathname;
  try {
    pathname = new URL(baseUri).pathname;
  } catch {
    return '';
  }
  try {
    pathname = decodeURIComponent(pathname);
  } catch {
    // Keep the encoded form.
  }
  const trimmed = pathname.replace(/\/+$/, '');
  return trimmed.startsWith('/') ? trimmed : '';
}

/**
 * The cookie path of an app: its base path, "/" at the root. "/x" matches /x and everything below it, but not /xy.
 * @param {string | null | undefined} baseUri
 * @param {boolean} hasBase
 * @returns {string}
 */
export function cookiePathFor(baseUri, hasBase = true) {
  const base = basePathFor(baseUri, hasBase);
  // A path with ";" or a control character would end the attribute early: do not scope to it.
  if (/[;\u0000-\u001f\u007f]/.test(base)) return '/';
  return base ? encodeURI(base) : '/';
}

/**
 * The base path as a token for a cookie name: segments joined by ".", every character outside A-Z a-z 0-9 _ - written as "~" and
 * the hex of its UTF-8 bytes, so two paths never give the same token and the name has no "/" or ";". "" at the root.
 * @param {string} basePath
 */
export function slugFor(basePath) {
  const encoder = new TextEncoder();
  return basePath
    .split('/')
    .filter(Boolean)
    .map((segment) =>
      [...segment]
        .map((ch) => (/[A-Za-z0-9_-]/.test(ch) ? ch : [...encoder.encode(ch)].map((b) => '~' + b.toString(16).padStart(2, '0')).join('')))
        .join(''))
    .join('.');
}

/**
 * The first part of the name of a sidebar cookie: "blux-sidebar-" at the root, so root apps keep their names, and
 * "blux-sidebar.{slug}~~" below a path base ("~~" never occurs in a slug, so no id can make two apps share a name).
 * @param {string} basePath
 */
export function sidebarCookiePrefix(basePath) {
  return basePath ? `blux-sidebar.${slugFor(basePath)}~~` : 'blux-sidebar-';
}

/**
 * The name of a cookie kept under `name`: the name at the root, "{name}.{slug}~~" below a path base (the rule of BxPathScope.CookieName).
 * @param {string} basePath
 * @param {string} name
 */
export function scopedCookieName(basePath, name) {
  return basePath ? `${name}.${slugFor(basePath)}~~` : name;
}

/**
 * The localStorage key of an app: the key itself at the root, prefixed with the base path below one.
 * @param {string} basePath
 * @param {string} key
 */
export function scopedKey(basePath, key) {
  return basePath ? `${basePath}:${key}` : key;
}

/** The base path of the current document. */
export function appBasePath() {
  if (typeof document === 'undefined') return '';
  return basePathFor(document.baseURI, !!document.querySelector('base[href]'));
}

/** The cookie path of the current document. */
export function appCookiePath() {
  if (typeof document === 'undefined') return '/';
  return cookiePathFor(document.baseURI, !!document.querySelector('base[href]'));
}

/** The localStorage key for this app (see scopedKey). The color mode is the exception: it is shared on purpose. */
export function appStorageKey(key) {
  return scopedKey(appBasePath(), key);
}
