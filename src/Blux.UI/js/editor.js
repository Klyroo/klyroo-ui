import { Reporter, ensureId, float, observeChildren } from './shared.js';

// The editing engine (TipTap) is a separate file, fetched the first time an editor element connects: a page without
// an editor never requests it. It sits next to this file in the package's static assets.
let enginePromise = null;
const loadEngine = () => (enginePromise ??= import(new URL('./blux-editor.js', import.meta.url).href));

const IDLE_MS = 400;
const PARTS = 'bx-editor-toolbar, bx-editor-drag-handle, bx-editor-menu';

/**
 * <bx-editor data-value="<p>…</p>" data-placeholder data-readonly disabled data-label data-labelledby data-describedby data-invalid>
 *   <input type="hidden" data-slot="value" name="…">          the posted HTML, kept in sync
 *   <bx-editor-toolbar>, <bx-editor-drag-handle>, <bx-editor-menu> …   parts, wired when the engine is ready
 *   <div data-slot="fallback">…</div>                          server-rendered HTML, shown until the engine is ready
 *   <div data-slot="editor" data-permanent></div>              empty: the editable surface is appended here
 * </bx-editor>
 *
 * The value is HTML. Changes are reported as bx-change after IDLE_MS without typing and on blur, only when bound.
 * The engine's dom is created by TipTap, so the surface carries data-permanent: an enhanced navigation leaves it alone.
 */
export class BxEditorElement extends HTMLElement {
  static observedAttributes = [
    'data-bx-seq', 'data-value', 'data-readonly', 'disabled', 'data-placeholder',
    'data-label', 'data-labelledby', 'data-describedby', 'data-invalid', 'data-required',
  ];

  engine = null;
  #loading = false;
  #observer = null;
  #reporter = new Reporter(this);
  #timer = 0;
  #lastReported = '';
  #wired = new Map();

