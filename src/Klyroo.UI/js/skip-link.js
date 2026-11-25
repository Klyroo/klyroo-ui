// KlySkipLink: moves the focus to the element the link names, on the current page. The href is a hash, so the browser would resolve it against
// <base href> and navigate to the home page from any other page, and the Blazor router only scrolls a same-page hash link: the focus would
// stay on the link and the next Tab would go through the header again. Capture phase, so this runs before the router's handler.
document.addEventListener(
  'click',
  (event) => {
    const link = event.target instanceof Element ? event.target.closest('a[data-kly-skip-link]') : null;
    if (!link) return;
    let id = link.getAttribute('href')?.slice(1) ?? '';
    try {
      id = decodeURIComponent(id);
    } catch {
      // A malformed escape: use the id as written.
    }
    const target = id ? document.getElementById(id) : null;
    if (!target) return;
    event.preventDefault();
    // A landmark is not focusable on its own.
    if (!target.hasAttribute('tabindex')) target.setAttribute('tabindex', '-1');
    target.focus({ preventScroll: true });
    target.scrollIntoView();
  },
  true,
);
