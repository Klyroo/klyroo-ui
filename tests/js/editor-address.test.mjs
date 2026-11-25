import { test } from 'node:test';
import assert from 'node:assert/strict';
import { MAX_ADDRESS, describeSchemes, isImageAddress, isLinkAddress, parseSchemes } from '../../src/Klyroo.UI/js/editor-address.js';

test('schemes are read from a list of words, in lower case, and none means the default', () => {
  assert.deepEqual(parseSchemes('HTTPS  http,mailto'), ['https', 'http', 'mailto']);
  assert.equal(parseSchemes(''), null);
  assert.equal(parseSchemes(null), null);
  assert.equal(describeSchemes(['https', 'mailto']), 'https, mailto');
});

test('an image comes from http or https unless the page narrows it to one of them', () => {
  assert.equal(isImageAddress('https://example.com/a.png'), true);
  assert.equal(isImageAddress('http://example.com/a.png'), true);
  assert.equal(isImageAddress('http://example.com/a.png', ['https']), false);
  assert.equal(isImageAddress('https://example.com/a.png', ['https']), true);
  assert.equal(isImageAddress('HTTPS://EXAMPLE.COM/A.PNG', ['https']), true);
});

test('an image is never taken from another scheme, even when the page lists it', () => {
  for (const address of ['data:image/png;base64,AAAA', 'javascript:alert(1)', 'ftp://example.com/a.png', '/a.png', '//example.com/a.png', 'example.com/a.png']) {
    assert.equal(isImageAddress(address), false, address);
    assert.equal(isImageAddress(address, ['data', 'ftp', 'javascript']), false, address);
  }
});

test('an address with a space or a control character, no host, a user name or too long is refused', () => {
  for (const address of ['https://exa mple.com/a', 'https://example.com/a b.png', 'https://example.com/a\tb', 'https://example.com/\u0001', 'https://', 'https:example.com', 'https://user@example.com/a.png', 'https://user:secret@example.com/a.png', '']) {
    assert.equal(isImageAddress(address), false, JSON.stringify(address));
    assert.equal(isLinkAddress(address, ['https']), false, JSON.stringify(address));
  }
  const long = `https://example.com/${'a'.repeat(MAX_ADDRESS)}`;
  assert.equal(isImageAddress(long), false);
  assert.equal(isLinkAddress(long, ['https']), false);
});

test('without a list of schemes a link is left to the engine', () => {
  assert.equal(isLinkAddress('/relative', null), null);
  assert.equal(isLinkAddress('tel:+123', []), null);
});

test('with a list of schemes a link is an absolute address of one of them', () => {
  const schemes = ['https', 'http', 'mailto'];
  for (const address of ['https://example.com/', 'http://example.com/path?q=1#f', 'HTTPS://EXAMPLE.COM/', 'mailto:someone@example.com', '  https://example.com/ ']) {
    assert.equal(isLinkAddress(address, schemes), true, address);
  }
  for (const address of ['/relative', 'example.com', '//example.com/x', 'tel:+123456', 'ftp://example.com/f', 'javascript:alert(1)', 'java script:alert(1)', 'data:text/html;base64,AA', 'mailto:']) {
    assert.equal(isLinkAddress(address, schemes), false, address);
  }
  assert.equal(isLinkAddress('tel:+123456', ['tel']), true);
  assert.equal(isLinkAddress('https://example.com/', ['mailto']), false);
});
