import { DIRECTIONS, pickNext } from './spatial-core.js';
import { pickTopmost, pickConfinement } from './overlay-core.js';
import { editKind, wantsKeyboard, enterLeaves, stepOf, caretAfter } from './tv-edit-core.js';

/**
 * Remote control (D-pad) navigation for TV screens. Experimental and opt in: it runs only while <html> carries
 * data-kly-tv, which <KlyApp SpatialNavigation="true">, the <kly-spatial> element or Klyroo.setSpatialNavigation(true) set.
 *
 * - The arrow keys move focus to the nearest focusable element in that direction (see spatial-core.js). Widgets that
 *   already use the arrows (open menus, tabs, listboxes, sliders) call preventDefault, so their own navigation wins.
 *   A field never keeps the arrows by itself: a forwarded key has no default action, so a text field, a textarea, a
 *   select or a number would trap the focus. Select/Enter puts a field in editing mode (data-kly-editing): text
 *   fields get the caret (and the soft keyboard, see Klyroo.tvTextEditing), number, range and native selects take the
 *   arrows as value changes. Back, or Enter in a single line field, leaves it.
 * - Inside the topmost modal dialog, or an open popover holding the focus, focus stays in it.
 * - A web view on Android TV may not receive the D-pad as key events: the app forwards them from the Activity with
 *   Klyroo.tvKey('ArrowLeft'), which dispatches a synthetic keydown. Synthetic Enter clicks the focused element and
 *   synthetic Escape (the Back button) leaves editing mode or closes the topmost overlay, because untrusted events
 *   have no default action.
 * - Klyroo.tvBack() leaves editing mode, else closes the topmost overlay (the one opened last; a modal that is not
 *   dismissible swallows Back), else goes back in the history. It returns false when there is nothing left, so the
 *   app can leave the screen.
 */
const ATTRIBUTE = 'data-kly-tv';
const EDITING = 'data-kly-editing';
const FOCUSABLE_TV =
  'a[href], button:not([disabled]), input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), ' +
  '[tabindex]:not([tabindex="-1"]), summary, [contenteditable="true"]';

let owners = 0;
let enabled = false;
let editing = null;

export function isSpatialNavigationOn() {
  return document.documentElement.hasAttribute(ATTRIBUTE);
}

export function setSpatialNavigation(on) {
  const root = document.documentElement;
  if (on) {
    root.setAttribute(ATTRIBUTE, '');
    start();
  } else {
    leaveEditing();
    root.removeAttribute(ATTRIBUTE);
  }
}

function start() {
  if (enabled) return;
  enabled = true;
  // Capture: the arrows and Back are decided before a widget of the field (a number stepper, a modal) sees them.
  document.addEventListener('keydown', onKeyDownCapture, true);
  document.addEventListener('keydown', onKeyDown);
  document.addEventListener('focusout', onFocusOut);
  // Something must have focus for the arrows to move from.
  requestAnimationFrame(() => {
    if (isSpatialNavigationOn() && (document.activeElement === document.body || !document.activeElement)) focusFirst(container(null));
  });
}

// Overlays, in the order they opened. The DOM does not tell what is on top: a popover opened from a dialog comes later
// in the document, but a menu can also open from a page that is earlier in it. "beforetoggle" fires synchronously when a
// popover opens; a dialog has no such event, so its "open" attribute is watched (pending records are flushed before every
// stamp and read, which keeps the order of two overlays opened in the same task).
const stamps = new WeakMap();
let sequence = 0;
let dialogWatch = null;

function flushDialogs() {
  if (!dialogWatch) return;
  for (const record of dialogWatch.takeRecords()) {
    const dialog = record.target;
    if (record.oldValue === null && dialog.hasAttribute('open')) stamps.set(dialog, ++sequence);
  }
}

if (typeof document !== 'undefined' && typeof MutationObserver !== 'undefined') {
  dialogWatch = new MutationObserver(flushDialogs);
  dialogWatch.observe(document, { subtree: true, attributes: true, attributeFilter: ['open'], attributeOldValue: true });
  document.addEventListener(
    'beforetoggle',
    (event) => {
      if (event.newState !== 'open') return;
      flushDialogs();
      stamps.set(event.target, ++sequence);
    },
    true,
  );
}

function openLayers() {
  flushDialogs();
  // A tooltip (popover="manual", role="tooltip") is not a layer: Back closes what it sits on, never the tooltip first.
  return [...document.querySelectorAll('dialog[open], [popover]:popover-open:not([role="tooltip"])')].map((element, order) => ({
    element,
    order,
    stamp: stamps.get(element) ?? 0,
    modal: element.matches('dialog:modal'),
  }));
}

