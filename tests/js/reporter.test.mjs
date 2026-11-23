import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Reporter } from '../../src/Blux.UI/js/shared.js';

// The part of an element a Reporter touches.
function host(seq) {
  const attributes = new Map(seq === undefined ? [] : [['data-bx-seq', String(seq)]]);
  const events = [];
  return {
    attributes,
    events,
    getAttribute: (name) => (attributes.has(name) ? attributes.get(name) : null),
    hasAttribute: (name) => attributes.has(name),
    dispatchEvent: (event) => events.push(event),
  };
}

globalThis.CustomEvent ??= class {
  constructor(type, init) {
    this.type = type;
    this.detail = init?.detail;
  }
};

test('a reporter created on an input that already rendered sequence N continues above N once adopted', () => {
  const el = host(5);
  const reporter = new Reporter(el);
  reporter.adopt();
  reporter.report('bx-change', { value: 'x' });
  assert.equal(el.events[0].detail.sequence, 6);
  // A render that saw only report 5 is older than the report just sent.
  assert.equal(reporter.isStale(), true);
  el.attributes.set('data-bx-seq', '6');
  assert.equal(reporter.isStale(), false);
});

test('a change reserved before its report makes the render made before it stale, and the report goes above the reservation', () => {
  const el = host(3);
  const reporter = new Reporter(el);
  reporter.adopt();
  assert.equal(reporter.isStale(), false);
  reporter.reserve();
  assert.equal(reporter.isStale(), true);
  reporter.report('bx-toggle', { open: false });
  assert.equal(el.events[0].detail.sequence, 5);
  el.attributes.set('data-bx-seq', '5');
  assert.equal(reporter.isStale(), false);
});

test('without adopting, a recreated input would treat an old render as current', () => {
  const el = host(5);
  const reporter = new Reporter(el);
  reporter.report('bx-change', { value: 'x' });
  assert.equal(el.events[0].detail.sequence, 1);
  assert.equal(reporter.isStale(), false);
});
