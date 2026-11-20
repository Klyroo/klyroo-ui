import { test } from 'node:test';
import assert from 'node:assert/strict';
import { cookiePathFor } from '../../src/Blux.UI/js/base-path.js';

test('an app at the root keeps the cookie at /', () => {
  assert.equal(cookiePathFor('https://example.com/'), '/');
  assert.equal(cookiePathFor('http://127.0.0.1:5000'), '/');
});

test('an app under a path base scopes the cookie to it, without the trailing slash', () => {
  assert.equal(cookiePathFor('https://example.com/sub/'), '/sub');
  assert.equal(cookiePathFor('https://example.com/templates/blog/demo/'), '/templates/blog/demo');
  assert.equal(cookiePathFor('https://example.com/sub'), '/sub');
});

test('the page URL, the query and the fragment of the base do not matter', () => {
  assert.equal(cookiePathFor('https://example.com/sub/?a=1#top'), '/sub');
  assert.equal(cookiePathFor('https://example.com/sub//'), '/sub');
});

test('a path with a character that would end the cookie attribute falls back to /', () => {
  assert.equal(cookiePathFor('https://example.com/a;b/'), '/');
  assert.equal(cookiePathFor('https://example.com/a%3Bb/'), '/a%3Bb');
});

test('a missing or unparsable base falls back to /', () => {
  assert.equal(cookiePathFor(undefined), '/');
  assert.equal(cookiePathFor(null), '/');
  assert.equal(cookiePathFor(''), '/');
  assert.equal(cookiePathFor('not a url'), '/');
});

test('a web view base such as app://localhost/ is the root', () => {
  assert.equal(cookiePathFor('app://localhost/'), '/');
  assert.equal(cookiePathFor('https://0.0.0.0/'), '/');
});
