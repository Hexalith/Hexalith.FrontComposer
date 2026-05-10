import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const repoRoot = join(import.meta.dirname, '..', '..', '..');
const baseRef = process.env.GITHUB_BASE_REF;
let changed = [];

try {
if (baseRef) {
    execFileSync('git', ['fetch', 'origin', baseRef, '--depth=1'], { cwd: repoRoot, stdio: 'ignore' });
    changed = execFileSync('git', ['diff', '--name-only', 'FETCH_HEAD...HEAD'], { cwd: repoRoot, encoding: 'utf8' })
      .split(/\r?\n/)
      .filter(Boolean);
  } else {
    const tracked = execFileSync('git', ['diff', '--name-only', 'HEAD'], { cwd: repoRoot, encoding: 'utf8' })
      .split(/\r?\n/)
      .filter(Boolean);
    const untracked = execFileSync('git', ['ls-files', '--others', '--exclude-standard'], { cwd: repoRoot, encoding: 'utf8' })
      .split(/\r?\n/)
      .filter(Boolean);
    changed = [...tracked, ...untracked];
  }
} catch (error) {
  console.warn(`Visual baseline governance check could not inspect git diff: ${error.message}`);
  process.exit(0);
}

const snapshotChanges = changed.filter((path) =>
  path.startsWith('tests/e2e/specs/') && path.includes('-snapshots/') && path.endsWith('.png'));

if (snapshotChanges.length === 0) {
  console.log('No committed visual baseline changes detected.');
  process.exit(0);
}

const rationalePath = join(repoRoot, 'docs', 'accessibility-verification', 'baseline-change-rationale.md');
if (!existsSync(rationalePath)) {
  console.error(`Visual baselines changed without ${rationalePath}.`);
  process.exit(1);
}

const rationale = readFileSync(rationalePath, 'utf8').trim();
if (rationale.length < 120 || !/before/i.test(rationale) || !/after/i.test(rationale)) {
  console.error('Visual baseline rationale must include a paragraph plus before/after evidence or links.');
  process.exit(1);
}

console.log(`Visual baseline governance passed for ${snapshotChanges.length} changed snapshot(s).`);
