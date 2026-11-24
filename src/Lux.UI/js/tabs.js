import { Reporter, observeChildren, rovingKey } from './shared.js';

/**
 * <lx-tabs value="account" orientation="horizontal">
 *   <div role="tablist"><button role="tab" data-value="account" aria-selected="true" aria-controls="…">…</button>…</div>
 *   <div role="tabpanel" data-value="account">…</div> <div role="tabpanel" data-value="…" hidden>…</div>
 * </lx-tabs>
 *
 * WAI-ARIA tabs with automatic activation. The server renders the initial selection, so it reads correctly
 * without scripts; changes are reported with "lx-change".
 */
export class LxTabsElement extends HTMLElement {
  static observedAttributes = ['data-lx-seq', 'value'];

  #observer = null;
  #reporter = new Reporter(this);

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('keydown', this.#onKeyDown);
    this.#observer = observeChildren(this, () => this.#syncTabIndex());
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
  }

  // A render older than the last report is ignored; once .NET has caught up (data-lx-seq), its value is applied,
  // which also covers a handler that rejected or changed the user's choice.
  attributeChangedCallback(name) {
    // data-lx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-lx-seq') queueMicrotask(() => this.#apply());
    else this.#apply();
  }

  #apply() {
    const value = this.getAttribute('value');
    if (value !== null && !this.#reporter.isStale()) this.#select(value, false);
  }

  #own(selector) {
    return [...this.querySelectorAll(selector)].filter((el) => el.closest('lx-tabs') === this);
  }

  #tabs() {
    return this.#own('[role="tab"]').filter((tab) => !tab.disabled);
  }

  #syncTabIndex() {
    for (const tab of this.#own('[role="tab"]')) tab.tabIndex = tab.getAttribute('aria-selected') === 'true' ? 0 : -1;
  }

  #select(value, report) {
    let changed = false;
    for (const tab of this.#own('[role="tab"]')) {
      const selected = tab.dataset.value === value;
      if (selected && tab.getAttribute('aria-selected') !== 'true') changed = true;
      tab.setAttribute('aria-selected', String(selected));
      tab.dataset.state = selected ? 'active' : 'inactive';
      tab.tabIndex = selected ? 0 : -1;
    }
    for (const panel of this.#own('[role="tabpanel"]')) panel.hidden = panel.dataset.value !== value;
    if (changed && report) this.#reporter.report('lx-change', { value });
  }

  #onClick = (event) => {
    const tab = event.target.closest?.('[role="tab"]');
    if (tab && !tab.disabled && tab.closest('lx-tabs') === this) this.#select(tab.dataset.value, true);
  };

  #onKeyDown = (event) => {
    const tab = event.target.closest?.('[role="tab"]');
    if (!tab || tab.closest('lx-tabs') !== this) return;
    const vertical = this.getAttribute('orientation') === 'vertical';
    if (event.key.length === 1) return; // no typeahead in tab lists
    if (rovingKey(event, this.#tabs(), tab, { vertical })) this.#select(document.activeElement.dataset.value, true);
  };
}

if (!customElements.get('lx-tabs')) customElements.define('lx-tabs', LxTabsElement);
