// Pointer-drag column resizing for BxDataListTable.
// During drag, updates the matching <col> width directly in the browser for smooth feedback without SignalR round trips;
// on pointerup, reports the final width via a single bx-change event ("col-width:<key>:<px>") on [data-slot=table-root].

export function attachColumnResize(doc) {
  if (!doc) return;
  doc.addEventListener('pointerdown', (event) => {
    if (event.button !== 0) return;
    const handle = event.target instanceof Element ? event.target.closest('[data-slot="col-resize"]') : null;
    if (!handle) return;
    const th = handle.closest('th');
    const root = handle.closest('[data-slot="table-root"]');
    const table = handle.closest('table');
    const key = handle.getAttribute('data-col-key');
    if (!th || !root || !table || !key) return;

    event.preventDefault();
    const min = Number(handle.getAttribute('data-min-width')) || 64;
    const max = Number(handle.getAttribute('data-max-width')) || 900;
    const rtl = getComputedStyle(th).direction === 'rtl';
    const startX = event.clientX;
    const startWidth = Math.round(th.getBoundingClientRect().width);
    const col = table.querySelector(`col[data-col-key="${CSS.escape(key)}"]`);
    let latest = startWidth;

    handle.setPointerCapture?.(event.pointerId);
    handle.setAttribute('data-resizing', 'true');

    const onMove = (moveEvent) => {
      const delta = rtl ? startX - moveEvent.clientX : moveEvent.clientX - startX;
      latest = Math.min(max, Math.max(min, Math.round(startWidth + delta)));
      if (col) col.style.width = `${latest}px`;
      th.style.width = `${latest}px`;
    };

    const finish = (commit) => {
      handle.removeEventListener('pointermove', onMove);
      handle.removeEventListener('pointerup', onUp);
      handle.removeEventListener('pointercancel', onCancel);
      handle.removeAttribute('data-resizing');
      if (commit && latest !== startWidth) {
        root.dispatchEvent(new CustomEvent('bx-change', { bubbles: true, detail: { value: `col-width:${key}:${latest}`, sequence: 0 } }));
      }
    };

    const onUp = () => finish(true);
    const onCancel = () => {
      if (col) col.style.width = `${startWidth}px`;
      th.style.width = '';
      finish(false);
    };

    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
    handle.addEventListener('pointercancel', onCancel);
  });
}
