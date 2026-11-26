import { test } from 'node:test';
import assert from 'node:assert/strict';

// A small fake DOM: just the elements, attributes, boxes and selectors chart.js reads.
class FakeElement {
  constructor(localName, attrs = {}, box = null) {
    this.localName = localName;
    this.attrs = { ...attrs };
    this.children = [];
    this.parent = null;
    this.listeners = {};
    this.box = box ?? { left: 0, top: 0, width: 0, height: 0 };
    this.style = {};
    this.offsetWidth = 10;
    this.offsetHeight = 10;
    this.innerHTML = '';
    this.textContent = '';
  }
  get dataset() {
    const self = this;
    return new Proxy({}, {
      get: (_, key) => self.attrs['data-' + String(key)],
      set: (_, key, value) => {
        self.attrs['data-' + String(key)] = value;
        return true;
      },
    });
  }
  add(...nodes) {
    for (const node of nodes) {
      node.parent = this;
      this.children.push(node);
    }
    return this;
  }
  replaceChildren() {}
  hasAttribute(name) { return name in this.attrs; }
  setAttribute(name, value) { this.attrs[name] = value; }
  removeAttribute(name) { delete this.attrs[name]; }
  matches() { return false; }
  getBoundingClientRect() {
    const { left, top, width, height } = this.box;
    return { left, top, width, height, right: left + width, bottom: top + height };
  }
  contains(node) {
    for (let n = node; n; n = n.parent) if (n === this) return true;
    return false;
  }
  closest(selector) {
    for (let n = this; n; n = n.parent) if (n.matchesCompound?.(parseCompound(selector))) return n;
    return null;
  }
  matchesCompound(compound) {
    return (!compound.tag || compound.tag === this.localName) && compound.attrs.every(([name, value]) => name in this.attrs && (value === null || this.attrs[name] === value));
  }
  *descendants() {
    for (const child of this.children) {
      yield child;
      yield* child.descendants();
    }
  }
  querySelectorAll(selector) {
    return [...select(this, selector)];
  }
  querySelector(selector) {
    return select(this, selector).next().value ?? null;
  }
  addEventListener(type, fn) { (this.listeners[type] ??= []).push(fn); }
  removeEventListener(type, fn) { this.listeners[type] = (this.listeners[type] ?? []).filter((f) => f !== fn); }
  dispatchEvent(event) {
    event.target ??= this;
    for (let n = this; n; n = event.bubbles ? n.parent : null) for (const fn of n.listeners[event.type] ?? []) fn(event);
    return true;
  }
}

function parseCompound(text) {
  const match = /^([a-z]*)((?:\[[^\]]+\])*)$/.exec(text);
  const attrs = [...match[2].matchAll(/\[([^\]=]+)(?:="([^"]*)")?\]/g)].map((m) => [m[1], m[2] ?? null]);
  return { tag: match[1] || null, attrs };
}

// Supports "a b", "a > b", compound selectors with attributes, and a leading ":scope >".
function* select(root, selector) {
  const tokens = selector.replace(/\s*>\s*/g, ' > ').split(/\s+/);
  const steps = [];
  let combinator = ' ';
  for (const token of tokens) {
    if (token === '>') combinator = '>';
    else if (token === ':scope') steps.push({ scope: true });
    else {
      steps.push({ ...parseCompound(token), combinator });
      combinator = ' ';
    }
  }
  const matchesFrom = (node, index) => {
    const step = steps[index];
    if (step.scope) return node === root;
    if (!node.matchesCompound(step)) return false;
    if (index === 0) return true;
    const previous = steps[index - 1];
    if (step.combinator === '>') return node.parent ? matchesFrom(node.parent, index - 1) : false;
    for (let up = node.parent; up; up = up.parent) if (matchesFrom(up, index - 1)) return true;
    return previous.scope && root.contains(node);
  };
  for (const node of root.descendants()) if (matchesFrom(node, steps.length - 1)) yield node;
}

globalThis.HTMLElement = FakeElement;
globalThis.Element = FakeElement;
globalThis.CustomEvent = class { constructor(type, init = {}) { this.type = type; this.bubbles = !!init.bubbles; this.detail = init.detail; this.target = null; } };
globalThis.document = { activeElement: null };
const defined = new Map();
globalThis.customElements = { get: (name) => defined.get(name), define: (name, ctor) => defined.set(name, ctor) };

await import('../../src/Klyroo.UI/js/chart.js');
const KlyChartElement = defined.get('kly-chart');

const box = (left, top, width, height) => ({ left, top, width, height });
const key = (key) => ({ type: 'keydown', key, bubbles: true, altKey: false, ctrlKey: false, metaKey: false, preventDefault() {}, stopPropagation() {}, target: null });

