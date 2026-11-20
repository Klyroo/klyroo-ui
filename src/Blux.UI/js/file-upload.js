import { observeChildren } from './shared.js';

const SIZE_ATTRIBUTES = ['data-text-size-b', 'data-text-size-kb', 'data-text-size-mb', 'data-text-size-gb'];
// Last resort when the markup carries no templates.
const SIZE_FALLBACKS = ['{0} B', '{0} KB', '{0} MB', '{0} GB'];

/** The size as the locale writes it: the templates and the culture come from the attributes C# rendered. */
function formatSize(bytes, element) {
  let size = bytes;
  let unit = 0;
  while (size >= 1024 && unit < SIZE_ATTRIBUTES.length - 1) {
    size /= 1024;
    unit++;
  }
  const culture = element.getAttribute('data-culture') || undefined;
  let number;
  try {
    number = new Intl.NumberFormat(culture, { maximumFractionDigits: 1 }).format(size);
  } catch {
    number = String(Math.round(size * 10) / 10);
  }
  return (element.getAttribute(SIZE_ATTRIBUTES[unit]) ?? SIZE_FALLBACKS[unit]).replace('{0}', number);
}

const format = (template, ...args) => (template ?? '').replace(/\{(\d)\}/g, (_, i) => String(args[Number(i)] ?? ''));

function matchesAccept(file, accept) {
  const tokens = (accept ?? '').split(',').map((t) => t.trim().toLowerCase()).filter(Boolean);
  if (tokens.length === 0) return true;
  const name = file.name.toLowerCase();
  const type = (file.type ?? '').toLowerCase();
  return tokens.some((token) => {
    if (token.startsWith('.')) return name.endsWith(token);
    if (token.endsWith('/*')) return type.startsWith(token.slice(0, -1));
    return type === token;
  });
}

const CLOSE = 'M18 6 6 18M6 6l12 12';

/**
 * Drop zone, file list and limits around a native file input.
 *
 * <bx-file-upload multiple accept="image/*,.pdf" max-size="2097152" max-files="5">
 *   <label data-slot="dropzone"><input type="file" name="…"></label>
 *   <ul data-slot="files"></ul> <p data-slot="error" aria-live="polite"></p>
 * </bx-file-upload>
 *
 * The input stays the source of truth: dropped files and removals rewrite input.files (through DataTransfer) and
 * fire "change", so a static form posts exactly the files shown and a bound InputFile sees the same list.
 * The "change" listener runs in the capture phase, before Blazor's own, so files refused by the limits never reach .NET.
 */
export class BxFileUploadElement extends HTMLElement {
  #observer = null;
  #rendered = false;

