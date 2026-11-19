/**
 * <button data-bx-hotkey="mod+k">: clicks the element when the shortcut is pressed. "mod" is Cmd on Apple platforms
 * and Ctrl elsewhere; ctrl, meta, alt and shift are accepted too. It runs in the browser, so a shortcut can open a
 * BxModal (or a drawer) on a static page. A shortcut held in a text field still works: mod combinations do not type.
 */
const APPLE = /Mac|iPhone|iPad|iPod/.test(navigator.platform || navigator.userAgent);

function matches(event, combo) {
  const parts = combo.toLowerCase().split('+').map((p) => p.trim());
  const key = parts.pop();
  const wants = new Set(parts);
  const mod = wants.has('mod');
  const ctrl = wants.has('ctrl') || (mod && !APPLE);
  const meta = wants.has('meta') || (mod && APPLE);
  return (
    event.key.toLowerCase() === key &&
    event.ctrlKey === ctrl &&
    event.metaKey === meta &&
    event.altKey === wants.has('alt') &&
    event.shiftKey === wants.has('shift')
  );
}

document.addEventListener('keydown', (event) => {
  if (event.defaultPrevented || event.repeat) return;
  for (const element of document.querySelectorAll('[data-bx-hotkey]')) {
    if (!matches(event, element.getAttribute('data-bx-hotkey'))) continue;
    event.preventDefault();
    element.click();
    return;
  }
});
