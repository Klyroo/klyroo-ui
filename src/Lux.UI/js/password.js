/**
 * Shows and hides the characters of a password field, with no .NET involved:
 *
 * <lx-password>
 *   <input type="password" data-slot="control">
 *   <button type="button" data-slot="toggle" aria-pressed="false" aria-label="Show password">…</button>
 * </lx-password>
 *
 * The button is a toggle button: its name never changes, aria-pressed says whether the password is visible.
 * The state is the type of the input, which Blazor renders as "password" and never touches again, so a render
 * cannot undo the user's choice.
 */
export class LxPasswordElement extends HTMLElement {
  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.refresh();
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
  }

  /** Re-applies the pressed state after enhanced navigation merged server HTML over the button. */
  refresh() {
    this.#show(this.#input?.type === 'text');
  }

  get #input() {
    return this.querySelector('input[data-slot="control"]');
  }

  get #button() {
    return this.querySelector('button[data-slot="toggle"]');
  }

  #show(visible) {
    this.#button?.setAttribute('aria-pressed', String(visible));
  }

  #onClick = (event) => {
    const button = this.#button;
    const input = this.#input;
    if (!button || !input || !(event.target instanceof Node) || !button.contains(event.target) || button.disabled) return;
    const visible = input.type === 'password';
    input.type = visible ? 'text' : 'password';
    this.#show(visible);
  };
}

if (!customElements.get('lx-password')) customElements.define('lx-password', LxPasswordElement);
