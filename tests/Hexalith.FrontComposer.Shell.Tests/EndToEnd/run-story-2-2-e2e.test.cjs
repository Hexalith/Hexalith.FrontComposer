const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const test = require('node:test');

const runner = path.join(__dirname, 'run-story-2-2-e2e.cjs');

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
