// Resizing the columns of BxDataListTable, by pointer drag and by keyboard, on the handle ([data-slot=col-resize], a vertical separator in the header cell).
// The browser owns the width while it changes: the <col>, the header cell and every cell of the column follow at once, without a round trip. Only the end of the gesture is reported:
// a bx-change event on the handle whose value is the width in pixels, or "reset" for the default width, which the list stores in its column preferences.

const KEYBOARD_STEP = 16;
const KEYBOARD_STEP_LARGE = 64;
const REPORT_DELAY = 200;

/**
 * The width a key gives the column, "reset" for the default width, or null when the key does nothing here.
 * The arrows grow or shrink by 16 px (64 with Shift), the one that points away from the column's start edge grows it, so in right-to-left text ArrowLeft grows.
 * Home goes to the minimum, End to the maximum, Delete and Backspace back to the default.
 * @param {string} key KeyboardEvent.key
 * @param {number} current the width now
 * @param {number} min
 * @param {number} max
 * @param {{ rtl?: boolean, shift?: boolean }} [options]
 * @returns {number | 'reset' | null}
 */
export function keyboardWidth(key, current, min, max, options = {}) {
  const step = options.shift ? KEYBOARD_STEP_LARGE : KEYBOARD_STEP;
  const grow = options.rtl ? 'ArrowLeft' : 'ArrowRight';
  const shrink = options.rtl ? 'ArrowRight' : 'ArrowLeft';
  const clamp = (value) => Math.min(max, Math.max(min, Math.round(value)));
  switch (key) {
    case grow:
      return clamp(current + step);
    case shrink:
      return clamp(current - step);
    case 'Home':
      return clamp(min);
    case 'End':
      return clamp(max);
    case 'Delete':
    case 'Backspace':
      return 'reset';
    default:
      return null;
  }
}

/** The width a drag gives the column: the pointer moved by `delta` px along the text direction. */
export function dragWidth(start, delta, min, max, rtl = false) {
  return Math.min(max, Math.max(min, Math.round(start + (rtl ? -delta : delta))));
}

const SIZED = ['width', 'minWidth', 'maxWidth', 'boxSizing', 'overflow', 'textOverflow'];

// The cells of the column of `th`: the header cell and the cell at the same place of every row that has as many cells as the header.
function cellsOf(th) {
  const table = th.closest('table');
  if (!table) return [];
  const index = th.cellIndex;
  const count = th.parentElement.cells.length;
  const cells = [];
  for (const row of table.rows) {
    if (row.cells.length === count && row.cells[index]) cells.push(row.cells[index]);
  }
  return cells;
}

function setWidth(handle, th, px) {
  const col = colOf(handle, th);
  const value = `${px}px`;
  if (col) col.style.width = value;
  for (const cell of cellsOf(th)) {
    cell.style.width = value;
    cell.style.minWidth = value;
    cell.style.maxWidth = value;
    cell.style.boxSizing = 'border-box';
    cell.style.overflow = 'hidden';
    cell.style.textOverflow = 'ellipsis';
  }
  show(handle, px);
}

function colOf(handle, th) {
  return th.closest('table')?.querySelector(`col[data-col-key="${CSS.escape(handle.getAttribute('data-col-key'))}"]`);
}

// What the column looked like before a gesture, to put back when the gesture is cancelled.
function snapshot(handle, th) {
  const col = colOf(handle, th);
  const colWidth = col?.style.width ?? '';
  const saved = cellsOf(th).map((cell) => [cell, SIZED.map((name) => cell.style[name])]);
  return () => {
    if (col) col.style.width = colWidth;
    for (const [cell, values] of saved) SIZED.forEach((name, i) => (cell.style[name] = values[i]));
  };
}

// The column at the width the list declared for it, or at the width of its content when it declared none.
function resetWidth(handle, th) {
  const declared = Number(handle.getAttribute('data-default-width'));
  if (declared > 0) return setWidth(handle, th, declared);
  const col = colOf(handle, th);
  if (col) col.style.width = '';
  for (const cell of cellsOf(th)) for (const name of SIZED) cell.style[name] = '';
  show(handle, Math.round(th.getBoundingClientRect().width));
}

