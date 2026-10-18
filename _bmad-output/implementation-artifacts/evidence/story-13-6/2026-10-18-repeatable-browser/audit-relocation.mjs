// Successful portability does not turn an unsuccessful accessibility gate green.
// Run the unchanged fail-closed replay, then require its gate failures to match
// the original full runs exactly and to identify only retained visual failures.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(import.meta.url));
const candidateBytes = readFileSync(join(root, 'candidate.json'));
const candidate = JSON.parse(candidateBytes);
const repository = resolve(root, '../../../../..');
const script = join(root, 'validate-state-packet.mjs');
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
// Historical candidate/report bytes stay immutable. Bind the runtime imports
// separately, before executing the candidate-bound replay or either validator.
const runtimePaths = [
  'tests/e2e/scripts/a11y-state-evidence.mjs',
  'tests/e2e/scripts/validate-a11y-artifacts.mjs',
  'tests/e2e/specimens/frontcomposer-specimen-manifest.json',
];
const runtime = JSON.parse(readFileSync(join(root, 'runtime-inputs.json')));
const same = (left, right) => JSON.stringify([...left].sort()) === JSON.stringify([...right].sort());
if (runtime.version !== 1 || !same(Object.keys(runtime.files ?? {}), runtimePaths)) throw new Error('Exact replay runtime inputs required');
for (const path of runtimePaths) {
  const bytes = readFileSync(join(repository, path)), expected = runtime.files[path];
  if (bytes.length !== expected.bytes || digest(bytes) !== expected.sha256) throw new Error(`Runtime input digest mismatch: ${path}`);
}
const engines = ['chromium', 'firefox', 'webkit'];
const runs = JSON.parse(readFileSync(join(root, 'browser-runs.json')));
const map = JSON.parse(readFileSync(join(root, 'attachment-path-map.json')));
if (runs.candidateSha256 !== digest(candidateBytes) || map.candidateSha256 !== digest(candidateBytes)) throw new Error('Candidate/run/mapping identity mismatch');
if (!runs.complete || !Array.isArray(runs.lanes) || !same(runs.lanes.map(lane => lane.engine), engines)) throw new Error('Exactly one complete lane per Chromium, Firefox and WebKit required');
const completeMappings = map.lanes.filter(lane => lane.completeGate);
if (!same(completeMappings.map(lane => lane.key), engines) || new Set(map.lanes.map(lane => lane.key)).size !== map.lanes.length) throw new Error('Exactly one complete mapping per engine required');
for (const lane of runs.lanes) {
  const retained = completeMappings.find(entry => entry.key === lane.engine);
  if (retained.engine !== lane.engine || !lane.runId || retained.runId !== lane.runId
    || lane.variables.FC_A11Y_EVIDENCE_RUN_ID !== lane.runId || lane.variables.FC_A11Y_EVIDENCE_ENGINE !== lane.engine
    || resolve(retained.path) !== resolve(runs.rawRoot, lane.engine)
    || resolve(lane.variables.FC_E2E_OUTPUT_DIR) !== resolve(retained.path, 'results')
    || resolve(lane.variables.FC_E2E_HTML_REPORT_DIR) !== resolve(retained.path, 'html')
    || lane.candidateSha256 && lane.candidateSha256 !== runs.candidateSha256) throw new Error(`Lane/mapping identity mismatch: ${lane.engine}`);
}
const supportLogs = JSON.parse(readFileSync(join(root, 'support-logs.json')));
for (const log of supportLogs.files) {
  const bytes = readFileSync(join(root, log.path));
  if (bytes.length !== log.bytes || digest(bytes) !== log.sha256) throw new Error(`Support log digest mismatch: ${log.path}`);
}
if (digest(readFileSync(script)) !== candidate.packetTools['validate-state-packet.mjs']) throw new Error('Replay tool differs from the candidate-bound tool');
const execution = spawnSync(process.execPath, [script], { encoding: 'utf8', maxBuffer: 16 * 1024 * 1024 });
if (![0, 1].includes(execution.status) || execution.stderr.trim()) throw new Error(`Replay execution failed: ${execution.stderr}`);
const replay = JSON.parse(execution.stdout);
const completeRows = replay.summary.filter(row => row.completeGate);
if (!same(completeRows.map(row => row.lane), engines) || !same(replay.summary.map(row => row.lane), map.lanes.map(lane => lane.key))) throw new Error('Replay must account for every retained lane and exactly one complete row per engine');
for (const lane of runs.lanes) {
  const row = completeRows.find(row => row.lane === lane.engine);
  if (!row || !row.outcomesUnchanged || row.requiredStates !== 116) throw new Error(`Incomplete replay: ${lane.engine}`);
  const report = JSON.parse(readFileSync(join(replay.restored, lane.engine, 'results/playwright.json')));
  const retained = completeMappings.find(entry => entry.key === lane.engine);
  if (digest(readFileSync(join(replay.restored, lane.engine, 'results/playwright.json'))) !== retained.reportSha256
    || report.config?.metadata?.a11yEvidenceRunId !== lane.runId
    || JSON.stringify(report.stats) !== JSON.stringify(lane.stats) || JSON.stringify(report.stats) !== JSON.stringify(retained.stats)) throw new Error(`Report/run identity mismatch: ${lane.engine}`);
  const expectedFailures = [], outcomes = [];
  const visit = suite => {
    for (const spec of suite.specs ?? []) for (const test of spec.tests ?? []) {
      if (test.projectName !== lane.engine) throw new Error(`Report engine identity mismatch: ${lane.engine}`);
      outcomes.push({ file: spec.file, title: spec.title, engine: test.projectName, status: test.status, expectedStatus: test.expectedStatus, attempts: test.results.map(result => ({ status: result.status, retry: result.retry })) });
      if (test.status === 'unexpected') {
        if (!spec.title.startsWith('visual baseline ')) throw new Error(`Unexpected nonvisual gate failure: ${spec.title}`);
        expectedFailures.push(`Ambiguous or unsuccessful Playwright outcome: ${JSON.stringify([test.projectName, spec.id])}`);
      }
    }
    for (const child of suite.suites ?? []) visit(child);
  };
  visit(report);
  if (!outcomes.length || JSON.stringify(outcomes) !== JSON.stringify(lane.outcomes)) throw new Error(`Report/run outcomes mismatch: ${lane.engine}`);
  const original = readFileSync(join(root, `${lane.engine}-artifacts.log.txt`), 'utf8');
  if (!same(row.errors, expectedFailures) || row.artifactValidationOutput !== original || row.artifactValidationExitCode !== lane.validationExitCode) throw new Error(`Relocation changed gate/ownership results: ${lane.engine}`);
  if (lane.bytes.results > 52_428_800 || lane.bytes.html > 52_428_800) throw new Error(`Original root exceeds the unchanged limit: ${lane.engine}`);
  // JUnit's expected-image markers can escape the original results directory
  // into the source checkout. Preserve raw XML and produce a path-only replay
  // beside it, using the same captured external images as JSON relocation.
  const destination = join(replay.restored, lane.engine);
  const rawJUnit = readFileSync(join(destination, 'results/junit.xml'), 'utf8');
  const originalMarkers = [];
  const relocatedJUnit = rawJUnit.replace(/\[\[ATTACHMENT\|([^\]]+)\]\]/gu, (marker, path) => {
    originalMarkers.push(marker);
    const attachment = retained.paths[resolve(lane.variables.FC_E2E_OUTPUT_DIR, path)];
    if (!attachment) throw new Error(`Unmapped JUnit attachment: ${lane.engine}/${path}`);
    const target = resolve(destination, attachment.path);
    if (digest(readFileSync(target)) !== attachment.sha256) throw new Error(`JUnit attachment digest mismatch: ${path}`);
    return `[[ATTACHMENT|${relative(destination, target)}]]`;
  });
  let index = 0;
  if (relocatedJUnit.replace(/\[\[ATTACHMENT\|([^\]]+)\]\]/gu, () => originalMarkers[index++]) !== rawJUnit) throw new Error('JUnit relocation changed more than attachment paths');
  writeFileSync(join(destination, 'relocated-junit.xml'), relocatedJUnit);
  row.junitAttachmentMarkersVerified = originalMarkers.length;
  row.junitOutcomesUnchanged = true;
  row.htmlFilesVerified = Object.keys(retained.files).filter(path => path.startsWith('html/')).length;
}
console.log(JSON.stringify({
  structuralIntegrity: 'passed',
  attachmentRelocationAndOwnership: 'passed; no additional errors beyond the identical original visual outcomes',
  completeStateRowsPerEngine: 116,
  supportLogsVerified: supportLogs.files.length,
  runtimeInputsVerified: runtimePaths.length,
  completeLaneIdentitiesVerified: engines,
  rawReplayExitCode: execution.status,
  fullArtifactGate: 'failed in every engine; original visual-baseline failures preserved unchanged',
  overallAccessibilityClassification: 'blocked',
  replay,
}, null, 2));
