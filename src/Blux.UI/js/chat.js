import { Reporter } from './shared.js';

const NEAR_BOTTOM = 48;

const reducedMotion = () => matchMedia('(prefers-reduced-motion: reduce)').matches;

/**
 * <bx-chat-messages data-status data-auto-scroll="true">
 *   <div role="log" aria-live="off" tabindex="0" data-bx-chat-viewport>
 *     <div data-bx-chat-content-list>
 *       <article data-bx-chat-message data-role="assistant" data-streaming="true">…<div data-bx-chat-content>…</div></article>
 *     </div>
 *   </div>
 *   <button hidden data-bx-chat-jump>Jump to latest</button>
 *   <div role="status" data-bx-chat-announce></div>
 * </bx-chat-messages>
 *
 * Scroll: follows the end of the list while the user is at the bottom, stops as soon as they scroll up and shows the jump
 * button; sending a message (a new user message) follows again. The log is aria-live="off": an assistant message is announced
 * once, in the status region, when it stops streaming (or when it appears complete), never token by token. The text of the
 * region is written here; Blazor renders it empty.
 */
export class BxChatMessagesElement extends HTMLElement {
  #observer = null;
  #resize = null;
  #stick = true;
  #smoothing = 0;
  #ready = false;
  #known = new WeakSet();
  #announceTimer = 0;
  #initTimer = 0;
  #baselined = false;

  connectedCallback() {
    this.addEventListener('scroll', this.#onScroll, true);
    this.addEventListener('click', this.#onClick);
    this.#observer = new MutationObserver(this.#onMutation);
    this.#observer.observe(this, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['data-streaming'] });
    // Blazor inserts the element before its children: baseline once the whole batch is in.
    this.#initTimer = setTimeout(() => this.#init(), 0);
  }

  disconnectedCallback() {
    this.removeEventListener('scroll', this.#onScroll, true);
    this.removeEventListener('click', this.#onClick);
    this.#observer?.disconnect();
    this.#resize?.disconnect();
    this.#resize = null;
    clearTimeout(this.#initTimer);
    clearTimeout(this.#announceTimer);
    clearTimeout(this.#smoothing);
    this.#ready = false;
    this.#baselined = false;
  }

  /** After an enhanced navigation merge: the button and the position are recomputed from what is there. */
  refresh() {
    if (!this.#ready) return;
    this.#follow();
    this.#updateButton();
  }

  get #viewport() {
    return this.querySelector(':scope > [data-bx-chat-viewport]');
  }

  get #button() {
    return this.querySelector(':scope > [data-bx-chat-jump]');
  }

  get #follows() {
    return this.getAttribute('data-auto-scroll') !== 'false';
  }

  #init() {
    if (!this.isConnected || this.#ready) return;
    this.#ready = true;
    this.#scan();
    const list = this.#viewport?.querySelector('[data-bx-chat-content-list]');
    if (list && typeof ResizeObserver !== 'undefined') {
      this.#resize = new ResizeObserver(() => this.#onResize());
      this.#resize.observe(list);
    }
    this.#scrollToBottom(false);
    this.#updateButton();
  }

  #atBottom() {
    const v = this.#viewport;
    return !v || v.scrollHeight - v.scrollTop - v.clientHeight <= NEAR_BOTTOM;
  }

  #scrollToBottom(smooth) {
    const v = this.#viewport;
    if (!v) return;
    if (smooth && !reducedMotion()) {
      // The scroll events of the animation must not look like the user leaving the bottom.
      clearTimeout(this.#smoothing);
      this.#smoothing = setTimeout(() => {
        this.#smoothing = 0;
        this.#stick = this.#atBottom();
        this.#updateButton();
      }, 600);
      v.scrollTo({ top: v.scrollHeight, behavior: 'smooth' });
    } else {
      v.scrollTop = v.scrollHeight;
    }
  }

  #follow() {
    if (this.#follows && this.#stick && !this.#smoothing) this.#scrollToBottom(false);
  }

  #updateButton() {
    const button = this.#button;
    if (!button) return;
    const show = this.#follows && !this.#atBottom();
    if (button.hidden === show) button.hidden = !show;
  }

  #onScroll = (event) => {
    if (event.target !== this.#viewport || !this.#ready) return;
    if (!this.#smoothing) this.#stick = this.#atBottom();
    this.#updateButton();
  };

  #onClick = (event) => {
    if (!event.target.closest?.('[data-bx-chat-jump]')) return;
    this.#stick = true;
    this.#scrollToBottom(true);
    this.#viewport?.focus({ preventScroll: true });
  };

  #onResize() {
    this.#follow();
    this.#updateButton();
  }