// The separator tells assistive technology its value: the width in pixels, spoken with the text the list rendered ("166 pixels").
function show(handle, px) {
  handle.setAttribute('aria-valuenow', String(px));
  const template = handle.getAttribute('data-value-text');
  if (template) handle.setAttribute('aria-valuetext', template.replace('{0}', String(px)));
}

function report(handle, value) {
  handle.dispatchEvent(new CustomEvent('bx-change', { bubbles: true, detail: { value: String(value), sequence: 0 } }));
}

function limits(handle) {
  return [Number(handle.getAttribute('data-min-width')) || 64, Number(handle.getAttribute('data-max-width')) || 900];
}

export function attachColumnResize(doc) {
  if (!doc) return;
  const pending = new WeakMap();

  // The width the header cell has now, rounded; the separator shows it from the moment it gets the focus.
  doc.addEventListener('focusin', (event) => {
    const handle = event.target instanceof Element ? event.target.closest('[data-slot="col-resize"]') : null;
    const th = handle?.closest('th');
    if (handle && th) show(handle, Math.round(th.getBoundingClientRect().width));
  });

  doc.addEventListener('keydown', (event) => {
    const handle = event.target instanceof Element ? event.target.closest('[data-slot="col-resize"]') : null;
    const th = handle?.closest('th');
    if (!handle || !th || event.altKey || event.ctrlKey || event.metaKey) return;
    const [min, max] = limits(handle);
    const rtl = getComputedStyle(th).direction === 'rtl';
    const current = Math.round(th.getBoundingClientRect().width);
    const next = keyboardWidth(event.key, current, min, max, { rtl, shift: event.shiftKey });
    if (next === null) return;
    event.preventDefault();
    event.stopPropagation();
    if (next === 'reset') {
      resetWidth(handle, th);
      clearTimeout(pending.get(handle));
      report(handle, 'reset');
      return;
    }
    setWidth(handle, th, next);
    // A key held down repeats: one report at the end, so a render that carries an older width never undoes the last one.
    clearTimeout(pending.get(handle));
    pending.set(handle, setTimeout(() => report(handle, next), REPORT_DELAY));
  });

  doc.addEventListener('dblclick', (event) => {
    const handle = event.target instanceof Element ? event.target.closest('[data-slot="col-resize"]') : null;
    const th = handle?.closest('th');
    if (!handle || !th) return;
    resetWidth(handle, th);
    report(handle, 'reset');
  });

  doc.addEventListener('pointerdown', (event) => {
    if (event.button !== 0) return;
    const handle = event.target instanceof Element ? event.target.closest('[data-slot="col-resize"]') : null;
    if (!handle) return;
    const th = handle.closest('th');
    if (!th) return;

    event.preventDefault();
    const [min, max] = limits(handle);
    const rtl = getComputedStyle(th).direction === 'rtl';
    const startX = event.clientX;
    const startWidth = Math.round(th.getBoundingClientRect().width);
    let latest = startWidth;

    const restore = snapshot(handle, th);
    handle.focus({ preventScroll: true });
    handle.setPointerCapture?.(event.pointerId);
    handle.setAttribute('data-resizing', 'true');

    const onMove = (moveEvent) => {
      latest = dragWidth(startWidth, moveEvent.clientX - startX, min, max, rtl);
      setWidth(handle, th, latest);
    };

    const finish = (commit) => {
      handle.removeEventListener('pointermove', onMove);
      handle.removeEventListener('pointerup', onUp);
      handle.removeEventListener('pointercancel', onCancel);
      handle.removeAttribute('data-resizing');
      if (commit && latest !== startWidth) report(handle, latest);
    };

    const onUp = () => finish(true);
    const onCancel = () => {
      restore();
      show(handle, startWidth);
      finish(false);
    };

    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
    handle.addEventListener('pointercancel', onCancel);
  });
}
