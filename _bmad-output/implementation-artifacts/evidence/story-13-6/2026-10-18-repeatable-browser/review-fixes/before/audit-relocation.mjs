// Successful portability does not turn an unsuccessful accessibility gate green.
// Run the unchanged fail-closed replay, then require its gate failures to match
// the original full runs exactly and to identify only retained visual failures.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(import.meta.url));
const candidate = JSON.parse(readFileSync(join(root, 'candidate.json')));
const script = join(root, 'validate-state-packet.mjs');
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
const supportLogs = JSON.parse(readFileSync(join(root, 'support-logs.json')));
for (const log of supportLogs.files) {
  const bytes = readFileSync(join(root, log.path));
  if (bytes.length !== log.bytes || digest(bytes) !== log.sha256) throw new Error(`Support log digest mismatch: ${log.path}`);
}
if (digest(readFileSync(script)) !== candidate.packetTools['validate-state-packet.mjs']) throw new Error('Replay tool differs from the candidate-bound tool');
const execution = spawnSync(process.execPath, [script], { encoding: 'utf8', maxBuffer: 16 * 1024 * 1024 });
if (![0, 1].includes(execution.status) || execution.stderr.trim()) throw new Error(`Replay execution failed: ${execution.stderr}`);
const replay = JSON.parse(execution.stdout);
const runs = JSON.parse(readFileSync(join(root, 'browser-runs.json')));
const map = JSON.parse(readFileSync(join(root, 'attachment-path-map.json')));
if (!runs.complete || runs.lanes.length !== 3) throw new Error('Complete three-engine gate record required');
const same = (left, right) => JSON.stringify([...left].sort()) === JSON.stringify([...right].sort());
for (const lane of runs.lanes) {
  const row = replay.summary.find(row => row.lane === lane.engine && row.completeGate);
  if (!row || !row.outcomesUnchanged || row.requiredStates !== 116) throw new Error(`Incomplete replay: ${lane.engine}`);
  const report = JSON.parse(readFileSync(join(replay.restored, lane.engine, 'results/playwright.json')));
  const expectedFailures = [];
  const visit = suite => {
    for (const spec of suite.specs ?? []) for (const test of spec.tests ?? []) {
      if (test.status === 'unexpected') {
        if (!spec.title.startsWith('visual baseline ')) throw new Error(`Unexpected nonvisual gate failure: ${spec.title}`);
        expectedFailures.push(`Ambiguous or unsuccessful Playwright outcome: ${JSON.stringify([test.projectName, spec.id])}`);
      }
    }
    for (const child of suite.suites ?? []) visit(child);
  };
  visit(report);
  const original = readFileSync(join(root, `${lane.engine}-artifacts.log.txt`), 'utf8');
  if (!same(row.errors, expectedFailures) || row.artifactValidationOutput !== original || row.artifactValidationExitCode !== lane.validationExitCode) throw new Error(`Relocation changed gate/ownership results: ${lane.engine}`);
  if (lane.bytes.results > 52_428_800 || lane.bytes.html > 52_428_800) throw new Error(`Original root exceeds the unchanged limit: ${lane.engine}`);
  // JUnit's expected-image markers can escape the original results directory
  // into the source checkout. Preserve raw XML and produce a path-only replay
  // beside it, using the same captured external images as JSON relocation.
  const retained = map.lanes.find(entry => entry.key === lane.engine);
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
  rawReplayExitCode: execution.status,
  fullArtifactGate: 'failed in every engine; original visual-baseline failures preserved unchanged',
  overallAccessibilityClassification: 'blocked',
  replay,
}, null, 2));
