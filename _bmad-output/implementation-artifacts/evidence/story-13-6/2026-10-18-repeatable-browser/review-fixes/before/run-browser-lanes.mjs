// Run complete independent engine gates against the already-started Test AppHost.
// Captures remain on failure. No snapshots, dependencies or policy are updated.
import { createHash, randomUUID } from 'node:crypto';
import { closeSync, mkdirSync, mkdtempSync, openSync, readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const packet = dirname(fileURLToPath(import.meta.url));
const repository = resolve(packet, '../../../../..');
const workspace = join(repository, 'tests/e2e');
const candidateBytes = readFileSync(join(packet, 'candidate.json'));
const candidate = JSON.parse(candidateBytes);
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
const checkCandidate = () => {
  for (const [path, expected] of Object.entries({ ...candidate.files, ...candidate.assemblies, ...candidate.protectedBaselines })) {
    if (digest(readFileSync(join(repository, path))) !== expected) throw new Error(`Candidate changed: ${path}`);
  }
};
const size = path => readdirSync(path, { withFileTypes: true }).reduce((total, entry) => total + (entry.isDirectory() ? size(join(path, entry.name)) : statSync(join(path, entry.name)).size), 0);
const specs = ['specs/specimen-accessibility.spec.ts', 'specs/responsive-accessibility.spec.ts', 'specs/accessibility-state-matrix.spec.ts'];
const rawRoot = mkdtempSync(join(tmpdir(), 'story-13-6-repeatable-full-'));
const record = { candidateSha256: digest(candidateBytes), rawRoot, lanes: [], complete: false };
writeFileSync(join(packet, 'browser-runs.json'), JSON.stringify(record, null, 2) + '\n');
for (const engine of ['chromium', 'firefox', 'webkit']) {
  checkCandidate();
  const laneRoot = join(rawRoot, engine);
  mkdirSync(laneRoot, { recursive: true });
  const runId = `story-13-6-repeatable-20261018-${engine}-${randomUUID()}`;
  const variables = {
    BASE_URL: process.env.BASE_URL ?? 'https://localhost:7201',
    PLAYWRIGHT_SKIP_WEBSERVER: '1',
    FC_A11Y_EVIDENCE_RUN_ID: runId,
    FC_A11Y_EVIDENCE_ENGINE: engine,
    FC_E2E_OUTPUT_DIR: join(laneRoot, 'results'),
    FC_E2E_HTML_REPORT_DIR: join(laneRoot, 'html'),
  };
  const env = { ...process.env, ...variables };
  const args = ['playwright', 'test', ...specs, `--project=${engine}`, '--workers=4', '--retries=0'];
  const log = openSync(join(packet, `${engine}-browser.log`), 'wx');
  const started = new Date().toISOString();
  const run = spawnSync('npx', args, { cwd: workspace, env, stdio: ['ignore', log, log] });
  closeSync(log);
  const validationLog = openSync(join(packet, `${engine}-artifacts.log`), 'wx');
  const validation = spawnSync(process.execPath, ['scripts/validate-a11y-artifacts.mjs'], { cwd: workspace, env, stdio: ['ignore', validationLog, validationLog] });
  closeSync(validationLog);
  const report = JSON.parse(readFileSync(join(laneRoot, 'results/playwright.json')));
  const outcomes = [];
  const visit = suite => {
    for (const spec of suite.specs ?? []) for (const test of spec.tests ?? []) outcomes.push({ file: spec.file, title: spec.title, engine: test.projectName, status: test.status, expectedStatus: test.expectedStatus, attempts: test.results.map(result => ({ status: result.status, retry: result.retry })) });
    for (const child of suite.suites ?? []) visit(child);
  };
  visit(report);
  const lane = { engine, runId, started, finished: new Date().toISOString(), cwd: 'tests/e2e', variables, command: ['npx', ...args], exitCode: run.status, validationCommand: ['node', 'scripts/validate-a11y-artifacts.mjs'], validationExitCode: validation.status, bytes: { results: size(variables.FC_E2E_OUTPUT_DIR), html: size(variables.FC_E2E_HTML_REPORT_DIR) }, stats: report.stats, outcomes, runnerErrors: report.errors };
  record.lanes.push(lane);
  writeFileSync(join(packet, 'browser-runs.json'), JSON.stringify(record, null, 2) + '\n');
  console.log(`${engine}: ${JSON.stringify({ exitCode: run.status, validationExitCode: validation.status, stats: report.stats, bytes: lane.bytes })}`);
  checkCandidate();
}
record.complete = true;
writeFileSync(join(packet, 'browser-runs.json'), JSON.stringify(record, null, 2) + '\n');
