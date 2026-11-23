import test from 'node:test';
import assert from 'node:assert/strict';
import { readColumnPrefs, writeColumnPrefs } from '../../src/Blux.UI.Tools/js/column-prefs-cookie.js';

function fakeDoc(baseURI = 'http://localhost/', hasBase = true, initial = '') {
  let stored = initial;
  const writes = [];
  return {
    baseURI,
    querySelector: () => (hasBase ? {} : null),
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

test('writes and reads column prefs at root path', () => {
  const doc = fakeDoc('http://localhost/customers', true);
  assert.equal(writeColumnPrefs(doc, 'invoices', 'v1!hstatus!wcustomer*220', 'http:'), true);
  assert.equal(readColumnPrefs(doc, 'invoices'), 'v1!hstatus!wcustomer*220');
  assert.match(doc.writes[0], /^blux-dl-cols-invoices=v1!hstatus!wcustomer\*220; path=\/; max-age=31536000; SameSite=Lax$/);
});

test('scopes column prefs cookie below a path base and adds Secure on https', () => {
  const doc = fakeDoc('https://example.com/demo/dashboard/customers', true);
  doc.querySelector = (s) => (s === 'base[href]' ? { getAttribute: () => '/demo/dashboard/' } : null);
  // basePathFor reads doc.baseURI directly when hasBase is true.
  const baseDoc = fakeDoc('https://example.com/demo/dashboard/', true);
  assert.equal(writeColumnPrefs(baseDoc, 'cust', 'v1!sname', 'https:'), true);
  assert.equal(readColumnPrefs(baseDoc, 'cust'), 'v1!sname');
  assert.match(baseDoc.writes[0], /path=\/demo\/dashboard\/; max-age=31536000; SameSite=Lax; Secure$/);
});

test('empty value deletes the cookie', () => {
  const doc = fakeDoc('http://localhost/', true);
  writeColumnPrefs(doc, 'invoices', 'v1!hstatus');
  assert.equal(readColumnPrefs(doc, 'invoices'), 'v1!hstatus');
  assert.equal(writeColumnPrefs(doc, 'invoices', ''), true);
  assert.equal(readColumnPrefs(doc, 'invoices'), null);
  assert.match(doc.writes[1], /max-age=0/);
});

test('rejects unsafe keys and cookie values', () => {
  const doc = fakeDoc();
  assert.equal(writeColumnPrefs(doc, 'bad;key', 'v1!ha'), false);
  assert.equal(writeColumnPrefs(doc, 'good', 'bad;value'), false);
  assert.equal(readColumnPrefs(doc, 'bad key'), null);
});
