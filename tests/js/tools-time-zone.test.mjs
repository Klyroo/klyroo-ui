import { test } from 'node:test';
import assert from 'node:assert/strict';
import { storeTimeZone, TIME_ZONE_COOKIE } from '../../src/Blux.UI.Tools/js/time-zone-cookie.js';

// A document with a cookie jar that keeps what is assigned, the way the name=value part of document.cookie does.
function fakeDocument(baseURI, hasBase = true) {
  const jar = new Map();
  const writes = [];
  return {
    baseURI,
    querySelector: () => (hasBase ? {} : null),
    writes,
    get cookie() {
      return [...jar].map(([k, v]) => `${k}=${v}`).join('; ');
    },
    set cookie(text) {
      writes.push(text);
      const [pair] = text.split(';');
      const i = pair.indexOf('=');
      jar.set(pair.slice(0, i), pair.slice(i + 1));
    },
  };
}

test('the zone is written under the plain name with path /, Lax and a long expiry at the root', () => {
  const doc = fakeDocument('https://example.com/', false);
  assert.equal(storeTimeZone(doc, 'Europe/Paris', 'https:'), true);
  assert.equal(doc.writes[0], `${TIME_ZONE_COOKIE}=Europe/Paris; path=/; max-age=34560000; SameSite=Lax; Secure`);
});

test('no Secure on http, and nothing is written again for the same zone', () => {
  const doc = fakeDocument('http://127.0.0.1:5000/', false);
  assert.equal(storeTimeZone(doc, 'Asia/Tokyo', 'http:'), true);
  assert.ok(!doc.writes[0].includes('Secure'));
  assert.equal(storeTimeZone(doc, 'Asia/Tokyo', 'http:'), false);
  assert.equal(doc.writes.length, 1);
  assert.equal(storeTimeZone(doc, 'Europe/Paris', 'http:'), true);
});

test('below a path base the name carries the slug and the path is the base', () => {
  const doc = fakeDocument('https://example.com/sub/');
  storeTimeZone(doc, 'Europe/Paris', 'https:');
  assert.ok(doc.writes[0].startsWith('blux-timezone.sub~~=Europe/Paris; path=/sub;'));
});

test('a missing zone or one that is not safe in a cookie is not written', () => {
  const doc = fakeDocument('https://example.com/', false);
  assert.equal(storeTimeZone(doc, null), false);
  assert.equal(storeTimeZone(doc, 'a;b'), false);
  assert.equal(storeTimeZone(doc, 'x'.repeat(65)), false);
  assert.equal(doc.writes.length, 0);
});

test('zones with a plus, like Etc/GMT+5, are written', () => {
  const doc = fakeDocument('https://example.com/', false);
  assert.equal(storeTimeZone(doc, 'Etc/GMT+5'), true);
});
