// Replay existing state validation against the owned final packet for each engine.
import { readdirSync, readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { dirname, join, resolve, basename } from 'node:path';
import { fileURLToPath } from 'node:url';
import { validateStateEvidence, requiredStates } from '../../../../../tests/e2e/scripts/a11y-state-evidence.mjs';

const root = dirname(fileURLToPath(import.meta.url));
const map = JSON.parse(readFileSync(join(root, 'attachment-path-map.json'), 'utf8'));
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
const walk = path => readdirSync(path, { withFileTypes: true }).flatMap(entry => entry.isDirectory() ? walk(join(path, entry.name)) : [join(path, entry.name)]);
const summary = [];
const lanes = [
  { directory: 'browser', engines: ['chromium', 'firefox'], runId: 'story-13-6-attempt-ownership-20261018' },
  { directory: 'browser-webkit', engines: ['webkit'], runId: 'story-13-6-attempt-ownership-20261018-webkit-capability' },
];
for (const lane of lanes) {
  const reportPath = `${lane.directory}/results/playwright.json`;
  const rawReport = readFileSync(join(root, reportPath));
  if (digest(rawReport) !== map.reports[reportPath]) throw new Error(`Report digest mismatch: ${reportPath}`);
  const report = JSON.parse(rawReport);
  const outcomes = [];
  const visit = suite => {
    for (const spec of suite.specs ?? []) for (const test of spec.tests ?? []) {
      outcomes.push(test);
      for (const attempt of test.results ?? []) for (const attachment of attempt.attachments ?? []) {
        if (!attachment.path) continue;
        const relocation = map.paths[attachment.path];
        if (!relocation) throw new Error(`Unmapped attachment: ${attachment.path}`);
        attachment.path = resolve(root, relocation.path);
        if (digest(readFileSync(attachment.path)) !== relocation.sha256) throw new Error(`Attachment digest mismatch: ${relocation.path}`);
      }
    }
    for (const child of suite.suites ?? []) visit(child);
  };
  for (const suite of report.suites ?? []) visit(suite);
  const files = walk(join(root, lane.directory, 'results'));
  for (const expectedEngine of lane.engines) {
    const owned = files.filter(file => !/^ae-state-.+\.json$/u.test(basename(file)) || JSON.parse(readFileSync(file, 'utf8')).engine === expectedEngine);
    const capabilities = outcomes.filter(test => test.projectName === expectedEngine && test.expectedStatus === 'passed' && ['expected', 'flaky'].includes(test.status) && test.results?.at(-1)?.status === 'passed')
      .flatMap(test => test.results.at(-1).attachments ?? []).filter(attachment => attachment.name === 'forced-color-capability' && attachment.contentType === 'application/json');
    const unsupportedForcedColorsCapability = capabilities.length === 1 ? JSON.parse(readFileSync(capabilities[0].path, 'utf8')) : null;
    const errors = validateStateEvidence(owned, { expectedEngine, expectedRun: lane.runId, report, unsupportedForcedColorsCapability });
    summary.push({ engine: expectedEngine, report: reportPath, requiredStates: requiredStates.length, errors });
  }
}
console.log(JSON.stringify(summary, null, 2));
if (summary.some(row => row.errors.length)) process.exitCode = 1;
