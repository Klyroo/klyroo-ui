import test from 'node:test';
import assert from 'node:assert/strict';
import { readColumnPrefs, writeColumnPrefs } from '../../src/Lux.UI.Tools/js/column-prefs-cookie.js';
import { keyboardWidth, dragWidth } from '../../src/Lux.UI.Tools/js/column-resize.js';

function fakeDoc(baseURI = 'http://localhost/', hasBase = true, initial = '', host = null) {
  let stored = initial;
  const writes = [];
  return {
    baseURI,
    querySelector: () => (hasBase ? {} : null),
    documentElement: { getAttribute: (name) => (name === 'data-bx-host' ? host : null) },
    get cookie() { return stored; },
    set cookie(v) {
      writes.push(v);
      const pair = v.split(';')[0];
      const name = pair.split('=')[0];
      const parts = stored ? stored.split('; ').filter((p) => !p.startsWith(name + '=')) : [];
      if (!v.includes('max-age=0')) parts.push(pair);
      stored = parts.join('; ');
    },
    writes,
  };
}

function fakeStorage() {
  const map = new Map();
  return { map, getItem: (k) => map.get(k) ?? null, setItem: (k, v) => map.set(k, v), removeItem: (k) => map.delete(k) };
}

test('writes and reads column prefs at the root path in one grouped cookie', () => {
  const doc = fakeDoc('http://localhost/');
  assert.equal(writeColumnPrefs(doc, 'invoices', 'v1!hstatus!wcustomer*220', 'http:'), true);
  assert.equal(readColumnPrefs(doc, 'invoices'), 'v1!hstatus!wcustomer*220');
  assert.equal(doc.writes[0], 'blux-dl-cols=invoices.v1!hstatus!wcustomer*220; path=/; max-age=31536000; SameSite=Lax');
});

test('a page below the root without a base element is still the root app', () => {
  const doc = fakeDoc('http://localhost/customers/list', false);
  assert.equal(writeColumnPrefs(doc, 'invoices', 'v1!hstatus', 'http:'), true);
  assert.match(doc.writes[0], /^blux-dl-cols=invoices\.v1!hstatus; path=\/; /);
});

test('scopes the name and the path below a path base and adds Secure on https', () => {
  const doc = fakeDoc('https://example.com/demo/dashboard/');
  assert.equal(writeColumnPrefs(doc, 'cust', 'v1!sname', 'https:'), true);
  assert.equal(readColumnPrefs(doc, 'cust'), 'v1!sname');
  // The name carries the slug of the base (BxPathScope.CookieName), the path is the base itself.
  assert.equal(doc.writes[0], 'blux-dl-cols.demo.dashboard~~=cust.v1!sname; path=/demo/dashboard; max-age=31536000; SameSite=Lax; Secure');
});

test('the cookie of another app on the origin is not read', () => {
  const other = fakeDoc('https://example.com/other/');
  writeColumnPrefs(other, 'cust', 'v1!sother', 'https:');
  const doc = fakeDoc('https://example.com/demo/', true, other.cookie);
  assert.equal(readColumnPrefs(doc, 'cust'), null);
});

test('multiple lists share one grouped cookie in LRU order and clearing one keeps the others until the last is removed', () => {
  const doc = fakeDoc('http://localhost/');
  assert.equal(writeColumnPrefs(doc, 'invoices', 'v1!hstatus'), true);
  assert.equal(writeColumnPrefs(doc, 'customers', 'v1!sname'), true);
  assert.equal(doc.cookie, 'blux-dl-cols=customers.v1!sname!!invoices.v1!hstatus');
  assert.equal(readColumnPrefs(doc, 'invoices'), 'v1!hstatus');
  assert.equal(readColumnPrefs(doc, 'customers'), 'v1!sname');

  // Updating invoices promotes it to the front.
  assert.equal(writeColumnPrefs(doc, 'invoices', 'v1!hamount'), true);
  assert.equal(doc.cookie, 'blux-dl-cols=invoices.v1!hamount!!customers.v1!sname');

  // Clearing invoices keeps customers in the cookie.
  assert.equal(writeColumnPrefs(doc, 'invoices', ''), true);
  assert.equal(readColumnPrefs(doc, 'invoices'), null);
  assert.equal(readColumnPrefs(doc, 'customers'), 'v1!sname');
  assert.equal(doc.cookie, 'blux-dl-cols=customers.v1!sname');

  // Clearing the last list deletes the cookie.
  assert.equal(writeColumnPrefs(doc, 'customers', ''), true);
  assert.equal(readColumnPrefs(doc, 'customers'), null);
  assert.match(doc.writes.at(-1), /max-age=0/);
});

