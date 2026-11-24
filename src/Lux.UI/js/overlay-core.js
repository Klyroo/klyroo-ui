/**
 * The stacking order of open overlays, without the DOM so it can be tested. Each layer carries the sequence number it got
 * when it opened (higher is later, so above) and its position in the document, which breaks ties and orders layers that
 * were already open before the sequence started (stamp 0).
 */

function above(a, b) {
  return a.stamp > b.stamp || (a.stamp === b.stamp && a.order > b.order);
}

/** The topmost layer: the one opened last, and among layers with the same stamp the one last in document order. */
export function pickTopmost(layers) {
  let top = null;
  for (const layer of layers) {
    if (top === null || above(layer, top)) top = layer;
  }
  return top;
}

/**
 * What confines the focus: the topmost modal dialog, unless an open popover that holds the focus was opened after it
 * (a menu or a select list above a modal). Non-modal dialogs and popovers without the focus confine nothing.
 * `modals` are the modal dialogs, `popovers` the open popovers that contain the focused element.
 */
export function pickConfinement(modals, popovers) {
  const modal = pickTopmost(modals);
  const popover = pickTopmost(popovers);
  if (popover && (!modal || above(popover, modal))) return popover;
  return modal ?? popover ?? null;
}
