// Run frozen final regressions against the unchanged baseline validator in isolation.
import { createHash } from 'node:crypto';
import { copyFileSync, mkdtempSync, mkdirSync, readFileSync, rmSync, symlinkSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const evidence = dirname(fileURLToPath(import.meta.url));
const repo = resolve(evidence, '../../../../..');
const manifest = JSON.parse(readFileSync(join(evidence, 'red-source.json'), 'utf8'));
const scratch = mkdtempSync(join(tmpdir(), 'fc-png-red-replay-'));
try {
  for (const entry of manifest.inputs) {
    const source = resolve(evidence, entry.source);
    const bytes = readFileSync(source);
    if (createHash('sha256').update(bytes).digest('hex') !== entry.sha256) throw new Error(`Red input hash mismatch: ${entry.source}`);
    const target = join(scratch, entry.target);
    mkdirSync(dirname(target), { recursive: true });
    copyFileSync(source, target);
  }
  const e2e = join(scratch, 'tests/e2e');
  symlinkSync(join(repo, 'tests/e2e/node_modules'), join(e2e, 'node_modules'), 'dir');
  symlinkSync(join(repo, 'tests/e2e/specs'), join(e2e, 'specs'), 'dir');
  const result = spawnSync(process.execPath, ['--test', join(e2e, 'scripts/a11y-state-evidence.test.mjs')], { cwd: scratch, encoding: 'utf8' });
  process.stdout.write(result.stdout ?? ''); process.stderr.write(result.stderr ?? '');
  if (result.error) throw result.error;
  process.exitCode = result.status ?? 1;
} finally {
  rmSync(scratch, { recursive: true, force: true });
}
