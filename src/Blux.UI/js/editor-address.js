// What the editor accepts as the address of an image or of a link. Plain functions with no dependency, so they can be tested without a browser and
// shared by the editing engine (what is inserted, pasted or loaded) and the toolbar (what its forms accept).

/** The longest address kept. */
export const MAX_ADDRESS = 2048;

/** Schemes an image may come from when the page does not choose. Images are always fetched over the network, so no other scheme is ever allowed. */
export const DEFAULT_IMAGE_SCHEMES = ['http', 'https'];

const WEB = new Set(['http', 'https']);

/** "https http" or "https,http" to ['https', 'http']; null when the list is missing or empty (the default applies). */
export function parseSchemes(value) {
  const list = String(value ?? '')
    .toLowerCase()
    .split(/[\s,]+/)
    .filter(Boolean);
  return list.length > 0 ? list : null;
}

/** "https, http or mailto" for a message that names the schemes. */
export function describeSchemes(schemes) {
  return schemes.join(', ');
}

/** An address as it is read: no space or control character, not empty, not too long. Returns the address as typed (trimmed) or null. */
function plain(address) {
  const value = String(address ?? '').trim();
  if (value.length === 0 || value.length > MAX_ADDRESS || /[\s\p{Cc}]/u.test(value)) return null;
  return value;
}

/** Parsed when it is an absolute address of one of the schemes, with the host a web address needs and no user name or password in it. */
function absolute(value, schemes) {
  let url;
  try {
    url = new URL(value);
  } catch {
    return null;
  }
  const scheme = url.protocol.slice(0, -1).toLowerCase();
  if (!schemes.includes(scheme)) return null;
  if (WEB.has(scheme)) {
    // "https:example.com" parses, and "https://" does not: a web address has two slashes and a host.
    if (!/^[a-z][a-z0-9+.-]*:\/\//i.test(value) || url.host.length === 0 || url.username || url.password) return null;
  } else if (value.length <= scheme.length + 1) {
    // "mailto:" with nothing after it.
    return null;
  }
  return url;
}

/** Whether `address` is an image source: an absolute address of one of the schemes (http and https only). */
export function isImageAddress(address, schemes) {
  const value = plain(address);
  if (!value) return false;
  const allowed = (schemes?.length ? schemes : DEFAULT_IMAGE_SCHEMES).filter((s) => WEB.has(s));
  return absolute(value, allowed) !== null;
}

/**
 * Whether `address` is an allowed address for a link. With a list of schemes, only absolute addresses of those schemes: a relative address, a
 * bare domain or another scheme is refused. Without one, null: the engine's own default applies.
 */
export function isLinkAddress(address, schemes) {
  if (!schemes?.length) return null;
  const value = plain(address);
  return value !== null && absolute(value, schemes) !== null;
}
