/**
 * The UI culture stored in the ASP.NET Core culture cookie ("c=fr|uic=fr", URL encoded), which BxLocaleSelect writes.
 * WebAssembly has no request, so the client reads the cookie to pick the same culture as the server did
 * (see BluxCulture.ApplyCultureCookieAsync). Returns null when there is none.
 */
export function cultureFromCookie(cookieName = '.AspNetCore.Culture') {
  try {
    for (const part of document.cookie.split(';')) {
      const [name, ...rest] = part.trim().split('=');
      if (name !== cookieName) continue;
      const value = decodeURIComponent(rest.join('='));
      const fields = Object.fromEntries(value.split('|').map((field) => field.split('=')));
      return fields.uic || fields.c || null;
    }
  } catch {
    // Cookies blocked or malformed: nothing to read.
  }
  return null;
}
