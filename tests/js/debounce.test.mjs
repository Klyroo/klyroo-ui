import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createDebouncer, parseDelay, holdsTypedText } from '../../src/Klyroo.UI/js/debounce.js';

/** Timers driven by hand: tick(ms) advances the clock and runs what is due. */
function fakeTimers() {
  let now = 0;
  let next = 1;
  const pending = new Map();
  return {
    set: (fn, ms) => {
      const id = next++;
      pending.set(id, { at: now + ms, fn });
      return id;
    },
    clear: (id) => pending.delete(id),
    tick(ms) {
      now += ms;
      for (const [id, timer] of [...pending]) {
        if (timer.at <= now) {
          pending.delete(id);
          timer.fn();
        }
      }
    },
    get count() {
      return pending.size;
    },
  };
}

test('a burst of schedules runs the callback once, after the last quiet period', () => {
  const timers = fakeTimers();
  let calls = 0;
  const d = createDebouncer(() => calls++, timers);
  d.schedule(300);
  timers.tick(200);
  d.schedule(300);
  timers.tick(200);
  d.schedule(300);
  timers.tick(299);
  assert.equal(calls, 0);
  assert.equal(d.pending, true);
  timers.tick(1);
  assert.equal(calls, 1);
  assert.equal(d.pending, false);
  assert.equal(timers.count, 0);
});

test('flush runs a pending callback now and only once', () => {
  const timers = fakeTimers();
  let calls = 0;
  const d = createDebouncer(() => calls++, timers);
  assert.equal(d.flush(), false);
  d.schedule(300);
  assert.equal(d.flush(), true);
  assert.equal(calls, 1);
  timers.tick(1000);
  assert.equal(calls, 1);
  assert.equal(d.flush(), false);
});

test('cancel drops the pending run', () => {
  const timers = fakeTimers();
  let calls = 0;
  const d = createDebouncer(() => calls++, timers);
  d.schedule(100);
  d.cancel();
  timers.tick(1000);
  assert.equal(calls, 0);
  assert.equal(d.pending, false);
  d.cancel();
});

test('the callback may schedule again', () => {
  const timers = fakeTimers();
  let calls = 0;
  const d = createDebouncer(() => {
    if (++calls < 2) d.schedule(10);
  }, timers);
  d.schedule(10);
  timers.tick(10);
  timers.tick(10);
  assert.equal(calls, 2);
});

test('the delay is a non-negative number of milliseconds, or null', () => {
  assert.equal(parseDelay('400'), 400);
  assert.equal(parseDelay('0'), 0);
  assert.equal(parseDelay(' 250 '), 250);
  for (const bad of [null, undefined, '', '  ', 'abc', '-1', 'Infinity']) assert.equal(parseDelay(bad), null);
});

test('a rendered value is ignored while typed text, a composition or an older render would be undone', () => {
  assert.equal(holdsTypedText({ dirty: false, composing: false, stale: false }), false);
  assert.equal(holdsTypedText({ dirty: true, composing: false, stale: false }), true);
  assert.equal(holdsTypedText({ dirty: false, composing: true, stale: false }), true);
  assert.equal(holdsTypedText({ dirty: false, composing: false, stale: true }), true);
});
