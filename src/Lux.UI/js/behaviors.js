// Composable headless UI behaviors inspired by ui-sdk.dev (@ui-sdk/core) for Lux.UI.
import { FOCUSABLE, ensureId, float, rovingKey } from './shared.js';

let liveRegionPolite = null;
let liveRegionAssertive = null;

/** Announces a message to screen readers via a shared ARIA live region. */
export function announce(message, politeness = 'polite') {
  if (typeof document === 'undefined' || !message) return;
  const isAssertive = politeness === 'assertive';
  let region = isAssertive ? liveRegionAssertive : liveRegionPolite;
  if (!region || !region.isConnected) {
    region = document.createElement('div');
    region.setAttribute('aria-live', isAssertive ? 'assertive' : 'polite');
    region.setAttribute('aria-atomic', 'true');
    region.className = 'sr-only';
    Object.assign(region.style, {
      position: 'absolute',
      width: '1px',
      height: '1px',
      padding: '0',
      margin: '-1px',
      overflow: 'hidden',
      clip: 'rect(0, 0, 0, 0)',
      whiteSpace: 'nowrap',
      border: '0',
    });
    document.body.appendChild(region);
    if (isAssertive) liveRegionAssertive = region;
    else liveRegionPolite = region;
  }
  region.textContent = '';
  requestAnimationFrame(() => {
    region.textContent = message;
  });
}

/** Creates an undo/redo history stack over immutable state snapshots. */
export function createUndoStack({ limit = 50, initial = null } = {}) {
  const past = [];
  const future = [];
  let current = initial;
  return {
    get current() {
      return current;
    },
    get canUndo() {
      return past.length > 0;
    },
    get canRedo() {
      return future.length > 0;
    },
    record(next) {
      if (current !== null && Object.is(current, next)) return current;
      if (current !== null) {
        past.push(current);
        if (past.length > limit) past.shift();
      }
      current = next;
      future.length = 0;
      return current;
    },
    undo() {
      if (past.length === 0) return current;
      future.push(current);
      current = past.pop();
      return current;
    },
    redo() {
      if (future.length === 0) return current;
      past.push(current);
      current = future.pop();
      return current;
    },
    clear() {
      past.length = 0;
      future.length = 0;
    },
  };
}

/** Roving focus behavior: one Tab stop for a list of items, arrow keys + Home/End + typeahead move focus. */
export function createRovingFocus(context, { vertical = true, selector = FOCUSABLE } = {}) {
  return {
    keydown(event, container = context?.element?.('root')) {
      if (!container) return false;
      const items = [...container.querySelectorAll(selector)].filter((el) => !el.disabled && el.offsetParent !== null);
      return rovingKey(event, items, document.activeElement, { vertical });
    },
  };
}

/** 2D Grid focus behavior: moves focus by row and column with ArrowUp/Down/Left/Right, Home, End. */
export function createGridFocus(context, { rowSelector = 'tr, [role="row"]', cellSelector = 'td, th, [role="gridcell"], [role="columnheader"]' } = {}) {
  return {
    keydown(event, grid = context?.element?.('root')) {
      if (!grid) return false;
      const rows = [...grid.querySelectorAll(rowSelector)];
      if (rows.length === 0) return false;
      const active = document.activeElement;
      const currentCell = active?.closest(cellSelector);
      const currentRow = currentCell?.closest(rowSelector);
      const rIdx = rows.indexOf(currentRow);
      if (rIdx < 0) return false;
      const cells = [...currentRow.querySelectorAll(cellSelector)];
      const cIdx = cells.indexOf(currentCell);
      if (cIdx < 0) return false;

      let nextRow = rIdx;
      let nextCol = cIdx;
      if (event.key === 'ArrowRight') nextCol = Math.min(cells.length - 1, cIdx + 1);
      else if (event.key === 'ArrowLeft') nextCol = Math.max(0, cIdx - 1);
      else if (event.key === 'ArrowDown') nextRow = Math.min(rows.length - 1, rIdx + 1);
      else if (event.key === 'ArrowUp') nextRow = Math.max(0, rIdx - 1);
      else if (event.key === 'Home') nextCol = 0;
      else if (event.key === 'End') nextCol = cells.length - 1;
      else return false;

      const targetRow = rows[nextRow];
      const targetCells = [...targetRow.querySelectorAll(cellSelector)];
      const targetCell = targetCells[Math.min(nextCol, targetCells.length - 1)];
      if (!targetCell || targetCell === currentCell) return false;

      event.preventDefault();
      currentCell.setAttribute('tabindex', '-1');
      targetCell.setAttribute('tabindex', '0');
      const focusTarget = targetCell.querySelector(FOCUSABLE) ?? targetCell;
      focusTarget.focus();
      return true;
    },
  };
}

