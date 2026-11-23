// A virtualized table mounts only the rows around its viewport: a row that scrolls out of the window is removed from the DOM, and the focus it held (a checkbox, an action button) falls to the page.
// The table root calls keepVirtualFocus once. When the focused control's row leaves the window and the focus was lost with it, the same control of the same row (by its aria-rowindex and its place in the row)
// gets the focus back when the row comes into view again, without scrolling the page. The user's own next move wins: a key, a pointer press or the focus going anywhere else forgets it.
// Restoring is the least surprising of the two ways out: moving the focus to the table region would send a keyboard user back to the start of the table, while this puts them where they were.

const attached = new WeakSet();

// The place of a control in its row: the indexes of the children from the row down to it.
function pathIn(row, el) {
  const path = [];
  for (let node = el; node && node !== row; node = node.parentElement) {
    if (!node.parentElement) return null;
    path.unshift([...node.parentElement.children].indexOf(node));
  }
  return path;
}

function resolve(row, path) {
  let node = row;
  for (const index of path) {
    node = node?.children[index];
    if (!node) return null;
  }
  return node;
}

const lostFocus = () => {
  const active = document.activeElement;
  return !active || active === document.body || !active.isConnected;
};

export function keepVirtualFocus(root) {
  if (!root || attached.has(root) || typeof MutationObserver === 'undefined') return;
  attached.add(root);
  // The control of a row that has the focus now, and the one whose row left while it had it.
  let held = null;
  let lost = null;

  root.addEventListener('focusin', (event) => {
    const row = event.target instanceof Element ? event.target.closest('tr[data-slot=row][aria-rowindex]') : null;
    held = row && row.contains(event.target) ? { element: event.target, row: row.getAttribute('aria-rowindex'), path: pathIn(row, event.target) } : null;
  });
  // The focus went anywhere else, or the user acted: nothing to give back.
  const forget = () => {
    lost = null;
  };
  document.addEventListener('focusin', (event) => {
    if (!root.contains(event.target)) forget();
  }, true);
  // A press on the scrollbar of the table is the user scrolling, not moving on; a press anywhere else drops what was held (a click on the page's background leaves the focus on the body, with nothing to give back later).
  // The focus that a press on a control gives comes after the press, and sets what is held again.
  document.addEventListener('pointerdown', (event) => {
    if (event.target === root) return;
    forget();
    held = null;
  }, true);
  document.addEventListener('keydown', forget, true);

  new MutationObserver((records) => {
    for (const record of records) {
      for (const node of record.removedNodes) {
        if (held && node instanceof Element && node.contains(held.element) && lostFocus() && held.path) {
          lost = { row: held.row, path: held.path };
          held = null;
        }
      }
      if (!lost) continue;
      for (const node of record.addedNodes) {
        if (!(node instanceof Element) || !node.matches('tr[data-slot=row]') || node.getAttribute('aria-rowindex') !== lost.row) continue;
        const target = resolve(node, lost.path);
        if (target && typeof target.focus === 'function' && lostFocus()) {
          lost = null;
          // The row is built, but its controls may not be ready (a button that renders as enabled one render later): the next frame.
          requestAnimationFrame(() => {
            if (lostFocus() && !target.disabled && target.isConnected) target.focus({ preventScroll: true });
          });
        }
      }
    }
  }).observe(root, { childList: true, subtree: true });
}
