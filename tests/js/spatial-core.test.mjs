import { test } from 'node:test';
import assert from 'node:assert/strict';
import { DIRECTIONS, pickNext, score } from '../../src/Blux.UI/js/spatial-core.js';

const box = (left, top, width = 100, height = 40) => ({ left, top, right: left + width, bottom: top + height });

// A 2 x 2 grid of buttons, with a wide one below.
const a = box(0, 0);
const b = box(150, 0);
const c = box(0, 100);
const d = box(150, 100);
const wide = box(0, 200, 250);

test('moves to the neighbour in the direction of travel', () => {
  assert.equal(pickNext(a, [b, c, d], 'right'), 0);
  assert.equal(pickNext(a, [b, c, d], 'down'), 1);
  assert.equal(pickNext(d, [a, b, c], 'left'), 2);
  assert.equal(pickNext(d, [a, b, c], 'up'), 1);
});

test('returns -1 when nothing lies that way', () => {
  assert.equal(pickNext(a, [b, c, d], 'left'), -1);
  assert.equal(pickNext(a, [b, c, d], 'up'), -1);
});

test('prefers the element in line over a closer one off to the side', () => {
  const near = box(120, 300);
  const inLine = box(0, 220, 100, 40);
  assert.equal(pickNext(a, [near, inLine], 'down'), 1);
});

test('an element that shares no row still wins when it is the only one that way', () => {
  assert.equal(pickNext(a, [box(300, 90)], 'right'), 0);
});

test('a wide element below is reached from either column', () => {
  assert.equal(pickNext(c, [a, d, wide], 'down'), 2);
  assert.equal(pickNext(d, [a, c, wide], 'down'), 2);
});

test('elements that overlap on the axis of travel count when their center lies beyond', () => {
  const inner = box(10, 10, 20, 20);
  const beyond = box(40, 10, 100, 20);
  assert.notEqual(score(inner, beyond, 'right'), null);
  assert.equal(score(beyond, inner, 'right'), null);
});

test('maps the arrow keys to directions', () => {
  assert.deepEqual(DIRECTIONS, { ArrowLeft: 'left', ArrowRight: 'right', ArrowUp: 'up', ArrowDown: 'down' });
});