/** Typeahead buffer behavior: jumps to matching item as user types characters quickly. */
export function createTypeahead({ timeout = 500, onMatch = null } = {}) {
  let buffer = '';
  let timer = null;
  return {
    keydown(event, items) {
      if (event.ctrlKey || event.metaKey || event.altKey || event.key.length !== 1 || !/\S/.test(event.key)) {
        return null;
      }
      buffer += event.key.toLowerCase();
      clearTimeout(timer);
      timer = setTimeout(() => {
        buffer = '';
      }, timeout);
      const match = items.find((item) => {
        const label = (typeof item === 'string' ? item : item?.label ?? item?.textContent ?? '').trim().toLowerCase();
        return label.startsWith(buffer);
      }) ?? null;
      if (match && onMatch) onMatch(match, buffer);
      return match;
    },
  };
}

/** Selection behavior: tracks single or multiple selected keys with Shift-range and Ctrl/Meta-toggle support. */
export function createSelection(context, { key = 'value', multiple = false, set = null } = {}) {
  let anchor = null;
  const read = () => {
    const val = context?.store?.get()?.[key];
    if (multiple) return Array.isArray(val) ? val : [];
    return val ?? null;
  };
  const write = (next, details) => {
    context?.store?.set({ [key]: next });
    if (set) set(next, details);
  };
  return {
    isSelected(itemKey) {
      const current = read();
      return multiple ? current.includes(itemKey) : current === itemKey;
    },
    select(itemKey, { event = null, orderedKeys = null } = {}) {
      if (!multiple) {
        anchor = itemKey;
        write(itemKey, { reason: 'item-select', source: event ? 'pointer' : 'programmatic' });
        return;
      }
      const current = [...read()];
      if (event?.shiftKey && anchor !== null && Array.isArray(orderedKeys)) {
        const a = orderedKeys.indexOf(anchor);
        const b = orderedKeys.indexOf(itemKey);
        if (a >= 0 && b >= 0) {
          const [start, end] = a < b ? [a, b] : [b, a];
          const range = orderedKeys.slice(start, end + 1);
          const merged = [...new Set([...current, ...range])];
          write(merged, { reason: 'range-select', source: 'keyboard' });
          return;
        }
      }
      anchor = itemKey;
      const idx = current.indexOf(itemKey);
      if (idx >= 0) current.splice(idx, 1);
      else current.push(itemKey);
      write(current, { reason: 'item-toggle', source: event ? 'pointer' : 'programmatic' });
    },
    clear() {
      anchor = null;
      write(multiple ? [] : null, { reason: 'clear', source: 'programmatic' });
    },
  };
}

/** Popup behavior: manages open state, floating positioning, and Escape/outside dismissal. */
export function createPopup(context, { key = 'open', placement = 'bottom', distance = 8, matchWidth = false, set = null } = {}) {
  let cleanupFloat = null;
  const isOpen = () => Boolean(context?.store?.get()?.[key]);
  const setOpen = (next, reason = 'toggle') => {
    if (isOpen() === Boolean(next)) return;
    context?.store?.set({ [key]: Boolean(next) });
    if (set) set(Boolean(next), { reason });
  };
  return {
    isOpen,
    open: () => setOpen(true, 'open'),
    close: (reason = 'close') => setOpen(false, reason),
    toggle: () => setOpen(!isOpen(), 'toggle'),
    attach(anchorEl, contentEl) {
      if (cleanupFloat) {
        cleanupFloat();
        cleanupFloat = null;
      }
      if (anchorEl && contentEl && isOpen()) {
        cleanupFloat = float(anchorEl, contentEl, { placement, distance, matchWidth });
      }
      return () => {
        if (cleanupFloat) {
          cleanupFloat();
          cleanupFloat = null;
        }
      };
    },
  };
}

/** Reorder behavior: sorts a list with keyboard (Alt+ArrowUp/Down) or pointer and announces moves to screen readers. */
export function createReorder(context, { key = 'items', set = null } = {}) {
  const read = () => context?.store?.get()?.[key] ?? [];
  return {
    move(fromIndex, toIndex, source = 'programmatic') {
      const list = [...read()];
      if (fromIndex < 0 || fromIndex >= list.length || toIndex < 0 || toIndex >= list.length || fromIndex === toIndex) {
        return list;
      }
      const [moved] = list.splice(fromIndex, 1);
      list.splice(toIndex, 0, moved);
      context?.store?.set({ [key]: list });
      if (set) set(list, { fromIndex, toIndex, source });
      announce(`Moved item to position ${toIndex + 1} of ${list.length}`);
      return list;
    },
    keydown(event, index) {
      if (!event.altKey) return false;
      if (event.key === 'ArrowUp') {
        event.preventDefault();
        this.move(index, index - 1, 'keyboard');
        return true;
      }
      if (event.key === 'ArrowDown') {
        event.preventDefault();
        this.move(index, index + 1, 'keyboard');
        return true;
      }
      return false;
    },
  };
}

