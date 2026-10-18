// Run complete independent engine gates against the already-started Test AppHost.
// Captures remain on failure. No snapshots, dependencies or policy are updated.
import { createHash, randomUUID } from 'node:crypto';
import { closeSync, existsSync, mkdirSync, mkdtempSync, openSync, readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const packet = dirname(fileURLToPath(import.meta.url));
const repository = resolve(packet, '../../../../..');
const workspace = join(repository, 'tests/e2e');
const engines = ['chromium', 'firefox', 'webkit'];
const recordPath = join(packet, 'browser-runs.json');
// Check every destination before creating either a record or a raw capture root.
// Include legacy log names so a previous capture cannot be mistaken for a new one.
for (const path of [recordPath, ...engines.flatMap(engine => ['browser', 'artifacts'].flatMap(kind => ['.log', '.log.txt'].map(extension => join(packet, `${engine}-${kind}${extension}`))))]) {
  if (existsSync(path)) throw new Error(`Refusing to replace retained evidence: ${path}`);
}
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
checkCandidate();
const record = { candidateSha256: digest(candidateBytes), rawRoot: null, lanes: [], complete: false, gatesPassed: false };
writeFileSync(recordPath, JSON.stringify(record, null, 2) + '\n', { flag: 'wx' });
const save = () => writeFileSync(recordPath, JSON.stringify(record, null, 2) + '\n');
record.rawRoot = mkdtempSync(join(tmpdir(), 'story-13-6-repeatable-full-'));
save();
const terminate = result => ({ exitCode: result.status, signal: result.signal, error: result.error?.message ?? null });
for (const engine of engines) {
  const laneRoot = join(record.rawRoot, engine);
  const runId = `story-13-6-repeatable-20261018-${engine}-${randomUUID()}`;
  const variables = {
    BASE_URL: process.env.BASE_URL ?? 'https://localhost:7201',
    PLAYWRIGHT_SKIP_WEBSERVER: '1',
    FC_A11Y_EVIDENCE_RUN_ID: runId,
    FC_A11Y_EVIDENCE_ENGINE: engine,
    FC_E2E_OUTPUT_DIR: join(laneRoot, 'results'),
    FC_E2E_HTML_REPORT_DIR: join(laneRoot, 'html'),
    FC_E2E_JSON_PATH: join(laneRoot, 'results/playwright.json'),
    FC_E2E_JUNIT_PATH: join(laneRoot, 'results/junit.xml'),
  };
  const env = { ...process.env, ...variables };
  const args = ['playwright', 'test', ...specs, `--project=${engine}`, '--workers=4', '--retries=0'];
  const lane = {
    engine, runId, candidateSha256: record.candidateSha256, started: new Date().toISOString(), finished: null,
    cwd: 'tests/e2e', variables, command: ['npx', ...args], validationCommand: ['node', 'scripts/validate-a11y-artifacts.mjs'],
    log: `${engine}-browser.log.txt`, validationLog: `${engine}-artifacts.log.txt`,
    termination: null, validationTermination: null, exitCode: null, validationExitCode: null,
    complete: false, disposition: 'incomplete', incompleteReasons: [], reports: {}, bytes: { results: null, html: null },
  };
  // Persist intent and identity before any launch, including one that cannot start.
  record.lanes.push(lane);
  save();
  try {
    checkCandidate();
    mkdirSync(laneRoot, { recursive: true });
    for (const [field, executable, command, logPath] of [
      ['termination', 'npx', args, lane.log],
      ['validationTermination', process.execPath, ['scripts/validate-a11y-artifacts.mjs'], lane.validationLog],
    ]) {
      let log;
      try {
        log = openSync(join(packet, logPath), 'wx');
        lane[field] = terminate(spawnSync(executable, command, { cwd: workspace, env, stdio: ['ignore', log, log] }));
      } catch (error) {
        lane[field] = { exitCode: null, signal: null, error: error.message };
      } finally {
        if (log !== undefined) closeSync(log);
      }
      if (lane[field].error || lane[field].signal || lane[field].exitCode === null) lane.incompleteReasons.push(`${field}: ${JSON.stringify(lane[field])}`);
      lane.exitCode = lane.termination?.exitCode ?? null;
      lane.validationExitCode = lane.validationTermination?.exitCode ?? null;
      save();
    }
    for (const [name, path] of Object.entries({ results: variables.FC_E2E_OUTPUT_DIR, html: variables.FC_E2E_HTML_REPORT_DIR })) {
      try { lane.bytes[name] = size(path); } catch (error) { lane.incompleteReasons.push(`${name} root unavailable: ${error.message}`); }
    }
    for (const [name, path] of Object.entries({ json: variables.FC_E2E_JSON_PATH, junit: variables.FC_E2E_JUNIT_PATH, html: join(variables.FC_E2E_HTML_REPORT_DIR, 'index.html') })) {
      try {
        const bytes = readFileSync(path);
        lane.reports[name] = { path, available: true, bytes: bytes.length, sha256: digest(bytes) };
        if (name === 'json') {
          const report = JSON.parse(bytes);
          if (!Array.isArray(report.suites) || !report.stats || !Array.isArray(report.errors)) throw new Error('Invalid Playwright report structure');
          if (report.config?.metadata?.a11yEvidenceRunId !== runId) throw new Error('Playwright report run identity mismatch');
          const outcomes = [];
          const visit = suite => {
            for (const spec of suite.specs ?? []) for (const test of spec.tests ?? []) {
              if (test.projectName !== engine || !Array.isArray(test.results)) throw new Error('Playwright report engine/attempt mismatch');
              outcomes.push({ file: spec.file, title: spec.title, engine: test.projectName, status: test.status, expectedStatus: test.expectedStatus, attempts: test.results.map(result => ({ status: result.status, retry: result.retry })) });
            }
            for (const child of suite.suites ?? []) visit(child);
          };
          visit(report);
          if (!outcomes.length) throw new Error('Playwright report has no outcomes');
          Object.assign(lane, { stats: report.stats, outcomes, runnerErrors: report.errors });
          if (report.errors.length) lane.incompleteReasons.push('Playwright runner errors present');
        }
      } catch (error) {
        lane.reports[name] = { ...lane.reports[name], path, available: false, error: error.message };
        lane.incompleteReasons.push(`${name} report unavailable/invalid: ${error.message}`);
      }
    }
    checkCandidate();
  } catch (error) {
    lane.incompleteReasons.push(error.message);
  }
  lane.finished = new Date().toISOString();
  lane.complete = lane.incompleteReasons.length === 0;
  lane.disposition = lane.complete ? 'complete' : 'incomplete';
  save();
  console.log(`${engine}: ${JSON.stringify({ disposition: lane.disposition, exitCode: lane.exitCode, validationExitCode: lane.validationExitCode, stats: lane.stats, bytes: lane.bytes, incompleteReasons: lane.incompleteReasons })}`);
}
record.complete = record.lanes.length === engines.length && record.lanes.every(lane => lane.complete);
record.gatesPassed = record.complete && record.lanes.every(lane => lane.exitCode === 0 && lane.validationExitCode === 0);
save();
if (!record.gatesPassed) process.exitCode = 1;