function visible(element) {
  if (element.closest('[inert]') || element.hasAttribute('disabled')) return false;
  if (element.getClientRects().length === 0) return false;
  return getComputedStyle(element).visibility !== 'hidden';
}

/** The element that confines navigation: the topmost modal dialog, an open popover holding the focus that opened after it, else the document. */
function container(from) {
  const layers = openLayers();
  const modals = layers.filter((layer) => layer.modal);
  const holders = from instanceof Element ? layers.filter((layer) => layer.element.hasAttribute('popover') && layer.element.contains(from)) : [];
  return pickConfinement(modals, holders)?.element ?? document;
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

function kindOf(element) {
  if (!(element instanceof HTMLElement)) return null;
  return editKind({ tag: element.tagName.toLowerCase(), type: element.type ?? '', editable: element.isContentEditable });
}

function enterEditing(element) {
  if (editing === element) return;
  leaveEditing();
  editing = element;
  element.setAttribute(EDITING, '');
  element.focus();
  const length = element.value?.length;
  // The caret goes to the end; types without a selection API (email, number) have none to place.
  if (typeof length === 'number' && element.selectionStart !== null && element.setSelectionRange) element.setSelectionRange(length, length);
  else if (element.isContentEditable) placeCaretAtEnd(element);
  announce(true);
}

function leaveEditing() {
  if (!editing) return;
  const element = editing;
  editing = null;
  element.removeAttribute(EDITING);
  announce(false, element);
}

function placeCaretAtEnd(element) {
  const range = document.createRange();
  range.selectNodeContents(element);
  range.collapse(false);
  const selection = getSelection();
  selection?.removeAllRanges();
  selection?.addRange(range);
}

/**
 * Tells the host app that a field entered or left editing mode: a "kly-tv-edit" event on the document (detail.editing,
 * detail.keyboard) and the optional Klyroo.onEditField(editing, keyboard) callback. The Android TV sample uses it to show
 * and hide the soft keyboard, which a script focus alone may not open.
 */
function announce(on, element = editing) {
  const keyboard = on && element ? wantsKeyboard(kindOf(element), element.type) : false;
  document.dispatchEvent(new CustomEvent('kly-tv-edit', { detail: { editing: on, keyboard, element } }));
  try {
    globalThis.Klyroo?.onEditField?.(on, keyboard);
  } catch {
    // An app callback must not break navigation.
  }
}

/** True while a field that is typed into (text, textarea, number) is in editing mode: the soft keyboard is wanted. */
export function tvTextEditing() {
  return editing !== null && wantsKeyboard(kindOf(editing), editing.type);
}

function onFocusOut(event) {
  const element = event.target;
  if (editing !== element) return;
  // The window losing focus also fires it, with the field still the active element: not a move of the focus.
  setTimeout(() => {
    if (editing === element && document.activeElement !== element) leaveEditing();
  });
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

/** A visible tooltip goes through its own hide path (it keeps its state), as a side effect of Back. */
function hideTooltips() {
  for (const tooltip of document.querySelectorAll('kly-tooltip[data-state="open"]')) tooltip.hide?.();
}

/**
 * Closes the topmost overlay, the one opened last. Returns "closed", "blocked" when it refused (a modal that is not
 * dismissible: the app must not treat Back as unused and leave), or null when there was no overlay.
 */
function closeOverlay() {
  hideTooltips();
  const top = pickTopmost(openLayers());
  if (!top) return null;
  const element = top.element;
  if (element instanceof HTMLDialogElement) {
    // The "cancel" a close request (Escape) raises: <kly-modal> cancels it unless it is dismissible.
    const request = new Event('cancel', { cancelable: true });
    element.dispatchEvent(request);
    if (request.defaultPrevented) return 'blocked';
    element.close();
    return 'closed';
  }
  element.hidePopover();
  return 'closed';
}

function hasModifier(event) {
  return event.ctrlKey || event.altKey || event.metaKey;
}

function onKeyDownCapture(event) {
  if (!isSpatialNavigationOn() || event.defaultPrevented || hasModifier(event)) return;
  const target = event.target;
  if (editing && target !== editing) leaveEditing();

  if (editing && (event.key === 'Escape' || event.key === 'GoBack')) {
    leaveEditing();
    event.preventDefault();
    event.stopPropagation();
    return;
  }

  const kind = kindOf(target);
  if (!kind) return;
  // A widget list that is open (a combobox) navigates itself.
  if (target.getAttribute('aria-expanded') === 'true') return;

  // Select/Enter puts a field in editing mode, from a forwarded key and from a real one (a physical keyboard, a WebView
  // that delivers the D-pad as key events). While editing, a real Enter and the real arrows are left to the browser.
  if (event.key === 'Enter' && !event.isComposing) {
    if (editing === target || target.readOnly) return;
    enterEditing(target);
    event.preventDefault();
    event.stopPropagation();
    return;
  }

  const direction = DIRECTIONS[event.key];
  if (!direction || event.shiftKey || editing === target) return;
  if (move(direction, target)) {
    event.preventDefault();
    event.stopPropagation();
  }
}

function insertNewline(target) {
  if (target.isContentEditable) {
    document.execCommand?.('insertLineBreak');
    return;
  }
  target.setRangeText('\n', target.selectionStart, target.selectionEnd, 'end');
  target.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertLineBreak' }));
}

/** What an arrow does to the field being edited, since a forwarded key has no default action. */
function editArrow(target, key) {
  const kind = kindOf(target);
  if (kind === 'text') {
    if (target.isContentEditable) {
      const vertical = key === 'ArrowUp' || key === 'ArrowDown';
      getSelection()?.modify?.('move', key === 'ArrowLeft' || key === 'ArrowUp' ? 'backward' : 'forward', vertical ? 'line' : 'character');
    } else if (target.selectionStart !== null && target.setSelectionRange) {
      const caret = caretAfter(target.value, target.selectionStart, key, target instanceof HTMLTextAreaElement);
      target.setSelectionRange(caret, caret);
    }
    return;
  }
  const step = stepOf(key);
  if (kind === 'step') {
    try {
      if (step > 0) target.stepUp();
      else target.stepDown();
    } catch {
      return;
    }
  } else if (kind === 'select') {
    const index = Math.min(Math.max(target.selectedIndex - step, 0), target.options.length - 1);
    if (index === target.selectedIndex) return;
    target.selectedIndex = index;
  } else {
    return;
  }
  target.dispatchEvent(new Event('input', { bubbles: true }));
  target.dispatchEvent(new Event('change', { bubbles: true }));
}

function onKeyDown(event) {
  if (!isSpatialNavigationOn() || event.defaultPrevented || hasModifier(event)) return;
  const target = event.target;

  // A real Enter ends a single line field's editing and the browser keeps its default (submitting the form, as a keyboard
  // does); in a textarea or an editable region it is typed.
  if (event.isTrusted && event.key === 'Enter' && editing === target && !event.isComposing) {
    if (enterLeaves({ tag: target.tagName.toLowerCase(), editable: target.isContentEditable })) leaveEditing();
    return;
  }

  // Untrusted events (forwarded by the app) have no default action: do what the key would have done.
  if (!event.isTrusted) {
    if (event.key === 'Enter' && editing === target) {
      event.preventDefault();
      if (enterLeaves({ tag: target.tagName.toLowerCase(), editable: target.isContentEditable })) leaveEditing();
      else insertNewline(target);
      return;
    }
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
  if (!direction || event.shiftKey) return;
  if (editing === target) {
    if (!event.isTrusted) {
      event.preventDefault();
      editArrow(target, event.key);
    }
    return;
  }
  // A field that could not move the focus (nothing in that direction) leaves the key alone.
  if (kindOf(target)) return;
  if (move(direction, target)) event.preventDefault();
}

/** For the Android TV Activity: dispatches a key into the page as the remote would have. Returns whether the page used it. */
export function tvKey(key) {
  const target = document.activeElement ?? document.body;
  const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
  target.dispatchEvent(event);
  return event.defaultPrevented;
}

/**
 * The Back button: leaves editing mode, else closes the topmost overlay, else goes back in the history. A modal that is
 * not dismissible swallows it (true: the page used the key). False when nothing is left to go back to.
 */
export function tvBack() {
  if (editing) {
    leaveEditing();
    return true;
  }
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
 * <kly-spatial></kly-spatial>: rendered by <KlyApp SpatialNavigation="true">. Turns TV mode on while it is in the page.
 */
export class KlySpatialElement extends HTMLElement {
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

if (!customElements.get('kly-spatial')) customElements.define('kly-spatial', KlySpatialElement);

// A page that ships <html data-kly-tv> (a TV app's index.html) is in TV mode from the start.
if (isSpatialNavigationOn()) start();
