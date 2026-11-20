/**
 * <kbd data-bx-mod data-label="Ctrl">: the hint of the "mod" key of a shortcut. Blazor renders the element empty with a
 * label in an attribute, shown with before:content-[attr(data-label)]; this sets the label of the platform ("⌘" on Apple
 * devices, which is what mod+k means there, "Ctrl" elsewhere). Only an attribute is touched, never a text node, so a
 * render of Blazor cannot fight it, and an enhanced navigation that merges the server markup calls applyPlatformKeys again.
 */
const APPLE = /Mac|iPhone|iPad|iPod/.test(navigator.platform || navigator.userAgent);
const LABEL = APPLE ? '⌘' : 'Ctrl';

export function applyPlatformKeys(root = document) {
  const own = root.matches?.('[data-bx-mod]') ? [root] : [];
  for (const element of [...own, ...root.querySelectorAll('[data-bx-mod]')]) {
    if (element.getAttribute('data-label') !== LABEL) element.setAttribute('data-label', LABEL);
  }
}

applyPlatformKeys();
new MutationObserver((mutations) => {
  for (const mutation of mutations) {
    for (const node of mutation.addedNodes) {
      if (node.nodeType === Node.ELEMENT_NODE) applyPlatformKeys(node);
    }
    // A merge can put the server's label back on an element that stays.
    if (mutation.type === 'attributes' && mutation.target.hasAttribute('data-bx-mod')) applyPlatformKeys(mutation.target);
  }
}).observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-label'] });