  #onMutation = (records) => {
    if (!this.#ready) return;
    for (const record of records) {
      for (const node of record.addedNodes) {
        // The user sent something: show it, and the answer that follows.
        if (node.nodeType === Node.ELEMENT_NODE && this.#isMessage(node, 'user')) this.#stick = true;
      }
    }
    this.#follow();
    this.#scan();
    this.#updateButton();
  };

  #isMessage(node, role) {
    return node.matches?.(`[data-bx-chat-message][data-role="${role}"]`) || node.querySelector?.(`[data-bx-chat-message][data-role="${role}"]`) !== null;
  }

  #messages() {
    return [...this.querySelectorAll('[data-bx-chat-message][data-role="assistant"]')].filter((m) => m.closest('bx-chat-messages') === this);
  }

  /** Announces each assistant message once, when it is complete. Before the baseline nothing is announced. */
  #scan() {
    for (const message of this.#messages()) {
      if (this.#known.has(message) || message.getAttribute('data-streaming') === 'true') continue;
      this.#known.add(message);
      if (this.#baselined) this.#announce(message);
    }
    this.#baselined = true;
  }

  #announce(message) {
    const region = this.querySelector(':scope > [data-bx-chat-announce]');
    const text = (message.querySelector('[data-bx-chat-content]')?.textContent ?? '').replace(/\s+/g, ' ').trim();
    if (!region || !text) return;
    // Emptied first so the same text twice in a row is read twice.
    region.textContent = '';
    clearTimeout(this.#announceTimer);
    this.#announceTimer = setTimeout(() => {
      region.textContent = text;
    }, 60);
  }
}

if (!customElements.get('bx-chat-messages')) customElements.define('bx-chat-messages', BxChatMessagesElement);

/**
 * <bx-chat-prompt data-status data-busy data-bx-live data-bx-post data-submit-on-enter="false" data-max-rows="6">
 *   <span hidden data-bx-chat-sink data-bx-seq @onbxchange></span>
 *   <form method="post"><div data-bx-chat-attachments>…</div><textarea data-bx-chat-input></textarea> … </form>
 * </bx-chat-prompt>
 *
 * The textarea grows with its text up to data-max-rows. Enter submits (Shift+Enter adds a line; with data-submit-on-enter="false"
 * Ctrl or Cmd plus Enter submits); Enter while an IME is composing is never a submit. The submit event is handled here:
 * - nothing to send (empty, nothing attached), busy or disabled: cancelled;
 * - data-bx-live (interactive .NET listening): cancelled, and the text is reported with "bx-change" on the sink element, then cleared;
 * - data-bx-post: left to the browser, which posts the form (static server rendering);
 * - neither: cancelled, nothing could receive it.
 */
export class BxChatPromptElement extends HTMLElement {
  #reporter = null;
  #reporterSink = null;
  #resizeObserver = null;
  #width = 0;