  connectedCallback() {
    this.#lastReported = this.getAttribute('data-value') ?? '';
    this.addEventListener('focusout', this.#onFocusOut);
    this.addEventListener('keydown', this.#onKeyDown);
    this.#observer = observeChildren(this, () => this.#wire(), { ignore: '[data-slot="editor"]' });
  }

  disconnectedCallback() {
    this.removeEventListener('focusout', this.#onFocusOut);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.#observer?.disconnect();
    clearTimeout(this.#timer);
    for (const [part] of this.#wired) part.detach?.();
    this.#wired.clear();
    this.engine?.destroy();
    this.engine = null;
    this.removeAttribute('data-ready');
  }

  attributeChangedCallback(name) {
    if (!this.isConnected) return;
    if (name === 'data-bx-seq') {
      queueMicrotask(() => {
        this.#apply(true);
        this.#syncInput();
      });
    } else if (name === 'data-value') {
      this.#apply(false);
      queueMicrotask(() => this.#syncInput());
    } else if (name === 'data-readonly' || name === 'disabled') {
      this.#applyEditable();
    } else if (name === 'data-placeholder') {
      this.engine?.refresh();
    } else {
      this.#applyAria();
    }
  }

  /** After an enhanced load merged server HTML over this element. */
  refresh() {
    this.#reporter.adopt();
    if (!this.engine) return;
    this.#showEditor();
    this.#apply(this.#reporter.bound);
    this.#syncInput();
    this.#notify();
  }

  get editable() {
    return !this.hasAttribute('disabled') && !this.hasAttribute('data-readonly');
  }

  get #surface() {
    return this.querySelector(':scope [data-slot="editor"]');
  }

  get #input() {
    return this.querySelector(':scope > [data-slot="value"]');
  }

  #wire() {
    if (!this.engine && !this.#loading && this.#surface) this.#start();
    if (!this.engine) return;
    const parts = new Set(this.querySelectorAll(PARTS));
    for (const part of parts) {
      if (!this.#wired.has(part)) {
        this.#wired.set(part, true);
        part.attach?.(this);
      }
    }
    for (const part of [...this.#wired.keys()]) {
      if (!parts.has(part)) {
        part.detach?.();
        this.#wired.delete(part);
      }
    }
  }

  async #start() {
    this.#loading = true;
    try {
      const { createEngine } = await loadEngine();
      const surface = this.#surface;
      if (!this.isConnected || !surface || this.engine) return;
      this.#reporter.take('data-value', true);
      const html = this.getAttribute('data-value') ?? '';
      this.engine = createEngine({
        element: surface,
        html,
        editable: this.editable,
        placeholder: () => this.getAttribute('data-placeholder') ?? '',
        attributes: { role: 'textbox', 'aria-multiline': 'true', ...(this.dataset.editorId ? { id: this.dataset.editorId } : {}) },
        onUpdate: () => this.#onUpdate(),
        onState: () => this.#notify(),
        onBlur: () => {},
      });
      this.#lastReported = html;
      this.#applyAria();
      this.#showEditor();
      this.setAttribute('data-ready', '');
      this.#syncInput();
      this.#wire();
      this.#notify();
    } catch (error) {
      console.error('Blux: the editor could not be loaded.', error);
      this.setAttribute('data-error', '');
      enginePromise = null;
    } finally {
      this.#loading = false;
    }
  }

  /** The server-rendered HTML gives way to the editor. */
  #showEditor() {
    const fallback = this.querySelector(':scope [data-slot="fallback"]');
    if (fallback) fallback.hidden = true;
    this.setAttribute('data-ready', '');
  }

  #applyAria() {
    const dom = this.engine?.dom;
    if (!dom) return;
    const set = (name, value) => (value ? dom.setAttribute(name, value) : dom.removeAttribute(name));
    set('aria-label', this.getAttribute('data-labelledby') ? null : this.getAttribute('data-label'));
    set('aria-labelledby', this.getAttribute('data-labelledby'));
    set('aria-describedby', this.getAttribute('data-describedby'));
    set('aria-invalid', this.getAttribute('data-invalid'));
    set('aria-required', this.getAttribute('data-required'));
    set('aria-readonly', this.hasAttribute('data-readonly') ? 'true' : null);
    set('aria-disabled', this.hasAttribute('disabled') ? 'true' : null);
  }

  #applyEditable() {
    this.engine?.setEditable(this.editable);
    this.#applyAria();
    this.#notify();
  }

  /** The rendered value, when it is not just what this element reported itself. */
  #apply(force) {
    if (!this.engine) return;
    const value = this.#reporter.take('data-value', force);
    if (value === undefined || value === this.#lastReported) return;
    clearTimeout(this.#timer);
    this.#lastReported = value;
    if (value !== this.engine.getHTML()) this.engine.setHTML(value);
  }

  #onUpdate() {
    this.#syncInput();
    if (!this.#reporter.bound) return;
    clearTimeout(this.#timer);
    this.#timer = setTimeout(() => this.#flush(), IDLE_MS);
  }

  #flush() {
    clearTimeout(this.#timer);
    if (!this.engine || !this.#reporter.bound) return;
    const html = this.engine.getHTML();
    if (html === this.#lastReported) return;
    this.#lastReported = html;
    this.dispatchEvent(new Event('change', { bubbles: true }));
    this.#reporter.report('bx-change', { value: html });
  }

  #syncInput() {
    const input = this.#input;
    if (input && this.engine && input.value !== this.engine.getHTML()) input.value = this.engine.getHTML();
  }

  #notify() {
    for (const part of this.#wired.keys()) part.update?.();
  }

  #onFocusOut = (event) => {
    if (!this.contains(event.relatedTarget)) this.#flush();
  };

  #onKeyDown = (event) => {
    // Alt+F10 moves focus to a toolbar, as in other rich text editors.
    if (event.altKey && event.key === 'F10' && event.target === this.engine?.dom) {
      const toolbar = [...this.querySelectorAll('bx-editor-toolbar')].find((t) => !t.hidden && t.matches(':not([popover]), :popover-open'));
      if (toolbar) {
        event.preventDefault();
        toolbar.focusTool();
      }
    }
  };

  /** A menu opens or closes on the editable surface: it becomes a combobox owning the list. */
  setPopup(listboxId, activeId) {
    const dom = this.engine?.dom;
    if (!dom) return;
    if (listboxId) {
      dom.setAttribute('role', 'combobox');
      dom.removeAttribute('aria-multiline');
      dom.setAttribute('aria-expanded', 'true');
      dom.setAttribute('aria-controls', listboxId);
      dom.setAttribute('aria-autocomplete', 'list');
      dom.setAttribute('aria-haspopup', 'listbox');
      if (activeId) dom.setAttribute('aria-activedescendant', activeId);
      else dom.removeAttribute('aria-activedescendant');
    } else {
      dom.setAttribute('role', 'textbox');
      dom.setAttribute('aria-multiline', 'true');
      for (const name of ['aria-expanded', 'aria-controls', 'aria-autocomplete', 'aria-haspopup', 'aria-activedescendant']) dom.removeAttribute(name);
    }
  }
}

/**
 * Toolbar of buttons (data-action, data-level) following the WAI-ARIA toolbar pattern: one tab stop, arrow keys move.
 * Layout "bubble" is a manual popover that shows above a non-empty selection.
 *
 * <bx-editor-toolbar role="toolbar" data-layout="fixed|bubble" [popover="manual"]>
 *   <button type="button" data-action="bold" data-toggle>…</button> …
 *   <div data-slot="link-form" hidden><input data-slot="link-input"><button data-link="apply"> <button data-link="remove"></div>
 * </bx-editor-toolbar>
 */
export class BxEditorToolbarElement extends HTMLElement {
  #host = null;
  #stop = null;
  #observer = null;
  #tool = null;

  connectedCallback() {
    this.addEventListener('mousedown', this.#onMouseDown);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('focusout', this.#onFocusOut);
    this.#observer = observeChildren(this, () => this.update());
  }

  disconnectedCallback() {
    this.removeEventListener('mousedown', this.#onMouseDown);
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('focusout', this.#onFocusOut);
    this.#observer?.disconnect();
    this.#stop?.();
  }

  attach(host) {
    this.#host = host;
    this.update();
  }

  detach() {
    this.#host = null;
    this.#stop?.();
    this.#stop = null;
  }

  refresh() {
    this.update();
  }

  get #bubble() {
    return this.getAttribute('data-layout') === 'bubble';
  }

  get #tools() {
    return [...this.querySelectorAll('[data-action]')];
  }

  get #linkForm() {
    return this.querySelector('[data-slot="link-form"]');
  }

  focusTool() {
    this.update();
    (this.#tools.find((t) => t.tabIndex === 0 && !t.disabled) ?? this.#tools.find((t) => !t.disabled))?.focus();
  }

  update() {
    const engine = this.#host?.engine;
    for (const tool of this.#tools) {
      const action = tool.dataset.action;
      tool.disabled = !engine || !engine.can(action);
      if (tool.hasAttribute('data-toggle')) tool.setAttribute('aria-pressed', engine?.isActive(action, tool.dataset.level) ? 'true' : 'false');
    }
    this.#roving();
    if (this.#bubble) this.#placeBubble();
  }

  /** Exactly one enabled tool is in the tab order. */
  #roving() {
    const tools = this.#tools;
    const enabled = tools.filter((t) => !t.disabled);
    if (!this.#tool || !tools.includes(this.#tool) || this.#tool.disabled) this.#tool = enabled.find((t) => t.tabIndex === 0) ?? enabled[0] ?? null;
    for (const tool of tools) tool.tabIndex = tool === this.#tool ? 0 : -1;
  }

  #placeBubble() {
    const engine = this.#host?.engine;
    const inside = this.matches(':focus-within');
    const show = !!engine && engine.editable && ((engine.focused && engine.hasSelection) || (inside && (engine.hasSelection || !this.#linkForm?.hidden)));
    if (!show) {
      this.#stop?.();
      this.#stop = null;
      if (this.matches(':popover-open')) this.hidePopover();
      return;
    }
    if (!this.matches(':popover-open')) this.showPopover();
    if (!this.#stop) {
      const anchor = { getBoundingClientRect: () => this.#host.engine.selectionRect(), contextElement: this.#host };
      this.#stop = float(anchor, this, { placement: 'top', distance: 8 });
    }
  }

  // Pressing a button must not take focus (and the selection) from the editor.
  #onMouseDown = (event) => {
    if (event.target.closest('[data-action]') || event.target.closest('[data-link]')) event.preventDefault();
  };

  #onClick = (event) => {
    const link = event.target.closest('[data-link]');
    if (link) {
      this.#submitLink(link.dataset.link === 'remove');
      return;
    }
    const tool = event.target.closest('[data-action]');
    const engine = this.#host?.engine;
    if (!tool || !engine || tool.disabled) return;
    this.#tool = tool;
    if (tool.dataset.action === 'link') this.#openLink();
    else {
      engine.run(tool.dataset.action, tool.dataset.level);
      if (this.#bubble && !this.matches(':focus-within')) engine.focus();
    }
  };

  #onKeyDown = (event) => {
    const input = event.target.closest('[data-slot="link-input"]');
    if (input) {
      if (event.key === 'Enter') {
        event.preventDefault();
        this.#submitLink(false);
      } else if (event.key === 'Escape') {
        event.preventDefault();
        event.stopPropagation();
        this.#closeLink();
      }
      return;
    }
    const tools = this.#tools.filter((t) => !t.disabled);
    const index = tools.indexOf(event.target);
    if (index < 0) return;
    let target = null;
    // The buttons follow the writing direction: in a right-to-left page the next one is to the left.
    const forward = getComputedStyle(this).direction === 'rtl' ? 'ArrowLeft' : 'ArrowRight';
    const backward = forward === 'ArrowRight' ? 'ArrowLeft' : 'ArrowRight';
    if (event.key === forward) target = tools[(index + 1) % tools.length];
    else if (event.key === backward) target = tools[(index - 1 + tools.length) % tools.length];
    else if (event.key === 'Home') target = tools[0];
    else if (event.key === 'End') target = tools[tools.length - 1];
    else if (event.key === 'Escape') {
      event.preventDefault();
      this.#host?.engine?.focus();
      return;
    }
    if (!target) return;
    event.preventDefault();
    this.#tool = target;
    this.#roving();
    target.focus();
  };

  #onFocusOut = (event) => {
    if (this.#bubble && !this.contains(event.relatedTarget)) setTimeout(() => this.update(), 0);
  };

  #openLink() {
    const form = this.#linkForm;
    const input = form?.querySelector('[data-slot="link-input"]');
    if (!form || !input) return;
    form.hidden = false;
    input.value = this.#host.engine.linkHref();
    input.focus();
    input.select();
  }

  #closeLink() {
    const form = this.#linkForm;
    if (form) form.hidden = true;
    this.#host?.engine?.focus();
    this.update();
  }

  #submitLink(remove) {
    const input = this.#linkForm?.querySelector('[data-slot="link-input"]');
    const engine = this.#host?.engine;
    if (!input || !engine) return;
    const href = remove ? '' : input.value.trim();
    engine.run('link', href);
    this.#closeLink();
  }
}

/** A button that drags the block next to it; the engine places and wires it. */
export class BxEditorDragHandleElement extends HTMLElement {
  #detach = null;

  attach(host) {
    const button = this.querySelector('button');
    if (!button || this.#detach) return;
    this.#detach = host.engine.attachDragHandle(host, this, button);
  }

  detach() {
    this.#detach?.();
    this.#detach = null;
  }

  refresh() {
    if (!this.#detach) return;
    this.hidden = true;
  }
}

/**
 * Suggestion menu opened by a trigger character typed in the editor. Items are rendered by Blazor as options
 * (data-value, data-label, data-keywords, data-action, data-level); filtering and keys run here.
 *
 * <bx-editor-menu data-char="/" data-kind="suggestion|mention|emoji" data-min-query="0" data-limit="42">
 *   <div popover="manual" data-slot="content">
 *     <div role="listbox" aria-label="…"><div role="option" …></div></div>
 *     <p data-slot="empty" hidden>No results</p>
 *   </div>
 * </bx-editor-menu>
 */
export class BxEditorMenuElement extends HTMLElement {
  #host = null;
  #unregister = null;
  #context = null;
  #active = null;
  #stop = null;

  attach(host) {
    this.#host = host;
    this.#unregister?.();
    this.#unregister = host.engine.registerSuggestion({
      id: ensureId(this, 'editor-menu'),
      char: this.dataset.char ?? '/',
      startOfLine: this.hasAttribute('data-start-of-line'),
      onStart: (context) => this.#open(context),
      onUpdate: (context) => this.#open(context),
      onExit: () => this.#close(),
      onKeyDown: (event) => this.#key(event),
    });
    this.addEventListener('mousedown', this.#onMouseDown);
    this.addEventListener('click', this.#onClick);
    this.addEventListener('mousemove', this.#onMouseMove);
  }

  detach() {
    this.#unregister?.();
    this.#unregister = null;
    this.#close();
    this.removeEventListener('mousedown', this.#onMouseDown);
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('mousemove', this.#onMouseMove);
    this.#host = null;
  }

  get #content() {
    return this.querySelector('[data-slot="content"]');
  }

  get #listbox() {
    return this.querySelector('[role="listbox"]');
  }

  get #options() {
    return [...this.querySelectorAll('[role="option"]')];
  }

  /** Options a user can land on, in the order they are shown. */
  get #visible() {
    return this.#options
      .filter((o) => !o.hidden && o.getAttribute('aria-disabled') !== 'true')
      .sort((a, b) => (Number(a.style.order) || 0) - (Number(b.style.order) || 0));
  }

  #open(context) {
    this.#context = context;
    const query = context.query.toLowerCase();
    const minimum = Number(this.dataset.minQuery) || 0;
    const limit = Number(this.dataset.limit) || 42;
    if (context.query.length < minimum) {
      this.#close(false);
      return;
    }
    // Best matches first, by CSS order: the options Blazor rendered keep their place in the DOM.
    const rank = (option) => {
      const label = (option.dataset.label ?? '').toLowerCase();
      if (!query) return 0;
      if (label.startsWith(query)) return 0;
      if (label.includes(query)) return 1;
      return (option.dataset.keywords ?? '').toLowerCase().includes(query) ? 2 : -1;
    };
    const matches = this.#options.map((option, index) => ({ option, index, score: rank(option) }));
    for (const { option, score } of matches) option.hidden = score < 0;
    const shown = matches.filter((m) => m.score >= 0).sort((a, b) => a.score - b.score || a.index - b.index);
    shown.forEach(({ option }, position) => {
      option.hidden = position >= limit;
      option.style.order = String(position);
    });
    const empty = this.querySelector('[data-slot="empty"]');
    if (empty) empty.hidden = shown.length > 0;
    for (const option of this.#options) ensureId(option, 'editor-option');
    const first = this.#visible[0] ?? null;
    this.#highlight(this.#active && this.#visible.includes(this.#active) ? this.#active : first);
    const content = this.#content;
    if (!content.matches(':popover-open')) content.showPopover();
    if (!this.#stop) {
      const anchor = { getBoundingClientRect: () => this.#context?.rect() ?? new DOMRect(), contextElement: this.#host };
      this.#stop = float(anchor, content, { placement: 'bottom-start', distance: 6 });
    }
    this.#sync();
  }

  #close(clear = true) {
    this.#stop?.();
    this.#stop = null;
    const content = this.#content;
    if (content?.matches(':popover-open')) content.hidePopover();
    if (clear) this.#context = null;
    this.#active = null;
    for (const option of this.#options) {
      option.removeAttribute('data-highlighted');
      option.setAttribute('aria-selected', 'false');
    }
    this.#host?.setPopup(null, null);
  }

  #sync() {
    this.#host?.setPopup(ensureId(this.#listbox, 'editor-list'), this.#active?.id ?? null);
  }

  #highlight(option) {
    this.#active = option;
    for (const o of this.#options) {
      const on = o === option;
      o.toggleAttribute('data-highlighted', on);
      o.setAttribute('aria-selected', on ? 'true' : 'false');
    }
    option?.scrollIntoView({ block: 'nearest' });
  }

  #key(event) {
    if (!this.#content?.matches(':popover-open')) return false;
    const visible = this.#visible;
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      if (visible.length === 0) return false;
      const step = event.key === 'ArrowDown' ? 1 : -1;
      const index = visible.indexOf(this.#active);
      this.#highlight(visible[(index + step + visible.length) % visible.length]);
      this.#sync();
      return true;
    }
    if ((event.key === 'Enter' || event.key === 'Tab') && this.#active) {
      this.#choose(this.#active);
      return true;
    }
    return false;
  }

  #choose(option) {
    const context = this.#context;
    if (!context || option.getAttribute('aria-disabled') === 'true') return;
    const kind = this.dataset.kind;
    if (kind === 'mention') context.select({ type: 'mention', id: option.dataset.value, label: option.dataset.label });
    else if (kind === 'emoji') context.select({ type: 'text', text: option.dataset.value });
    else context.select({ type: 'action', action: option.dataset.action, level: Number(option.dataset.level) || 0 });
  }

  #onMouseDown = (event) => event.preventDefault();

  #onClick = (event) => {
    const option = event.target.closest('[role="option"]');
    if (option) this.#choose(option);
  };

  #onMouseMove = (event) => {
    const option = event.target.closest('[role="option"]');
    if (option && option !== this.#active && option.getAttribute('aria-disabled') !== 'true') {
      this.#highlight(option);
      this.#sync();
    }
  };
}

for (const [name, element] of [
  ['bx-editor', BxEditorElement],
  ['bx-editor-toolbar', BxEditorToolbarElement],
  ['bx-editor-drag-handle', BxEditorDragHandleElement],
  ['bx-editor-menu', BxEditorMenuElement],
]) {
  if (!customElements.get(name)) customElements.define(name, element);
}