/** Two rows of two series of bars (row 0 left half, row 1 right half), the markup KlyChart renders, with the tips the keyboard reads. */
function barChart({ selectable = true } = {}) {
  const chart = new KlyChartElement('kly-chart', selectable ? { 'data-selectable': '' } : {});
  const plot = new FakeElement('div', { 'data-slot': 'plot' }, box(0, 0, 400, 200));
  const svg = new FakeElement('svg');
  const series0 = new FakeElement('g', { 'data-series': '0' }).add(new FakeElement('rect', {}, box(20, 100, 30, 80)), new FakeElement('rect', {}, box(220, 60, 30, 120)));
  const series1 = new FakeElement('g', { 'data-series': '1' }).add(new FakeElement('rect', {}, box(60, 140, 30, 40)), new FakeElement('rect', {}, box(260, 20, 30, 160)));
  const hit0 = new FakeElement('g', { 'data-hit': '', 'data-index': '0' }).add(new FakeElement('rect', {}, box(0, 0, 200, 200)));
  const hit1 = new FakeElement('g', { 'data-hit': '', 'data-index': '1' }).add(new FakeElement('rect', {}, box(200, 0, 200, 200)));
  hit0.box = box(0, 0, 200, 200);
  hit1.box = box(200, 0, 200, 200);
  svg.add(series0, series1, hit0, hit1);
  const tips = new FakeElement('div', { 'data-slot': 'tips', hidden: '' }).add(
    new FakeElement('div', { 'data-tip': '0', 'data-text': 'row 0' }),
    new FakeElement('div', { 'data-tip': '1', 'data-text': 'row 1' }),
  );
  plot.add(svg, new FakeElement('div', { 'data-slot': 'tooltip' }), new FakeElement('div', { 'data-slot': 'status' }), tips);
  chart.add(plot);
  const picks = [];
  chart.addEventListener('kly-pick', (event) => picks.push(event.detail));
  chart.connectedCallback();
  return { chart, plot, hits: [hit0, hit1], picks, rect: (hit) => hit.children[0] };
}

const click = (target, x, y) => target.dispatchEvent({ type: 'click', bubbles: true, clientX: x, clientY: y, target });
const pointerMove = (target, x, y) => target.dispatchEvent({ type: 'pointermove', bubbles: true, clientX: x, clientY: y, target });

test('a click picks the row and the bar under the pointer', () => {
  const { rect, hits, picks } = barChart();

  click(rect(hits[0]), 70, 160);
  click(rect(hits[0]), 35, 160);
  click(rect(hits[1]), 275, 100);

  assert.deepEqual(picks, [{ row: 0, series: 1 }, { row: 0, series: 0 }, { row: 1, series: 1 }]);
});

test('a click on empty room picks the nearest bar of the row', () => {
  const { rect, hits, picks } = barChart();

  click(rect(hits[1]), 235, 30);

  assert.deepEqual(picks, [{ row: 1, series: 1 }]);
});

test('a chart without data-selectable reports nothing', () => {
  const { rect, hits, picks, plot } = barChart({ selectable: false });

  click(rect(hits[0]), 70, 160);
  plot.dispatchEvent({ ...key('ArrowRight'), target: plot });
  plot.dispatchEvent({ ...key('Enter'), target: plot });

  assert.deepEqual(picks, []);
});

test('Enter and Space pick the row the arrow keys are on and name no series', () => {
  const { plot, picks } = barChart();

  plot.dispatchEvent({ ...key('Enter'), target: plot });
  assert.deepEqual(picks, [], 'nothing before the keyboard chose a row');

  plot.dispatchEvent({ ...key('ArrowRight'), target: plot });
  plot.dispatchEvent({ ...key('ArrowRight'), target: plot });
  plot.dispatchEvent({ ...key(' '), target: plot });

  assert.deepEqual(picks, [{ row: 1, series: -1 }]);
});

test('Enter picks the keyboard row, not the one the pointer hovers', () => {
  const { plot, hits, rect, picks } = barChart();

  plot.dispatchEvent({ ...key('Home'), target: plot });
  pointerMove(rect(hits[1]), 275, 100);
  plot.dispatchEvent({ ...key('Enter'), target: plot });

  assert.deepEqual(picks, [{ row: 0, series: -1 }]);
});

test('a hover alone is not a keyboard choice', () => {
  const { plot, hits, rect, picks } = barChart();

  pointerMove(rect(hits[1]), 275, 100);
  plot.dispatchEvent({ ...key('Enter'), target: plot });

  assert.deepEqual(picks, []);
});

test('Escape forgets the keyboard row', () => {
  const { plot, picks } = barChart();

  plot.dispatchEvent({ ...key('End'), target: plot });
  plot.dispatchEvent({ ...key('Escape'), target: plot });
  plot.dispatchEvent({ ...key('Enter'), target: plot });

  assert.deepEqual(picks, []);
});

test('a marker chart picks the nearest marker for a pointer and none for the keyboard', () => {
  const chart = new KlyChartElement('kly-chart', { 'data-selectable': '' });
  const plot = new FakeElement('div', { 'data-slot': 'plot' }, box(0, 0, 400, 200));
  const hit = new FakeElement('g', { 'data-hit': '', 'data-index': '0' }, box(0, 0, 200, 200)).add(
    new FakeElement('rect', {}, box(0, 0, 200, 200)),
    new FakeElement('path', { 'data-series': '0' }, box(95, 36, 8, 8)),
    new FakeElement('path', { 'data-series': '1' }, box(95, 146, 8, 8)),
  );
  const slice = new FakeElement('path', { 'data-hit': '', 'data-index': '2' }, box(0, 0, 100, 100));
  plot.add(new FakeElement('svg').add(hit, slice), new FakeElement('div', { 'data-slot': 'tooltip' }), new FakeElement('div', { 'data-slot': 'status' }),
    new FakeElement('div', { 'data-slot': 'tips', hidden: '' }).add(new FakeElement('div', { 'data-tip': '0', 'data-text': 'a' }), new FakeElement('div', { 'data-tip': '2', 'data-text': 'c' })));
  chart.add(plot);
  const picks = [];
  chart.addEventListener('kly-pick', (event) => picks.push(event.detail));
  chart.connectedCallback();

  click(hit.children[0], 99, 150);
  click(hit.children[0], 99, 30);
  click(slice, 50, 50);
  plot.dispatchEvent({ ...key('Home'), target: plot });
  plot.dispatchEvent({ ...key('Enter'), target: plot });

  assert.deepEqual(picks, [{ row: 0, series: 1 }, { row: 0, series: 0 }, { row: 2, series: 0 }, { row: 0, series: -1 }]);
});
