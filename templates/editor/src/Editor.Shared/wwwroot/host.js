// Small helpers of the app, imported by the shared components.

/**
 * Calls back with true when the page is dark, now and every time the color mode changes (Blux dispatches bx-color-mode).
 * Returns an object whose dispose() removes the listener: call it when the component goes away.
 */
export function watchDark(dotnet) {
  const send = () => dotnet.invokeMethodAsync('OnMode', document.documentElement.classList.contains('dark')).catch(() => {});
  document.addEventListener('bx-color-mode', send);
  send();
  return { dispose: () => document.removeEventListener('bx-color-mode', send) };
}

/**
 * Everything the app keeps in the local storage, as one JSON text: the documents (the id and the value of the keys that start with prefix + ':d:', at most 200) and the UTC
 * offset of the browser in minutes. Local storage is shared by every app of the site; the prefix is what tells this app's keys apart. A value longer than maxChars is
 * not what the app writes: it is not sent (the document comes back empty, and the app removes the key).
 */
export function loadAll(prefix, maxChars) {
  const documents = [];
  const start = prefix + ':d:';
  for (let i = 0; i < localStorage.length && documents.length < 200; i++) {
    const key = localStorage.key(i);
    if (key && key.startsWith(start)) {
      const value = localStorage.getItem(key);
      documents.push({ id: key.slice(start.length), json: value && value.length <= maxChars ? value : '' });
    }
  }
  return JSON.stringify({ documents, utcOffsetMinutes: -new Date().getTimezoneOffset() });
}

/** Writes a value. Throws when the storage is full or blocked: the caller says so. */
export function store(key, value) {
  localStorage.setItem(key, value);
}

export function remove(key) {
  localStorage.removeItem(key);
}
