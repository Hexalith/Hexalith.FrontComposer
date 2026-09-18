const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const test = require('node:test');

const runner = path.join(__dirname, 'run-story-2-2-e2e.cjs');
const { gotoInteractivePage, startServer, waitForServer } = require(runner);

function runFixture(results) {
  const fixture = path.join(os.tmpdir(), `frontcomposer-story-2-2-${process.pid}-${Math.random()}.json`);
  fs.writeFileSync(fixture, JSON.stringify(results));
  try {
    return spawnSync(process.execPath, [runner, '--verify-results', fixture], {
      encoding: 'utf8',
    });
  }
  finally {
    fs.rmSync(fixture, { force: true });
  }
}

test('runner exits nonzero when any scenario result fails', () => {
  const result = runFixture([
    { scenario: 'passing scenario', status: 'pass' },
    { scenario: 'failing scenario', status: 'fail' },
    { scenario: 'skipped scenario', status: 'skipped' },
  ]);

  assert.equal(result.status, 1, `stdout=${result.stdout}\nstderr=${result.stderr}`);
});

test('runner exits zero when no scenario result fails', () => {
  const result = runFixture([
    { scenario: 'passing scenario', status: 'pass' },
    { scenario: 'skipped scenario', status: 'skipped' },
  ]);

  assert.equal(result.status, 0, `stdout=${result.stdout}\nstderr=${result.stderr}`);
});

test('server readiness rejects a terminated child immediately', async () => {
  await assert.rejects(
    waitForServer(
      'http://127.0.0.1:1',
      1000,
      Promise.resolve({ code: 1, signal: null, error: null })),
    /Counter host exited/);
});

test('server readiness aborts a stalled request at its deadline', async () => {
  const originalFetch = global.fetch;
  global.fetch = (_url, options) => new Promise((resolve, reject) => {
    options.signal.addEventListener('abort', () => reject(options.signal.reason), { once: true });
  });

  try {
    await assert.rejects(
      waitForServer('http://127.0.0.1:1', 25, new Promise(() => {})),
      /Timed out waiting/);
  }
  finally {
    global.fetch = originalFetch;
  }
});

test('interactive navigation rejects a non-OK document response', async () => {
  const page = {
    goto: async () => ({
      ok: () => false,
      status: () => 500,
    }),
  };

  await assert.rejects(
    gotoInteractivePage(page, 'http://127.0.0.1:5055/counter', 'Counter sample'),
    /HTTP 500/);
});

test('server startup monitors spawn errors', async () => {
  const child = new EventEmitter();
  child.stdout = new EventEmitter();
  child.stderr = new EventEmitter();
  const { terminationPromise } = startServer(() => child);
  const expected = new Error('spawn dotnet ENOENT');

  child.emit('error', expected);

  const termination = await terminationPromise;
  assert.equal(termination.error, expected);
});
