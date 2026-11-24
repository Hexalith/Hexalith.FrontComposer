import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { copyFileSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';

const validator = 'tests/e2e/scripts/validate-visual-baseline-governance.mjs';
const screenshot = 'tests/e2e/specs/example.spec.ts-snapshots/unusual-é.png';
const rationale = 'docs/accessibility-verification/baseline-change-rationale.md';
const rationaleText = 'Before: the form heading wrapped across two lines at narrow widths. After: the form heading fits on one line and maintains the existing focus ring.';

function fixture(t) {
  const cwd = mkdtempSync(join(tmpdir(), 'fc-visual-governance-'));
  t.after(() => rmSync(cwd, { recursive: true, force: true }));
  const git = (...args) => execFileSync('git', args, { cwd, encoding: 'utf8' }).trim();
  const write = (path, contents) => {
    mkdirSync(dirname(join(cwd, path)), { recursive: true });
    writeFileSync(join(cwd, path), contents);
  };
  const commit = (subject) => {
    git('add', '--all');
    git('commit', '--quiet', '-m', subject);
    return git('rev-parse', 'HEAD');
  };
  const check = (event, payload, baseRef = '') => {
    const eventPath = join(cwd, '.git', 'event.json');
    writeFileSync(eventPath, JSON.stringify(payload));
    return spawnSync(process.execPath, [validator], {
      cwd,
      encoding: 'utf8',
      env: { ...process.env, CI: 'true', GITHUB_BASE_REF: baseRef, GITHUB_EVENT_NAME: event, GITHUB_EVENT_PATH: eventPath },
    });
  };

  git('init', '--quiet');
  git('config', 'user.name', 'Visual Governance Test');
  git('config', 'user.email', 'visual-test@example.com');
  git('config', 'commit.gpgsign', 'false');
  write(validator, '');
  copyFileSync(new URL('./validate-visual-baseline-governance.mjs', import.meta.url), join(cwd, validator));
  write(rationale, 'placeholder');
  const before = commit('Baseline');
  git('branch', 'base', before);
  git('remote', 'add', 'origin', cwd);
  return { before, check, commit, write };
}

test('a push checks snapshot changes from earlier commits in its range', (t) => {
  const { before, check, commit, write } = fixture(t);
  write(screenshot, 'changed');
  commit('Change baseline');
  write('README.md', 'unrelated');
  const after = commit('Finish push');

  const result = check('push', { before, after });
  assert.equal(result.status, 1);
  assert.match(result.stderr, /without updating/);
});

test('a committed rationale satisfies the push check', (t) => {
  const { before, check, commit, write } = fixture(t);
  write(screenshot, 'changed');
  write(rationale, rationaleText);
  const after = commit('Update baseline and rationale');

  const result = check('push', { before, after });
  assert.equal(result.status, 0, result.stderr);
  assert.match(result.stdout, /passed for 1 changed snapshot/);
});

test('missing, inaccessible, or mismatched push revisions fail verification', (t) => {
  const { before, check, commit, write } = fixture(t);
  write('README.md', 'unrelated');
  const after = commit('Finish push');
  for (const payload of [{ after }, { before: 'a'.repeat(40), after }, { before, after: before }]) {
    const result = check('push', payload);
    assert.equal(result.status, 1);
    assert.match(result.stderr, /could not inspect git diff/);
  }
});

test('local verification still detects an untracked snapshot', (t) => {
  const { check, write } = fixture(t);
  write(screenshot, 'untracked');

  const result = check('', {});
  assert.equal(result.status, 1);
  assert.match(result.stderr, /without updating/);
});

test('a dirty rationale cannot validate an invalid committed push rationale', (t) => {
  const { before, check, commit, write } = fixture(t);
  write(screenshot, 'changed');
  write(rationale, 'invalid replacement');
  const after = commit('Change baseline and invalid rationale');
  write(rationale, rationaleText);

  const result = check('push', { before, after });
  assert.equal(result.status, 1);
  assert.match(result.stderr, /must include/);
});

test('a push with unrelated changes requires no rationale', (t) => {
  const { before, check, commit, write } = fixture(t);
  write('README.md', 'unrelated');
  const after = commit('Documentation change');

  const result = check('push', { before, after });
  assert.equal(result.status, 0, result.stderr);
  assert.match(result.stdout, /No committed visual baseline changes/);
});

test('a pull request checks the merge-base diff', (t) => {
  const { check, commit, write } = fixture(t);
  write(screenshot, 'changed');
  commit('Change baseline');

  const result = check('pull_request', {}, 'base');
  assert.equal(result.status, 1);
  assert.match(result.stderr, /without updating/);
});
