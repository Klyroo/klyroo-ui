/**
 * <button data-bx-hotkey="mod+k">: clicks the element when the shortcut is pressed. "mod" is Cmd on Apple platforms
 * and Ctrl elsewhere; ctrl, meta, alt and shift are accepted too. It runs in the browser, so a shortcut can open a
 * BxModal (or a drawer) on a static page.
 *
 * A shortcut without ctrl, meta or mod ("/", "g", "shift+?") types a character, so it never fires while the focus is in
 * a text field, a select or a contenteditable element. Elements that are disabled, inert or not rendered are skipped,
 * and the key is only consumed when an element was actually clicked.
 */
const APPLE = /Mac|iPhone|iPad|iPod/.test(navigator.platform || navigator.userAgent);

function parse(combo) {
  const parts = combo.toLowerCase().split('+').map((p) => p.trim());
  const key = parts.pop();
  const wants = new Set(parts);
  const mod = wants.has('mod');
  return {
    key,
    ctrl: wants.has('ctrl') || (mod && !APPLE),
    meta: wants.has('meta') || (mod && APPLE),
    alt: wants.has('alt'),
    shift: wants.has('shift'),
  };
}

function matches(event, combo) {
  return (
    event.key.toLowerCase() === combo.key &&
    event.ctrlKey === combo.ctrl &&
    event.metaKey === combo.meta &&
    event.altKey === combo.alt &&
    event.shiftKey === combo.shift
  );
}

function isTyping(target) {
  if (!(target instanceof Element)) return false;
  return target.closest('input, textarea, select, [contenteditable]:not([contenteditable="false"])') !== null;
}

function isAvailable(element) {
  if (element.matches(':disabled') || element.closest('[inert]')) return false;
  // data-bx-hotkey-always: the element is a hidden trigger that only exists to be clicked (BxDashboardSearch).
  if (element.hasAttribute('data-bx-hotkey-always')) return true;
  return element.checkVisibility ? element.checkVisibility() : element.getClientRects().length > 0;
}

document.addEventListener('keydown', (event) => {
  // Autofill dispatches keydown events without a key.
  if (event.defaultPrevented || event.repeat || event.isComposing || typeof event.key !== 'string') return;
  const typing = isTyping(event.target);
  for (const element of document.querySelectorAll('[data-bx-hotkey]')) {
    const combo = parse(element.getAttribute('data-bx-hotkey'));
    if (!matches(event, combo)) continue;
    if (typing && !combo.ctrl && !combo.meta) continue;
    if (!isAvailable(element)) continue;
    event.preventDefault();
    element.click();
    return;
  }
});
