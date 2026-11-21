import { ensureId, float, isTvMode, observeChildren } from './shared.js';
import { ValueModel, SEP, navigable, syncGroups, syncSelectedIcon } from './form2-shared.js';
import { createDebouncer } from './debounce.js';

/**
 * Select with a filter field inside the list. The trigger is a select-only combobox button; the popover holds a
 * search input (focus lives there while open) and a listbox. Filtering runs in the browser.
 *
 * <bx-select-menu name="…" data-values="a␟b" data-multiple data-create data-create-label='Create "{0}"'>
 *   <span data-slot="values" hidden></span>
 *   <button role="combobox" data-slot="trigger"><span data-slot="label" data-label="…" data-placeholder-text="…"></span></button>
 *   <div popover data-slot="content">
 *     <input role="combobox" data-slot="search">
 *     <div role="listbox"><div role="option" data-value data-label>…</div><div role="option" data-slot="create" hidden>…</div></div>
 *     <p data-slot="empty" hidden>No results</p>
 *   </div>
 * </bx-select-menu>
 *
 * With data-remote (an interactive .NET component that loads the options from a source) the browser stops filtering: the list is
 * whatever .NET renders. The search text is reported as bx-search (value = the text) when the list opens (the empty text, at once)
 * and after data-debounce milliseconds of quiet while typing. .NET renders data-generation on the listbox, which changes each time
 * the options are replaced (not when a page is added): the list goes back to the top and the first option is highlighted. The labels
 * of the selected values are kept here (a search can drop their options from the list), seeded by data-known (value, label, value, label...).
 */
