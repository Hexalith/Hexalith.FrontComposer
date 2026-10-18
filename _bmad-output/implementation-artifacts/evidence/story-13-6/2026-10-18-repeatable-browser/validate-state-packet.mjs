// Adapted from ../2026-10-18-attempt-ownership/validate-state-packet.mjs.
// Restore raw reports and every HTML/results asset in a fresh location, verify
// original hashes, then relocate attachment paths only in the replay report.
import { createHash } from 'node:crypto';
import { mkdirSync, mkdtempSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { basename, dirname, join, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { validateStateEvidence, requiredStates } from '../../../../../tests/e2e/scripts/a11y-state-evidence.mjs';

const root = dirname(fileURLToPath(import.meta.url));
const repository = resolve(root, '../../../../..');
const map = JSON.parse(readFileSync(join(root, 'attachment-path-map.json')));
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
if (digest(readFileSync(join(root, 'candidate.json'))) !== map.candidateSha256) throw new Error('Candidate manifest digest mismatch');
const restored = mkdtempSync(join(tmpdir(), 'story-13-6-repeatable-replay-'));
const walk = path => readdirSync(path, { withFileTypes: true }).flatMap(entry => entry.isDirectory() ? walk(join(path, entry.name)) : [join(path, entry.name)]);
const outcomes = report => {
  const rows = [];
  const visit = suite => {
    for (const spec of suite.specs ?? []) for (const test of spec.tests ?? []) rows.push({ title: spec.title, ...test, results: test.results.map(result => ({ ...result, attachments: undefined })) });
    for (const child of suite.suites ?? []) visit(child);
  };
  visit(report); return JSON.stringify(rows);
};
const summary = [];
for (const lane of map.lanes) {
  const archive = join(root, lane.archive);
  if (digest(readFileSync(archive)) !== lane.archiveSha256) throw new Error(`Archive digest mismatch: ${lane.archive}`);
  const destination = join(restored, lane.key);
  mkdirSync(destination);
  const extraction = spawnSync('tar', ['-xzf', archive, '-C', destination], { encoding: 'utf8' });
  if (extraction.status !== 0) throw new Error(extraction.stderr);
  const actualFiles = walk(destination);
  if (actualFiles.length !== Object.keys(lane.files).length) throw new Error(`File inventory differs: ${lane.key}`);
  for (const [path, expected] of Object.entries(lane.files)) {
    const bytes = readFileSync(join(destination, path));
    if (bytes.length !== expected.bytes || digest(bytes) !== expected.sha256) throw new Error(`File digest mismatch: ${lane.key}/${path}`);
  }
  if (!lane.reportSha256) {
    summary.push({ lane: lane.key, interrupted: true, filesVerified: actualFiles.length, limitation: 'Interrupted before JSON/JUnit/HTML report generation; raw captures retained only.' });
    continue;
  }
  const raw = readFileSync(join(destination, 'results/playwright.json'));
  if (digest(raw) !== lane.reportSha256) throw new Error(`Report digest mismatch: ${lane.key}`);
  const report = JSON.parse(raw), originalOutcomes = outcomes(report);
  const tests = [];
  const visit = suite => {
    for (const spec of suite.specs ?? []) for (const test of spec.tests ?? []) {
      tests.push(test);
      for (const attempt of test.results ?? []) for (const attachment of attempt.attachments ?? []) {
        if (!attachment.path) continue;
        const relocation = lane.paths[attachment.path];
        if (!relocation) throw new Error(`Unmapped attachment: ${attachment.path}`);
        attachment.path = resolve(destination, relocation.path);
        if (digest(readFileSync(attachment.path)) !== relocation.sha256) throw new Error(`Attachment digest mismatch: ${relocation.path}`);
      }
    }
    for (const child of suite.suites ?? []) visit(child);
  };
  for (const suite of report.suites ?? []) visit(suite);
  if (outcomes(report) !== originalOutcomes) throw new Error(`Relocation changed outcomes: ${lane.key}`);
  const row = { lane: lane.key, completeGate: lane.completeGate, filesVerified: actualFiles.length, attachmentPathsVerified: Object.keys(lane.paths).length, outcomesUnchanged: true, stats: report.stats };
  if (lane.completeGate) {
    const files = walk(join(destination, 'results'));
    const capabilities = tests.filter(test => test.projectName === lane.engine && test.expectedStatus === 'passed' && ['expected','flaky'].includes(test.status) && test.results.at(-1)?.status === 'passed').flatMap(test => test.results.at(-1).attachments ?? []).filter(attachment => attachment.name === 'forced-color-capability' && attachment.contentType === 'application/json');
    const capability = capabilities.length === 1 ? JSON.parse(readFileSync(capabilities[0].path)) : null;
    const owned = files.filter(file => !/^ae-state-.+\.json$/u.test(basename(file)) || JSON.parse(readFileSync(file)).engine === lane.engine);
    row.requiredStates = requiredStates.length;
    row.errors = validateStateEvidence(owned, { report, expectedEngine: lane.engine, expectedRun: lane.runId, unsupportedForcedColorsCapability: capability });
    // Keep the relocated report outside both measured roots; raw report bytes stay intact.
    const relocatedReport = join(destination, 'relocated-report.json');
    writeFileSync(relocatedReport, JSON.stringify(report));
    const env = { ...process.env, FC_E2E_OUTPUT_DIR: join(destination, 'results'), FC_E2E_HTML_REPORT_DIR: join(destination, 'html'), FC_E2E_JSON_PATH: relocatedReport, FC_A11Y_EVIDENCE_ENGINE: lane.engine, FC_A11Y_EVIDENCE_RUN_ID: lane.runId };
    const validation = spawnSync(process.execPath, ['scripts/validate-a11y-artifacts.mjs'], { cwd: join(repository, 'tests/e2e'), env, encoding: 'utf8' });
    row.artifactValidationExitCode = validation.status;
    row.artifactValidationOutput = validation.stdout + validation.stderr;
  }
  summary.push(row);
}
console.log(JSON.stringify({ restored, summary }, null, 2));
if (summary.some(row => row.errors?.length || row.completeGate && row.artifactValidationExitCode !== 0)) process.exitCode = 1;
