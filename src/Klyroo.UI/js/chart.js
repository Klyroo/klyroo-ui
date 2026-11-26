/**
 * <kly-chart>
 *   <div data-slot="plot" tabindex="0">
 *     <svg>… <g data-hit data-index="0">…</g> …</svg>
 *     <div data-slot="tooltip" aria-hidden="true"></div>
 *     <div role="status" data-slot="status"></div>
 *     <div hidden data-slot="tips"><div data-tip="0" data-text="January: Desktop 186">…</div>…</div>
 *   </div>
 * </kly-chart>
 *
 * The server draws the whole chart; this element only shows a tooltip. Hovering a hit area (or a donut slice) shows the
 * hidden tip of its row next to the pointer; with the chart focused, the arrow keys, Home and End move between rows and
 * the tip's text is announced through the status region. Escape and blur hide it.
 *
 * Only a chart that has the data-selectable attribute (the server renders it when OnSelect has a handler) reports a pick to .NET:
 * a click on a hit area or a donut slice, and Enter or Space on the row the keyboard is on, raise kly-pick with the row (the
 * data-index) and the series the pick landed on (-1 when it cannot be told, and .NET takes the first series with a value).
 * A bar is found by the pointer position among the bars of the row, a point by the nearest marker, a slice is the only series.
 *
 * The tooltip is filled by cloning the tip into an element the server renders empty, and the active row is only marked
 * with an attribute the server does not render (data-active), so a render never fights it.
 */
export class KlyChartElement extends HTMLElement {
  #active = null;
  #shown = null;

