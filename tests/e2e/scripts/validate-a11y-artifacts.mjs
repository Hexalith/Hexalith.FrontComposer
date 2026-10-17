import { existsSync, readdirSync, statSync, readFileSync } from 'node:fs';
import { validateStateEvidence } from './a11y-state-evidence.mjs';
import { basename, join, resolve } from 'node:path';
import manifest from '../specimens/frontcomposer-specimen-manifest.json' with { type: 'json' };

const required = [
  join(process.env.FC_E2E_HTML_REPORT_DIR ?? 'playwright-report', 'index.html'),
  join(process.env.FC_E2E_OUTPUT_DIR ?? 'test-results', 'junit.xml'),
  process.env.FC_E2E_JSON_PATH ?? join(process.env.FC_E2E_OUTPUT_DIR ?? 'test-results', 'playwright.json'),
];

const optionalEvidenceRoots = [
  process.env.FC_E2E_OUTPUT_DIR ?? 'test-results',
  process.env.FC_E2E_HTML_REPORT_DIR ?? 'playwright-report',
];

const missing = required.filter((path) => !existsSync(resolve(process.cwd(), path)));
if (missing.length > 0) {
  console.error(`Missing required Playwright artifact(s): ${missing.join(', ')}`);
  process.exit(1);
}

const files = optionalEvidenceRoots
  .map((path) => resolve(process.cwd(), path))
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
  .map((path) => resolve(process.cwd(), path))
  .filter(existsSync)
  .filter((path) => treeSize(path) > 50 * 1024 * 1024);

if (oversized.length > 0) {
  console.error(`Accessibility artifact root is unexpectedly large: ${oversized.join(', ')}`);
  process.exit(1);
}

const report = JSON.parse(readFileSync(required[2], 'utf8'));
const engines = process.env.FC_A11Y_EVIDENCE_ENGINE ? [process.env.FC_A11Y_EVIDENCE_ENGINE] : ['chromium'];
const stateErrors = engines.flatMap(expectedEngine => {
  const owned = files.filter(file => !/^ae-state-.+\.json$/u.test(basename(file)) || JSON.parse(readFileSync(file, 'utf8')).engine === expectedEngine);
  // Retained failed attempts may have the same basename. Only the final successful
  // result's attachment can establish this run's capability.
  const capabilityAttachments = [];
  const visit = suite => {
    for (const spec of suite.specs ?? []) for (const test of spec.tests ?? []) {
      const final = test.results?.at(-1);
      if (test.projectName !== expectedEngine || test.expectedStatus !== 'passed' || !['expected','flaky'].includes(test.status) || final?.status !== 'passed') continue;
      capabilityAttachments.push(...(final.attachments ?? []).filter(attachment => attachment.name === 'forced-color-capability' && attachment.contentType === 'application/json'));
    }
    for (const child of suite.suites ?? []) visit(child);
  };
  for (const suite of report.suites ?? []) visit(suite);
  const capabilityPath = capabilityAttachments.length === 1 ? capabilityAttachments[0].path : null;
  const unsupportedForcedColorsCapability = capabilityPath ? JSON.parse(readFileSync(capabilityPath, 'utf8')) : null;
  return validateStateEvidence(owned, { report, expectedEngine, expectedRun: process.env.FC_A11Y_EVIDENCE_RUN_ID || null, unsupportedForcedColorsCapability });
});
if (stateErrors.length) {
  console.error(stateErrors.join('\n'));
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
