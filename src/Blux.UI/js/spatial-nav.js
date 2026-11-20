import { DIRECTIONS, pickNext } from './spatial-core.js';

/**
 * Remote control (D-pad) navigation for TV screens. Experimental and opt in: it runs only while <html> carries
 * data-bx-tv, which <BxApp SpatialNavigation="true">, the <bx-spatial> element or Blux.setSpatialNavigation(true) set.
 *
 * - The arrow keys move focus to the nearest focusable element in that direction (see spatial-core.js). Widgets that
 *   already use the arrows (menus, tabs, listboxes, sliders) call preventDefault, so their own navigation wins;
 *   text fields and native radio groups keep the arrows they need.
 * - Inside a modal dialog or an open popover, focus stays in it (candidates are limited to that container).
 * - A web view on Android TV may not receive the D-pad as key events: the app forwards them from the Activity with
 *   Blux.tvKey('ArrowLeft'), which dispatches a synthetic keydown. Synthetic Enter clicks the focused element and
 *   synthetic Escape (the Back button) closes the topmost overlay, because untrusted events have no default action.
 * - Blux.tvBack() closes the topmost overlay or goes back in the history, and returns false when there is nothing
 *   left to close, so the app can leave the screen.
 */
const ATTRIBUTE = 'data-bx-tv';
const FOCUSABLE_TV =
  'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), ' +
  '[tabindex]:not([tabindex="-1"]), summary, [contenteditable="true"]';

const TEXT_INPUTS = new Set(['text', 'search', 'email', 'url', 'tel', 'password', '']);

let owners = 0;
let enabled = false;

export function isSpatialNavigationOn() {
  return document.documentElement.hasAttribute(ATTRIBUTE);
}

export function setSpatialNavigation(on) {
  const root = document.documentElement;
  if (on) {
    root.setAttribute(ATTRIBUTE, '');
    start();
  } else {
    root.removeAttribute(ATTRIBUTE);
  }
}

function start() {
  if (enabled) return;
  enabled = true;
  document.addEventListener('keydown', onKeyDown);
  // Something must have focus for the arrows to move from.
  requestAnimationFrame(() => {
    if (isSpatialNavigationOn() && (document.activeElement === document.body || !document.activeElement)) focusFirst(container(null));
  });
}

function visible(element) {
  if (element.closest('[inert]') || element.hasAttribute('disabled')) return false;
  if (element.getClientRects().length === 0) return false;
  return getComputedStyle(element).visibility !== 'hidden';
}

/** The element that confines navigation: the topmost modal dialog, an open popover holding the focus, else the document. */
function container(from) {
  const dialogs = document.querySelectorAll('dialog[open]');
  if (dialogs.length > 0) return dialogs[dialogs.length - 1];
  const popover = from?.closest?.('[popover]:popover-open');
  return popover ?? document;
}

function candidates(scope) {
  return [...scope.querySelectorAll(FOCUSABLE_TV)].filter(visible);
}

function focusFirst(scope) {
  const list = candidates(scope);
  if (list.length === 0) return false;
  let first = list[0];
  let firstRect = first.getBoundingClientRect();
  for (const element of list) {
    const rect = element.getBoundingClientRect();
    if (rect.top < firstRect.top - 1 || (Math.abs(rect.top - firstRect.top) <= 1 && rect.left < firstRect.left)) {
      first = element;
      firstRect = rect;
    }
  }
  first.focus();
  return true;
}

/** Arrows that the focused element needs for itself. */
function keepsArrow(target, key) {
  if (!(target instanceof HTMLElement)) return false;
  if (target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement || target.isContentEditable) return true;
  if (target instanceof HTMLInputElement) {
    const type = target.type;
    if (type === 'radio' || type === 'range' || type === 'number') return true;
    if (TEXT_INPUTS.has(type)) {
      if (key === 'ArrowUp' || key === 'ArrowDown') return false;
      const { selectionStart: start, selectionEnd: end, value } = target;
      if (start === null || start !== end) return true;
      return key === 'ArrowLeft' ? start > 0 : start < value.length;
    }
  }
  return false;
}

function move(direction, target) {
  const scope = container(target);
  const active = target instanceof Element && target !== document.body && scope.contains?.(target) !== false && target !== document.documentElement ? target : null;
  if (!active) return focusFirst(scope);
  const list = candidates(scope).filter((element) => element !== active && !active.contains(element) && !element.contains(active));
  const next = pickNext(
    active.getBoundingClientRect(),
    list.map((element) => element.getBoundingClientRect()),
    direction,
  );
  if (next < 0) return false;
  list[next].focus();
  list[next].scrollIntoView?.({ block: 'nearest', inline: 'nearest' });
  return true;
}

/** Closes the topmost overlay. Returns true when it closed one. */
function closeOverlay() {
  const dialogs = document.querySelectorAll('dialog[open]');
  if (dialogs.length > 0) {
    dialogs[dialogs.length - 1].close();
    return true;
  }
  const popovers = document.querySelectorAll('[popover]:popover-open');
  if (popovers.length > 0) {
    popovers[popovers.length - 1].hidePopover();
    return true;
  }
  return false;
}

function onKeyDown(event) {
  if (!isSpatialNavigationOn() || event.defaultPrevented || event.ctrlKey || event.altKey || event.metaKey) return;
  const target = event.target;

  // Untrusted events (forwarded by the app) have no default action: do what the key would have done.
  if (!event.isTrusted) {
    if (event.key === 'Enter' || event.key === ' ') {
      if (target instanceof HTMLElement && target !== document.body) {
        event.preventDefault();
        target.click();
      }
      return;
    }
    if (event.key === 'Escape' || event.key === 'GoBack') {
      if (closeOverlay()) event.preventDefault();
      return;
    }
  }

  const direction = DIRECTIONS[event.key];
  if (!direction || event.shiftKey || keepsArrow(target, event.key)) return;
  if (move(direction, target)) event.preventDefault();
}

/** For the Android TV Activity: dispatches a key into the page as the remote would have. Returns whether the page used it. */
export function tvKey(key) {
  const target = document.activeElement ?? document.body;
  const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
  target.dispatchEvent(event);
  return event.defaultPrevented;
}

/** The Back button: closes the topmost overlay, else goes back in the history. False when nothing is left to go back to. */
export function tvBack() {
  if (closeOverlay()) return true;
  const navigation = globalThis.navigation;
  if (navigation && typeof navigation.canGoBack === 'boolean') {
    if (!navigation.canGoBack) return false;
    history.back();
    return true;
  }
  return false;
}

/**
 * <bx-spatial></bx-spatial>: rendered by <BxApp SpatialNavigation="true">. Turns TV mode on while it is in the page.
 */
export class BxSpatialElement extends HTMLElement {
  connectedCallback() {
    owners++;
    setSpatialNavigation(true);
  }

  disconnectedCallback() {
    owners--;
    if (owners <= 0) {
      owners = 0;
      setSpatialNavigation(false);
    }
  }
}

if (!customElements.get('bx-spatial')) customElements.define('bx-spatial', BxSpatialElement);

// A page that ships <html data-bx-tv> (a TV app's index.html) is in TV mode from the start.
if (isSpatialNavigationOn()) start();