  connectedCallback() {
    this.addEventListener('pointermove', this.#onPointerMove);
    this.addEventListener('pointerleave', this.#onPointerLeave);
    this.addEventListener('keydown', this.#onKeyDown);
    this.addEventListener('focusout', this.#onFocusOut);
    this.addEventListener('click', this.#onClick);
  }

  disconnectedCallback() {
    this.removeEventListener('pointermove', this.#onPointerMove);
    this.removeEventListener('pointerleave', this.#onPointerLeave);
    this.removeEventListener('keydown', this.#onKeyDown);
    this.removeEventListener('focusout', this.#onFocusOut);
    this.removeEventListener('click', this.#onClick);
    this.#hide();
  }

  /** After an enhanced navigation merged server HTML over the chart: the highlight and the tooltip are gone. */
  refresh() {
    this.#active = null;
    this.#hide();
  }

  get #plot() {
    return this.querySelector(':scope > [data-slot="plot"]');
  }

  get #hits() {
    return [...(this.#plot?.querySelectorAll('[data-hit]') ?? [])];
  }

  #show(hit, pointer) {
    const plot = this.#plot;
    const tooltip = plot?.querySelector(':scope > [data-slot="tooltip"]');
    if (!plot || !tooltip) return;
    const tip = plot.querySelector(`:scope > [data-slot="tips"] > [data-tip="${hit.dataset.index}"]`);
    if (!tip) return;
    if (this.#active !== hit) {
      this.#active?.removeAttribute('data-active');
      hit.setAttribute('data-active', '');
      this.#active = hit;
      this.#shown = null;
    }
    // A render can change the values of the row under the pointer: the tooltip follows the tip, not the row.
    const html = tip.innerHTML;
    if (this.#shown !== html) {
      this.#shown = html;
      tooltip.replaceChildren(...[...tip.children].map((child) => child.cloneNode(true)));
    }
    tooltip.dataset.state = 'open';

    const plotBox = plot.getBoundingClientRect();
    const hitBox = hit.getBoundingClientRect();
    const width = tooltip.offsetWidth;
    const height = tooltip.offsetHeight;
    // The pointer places the tooltip when there is one; the keyboard anchors it to the active row.
    const anchorX = pointer ? pointer.clientX : hitBox.left + hitBox.width / 2;
    const anchorY = pointer ? pointer.clientY : hitBox.top;
    let x = anchorX - plotBox.left - width / 2;
    let y = anchorY - plotBox.top - height - 12;
    x = Math.max(0, Math.min(x, plotBox.width - width));
    if (y < 0) y = anchorY - plotBox.top + 16;
    tooltip.style.transform = `translate(${Math.round(x)}px, ${Math.round(y)}px)`;
  }

  #hide() {
    this.#active?.removeAttribute('data-active');
    this.#active = null;
    this.#shown = null;
    const tooltip = this.#plot?.querySelector(':scope > [data-slot="tooltip"]');
    if (tooltip) tooltip.dataset.state = 'closed';
  }

  #announce(hit) {
    const status = this.#plot?.querySelector(':scope > [data-slot="status"]');
    const tip = this.#plot?.querySelector(`:scope > [data-slot="tips"] > [data-tip="${hit.dataset.index}"]`);
    if (status && tip) status.textContent = tip.dataset.text ?? '';
  }

  /** The series a pick landed on, or -1. A pointer picks the bar under it (else the nearest bar of the row) or the nearest marker; the keyboard (no pointer) the first. */
  #seriesAt(hit, pointer) {
    // A donut slice is its own series: .NET reads the one category.
    if (hit.localName !== 'g') return 0;
    const markers = [...hit.querySelectorAll('[data-series]')];
    if (markers.length > 0) {
      let best = markers[0];
      if (pointer) {
        let bestDistance = Infinity;
        for (const marker of markers) {
          const box = marker.getBoundingClientRect();
          const distance = Math.abs(box.top + box.height / 2 - pointer.clientY);
          if (distance < bestDistance) {
            best = marker;
            bestDistance = distance;
          }
        }
      }
      return Number(best.dataset.series);
    }
    // Bars: those whose center is in the hit area belong to this row.
    const area = hit.getBoundingClientRect();
    let found = -1;
    let foundDistance = Infinity;
    for (const group of this.#plot?.querySelectorAll('svg > g[data-series]') ?? []) {
      for (const bar of group.querySelectorAll('rect')) {
        const box = bar.getBoundingClientRect();
        const centerX = box.left + box.width / 2;
        const centerY = box.top + box.height / 2;
        if (centerX < area.left || centerX > area.right || centerY < area.top || centerY > area.bottom) continue;
        const dx = pointer ? Math.max(box.left - pointer.clientX, 0, pointer.clientX - box.right) : 0;
        const dy = pointer ? Math.max(box.top - pointer.clientY, 0, pointer.clientY - box.bottom) : 0;
        const distance = Math.hypot(dx, dy);
        if (distance < foundDistance) {
          found = Number(group.dataset.series);
          foundDistance = distance;
        }
      }
    }
    return found;
  }

  #pick(hit, pointer) {
    this.dispatchEvent(
      new CustomEvent('kly-pick', {
        bubbles: true,
        detail: { row: Number(hit.dataset.index), series: this.#seriesAt(hit, pointer) },
      }),
    );
  }

  #onClick = (event) => {
    if (!this.hasAttribute('data-selectable')) return;
    const hit = event.target instanceof Element ? event.target.closest('[data-hit]') : null;
    if (!hit || !this.#plot?.contains(hit)) return;
    this.#pick(hit, event);
  };

  #onPointerMove = (event) => {
    const hit = event.target instanceof Element ? event.target.closest('[data-hit]') : null;
    if (!hit || !this.#plot?.contains(hit)) {
      this.#hide();
      return;
    }
    this.#show(hit, event);
  };

  #onPointerLeave = () => {
    // The keyboard keeps its tooltip while the chart has focus.
    if (!this.contains(document.activeElement) || !this.#plot?.matches(':focus-visible')) this.#hide();
  };

  #onFocusOut = (event) => {
    if (!this.contains(event.relatedTarget)) this.#hide();
  };

  #onKeyDown = (event) => {
    if (event.target !== this.#plot || event.altKey || event.ctrlKey || event.metaKey) return;
    // Enter and Space pick the row the keyboard is on, when the chart reports picks.
    if ((event.key === 'Enter' || event.key === ' ') && this.hasAttribute('data-selectable')) {
      if (this.#active) {
        event.preventDefault();
        this.#pick(this.#active, null);
      }
      return;
    }
    const hits = this.#hits;
    if (hits.length === 0) return;
    const index = this.#active ? hits.indexOf(this.#active) : -1;
    let next = null;
    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        next = hits[Math.min(hits.length - 1, index + 1)];
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        next = hits[index < 0 ? hits.length - 1 : Math.max(0, index - 1)];
        break;
      case 'Home':
        next = hits[0];
        break;
      case 'End':
        next = hits[hits.length - 1];
        break;
      case 'Escape':
        // The first Escape dismisses the tooltip; with none open it belongs to whatever surrounds the chart (a modal).
        if (this.#plot?.querySelector(':scope > [data-slot="tooltip"]')?.dataset.state === 'open') {
          event.preventDefault();
          event.stopPropagation();
          this.#hide();
        }
        return;
      default:
        return;
    }
    event.preventDefault();
    this.#show(next, null);
    this.#announce(next);
  };
}

if (!customElements.get('kly-chart')) customElements.define('kly-chart', KlyChartElement);
