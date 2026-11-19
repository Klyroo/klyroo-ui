import { FOCUSABLE, Reporter, ensureId, observeChildren } from './shared.js';

/**
 * <bx-modal open dismissible>
 *   <span data-slot="trigger"><button>…</button></span>
 *   <dialog data-slot="content">… <button data-bx-close>…</button> …</dialog>
 * </bx-modal>
 *
 * Shows the native modal dialog (top layer, inert page, Escape) and reports every open/close with "bx-toggle".
 * Blazor never renders the dialog's own "open" attribute: that would make it non-modal.
 */
export class BxModalElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'open'];

  #dialog = null;
  #observer = null;
  #returnFocus = null;
  #reporter = new Reporter(this);

  #pressedBackdrop = false;

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('pointerdown', this.#onPointerDown);
    this.addEventListener('keydown', this.#onKeyDown);
    this.#observer = observeChildren(this, () => this.#sync());
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('pointerdown', this.#onPointerDown);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.#observer?.disconnect();
    this.#unbind();
  }

  attributeChangedCallback(name) {
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-bx-seq') queueMicrotask(() => this.#apply());
    else this.#apply();
  }

  #apply() {
    if (!this.#dialog || this.#reporter.isStale()) return;
    this.hasAttribute('open') ? this.#open() : this.#close();
  }

  refresh() {
    this.#sync();
  }

  get #trigger() {
    return this.querySelector(':scope > [data-slot="trigger"] button');
  }

  #sync() {
    const dialog = this.querySelector(':scope > dialog');
    if (dialog !== this.#dialog) {
      this.#unbind();
      this.#dialog = dialog;
      if (dialog) {
        dialog.addEventListener('close', this.#onClose);
        dialog.addEventListener('cancel', this.#onCancel);
        if (this.hasAttribute('open')) this.#open();
      }
    }
    const trigger = this.#trigger;
    if (trigger && dialog) {
      trigger.setAttribute('aria-haspopup', 'dialog');
      trigger.setAttribute('aria-controls', ensureId(dialog, 'modal'));
      trigger.setAttribute('aria-expanded', String(dialog.open));
    }
  }

  #unbind() {
    this.#dialog?.removeEventListener('close', this.#onClose);
    this.#dialog?.removeEventListener('cancel', this.#onCancel);
  }

  #open() {
    const dialog = this.#dialog;
    if (dialog.open) return;
    this.#returnFocus = document.activeElement;
    dialog.showModal();
    if (!dialog.contains(document.activeElement)) {
      (dialog.querySelector('[autofocus]') ?? dialog.querySelector(FOCUSABLE) ?? dialog).focus();
    }
    this.#changed(true);
  }

  #close() {
    if (this.#dialog.open) this.#dialog.close();
  }

  #changed(open) {
    this.dataset.state = this.#dialog.dataset.state = open ? 'open' : 'closed';
    this.#trigger?.setAttribute('aria-expanded', String(open));
    this.#reporter.report('bx-toggle', { open });
  }

  #onClick = (event) => {
    const dialog = this.#dialog;
    if (!dialog) return;
    const trigger = this.#trigger;
    // Clicks bubble out of a modal nested in this one: act only on what belongs to this modal.
    if (event.target.closest?.('bx-modal') !== this) return;
    if (trigger && trigger.contains(event.target)) {
      this.#open();
    } else if (event.target.closest?.('[data-bx-close]') && dialog.contains(event.target)) {
      this.#close();
    } else if (event.target === dialog && this.#pressedBackdrop && this.hasAttribute('dismissible')) {
      // The dialog element itself only receives clicks on its backdrop: its content fills it. Requiring the press to
      // start there too keeps a text selection dragged out of the content from closing the modal.
      this.#close();
    }
  };

  #onPointerDown = (event) => {
    this.#pressedBackdrop = event.target === this.#dialog;
  };

  // A cancelable "cancel" event is not guaranteed for repeated Escape presses, so stop the key itself as well.
  #onKeyDown = (event) => {
    if (event.key === 'Escape' && !this.hasAttribute('dismissible') && event.target.closest?.('bx-modal') === this) {
      event.preventDefault();
    }
  };

  #onCancel = (event) => {
    if (!this.hasAttribute('dismissible')) event.preventDefault();
  };

  #onClose = () => {
    this.#changed(false);
    const target = this.#returnFocus;
    this.#returnFocus = null;
    if (target?.isConnected && (document.activeElement === document.body || !document.activeElement)) target.focus();
  };
}

if (!customElements.get('bx-modal')) customElements.define('bx-modal', BxModalElement);