  connectedCallback() {
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('input', this.#onInput);
    this.addEventListener('submit', this.#onSubmit);
    queueMicrotask(() => this.#grow());
    if (typeof ResizeObserver !== 'undefined') {
      this.#resizeObserver = new ResizeObserver(() => {
        // Only a change of width rewraps the text; a change of height is our own.
        const width = this.getBoundingClientRect().width;
        if (width !== this.#width) {
          this.#width = width;
          this.#grow();
        }
      });
      this.#resizeObserver.observe(this);
    }
  }

  disconnectedCallback() {
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('input', this.#onInput);
    this.removeEventListener('submit', this.#onSubmit);
    this.#resizeObserver?.disconnect();
    this.#resizeObserver = null;
  }

  /** After an enhanced navigation or form post merged server HTML over this element. */
  refresh() {
    if (this.#sink) this.#report.adopt();
    this.#grow();
  }

  get #input() {
    return this.querySelector('[data-bx-chat-input]');
  }

  get #form() {
    return this.querySelector('form');
  }

  get #sink() {
    return this.querySelector(':scope > [data-bx-chat-sink]');
  }

  get #report() {
    // The sink carries the sequence, so a nested component's events are never mistaken for a message.
    const sink = this.#sink;
    if (!this.#reporter || this.#reporterSink !== sink) {
      this.#reporterSink = sink;
      this.#reporter = new Reporter(sink);
    }
    return this.#reporter;
  }

  #grow() {
    const input = this.#input;
    if (!input) return;
    input.style.height = 'auto';
    if (input.scrollHeight === 0) return;
    const style = getComputedStyle(input);
    const fontSize = parseFloat(style.fontSize) || 14;
    const line = parseFloat(style.lineHeight) || fontSize * 1.4;
    const padding = (parseFloat(style.paddingTop) || 0) + (parseFloat(style.paddingBottom) || 0);
    const rows = Number(this.getAttribute('data-max-rows')) || 6;
    const max = line * rows + padding;
    input.style.height = `${Math.min(input.scrollHeight, max)}px`;
    input.style.overflowY = input.scrollHeight > max ? 'auto' : 'hidden';
  }

  #hasAttachments() {
    return this.querySelector('[data-bx-chat-attachments]')?.firstElementChild != null;
  }

  #blocked() {
    return this.hasAttribute('data-busy') || this.hasAttribute('data-disabled');
  }

  #onInput = (event) => {
    if (event.target === this.#input) this.#grow();
  };

  #onKeyDown = (event) => {
    const input = this.#input;
    if (event.target !== input) return;
    if (event.key === 'Escape') {
      input.blur();
      return;
    }
    if (event.key !== 'Enter') return;
    // keyCode 229: some browsers report the Enter that confirms an IME composition without isComposing.
    if (event.isComposing || event.keyCode === 229) return;
    const submitOnEnter = this.getAttribute('data-submit-on-enter') !== 'false';
    const wanted = submitOnEnter ? !event.shiftKey && !event.ctrlKey && !event.metaKey && !event.altKey : event.ctrlKey || event.metaKey;
    if (!wanted) return;
    event.preventDefault();
    if (this.#blocked()) return;
    this.#form?.requestSubmit();
  };

  #onSubmit = (event) => {
    const input = this.#input;
    if (!input || event.target !== this.#form) return;
    const text = input.value;
    if (this.#blocked() || (!text.trim() && !this.#hasAttachments())) {
      event.preventDefault();
      event.stopPropagation();
      return;
    }
    if (this.hasAttribute('data-bx-live')) {
      // .NET gets the text over the sink and never sees the form event.
      event.preventDefault();
      event.stopPropagation();
      this.#report.report('bx-change', { value: text });
      input.value = '';
      this.#grow();
      return;
    }
    // Static rendering: the browser posts the form. Without a form name there is nobody to receive it.
    if (!this.hasAttribute('data-bx-post')) event.preventDefault();
  };
}

if (!customElements.get('bx-chat-prompt')) customElements.define('bx-chat-prompt', BxChatPromptElement);

/**
 * <bx-chat-disclosure open data-streaming="true" data-auto-open data-close-delay="500">
 *   <button data-bx-disclosure-trigger aria-expanded aria-controls>…</button>
 *   <div data-bx-disclosure-content hidden="until-found">…</div>
 * </bx-chat-disclosure>
 *
 * A disclosure like <bx-collapsible> with one addition for streaming content (reasoning): with data-auto-open it opens when
 * data-streaming appears and closes data-close-delay ms after it goes (0 keeps it open), unless the user toggled it in the
 * meantime. Changes, the automatic ones included, are reported with "bx-toggle".
 */
