import { test } from 'node:test';
import assert from 'node:assert/strict';
import { editKind, wantsKeyboard, enterLeaves, stepOf, caretAfter } from '../../src/Klyroo.UI/js/tv-edit-core.js';

test('fields that need an editing mode', () => {
  assert.equal(editKind({ tag: 'input', type: 'text' }), 'text');
  assert.equal(editKind({ tag: 'input', type: 'email' }), 'text');
  assert.equal(editKind({ tag: 'input', type: '' }), 'text');
  assert.equal(editKind({ tag: 'textarea' }), 'text');
  assert.equal(editKind({ tag: 'div', editable: true }), 'text');
  assert.equal(editKind({ tag: 'input', type: 'number' }), 'step');
  assert.equal(editKind({ tag: 'input', type: 'range' }), 'step');
  assert.equal(editKind({ tag: 'select' }), 'select');
});

test('buttons, checkboxes, radios and dates need no editing mode', () => {
  for (const type of ['checkbox', 'radio', 'button', 'submit', 'date', 'file']) assert.equal(editKind({ tag: 'input', type }), null);
  assert.equal(editKind({ tag: 'button' }), null);
});

test('the soft keyboard is for text and numbers, not for ranges and selects', () => {
  assert.equal(wantsKeyboard('text'), true);
  assert.equal(wantsKeyboard('step', 'number'), true);
  assert.equal(wantsKeyboard('step', 'range'), false);
  assert.equal(wantsKeyboard('select'), false);
  assert.equal(wantsKeyboard(null), false);
});

test('Enter leaves a single line and is typed in a multi-line field', () => {
  assert.equal(enterLeaves({ tag: 'input' }), true);
  assert.equal(enterLeaves({ tag: 'textarea' }), false);
  assert.equal(enterLeaves({ tag: 'div', editable: true }), false);
});

test('arrows raise or lower a value', () => {
  assert.equal(stepOf('ArrowUp'), 1);
  assert.equal(stepOf('ArrowRight'), 1);
  assert.equal(stepOf('ArrowDown'), -1);
  assert.equal(stepOf('ArrowLeft'), -1);
  assert.equal(stepOf('Enter'), 0);
});

test('the caret follows Left and Right inside the text', () => {
  assert.equal(caretAfter('abc', 1, 'ArrowLeft', false), 0);
  assert.equal(caretAfter('abc', 0, 'ArrowLeft', false), 0);
  assert.equal(caretAfter('abc', 2, 'ArrowRight', false), 3);
  assert.equal(caretAfter('abc', 3, 'ArrowRight', false), 3);
});

test('Up and Down keep the column across lines', () => {
  const text = 'hello\nab\nworld!';
  assert.equal(caretAfter(text, 4, 'ArrowDown', true), 8);
  assert.equal(caretAfter(text, 8, 'ArrowDown', true), 11);
  assert.equal(caretAfter(text, 15, 'ArrowUp', true), 8);
  assert.equal(caretAfter(text, 8, 'ArrowUp', true), 2);
  assert.equal(caretAfter(text, 2, 'ArrowUp', true), 0);
  assert.equal(caretAfter(text, 13, 'ArrowDown', true), text.length);
});

test('Up and Down in a single line go to its ends', () => {
  assert.equal(caretAfter('abc', 1, 'ArrowUp', false), 0);
  assert.equal(caretAfter('abc', 1, 'ArrowDown', false), 3);
});
