import { existsSync, readdirSync, statSync } from 'node:fs';
import { basename, join } from 'node:path';
import manifest from '../specimens/frontcomposer-specimen-manifest.json' with { type: 'json' };

const required = [
  'playwright-report/index.html',
  'test-results/junit.xml',
];

const optionalEvidenceRoots = [
  'test-results',
  'playwright-report',
];

const missing = required.filter((path) => !existsSync(join(process.cwd(), path)));
if (missing.length > 0) {
  console.error(`Missing required Playwright artifact(s): ${missing.join(', ')}`);
  process.exit(1);
}

const files = optionalEvidenceRoots
  .map((path) => join(process.cwd(), path))
  .filter(existsSync)
  .flatMap(listFiles);
const expectedEvidence = manifest.routes
  .flatMap((route) => route.expectedArtifacts)
  .filter((artifact) => artifact.startsWith('axe-') || artifact.startsWith('focus-'));
const missingEvidence = expectedEvidence.filter((artifact) => !files.some((file) => basename(file) === artifact));
if (missingEvidence.length > 0) {
  console.error(`Missing expected accessibility evidence artifact(s): ${missingEvidence.join(', ')}`);
  process.exit(1);
}

const repoRoot = join(import.meta.dirname, '..', '..', '..');
const missingSnapshots = manifest.themeDensityCombinations
  .map((combination) => join(
    repoRoot,
    'tests',
    'e2e',
    'specs',
    'specimen-accessibility.spec.ts-snapshots',
    combination.artifact.replace(/\.png$/u, '-chromium-win32.png')))
  .filter((path) => !existsSync(path));
if (missingSnapshots.length > 0) {
  console.error(`Missing committed visual baseline snapshot(s): ${missingSnapshots.join(', ')}`);
  process.exit(1);
}

const oversized = optionalEvidenceRoots
  .map((path) => join(process.cwd(), path))
  .filter(existsSync)
  .filter((path) => treeSize(path) > 50 * 1024 * 1024);

if (oversized.length > 0) {
  console.error(`Accessibility artifact root is unexpectedly large: ${oversized.join(', ')}`);
  process.exit(1);
}

console.log('Accessibility artifact validation passed.');

function listFiles(root) {
  const result = [];
  for (const entry of readdirSync(root, { withFileTypes: true })) {
    const fullPath = join(root, entry.name);
    if (entry.isDirectory()) {
      result.push(...listFiles(fullPath));
    } else {
      result.push(fullPath);
    }
  }

  return result;
}

function treeSize(path) {
  const stat = statSync(path);
  if (!stat.isDirectory()) {
    return stat.size;
  }

  return readdirSync(path, { withFileTypes: true })
    .reduce((total, entry) => total + treeSize(join(path, entry.name)), 0);
}