export class BxChatDisclosureElement extends HTMLElement {
  static observedAttributes = ['data-bx-seq', 'open', 'data-streaming'];

  #reporter = new Reporter(this);
  #ready = false;
  #streaming = false;
  #autoOpened = false;
  #userToggled = false;
  #timer = 0;

  connectedCallback() {
    this.addEventListener('click', this.#onClick);
    this.addEventListener('beforematch', this.#onBeforeMatch, true);
    // Blazor inserts the element before its attributes and children: read them once the whole batch is applied.
    queueMicrotask(() => this.#init());
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.#onClick);
    this.removeEventListener('beforematch', this.#onBeforeMatch, true);
    clearTimeout(this.#timer);
    this.#ready = false;
  }

  attributeChangedCallback(name) {
    if (!this.#ready) return;
    // data-bx-seq is written before the other attributes of the same render: re-apply once they are all in place.
    if (name === 'data-bx-seq') queueMicrotask(() => this.#applyRendered(true));
    else if (name === 'open') this.#applyRendered(false);
    else this.#streamingChanged();
  }

  refresh() {
    if (!this.#ready) return;
    this.#reporter.adopt();
    this.#reporter.take('open', true);
    this.#set(this.#isOpenRendered());
  }

  get #trigger() {
    return this.querySelector(':scope > [data-bx-disclosure-trigger]');
  }

  get #content() {
    return this.querySelector(':scope > [data-bx-disclosure-content]');
  }

  #isOpenRendered() {
    return this.getAttribute('open') !== null;
  }

  #init() {
    if (!this.isConnected || this.#ready) return;
    this.#ready = true;
    this.#reporter.take('open', true);
    this.#set(this.#isOpenRendered());
    this.#streamingChanged();
  }

  #applyRendered(force) {
    const value = this.#reporter.take('open', force);
    if (value === undefined) return;
    this.#set(value !== null);
  }

  #isOpen() {
    return this.#trigger?.getAttribute('aria-expanded') === 'true';
  }

  #set(open) {
    this.#trigger?.setAttribute('aria-expanded', String(open));
    this.dataset.state = open ? 'open' : 'closed';
    const content = this.#content;
    if (content) {
      if (open) content.removeAttribute('hidden');
      else content.setAttribute('hidden', 'until-found');
    }
  }

  #change(open) {
    this.#set(open);
    this.#reporter.report('bx-toggle', { open });
  }

  #streamingChanged() {
    const streaming = this.getAttribute('data-streaming') === 'true';
    if (streaming === this.#streaming) return;
    this.#streaming = streaming;
    clearTimeout(this.#timer);
    if (!this.hasAttribute('data-auto-open')) return;
    if (streaming) {
      this.#userToggled = false;
      this.#autoOpened = false;
      if (!this.#isOpen()) {
        this.#autoOpened = true;
        this.#change(true);
      }
    } else if (this.#autoOpened) {
      this.#autoOpened = false;
      const delay = Number(this.getAttribute('data-close-delay') ?? 500);
      if (!this.#userToggled && delay > 0) {
        this.#timer = setTimeout(() => {
          if (!this.#userToggled && this.#isOpen()) this.#change(false);
        }, delay);
      }
    }
  }

  #onClick = (event) => {
    const trigger = event.target.closest?.('[data-bx-disclosure-trigger]');
    if (!trigger || trigger.disabled || trigger !== this.#trigger) return;
    this.#userToggled = true;
    clearTimeout(this.#timer);
    this.#change(!this.#isOpen());
  };

  #onBeforeMatch = (event) => {
    if (event.target === this.#content && !this.#isOpen()) this.#change(true);
  };
}

if (!customElements.get('bx-chat-disclosure')) customElements.define('bx-chat-disclosure', BxChatDisclosureElement);
