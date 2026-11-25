/**
 * The hit area of a switch on a touch screen is the label that wraps its track (KlySwitch, KlyColorModeSwitch: an ::after
 * of the label reaches 44px). A tap there targets the label, and the browser then dispatches a second, synthetic click on
 * the input, so a click listener on an ancestor (a card with @onclick, say) saw two clicks for one tap.
 * The label's own click is stopped before it leaves the capture phase: its activation behavior still runs (stopping
 * propagation does not cancel it), and the input's click, the one that follows, is the one ancestors receive. A click on
 * the input (the track) is a single event already and a click on the text label (a separate label that is not the track's
 * container) is left alone: per the HTML spec it is the label's click and then the input's, as before.
 */
document.addEventListener(
  'click',
  (event) => {
    const label = event.target;
    if (!(label instanceof HTMLLabelElement) || event.defaultPrevented) return;
    const input = label.querySelector(':scope > input[role="switch"]');
    if (input && !input.disabled) event.stopPropagation();
  },
  true,
);
