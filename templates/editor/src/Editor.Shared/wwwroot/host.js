// Small helpers of the app, imported by the shared components.

/**
 * Calls back with true when the page is dark, now and every time the color mode changes (Lux dispatches lx-color-mode).
 * Returns an object whose dispose() removes the listener: call it when the component goes away.
 */
export function watchDark(dotnet) {
  const send = () => dotnet.invokeMethodAsync('OnMode', document.documentElement.classList.contains('dark')).catch(() => {});
  document.addEventListener('lx-color-mode', send);
  send();
  return { dispose: () => document.removeEventListener('lx-color-mode', send) };
}

// Local storage is shared by every app of the site; the prefix is what tells this app's keys apart: prefix + ':d:' + the id of the document.
const documentStart = (prefix) => prefix + ':d:';

/** The bytes a value takes when it is sent to the server as the result of an interop call: JSON.stringify, then UTF-8. DocumentJson.TransferBytes counts the same. */
function transferBytes(value) {
  return new TextEncoder().encode(JSON.stringify(value)).length;
}

/** A stored value the app could not have written, or could not hand to the server: too long, or heavier on the wire than what a message may carry. */
function unusable(value, maxChars, maxBytes) {
  if (typeof value !== 'string' || value.length === 0 || value.length > maxChars) return true;
  // A character weighs at least one byte and at most six: the exact count is only needed in between.
  if (value.length > maxBytes) return true;
  return value.length * 6 > maxBytes && transferBytes(value) > maxBytes;
}

/**
 * The list of the documents, as one JSON text, without any text of a document: for each key that starts with prefix + ':d:' (at most maxRead of them) the id the key
 * names, the id and the title the value holds, whether the title follows the text, the two dates and how many characters of HTML it has; and the UTC offset of the
 * browser in minutes. A value that is not a document of this app, is longer than maxChars or would weigh more than maxBytes on the wire comes back as unusable (the
 * app removes it). The values are read here, and only these few fields cross to the server.
 */
export function loadIndex(prefix, maxChars, maxBytes, maxRead) {
  const entries = [];
  const start = documentStart(prefix);
  for (let i = 0; i < localStorage.length && entries.length < maxRead; i++) {
    const key = localStorage.key(i);
    if (!key || !key.startsWith(start)) continue;
    const id = key.slice(start.length);
    const value = localStorage.getItem(key);
    if (unusable(value, maxChars, maxBytes)) {
      entries.push({ id, usable: false });
      continue;
    }
    try {
      const document = JSON.parse(value);
      if (document === null || typeof document !== 'object' || Array.isArray(document)) throw new Error('not a document');
      entries.push({
        id,
        usable: true,
        innerId: typeof document.id === 'string' ? document.id : null,
        title: typeof document.title === 'string' ? document.title : null,
        autoTitle: document.autoTitle !== false,
        createdAt: typeof document.createdAt === 'string' ? document.createdAt : null,
        updatedAt: typeof document.updatedAt === 'string' ? document.updatedAt : null,
        htmlLength: typeof document.html === 'string' ? document.html.length : 0,
      });
    } catch {
      entries.push({ id, usable: false });
    }
  }
  return JSON.stringify({ entries, utcOffsetMinutes: -new Date().getTimezoneOffset() });
}

/** The JSON of one document, or null when it is not there or is not one the app could hand to the server (see loadIndex). */
export function loadDocument(prefix, id, maxChars, maxBytes) {
  const value = localStorage.getItem(documentStart(prefix) + id);
  return unusable(value, maxChars, maxBytes) ? null : value;
}

/**
 * The ids of the documents whose text holds the query, in any case. The text is what a reader sees: the stored HTML is parsed into an inert document (nothing in it loads or
 * runs). Only ids go back, so nothing of the documents crosses to the server.
 */
export function searchDocuments(prefix, query, maxChars, maxRead) {
  const needle = String(query ?? '').toLowerCase();
  const ids = [];
  if (!needle) return JSON.stringify(ids);
  const start = documentStart(prefix);
  const parser = new DOMParser();
  let seen = 0;
  for (let i = 0; i < localStorage.length && seen < maxRead; i++) {
    const key = localStorage.key(i);
    if (!key || !key.startsWith(start)) continue;
    seen++;
    const value = localStorage.getItem(key);
    if (typeof value !== 'string' || value.length > maxChars) continue;
    try {
      const html = JSON.parse(value)?.html;
      if (typeof html === 'string' && parser.parseFromString(html, 'text/html').body.textContent.toLowerCase().includes(needle)) ids.push(key.slice(start.length));
    } catch {
      // Not a document: the list does not show it either.
    }
  }
  return JSON.stringify(ids);
}

/**
 * Calls back (OnStorageChanged) when another tab or window of the browser changes, adds or removes one of the documents, or clears the storage: the "storage" event is only sent
 * to the other documents of the site, never to the one that wrote. Returns an object whose dispose() removes the listener.
 */
export function watchStorage(dotnet, prefix) {
  const start = documentStart(prefix);
  const onStorage = (event) => {
    if (event.storageArea !== localStorage) return;
    // A null key is clear().
    if (event.key === null || event.key.startsWith(start)) dotnet.invokeMethodAsync('OnStorageChanged').catch(() => {});
  };
  window.addEventListener('storage', onStorage);
  return { dispose: () => window.removeEventListener('storage', onStorage) };
}


/** Writes a value. Throws when the storage is full or blocked: the caller says so. */
export function store(key, value) {
  localStorage.setItem(key, value);
}

export function remove(key) {
  localStorage.removeItem(key);
}
