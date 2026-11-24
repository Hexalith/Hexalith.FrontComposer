import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const repoRoot = join(import.meta.dirname, '..', '..', '..');
const baseRef = process.env.GITHUB_BASE_REF;
const event = process.env.GITHUB_EVENT_NAME;
let changed;
let pushedCommit;

function git(...args) {
  return execFileSync('git', args, { cwd: repoRoot, encoding: 'utf8' });
}

function paths(...args) {
  return git(...args).split('\0').filter(Boolean);
}

function pushRange() {
  const eventPath = process.env.GITHUB_EVENT_PATH;
  if (!eventPath) {
    throw new Error('GitHub push event path is missing.');
  }

  const { before, after } = JSON.parse(readFileSync(eventPath, 'utf8'));
  const validCommit = (value) => typeof value === 'string' && /^[0-9a-f]{40}$/u.test(value) && !/^0{40}$/u.test(value);
  if (!validCommit(before) || !validCommit(after) || after !== git('rev-parse', 'HEAD').trim()) {
    throw new Error('GitHub push event must identify the previous commit and the checked-out commit.');
  }

  return [before, after];
}

try {
  if (event === 'push') {
    const [before, after] = pushRange();
    pushedCommit = after;
    changed = paths('diff', '--name-only', '--no-renames', '-z', before, after, '--');
  } else if (baseRef) {
    execFileSync('git', ['fetch', '--depth=1', 'origin', `refs/heads/${baseRef}`], { cwd: repoRoot, stdio: 'ignore' });
    changed = paths('diff', '--name-only', '--no-renames', '-z', 'FETCH_HEAD...HEAD', '--');
  } else {
    const tracked = paths('diff', '--name-only', '--no-renames', '-z', 'HEAD', '--');
    const untracked = paths('ls-files', '--others', '--exclude-standard', '-z');
    changed = [...tracked, ...untracked];
  }
} catch (error) {
  console.warn(`Visual baseline governance check could not inspect git diff: ${error.message}`);
  process.exit(event === 'push' || process.env.CI ? 1 : 0);
}

const snapshotChanges = changed.filter((path) =>
  path.startsWith('tests/e2e/specs/') && path.includes('-snapshots/') && path.endsWith('.png'));

if (snapshotChanges.length === 0) {
  console.log('No committed visual baseline changes detected.');
  process.exit(0);
}

const rationalePath = join(repoRoot, 'docs', 'accessibility-verification', 'baseline-change-rationale.md');
const rationaleRepoPath = 'docs/accessibility-verification/baseline-change-rationale.md';
if (!changed.includes(rationaleRepoPath)) {
  console.error(`Visual baselines changed without updating ${rationaleRepoPath}.`);
  process.exit(1);
}

let rationale;
try {
  if (pushedCommit) {
    rationale = git('show', `${pushedCommit}:${rationaleRepoPath}`).trim();
  } else if (existsSync(rationalePath)) {
    rationale = readFileSync(rationalePath, 'utf8').trim();
  }
} catch (error) {
  console.error(`Visual baseline rationale could not be read: ${error.message}`);
  process.exit(1);
}

if (rationale === undefined) {
  console.error(`Visual baselines changed without ${rationaleRepoPath}.`);
  process.exit(1);
}

if (rationale.length < 120
  || !/before/i.test(rationale)
  || !/after/i.test(rationale)
  || /Use this file only when/i.test(rationale)) {
  console.error('Visual baseline rationale must include a PR-specific paragraph plus before/after evidence or links.');
  process.exit(1);
}

console.log(`Visual baseline governance passed for ${snapshotChanges.length} changed snapshot(s).`);
