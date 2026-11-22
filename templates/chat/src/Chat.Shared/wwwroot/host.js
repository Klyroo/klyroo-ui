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
 * Everything the app keeps in the local storage, as one JSON text: the conversations (the values of the keys that start with prefix + ':c:', at most 200), the model
 * chosen last and the UTC offset of the browser in minutes. Local storage is shared by every app of the site; the prefix is what tells this app's keys apart.
 */
export function loadAll(prefix) {
  const conversations = [];
  const start = prefix + ':c:';
  for (let i = 0; i < localStorage.length && conversations.length < 200; i++) {
    const key = localStorage.key(i);
    if (key && key.startsWith(start)) {
      const value = localStorage.getItem(key);
      if (value) conversations.push(value);
    }
  }
  return JSON.stringify({
    conversations,
    model: localStorage.getItem(prefix + ':model'),
    utcOffsetMinutes: -new Date().getTimezoneOffset(),
  });
}

/** Writes a value. Throws when the storage is full or blocked: the caller says so. */
export function store(key, value) {
  localStorage.setItem(key, value);
}

export function remove(key) {
  localStorage.removeItem(key);
}

/** Puts the focus in the field of the prompt, after the page settled (a navigation moves it to the title, and the prompt is where the user types next). */
export function focusPrompt() {
  setTimeout(() => {
    const field = document.querySelector('[data-bx-chat-input]');
    if (field instanceof HTMLElement && !field.hasAttribute('disabled')) field.focus();
  }, 60);
}

const fallbackCopy = (text) => {
  const area = document.createElement('textarea');
  area.value = text;
  area.setAttribute('readonly', '');
  area.style.position = 'fixed';
  area.style.opacity = '0';
  document.body.appendChild(area);
  area.select();
  const ok = document.execCommand('copy');
  area.remove();
  if (!ok) throw new Error('copy failed');
};

/** Copies text to the clipboard. The clipboard API needs a secure context and, in some browsers, a permission; the old way works on a click everywhere. */
export async function copyText(text) {
  try {
    await navigator.clipboard.writeText(text);
  } catch {
    fallbackCopy(text);
  }
}

let codeCopyInstalled = false;

/**
 * The "Copy" button of a code block of an answer: one listener on the page, so it also works for blocks that arrive while a reply streams. The markup it expects is
 * what the Markdown renderer writes: a .code-block with a [data-code-copy] button, a pre > code and a .code-copy-status element for screen readers.
 */
export function installCodeCopy() {
  if (codeCopyInstalled) return;
  codeCopyInstalled = true;
  const timers = new WeakMap();
  document.addEventListener('click', async (event) => {
    const button = event.target instanceof Element ? event.target.closest('[data-code-copy]') : null;
    if (!button) return;
    const block = button.closest('.code-block');
    const code = block && block.querySelector('pre code');
    const status = block && block.querySelector('.code-copy-status');
    if (!code) return;
    let label = 'Copied';
    try {
      await copyText((code.textContent || '').replace(/\n$/, ''));
    } catch {
      label = 'Copy failed';
    }
    button.focus();
    button.textContent = label;
    if (status) status.textContent = label;
    clearTimeout(timers.get(button));
    timers.set(button, setTimeout(() => {
      button.textContent = 'Copy';
      if (status) status.textContent = '';
    }, 2000));
  });
}