export class BxSelectMenuElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'data-values', 'name', 'disabled'];

  #observer = null;
  #stopFloating = null;
  #active = null;
  #model = new ValueModel(this, (values) => this.#paint(values));
  #labels = new Map();
  #generation = null;
  #awaiting = false;
  #searchSequence = 0;
  #debouncer = createDebouncer(() => this.#reportSearch(this.#search?.value ?? ''));

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('input', this.#onInput);
    this.addEventListener('pointermove', this.#onPointerMove);
    this.addEventListener('mousedown', this.#onMouseDown);
    this.addEventListener('toggle', this.#onToggle, true);
    this.#observer = observeChildren(this, () => this.#wire());
  }

  disconnectedCallback() {
    this.#observer?.disconnect();
    this.#stopFloating?.();
    this.#debouncer.cancel();
  }

  attributeChangedCallback(name) {
    if (!this.isConnected) return;
    this.#model.attributeChanged(name);
  }

  refresh() {
    this.#model.refresh();
  }

  get #remote() {
    return this.hasAttribute('data-remote');
  }

  get #multiple() {
    return this.hasAttribute('data-multiple');
  }

  get #trigger() {
    return this.querySelector(':scope > [data-slot="trigger"]');
  }

  get #content() {
    return this.querySelector(':scope > [popover]');
  }

  get #search() {
    return this.querySelector('[data-slot="search"]');
  }

  get #create() {
    return this.querySelector('[data-slot="create"]');
  }

  #options() {
    return [...this.querySelectorAll('[role="option"]:not([data-slot="create"])')];
  }

  #choices() {
    const create = this.#create;
    return navigable([...this.#options(), ...(create ? [create] : [])]);
  }

  #wire() {
    const trigger = this.#trigger;
    const content = this.#content;
    const listbox = content?.querySelector('[role="listbox"]');
    if (!trigger || !content || !listbox) return;
    if (trigger.popoverTargetElement !== content) trigger.popoverTargetElement = content;
    const listboxId = ensureId(listbox, 'listbox');
    trigger.setAttribute('aria-controls', listboxId);
    trigger.setAttribute('aria-expanded', String(this.#isOpen()));
    this.#search?.setAttribute('aria-controls', listboxId);
    for (const option of this.querySelectorAll('[role="option"]')) ensureId(option, 'option');
    if (this.#remote) this.#syncRemote(listbox);
    this.#model.ensure();
  }

  /** What a render of the options changes in remote mode: the labels to remember, and the highlight when the options were replaced. */
  #syncRemote(listbox) {
    const known = (this.getAttribute('data-known') ?? '').split(SEP);
    for (let i = 0; i + 1 < known.length; i += 2) {
      if (!this.#labels.has(known[i])) this.#labels.set(known[i], { label: known[i + 1], icon: '' });
    }
    for (const option of this.#options()) this.#remember(option);
    const generation = listbox.dataset.generation ?? null;
    if (generation !== this.#generation) {
      this.#generation = generation;
      this.#awaiting = false;
      const scroller = this.querySelector('[data-slot="scroller"]');
      if (scroller) scroller.scrollTop = 0;
      if (this.#isOpen()) this.#highlightDefault();
    } else if (this.#active && !this.#active.isConnected) {
      this.#active = null;
      this.#search?.removeAttribute('aria-activedescendant');
    }
  }

  #remember(option) {
    const value = option.dataset.value;
    if (value === undefined) return;
    const icon = option.hasAttribute('data-has-icon') ? (option.querySelector(':scope > svg')?.outerHTML ?? '') : '';
    this.#labels.set(value, { label: option.dataset.label ?? value, icon });
  }

  /** The selected option when the list holds it, else the first one a user can land on. */
  #highlightDefault() {
    const choices = this.#choices();
    this.#highlight(choices.find((o) => o.getAttribute('aria-selected') === 'true') ?? choices[0] ?? null);
  }

  #reportSearch(term) {
    this.dispatchEvent(new CustomEvent('bx-search', { bubbles: true, detail: { value: term, sequence: ++this.#searchSequence } }));
  }

  #isOpen() {
    return this.#content?.matches(':popover-open') ?? false;
  }

  #paint(values) {
    const options = this.#options();
    for (const option of options) option.setAttribute('aria-selected', String(values.includes(option.dataset.value)));
    // One selected value shows its icon; several have no single icon to show.
    const only = values.length === 1 ? values[0] : null;
    syncSelectedIcon(this, only === null ? null : (options.find((o) => o.dataset.value === only) ?? this.#cachedOption(only)));
    const label = this.querySelector('[data-slot="label"]');
    if (!label) return;
    if (values.length > 0) {
      label.dataset.label = values.map((v) => options.find((o) => o.dataset.value === v)?.dataset.label ?? this.#labels.get(v)?.label ?? v).join(', ');
      label.removeAttribute('data-placeholder');
    } else {
      label.dataset.label = label.dataset.placeholderText ?? '';
      label.setAttribute('data-placeholder', '');
    }
  }

  /** A stand-in for an option the list no longer holds, with the icon it had: what syncSelectedIcon reads. */
  #cachedOption(value) {
    const icon = this.#remote ? this.#labels.get(value)?.icon : '';
    if (!icon) return null;
    const stub = document.createElement('div');
    stub.setAttribute('data-has-icon', '');
    stub.innerHTML = icon;
    return stub;
  }

  #filter(text, { immediate = false } = {}) {
    if (this.#remote) {
      this.#filterRemote(text, immediate);
      return;
    }
    const query = text.trim().toLowerCase();
    let shown = 0;
    const options = this.#options();
    for (const option of options) {
      const match = !query || (option.dataset.label ?? '').toLowerCase().includes(query);
      option.hidden = !match;
      if (match) shown++;
    }
    syncGroups(this);
    const create = this.#create;
    if (create) {
      const exact = options.some((o) => (o.dataset.label ?? '').toLowerCase() === query) || this.#model.values.includes(text.trim());
      create.hidden = !query || exact;
      if (!create.hidden) {
        create.dataset.label = (this.dataset.createLabel ?? '{0}').replace('{0}', text.trim());
        shown++;
      }
    }
    const empty = this.querySelector('[data-slot="empty"]');
    if (empty) empty.hidden = shown > 0;
    this.#highlight(navigable(options)[0] ?? (create && !create.hidden ? create : null));
  }

  /**
   * Remote: the options are not filtered here, .NET renders the answer. The text is reported (at once when the list opens, after the
   * quiet time while typing); until the answer replaces the options nothing is highlighted, so Enter cannot choose an option of the
   * previous text. Only the option that creates the typed text is decided here.
   */
  #filterRemote(text, immediate) {
    const query = text.trim();
    const create = this.#create;
    if (create) {
      const lower = query.toLowerCase();
      const exact = this.#options().some((o) => (o.dataset.label ?? '').toLowerCase() === lower) || this.#model.values.includes(query);
      create.hidden = !query || exact;
      if (!create.hidden) create.dataset.label = (this.dataset.createLabel ?? '{0}').replace('{0}', query);
    }
    if (immediate) {
      this.#debouncer.cancel();
      this.#reportSearch(text);
      return;
    }
    this.#awaiting = true;
    this.#highlight(null);
    const delay = Number(this.getAttribute('data-debounce'));
    this.#debouncer.schedule(Number.isFinite(delay) && delay >= 0 ? delay : 250);
  }

  #highlight(option) {
    this.#active?.removeAttribute('data-highlighted');
    this.#active = option;
    const search = this.#search;
    if (option) {
      option.setAttribute('data-highlighted', '');
      option.scrollIntoView({ block: 'nearest' });
      search?.setAttribute('aria-activedescendant', option.id);
    } else {
      search?.removeAttribute('aria-activedescendant');
    }
  }

  #choose(option) {
    if (!option || option.getAttribute('aria-disabled') === 'true') return;
    const search = this.#search;
    const value = option.dataset.slot === 'create' ? search.value.trim() : option.dataset.value;
    if (this.#remote && option.dataset.slot !== 'create') this.#remember(option);
    const values = this.#model.values;
    if (this.#multiple) {
      this.#model.set(values.includes(value) ? values.filter((v) => v !== value) : [...values, value]);
      if (option.dataset.slot === 'create') {
        search.value = '';
        this.#filter('', { immediate: true });
      }
    } else {
      this.#model.set([value]);
      this.#content.hidePopover();
    }
  }

  // "toggle" fires asynchronously: keys typed right after opening from the keyboard are handled before it, so the
  // keyboard path calls this synchronously and the toggle handler repeats it harmlessly.
  #opened() {
    const content = this.#content;
    const search = this.#search;
    this.#trigger.setAttribute('aria-expanded', 'true');
    this.dataset.state = content.dataset.state = 'open';
    if (!this.#stopFloating) {
      this.#stopFloating = float(this.#trigger, content, { placement: 'bottom-start', distance: 4, matchWidth: true });
      if (search) search.value = '';
      this.#filter('', { immediate: true });
      if (this.#remote) {
        // The list kept from the last opening is shown while the empty text is searched again.
        this.#highlightDefault();
      } else {
        const selected = this.#choices().find((o) => o.getAttribute('aria-selected') === 'true');
        if (selected) this.#highlight(selected);
      }
    }
    search?.focus({ preventScroll: true });
  }

  #onToggle = (event) => {
    const content = this.#content;
    if (event.target !== content) return;
    if (event.newState === 'open') {
      this.#opened();
      return;
    }
    this.#trigger.setAttribute('aria-expanded', 'false');
    this.dataset.state = content.dataset.state = 'closed';
    this.#stopFloating?.();
    this.#stopFloating = null;
    this.#debouncer.cancel();
    this.#highlight(null);
  };

  #onKeyDown = (event) => {
    const trigger = this.#trigger;
    if (event.target === trigger) {
      if (!this.#isOpen() && !isTvMode() && ['ArrowDown', 'ArrowUp'].includes(event.key)) {
        event.preventDefault();
        this.#content.showPopover();
        this.#opened();
      }
      return;
    }
    if (event.target !== this.#search) return;
    const choices = this.#choices();
    const index = choices.indexOf(this.#active);
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.#highlight(choices[Math.min(index + 1, choices.length - 1)] ?? null);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.#highlight(choices[Math.max(index - 1, 0)] ?? null);
        break;
      case 'Enter': {
        event.preventDefault();
        // Nothing to choose while the list failed to load: Enter asks again.
        const retry = this.#active ? null : this.querySelector('[data-slot="retry"]');
        if (retry) retry.click();
        else this.#choose(this.#active);
        break;
      }
      case 'Tab':
        this.#content.hidePopover();
        trigger.focus({ preventScroll: true });
        break;
    }
  };

  #onInput = (event) => {
    if (event.target === this.#search) this.#filter(event.target.value);
  };

  #onClick = (event) => {
    const option = event.target.closest?.('[role="option"]');
    if (option && this.contains(option)) this.#choose(option);
  };

  // Keep focus in the search field while the pointer works the list.
  #onMouseDown = (event) => {
    if (this.#content?.contains(event.target) && event.target !== this.#search) event.preventDefault();
  };

  #onPointerMove = (event) => {
    const option = event.target.closest?.('[role="option"]');
    if (option && option !== this.#active && option.getAttribute('aria-disabled') !== 'true') this.#highlight(option);
  };
}

if (!customElements.get('bx-select-menu')) customElements.define('bx-select-menu', BxSelectMenuElement);
