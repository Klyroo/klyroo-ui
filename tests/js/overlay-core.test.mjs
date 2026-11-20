import { test } from 'node:test';
import assert from 'node:assert/strict';
import { pickTopmost, pickConfinement } from '../../src/Blux.UI/js/overlay-core.js';

const layer = (name, stamp, order) => ({ name, stamp, order });

test('the layer opened last is the topmost, whatever its place in the document', () => {
  const dialog = layer('dialog', 1, 5);
  const popover = layer('popover', 2, 1);
  assert.equal(pickTopmost([dialog, popover]).name, 'popover');
  assert.equal(pickTopmost([popover, dialog]).name, 'popover');
});

test('a dialog opened after a popover is above it', () => {
  assert.equal(pickTopmost([layer('popover', 1, 9), layer('dialog', 2, 0)]).name, 'dialog');
});

test('layers with the same stamp are ordered by the document', () => {
  assert.equal(pickTopmost([layer('a', 0, 1), layer('b', 0, 2)]).name, 'b');
  assert.equal(pickTopmost([layer('b', 0, 2), layer('a', 0, 1)]).name, 'b');
});

test('no layers, no topmost', () => {
  assert.equal(pickTopmost([]), null);
});

test('focus is confined to the topmost modal', () => {
  assert.equal(pickConfinement([layer('m1', 1, 0), layer('m2', 2, 1)], []).name, 'm2');
});

test('a popover holding the focus that opened after the modal confines the focus', () => {
  assert.equal(pickConfinement([layer('modal', 1, 0)], [layer('menu', 2, 3)]).name, 'menu');
});

test('a popover holding the focus that opened before the modal does not', () => {
  assert.equal(pickConfinement([layer('modal', 2, 0)], [layer('menu', 1, 3)]).name, 'modal');
});

test('with no modal, a popover holding the focus confines it; with neither, nothing does', () => {
  assert.equal(pickConfinement([], [layer('menu', 1, 0)]).name, 'menu');
  assert.equal(pickConfinement([], []), null);
});
