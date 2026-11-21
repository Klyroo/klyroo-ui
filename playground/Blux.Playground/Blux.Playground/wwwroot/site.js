// Copy buttons of code blocks. Runs without .NET, so it works on static pages too.
document.addEventListener('click', async (event) => {
  const button = event.target.closest?.('[data-copy]');
  if (!button) return;
  const code = button.parentElement.querySelector('pre code')?.innerText ?? '';
  try {
    await navigator.clipboard.writeText(code);
    button.textContent = 'Copied';
  } catch {
    button.textContent = 'Copy failed';
  }
  setTimeout(() => (button.textContent = 'Copy'), 1500);
});

// Copy buttons that carry their text (install command in the hero).
document.addEventListener('click', async (event) => {
  const button = event.target.closest?.('[data-copy-text]');
  if (!button) return;
  const label = button.querySelector('[data-copy-label]');
  try {
    await navigator.clipboard.writeText(button.dataset.copyText);
    if (label) label.textContent = 'Copied';
  } catch {
    if (label) label.textContent = 'Failed';
  }
  setTimeout(() => label && (label.textContent = 'Copy'), 1500);
});

// Dark mode toggle in the header.
document.addEventListener('click', (event) => {
  if (!event.target.closest?.('[data-color-mode-toggle]')) return;
  Blux.setColorMode(document.documentElement.classList.contains('dark') ? 'light' : 'dark');
});

// Showcase on the home page: list what the browser handled itself, without .NET.
const showcaseKeys = new Set(['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'Escape', 'Enter', ' ']);

function logBrowser(event, text) {
  const demo = event.target.closest?.('[data-pg-demo]');
  const list = demo?.querySelector('[data-pg-log="browser"]');
  if (!list) return;
  const item = document.createElement('li');
  item.className = 'pg-log-line rounded-sm px-1';
  const time = document.createElement('span');
  time.className = 'text-(--blux-text-dimmed)';
  time.textContent = `${(performance.now() / 1000).toFixed(1)}s `;
  item.append(time, text);
  list.prepend(item);
  while (list.children.length > 7) list.lastElementChild.remove();
}

document.addEventListener('bx-toggle', (e) => logBrowser(e, `bx-toggle open=${e.detail.open}`), true);
document.addEventListener('bx-change', (e) => logBrowser(e, `bx-change value="${e.detail.value}"`), true);
document.addEventListener(
  'toggle',
  (e) => {
    if (e.target.matches?.('[popover]')) logBrowser(e, `popover ${e.newState} (top layer)`);
  },
  true,
);
document.addEventListener(
  'keydown',
  (e) => {
    if (showcaseKeys.has(e.key)) logBrowser(e, `key ${e.key === ' ' ? 'Space' : e.key}`);
  },
  true,
);
document.addEventListener(
  'change',
  (e) => {
    if (e.target.matches?.('input[type="checkbox"]')) logBrowser(e, `checkbox checked=${e.target.checked}`);
  },
  true,
);