  connectedCallback() {
    this.addEventListener('change', this.#onChange, true);
    this.addEventListener('dragenter', this.#onDrag);
    this.addEventListener('dragover', this.#onDrag);
    this.addEventListener('dragleave', this.#onDragLeave);
    this.addEventListener('drop', this.#onDrop);
    this.addEventListener('click', this.#onClick);
    // Blazor inserts the element before its children: render the list (empty, or from a restored input) once they are there.
    this.#observer = observeChildren(this, () => {
      if (this.#list && !this.#rendered) {
        this.#rendered = true;
        this.#render();
      }
    });
  }

  disconnectedCallback() {
    this.removeEventListener('change', this.#onChange, true);
    this.removeEventListener('dragenter', this.#onDrag);
    this.removeEventListener('dragover', this.#onDrag);
    this.removeEventListener('dragleave', this.#onDragLeave);
    this.removeEventListener('drop', this.#onDrop);
    this.removeEventListener('click', this.#onClick);
    this.#observer?.disconnect();
    this.#rendered = false;
  }

  refresh() {
    this.#render();
  }

  get #input() {
    return this.querySelector('input[type="file"]');
  }

  get #list() {
    return this.querySelector('[data-slot="files"]');
  }

  get #error() {
    return this.querySelector('[data-slot="error"]');
  }

  get #multiple() {
    return this.hasAttribute('multiple');
  }

  get #maxFiles() {
    const n = Number(this.getAttribute('max-files'));
    return n > 0 ? n : this.#multiple ? Infinity : 1;
  }

  #cls(slot) {
    return this.getAttribute(`data-cls-${slot}`) ?? '';
  }

  #files() {
    return [...(this.#input?.files ?? [])];
  }

  #setFiles(files) {
    const input = this.#input;
    if (!input) return;
    const transfer = new DataTransfer();
    for (const file of files) transfer.items.add(file);
    input.files = transfer.files;
  }

  /** Splits files into the ones that fit the limits and one message per refusal. */
  #validate(files) {
    const accept = this.getAttribute('accept');
    const maxSize = Number(this.getAttribute('max-size')) || Infinity;
    const accepted = [];
    const errors = [];
    for (const file of files) {
      if (!matchesAccept(file, accept)) errors.push(format(this.getAttribute('data-text-not-accepted'), file.name));
      else if (file.size > maxSize) errors.push(format(this.getAttribute('data-text-too-large'), file.name, formatSize(maxSize, this)));
      else if (accepted.length >= this.#maxFiles) {
        const message = format(this.getAttribute('data-text-too-many'), this.#maxFiles);
        if (!errors.includes(message)) errors.push(message);
      } else accepted.push(file);
    }
    return { accepted, errors };
  }

  #showErrors(errors) {
    const error = this.#error;
    if (error) error.textContent = errors.join(' ');
  }

  #render() {
    const list = this.#list;
    if (!list) return;
    const items = this.#files().map((file, index) => {
      const li = document.createElement('li');
      li.className = this.#cls('file');
      const name = document.createElement('span');
      name.className = this.#cls('file-name');
      name.textContent = file.name;
      const size = document.createElement('span');
      size.className = this.#cls('file-size');
      size.textContent = formatSize(file.size, this);
      const remove = document.createElement('button');
      remove.type = 'button';
      remove.className = this.#cls('remove');
      remove.dataset.index = String(index);
      remove.setAttribute('aria-label', format(this.getAttribute('data-text-remove'), file.name));
      remove.disabled = this.#input?.disabled ?? false;
      const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
      svg.setAttribute('viewBox', '0 0 24 24');
      svg.setAttribute('fill', 'none');
      svg.setAttribute('stroke', 'currentColor');
      svg.setAttribute('stroke-width', '2');
      svg.setAttribute('stroke-linecap', 'round');
      svg.setAttribute('stroke-linejoin', 'round');
      svg.setAttribute('aria-hidden', 'true');
      svg.setAttribute('class', 'size-4');
      const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
      path.setAttribute('d', CLOSE);
      svg.append(path);
      remove.append(svg);
      li.append(name, size, remove);
      return li;
    });
    list.replaceChildren(...items);
  }

  #onChange = (event) => {
    if (event.target !== this.#input) return;
    const { accepted, errors } = this.#validate(this.#files());
    if (accepted.length !== this.#files().length) this.#setFiles(accepted);
    this.#showErrors(errors);
    this.#render();
  };

  #onDrag = (event) => {
    if (!event.dataTransfer?.types.includes('Files') || this.#input?.disabled) return;
    event.preventDefault();
    this.querySelector('[data-slot="dropzone"]')?.setAttribute('data-dragging', '');
  };

  #onDragLeave = (event) => {
    if (!this.contains(event.relatedTarget)) this.querySelector('[data-slot="dropzone"]')?.removeAttribute('data-dragging');
  };

  #onDrop = (event) => {
    if (!event.dataTransfer?.files.length) return;
    event.preventDefault();
    this.querySelector('[data-slot="dropzone"]')?.removeAttribute('data-dragging');
    const input = this.#input;
    if (!input || input.disabled) return;
    const dropped = [...event.dataTransfer.files];
    this.#setFiles(this.#multiple ? [...this.#files(), ...dropped] : dropped.slice(0, 1));
    input.dispatchEvent(new Event('change', { bubbles: true }));
  };

  #onClick = (event) => {
    const remove = event.target.closest?.('button[data-index]');
    if (!remove || !this.#list?.contains(remove)) return;
    const index = Number(remove.dataset.index);
    this.#setFiles(this.#files().filter((_, i) => i !== index));
    this.#input.dispatchEvent(new Event('change', { bubbles: true }));
    (this.#list.querySelectorAll('button[data-index]')[index] ?? this.#input).focus({ preventScroll: true });
  };
}

if (!customElements.get('bx-file-upload')) customElements.define('bx-file-upload', BxFileUploadElement);
