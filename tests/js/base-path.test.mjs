import { test } from 'node:test';
import assert from 'node:assert/strict';
import { basePathFor, cookiePathFor, slugFor, sidebarCookiePrefix, scopedKey, scopedCookieName } from '../../src/Blux.UI/js/base-path.js';

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
  assert.equal(cookiePathFor('https://example.com/a%3Bb/'), '/');
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

test('without a <base href> the document URL is not a base: a page at /dashboard/settings stays at the root', () => {
  assert.equal(cookiePathFor('https://example.com/dashboard/settings', false), '/');
  assert.equal(basePathFor('https://example.com/dashboard/settings', false), '');
  assert.equal(basePathFor('https://example.com/sub/', true), '/sub');
});

test('a base path is decoded for names and encoded again for the cookie path', () => {
  assert.equal(basePathFor('https://example.com/caf%C3%A9/'), '/café');
  assert.equal(cookiePathFor('https://example.com/caf%C3%A9/'), '/caf%C3%A9');
});

test('the slug of a base path is safe in a cookie name and never ambiguous', () => {
  assert.equal(slugFor(''), '');
  assert.equal(slugFor('/sub'), 'sub');
  assert.equal(slugFor('/templates/blog/demo'), 'templates.blog.demo');
  assert.equal(slugFor('/a-b'), 'a-b');
  assert.notEqual(slugFor('/a-b'), slugFor('/a/b'));
  assert.equal(slugFor('/a b'), 'a~20b');
  assert.equal(slugFor('/café'), 'caf~c3~a9');
  assert.match(slugFor('/x;y=z,"q"'), /^[A-Za-z0-9_.~-]+$/);
});

test('sidebar cookie names keep the root name and differ for every base path', () => {
  assert.equal(sidebarCookiePrefix(''), 'blux-sidebar-');
  assert.equal(sidebarCookiePrefix('/sub'), 'blux-sidebar.sub~~');
  assert.notEqual(sidebarCookiePrefix('/a'), sidebarCookiePrefix('/b'));
  // The id of a root app can never produce the name of an app below a base: they start differently.
  assert.ok(!(sidebarCookiePrefix('/sub') + 'app').startsWith('blux-sidebar-'));
});

test('storage keys are prefixed below a base path and untouched at the root', () => {
  assert.equal(scopedKey('', 'blux:dashboard:app:panels'), 'blux:dashboard:app:panels');
  assert.equal(scopedKey('/sub', 'blux:dashboard:app:panels'), '/sub:blux:dashboard:app:panels');
  assert.notEqual(scopedKey('/a', 'k'), scopedKey('/b', 'k'));
});

test('a named cookie keeps its name at the root and carries the slug below a path base', () => {
  assert.equal(scopedCookieName('', 'blux-timezone'), 'blux-timezone');
  assert.equal(scopedCookieName('/sub', 'blux-timezone'), 'blux-timezone.sub~~');
  assert.equal(scopedCookieName('/a/b c', 'blux-timezone'), 'blux-timezone.a.b~20c~~');
});
