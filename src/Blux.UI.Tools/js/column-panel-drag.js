// Reordering the columns in the column panel by dragging the grip of an item (mouse, pen or touch). Keyboard and screen reader users reorder with the Move up and Move down buttons of
// the same item: the grip is a pointer shortcut and is hidden from assistive technology.
// The browser only moves the dragged item with a transform and marks the item it would drop next to; the list is not changed until the drop, which is reported as a bx-change event on the
// grip with the value "before:<key>" or "after:<key>" (the key of the item it was dropped on). Blazor then renders the new order.

/**
 * Where a dragged item would land: the item the pointer is over and whether it goes before or after it. Null when the pointer is not over another item.
 * @param {{ top: number, bottom: number }[]} rects the items of the list, in order
 * @param {number} self index of the dragged item
 * @param {number} y pointer position
 * @returns {{ index: number, side: 'before' | 'after' } | null}
 */
export function dropTarget(rects, self, y) {
  for (let i = 0; i < rects.length; i++) {
    if (i === self) continue;
    const { top, bottom } = rects[i];
    if (y >= top && y <= bottom) {
      const side = y < (top + bottom) / 2 ? 'before' : 'after';
      // Next to itself: dropping after the item above it or before the item below it changes nothing.
      if ((side === 'after' && i === self - 1) || (side === 'before' && i === self + 1)) return null;
      return { index: i, side };
    }
  }
  return null;
}

export function attachColumnPanelDrag(doc) {
  if (!doc) return;
  doc.addEventListener('pointerdown', (event) => {
    if (event.button !== 0) return;
    const grip = event.target instanceof Element ? event.target.closest('[data-col-grip]') : null;
    const item = grip?.closest('[data-slot="columns-item"]');
    const list = item?.parentElement;
    if (!grip || !item || !list) return;

    event.preventDefault();
    const items = [...list.children].filter((el) => el.matches('[data-slot="columns-item"]'));
    const self = items.indexOf(item);
    const startY = event.clientY;
    let target = null;

    grip.setPointerCapture?.(event.pointerId);
    item.setAttribute('data-dragging', 'true');

    const mark = (next) => {
      for (const el of items) el.removeAttribute('data-drop');
      if (next) items[next.index].setAttribute('data-drop', next.side);
    };

    const onMove = (moveEvent) => {
      item.style.transform = `translateY(${moveEvent.clientY - startY}px)`;
      target = dropTarget(items.map((el) => el.getBoundingClientRect()), self, moveEvent.clientY);
      mark(target);
    };

    const finish = (commit) => {
      grip.removeEventListener('pointermove', onMove);
      grip.removeEventListener('pointerup', onUp);
      grip.removeEventListener('pointercancel', onCancel);
      item.removeAttribute('data-dragging');
      item.style.transform = '';
      mark(null);
      if (commit && target) {
        const key = items[target.index].getAttribute('data-col-key');
        grip.dispatchEvent(new CustomEvent('bx-change', { bubbles: true, detail: { value: `${target.side}:${key}`, sequence: 0 } }));
      }
    };

    const onUp = () => finish(true);
    const onCancel = () => finish(false);
    grip.addEventListener('pointermove', onMove);
    grip.addEventListener('pointerup', onUp);
    grip.addEventListener('pointercancel', onCancel);
  });
}