test('evicts the least-recently-updated lists when the grouped cookie exceeds 3000 characters or 32 lists', () => {
  const doc = fakeDoc('http://localhost/');
  const payload = 'v1!h' + 'a'.repeat(400);
  for (let i = 1; i <= 10; i++) {
    assert.equal(writeColumnPrefs(doc, `list-${i}`, payload), true);
  }
  assert.ok(doc.cookie.length <= 3020);
  // Most recent lists stay; oldest lists were evicted.
  assert.equal(readColumnPrefs(doc, 'list-10'), payload);
  assert.equal(readColumnPrefs(doc, 'list-1'), null);
});

test('rejects unsafe keys and values, on the way out and on the way in', () => {
  const doc = fakeDoc();
  assert.equal(writeColumnPrefs(doc, 'bad;key', 'v1!ha'), false);
  assert.equal(writeColumnPrefs(doc, 'good', 'bad;value'), false);
  assert.equal(writeColumnPrefs(doc, 'good', 'a%20b'), false, 'a percent sign would be decoded by the server');
  assert.equal(writeColumnPrefs(doc, 'good', 'a+b'), false);
  assert.equal(writeColumnPrefs(doc, 'good', 'v1!h' + 'a'.repeat(600)), false, 'longer than the per-list bound');
  assert.equal(readColumnPrefs(doc, 'bad key'), null);
  const tampered = fakeDoc('http://localhost/', true, 'blux-dl-cols=good.v1!h<script>');
  assert.equal(readColumnPrefs(tampered, 'good'), null);
  assert.equal(doc.writes.length, 0);
});

test('a web view keeps the preferences in localStorage under the same grouped name and writes no cookie', () => {
  const doc = fakeDoc('http://0.0.0.0/', false, '', 'webview');
  const storage = fakeStorage();
  assert.equal(writeColumnPrefs(doc, 'invoices', 'v1!hstatus', 'https:', storage), true);
  assert.equal(storage.map.get('blux-dl-cols'), 'invoices.v1!hstatus');
  assert.equal(doc.writes.length, 0);
  assert.equal(readColumnPrefs(doc, 'invoices', storage), 'v1!hstatus');
  assert.equal(writeColumnPrefs(doc, 'invoices', '', 'https:', storage), true);
  assert.equal(storage.map.size, 0);
  assert.equal(readColumnPrefs(doc, 'invoices', storage), null);
});

test('a web view whose storage is blocked reads nothing and reports the failure', () => {
  const doc = fakeDoc('http://0.0.0.0/', false, '', 'webview');
  const broken = { getItem() { throw new Error('blocked'); }, setItem() { throw new Error('blocked'); }, removeItem() { throw new Error('blocked'); } };
  assert.equal(writeColumnPrefs(doc, 'invoices', 'v1!hstatus', 'https:', broken), false);
  assert.equal(readColumnPrefs(doc, 'invoices', broken), null);
});

test('keyboard resize: arrows step by 16 and by 64 with Shift, limits clamp, Home and End go to the limits, Delete resets', () => {
  assert.equal(keyboardWidth('ArrowRight', 100, 64, 900), 116);
  assert.equal(keyboardWidth('ArrowLeft', 100, 64, 900), 84);
  assert.equal(keyboardWidth('ArrowRight', 100, 64, 900, { shift: true }), 164);
  assert.equal(keyboardWidth('ArrowLeft', 70, 64, 900), 64);
  assert.equal(keyboardWidth('ArrowRight', 890, 64, 900), 900);
  assert.equal(keyboardWidth('Home', 300, 80, 500), 80);
  assert.equal(keyboardWidth('End', 300, 80, 500), 500);
  assert.equal(keyboardWidth('Delete', 300, 80, 500), 'reset');
  assert.equal(keyboardWidth('a', 300, 80, 500), null);
});

test('keyboard resize in right-to-left text: ArrowLeft grows the column', () => {
  assert.equal(keyboardWidth('ArrowLeft', 100, 64, 900, { rtl: true }), 116);
  assert.equal(keyboardWidth('ArrowRight', 100, 64, 900, { rtl: true }), 84);
});

test('drag resize follows the pointer along the text direction and clamps', () => {
  assert.equal(dragWidth(150, 40, 64, 900), 190);
  assert.equal(dragWidth(150, -500, 64, 900), 64);
  assert.equal(dragWidth(150, 2000, 64, 900), 900);
  assert.equal(dragWidth(150, 40, 64, 900, true), 110);
});
