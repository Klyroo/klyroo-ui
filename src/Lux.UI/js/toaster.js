/**
 * <lx-toaster data-close-label="Close" data-duration="5000" data-ui-root="…" data-ui-title="…" …>
 *   <ol data-slot="viewport" class="…"></ol>
 * </lx-toaster>
 *
 * Toasts are created here, not by Blazor: the same toaster then serves .NET code running on the server or in
 * WebAssembly (they get different service instances in Auto mode), and plain scripts.
 */
const MAX_TOASTS = 5;

export class LxToasterElement extends HTMLElement {
  get #viewport() {
    return this.querySelector('[data-slot="viewport"]');
  }

  /**
   * @param {{ title?: string, description?: string, color?: string, duration?: number }} toast
   */
  add(toast) {
    const viewport = this.#viewport;
    if (!viewport) return;
    const ui = this.dataset;
    const item = document.createElement('li');
    item.className = ui.uiRoot ?? '';
    item.dataset.color = toast.color || 'neutral';
    item.dataset.state = 'open';
    item.setAttribute('role', toast.color === 'error' ? 'alert' : 'status');

    const wrapper = document.createElement('div');
    wrapper.className = ui.uiWrapper ?? '';
    if (toast.title) wrapper.append(this.#text('p', toast.title, ui.uiTitle));
    if (toast.description) wrapper.append(this.#text('p', toast.description, ui.uiDescription));
    item.append(wrapper);

    const close = document.createElement('button');
    close.type = 'button';
    close.className = ui.uiClose ?? '';
    close.setAttribute('aria-label', ui.closeLabel ?? 'Close');
    // The icon is the app's Close icon, drawn by .NET (LuxOptions.Icons.Close).
    close.innerHTML = ui.iconClose ?? '';
    close.addEventListener('click', () => this.#dismiss(item));
    item.append(close);

    viewport.append(item);
    while (viewport.children.length > MAX_TOASTS) viewport.firstElementChild.remove();

    const duration = toast.duration ?? Number(ui.duration ?? 5000);
    if (duration > 0) this.#autoDismiss(item, duration);
  }

  #text(tag, text, className) {
    const element = document.createElement(tag);
    element.className = className ?? '';
    element.textContent = text;
    return element;
  }

  // One countdown per toast, paused while the pointer is over it or focus is inside it.
  #autoDismiss(item, duration) {
    let remaining = duration;
    let started = 0;
    let timer = 0;
    let hovered = false;
    let focused = false;
    const run = () => {
      started = Date.now();
      timer = setTimeout(() => this.#dismiss(item), Math.max(remaining, 0));
    };
    const update = () => {
      const paused = hovered || focused;
      if (paused && timer) {
        clearTimeout(timer);
        timer = 0;
        remaining -= Date.now() - started;
      } else if (!paused && !timer) {
        remaining = Math.max(remaining, 1000);
        run();
      }
    };
    item.addEventListener('pointerenter', () => { hovered = true; update(); });
    item.addEventListener('pointerleave', () => { hovered = false; update(); });
    item.addEventListener('focusin', () => { focused = true; update(); });
    item.addEventListener('focusout', (e) => { focused = item.contains(e.relatedTarget); update(); });
    run();
  }

  #dismiss(item) {
    if (item.dataset.state === 'closed') return;
    item.dataset.state = 'closed';
    setTimeout(() => item.remove(), 200);
  }
}

if (!customElements.get('lx-toaster')) customElements.define('lx-toaster', LxToasterElement);

/**
 * Shows a toast in the page's <lx-toaster> (rendered by the LxToaster component).
 * Takes an options object, or (title, description, color, duration) as .NET passes them.
 */
export function toast(options, description, color, duration) {
  if (typeof options !== 'object' || options === null) {
    options = { title: options, description, color, duration: duration ?? undefined };
  }
  const toaster = document.querySelector('lx-toaster');
  if (!toaster) {
    console.warn('Lux.toast: no <LxToaster /> in the page.');
    return;
  }
  toaster.add(options);
}