/** Resize behavior: updates live CSS width/height during pointer drag or Alt+Arrow key presses and commits on release. */
export function createResize(context, { key = 'size', minWidth = 120, maxWidth = 1600, step = 8, set = null } = {}) {
  return {
    keydown(event, currentWidth) {
      if (!event.altKey) return false;
      const delta = event.shiftKey ? step * 5 : step;
      let next = currentWidth;
      if (event.key === 'ArrowRight') next = Math.min(maxWidth, currentWidth + delta);
      else if (event.key === 'ArrowLeft') next = Math.max(minWidth, currentWidth - delta);
      else return false;
      event.preventDefault();
      context?.store?.set({ [key]: next });
      if (set) set(next, { source: 'keyboard' });
      return true;
    },
  };
}

/** Viewport pan & zoom behavior for canvas surfaces ({ x, y, zoom }). */
export function createViewport(context, { key = 'viewport', minZoom = 0.25, maxZoom = 4 } = {}) {
  const read = () => context?.store?.get()?.[key] ?? { x: 0, y: 0, zoom: 1 };
  const write = (next) => context?.store?.set({ [key]: next });
  return {
    get: read,
    setViewport: write,
    panBy(dx, dy) {
      const v = read();
      write({ ...v, x: v.x + dx, y: v.y + dy });
    },
    zoomBy(factor) {
      const v = read();
      const nextZoom = Math.min(maxZoom, Math.max(minZoom, Number((v.zoom * factor).toFixed(3))));
      write({ ...v, zoom: nextZoom });
    },
    reset() {
      write({ x: 0, y: 0, zoom: 1 });
    },
    style(state = context?.store?.get()) {
      const v = state?.[key] ?? { x: 0, y: 0, zoom: 1 };
      return `transform: translate(${v.x}px, ${v.y}px) scale(${v.zoom}); transform-origin: 0 0;`;
    },
  };
}

/** Inline edit behavior: manages editing, committing (Enter/Tab), and canceling (Escape) a value in place. */
export function createInlineEdit(context, { key = 'value', onCommit = null, onCancel = null } = {}) {
  return {
    start(initialText = '') {
      context?.store?.set({ editing: true, draft: initialText });
    },
    commit() {
      const st = context?.store?.get() ?? {};
      if (!st.editing) return;
      const next = (st.draft ?? '').trim();
      context?.store?.set({ editing: false, [key]: next });
      if (onCommit) onCommit(next);
    },
    cancel() {
      context?.store?.set({ editing: false });
      if (onCancel) onCancel();
    },
    keydown(event) {
      if (event.key === 'Enter' || event.key === 'Tab') {
        this.commit();
        return true;
      }
      if (event.key === 'Escape') {
        event.preventDefault();
        this.cancel();
        return true;
      }
      return false;
    },
  };
}

/**
 * Defines a composable primitive that wires state, live DOM updates, behaviors, slots, and the Blux.agent bridge.
 */
export function definePrimitive(definition) {
  const { name, state: createInitialState, api: createApi, slots: createSlots, agent: agentConfig } = definition;
  return {
    name,
    agentConfig,
    instantiate(rootElement, initialOverrides = {}) {
      let currentState = { ...(createInitialState ? createInitialState() : {}), ...initialOverrides };
      const listeners = new Set();
      const partElements = new Map();
      if (rootElement) partElements.set('root', rootElement);

      const store = {
        get: () => currentState,
        set(partial) {
          currentState = { ...currentState, ...partial };
          for (const fn of listeners) fn(currentState);
        },
        subscribe(fn) {
          listeners.add(fn);
          return () => listeners.delete(fn);
        },
      };

      const context = {
        name,
        store,
        element: (partName = 'root') => partElements.get(partName) ?? null,
        registerPart: (partName, el) => {
          if (el) ensureId(el, `${name}-${partName}`);
          partElements.set(partName, el);
        },
      };

      const api = createApi ? createApi(context) : {};
      const slotDefs = createSlots ? createSlots((cfg) => cfg) : {};

      const instance = {
        name,
        store,
        api,
        slots: slotDefs,
        context,
        agent: agentConfig,
      };

      if (rootElement) {
        rootElement.__bxPrimitive = instance;
        rootElement.setAttribute('data-bx-primitive', name);
      }
      return instance;
    },
  };
}

export const BluxBehaviors = {
  announce,
  createUndoStack,
  createRovingFocus,
  createGridFocus,
  createTypeahead,
  createSelection,
  createPopup,
  createReorder,
  createResize,
  createViewport,
  createInlineEdit,
  definePrimitive,
};
