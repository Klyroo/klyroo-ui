/**
 * `indeterminate` is a DOM property of a checkbox, not an attribute, so Blazor cannot render it. KlyCheckbox renders
 * data-indeterminate (its value changes on every render while the parameter is true) and this file sets the property:
 * when the page loads, when an element is inserted or the attribute changes (any render mode), and after an enhanced
 * navigation (syncIndeterminate, which also covers an element the merge kept but reset). A click clears the property
 * natively; a render that still asks for it changes the attribute value, which sets it again.
 */
const SELECTOR = 'input[type="checkbox"][data-indeterminate]';

function apply(input) {
  input.indeterminate = input.hasAttribute('data-indeterminate');
}

/** Sets the property of every checkbox that asks for it under root (the whole document by default). */
export function syncIndeterminate(root = document) {
  for (const input of root.querySelectorAll(SELECTOR)) apply(input);
}

function observe() {
  const observer = new MutationObserver((records) => {
    for (const record of records) {
      if (record.type === 'attributes') {
        const target = record.target;
        // Removing the attribute must clear the property too, so every checkbox goes through apply.
        if (target instanceof HTMLInputElement && target.type === 'checkbox') apply(target);
        continue;
      }
      for (const node of record.addedNodes) {
        if (!(node instanceof Element)) continue;
        if (node.matches(SELECTOR)) apply(node);
        syncIndeterminate(node);
      }
    }
  });
  observer.observe(document.documentElement, {
    childList: true,
    subtree: true,
    attributes: true,
    attributeFilter: ['data-indeterminate'],
  });
}

observe();
if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => syncIndeterminate());
else syncIndeterminate();
