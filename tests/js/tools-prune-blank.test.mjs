import { test } from 'node:test';
import assert from 'node:assert/strict';
import { pruneBlank } from '../../src/Lux.UI.Tools/js/prune-blank.js';

// A form whose marked fields are the ones in the list.
const formOf = (fields) => ({ querySelectorAll: (selector) => (selector === '[data-lx-prune]' ? fields : []) });

test('a blank marked field is switched off for the submit and back on afterwards', () => {
  const blank = { value: '', disabled: false };
  const filled = { value: 'ann', disabled: false };

  const restore = pruneBlank(formOf([blank, filled]));

  assert.equal(blank.disabled, true);
  assert.equal(filled.disabled, false);
  restore();
  assert.equal(blank.disabled, false);
});

test('a field that was already disabled stays disabled', () => {
  const field = { value: '', disabled: true };

  pruneBlank(formOf([field]))();

  assert.equal(field.disabled, true);
});

test('a form without marked fields is left alone', () => {
  assert.doesNotThrow(() => pruneBlank(formOf([]))());
});
