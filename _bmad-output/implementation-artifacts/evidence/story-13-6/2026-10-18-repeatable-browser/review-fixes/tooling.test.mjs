// Focused regressions for the retained packet runner and relocation audit.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { chmodSync, copyFileSync, existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const source = dirname(dirname(fileURLToPath(import.meta.url)));
const tools = process.env.FC_REVIEW_BEFORE_TOOLS ? join(source, 'review-fixes/before') : source;
const engines = ['chromium', 'firefox', 'webkit'];
const runtimePaths = ['tests/e2e/scripts/a11y-state-evidence.mjs', 'tests/e2e/scripts/validate-a11y-artifacts.mjs', 'tests/e2e/specimens/frontcomposer-specimen-manifest.json'];
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
const put = (path, value) => { mkdirSync(dirname(path), { recursive: true }); writeFileSync(path, typeof value === 'string' ? value : JSON.stringify(value, null, 2) + '\n'); };
const json = path => JSON.parse(readFileSync(path));
function fixture(tool) {
  const root = mkdtempSync(join(tmpdir(), 'story-13-6-tool-regression-'));
  const packet = join(root, '_bmad-output/implementation-artifacts/evidence/story-13-6/packet');
  mkdirSync(packet, { recursive: true });
  copyFileSync(join(tools, tool), join(packet, tool));
  return { root, packet, run: (env = {}) => spawnSync(process.execPath, [join(packet, tool)], { cwd: root, env: { ...process.env, ...env }, encoding: 'utf8' }) };
}
function runnerFixture(mode = 'good', launchable = true) {
  const f = fixture('run-browser-lanes.mjs');
  put(join(f.packet, 'candidate.json'), { files: {}, assemblies: {}, protectedBaselines: {} });
  put(join(f.root, 'tests/e2e/scripts/validate-a11y-artifacts.mjs'), "console.log('fixture validation'); if(process.env.FIXTURE_MODE === 'gatefail') process.exitCode = 1;\n");
  const bin = join(f.root, 'bin'); mkdirSync(bin);
  if (launchable) {
    put(join(bin, 'npx'), `#!${process.execPath}
import {readFileSync,writeFileSync,mkdirSync,appendFileSync} from 'node:fs';
import {dirname} from 'node:path';
const record=JSON.parse(readFileSync(process.env.FIXTURE_PACKET+'/browser-runs.json'));
const lane=record.lanes.find(lane=>lane.engine===process.env.FC_A11Y_EVIDENCE_ENGINE);
appendFileSync(process.env.FIXTURE_PACKET+'/launches.jsonl',JSON.stringify({lane,engine:process.env.FC_A11Y_EVIDENCE_ENGINE})+'\\n');
const put=(p,x)=>{mkdirSync(dirname(p),{recursive:true});writeFileSync(p,x);};
const engine=process.env.FC_A11Y_EVIDENCE_ENGINE, mode=process.env.FIXTURE_MODE;
if(mode==='signal' && engine==='chromium') process.kill(process.pid,'SIGTERM');
const report={config:{metadata:{a11yEvidenceRunId:process.env.FC_A11Y_EVIDENCE_RUN_ID}},suites:[{specs:[{file:'fixture.spec.ts',id:'fixture',title:mode==='gatefail'?'visual baseline fixture':'fixture',tests:[{projectName:engine,status:mode==='gatefail'?'unexpected':'expected',expectedStatus:'passed',results:[{status:mode==='gatefail'?'failed':'passed',retry:0}]}]}]}],stats:{expected:mode==='gatefail'?0:1,unexpected:mode==='gatefail'?1:0,skipped:0,flaky:0},errors:[]};
if(!(mode==='mixed' && engine==='firefox')) put(process.env.FC_E2E_JSON_PATH ?? process.env.FC_E2E_OUTPUT_DIR+'/playwright.json',mode==='mixed' && engine==='chromium'?'{invalid':JSON.stringify(report));
put(process.env.FC_E2E_JUNIT_PATH ?? process.env.FC_E2E_OUTPUT_DIR+'/junit.xml','<testsuites/>');
put(process.env.FC_E2E_HTML_REPORT_DIR+'/index.html','<html/>');
if(mode==='gatefail') process.exitCode=1;
`);
    chmodSync(join(bin, 'npx'), 0o755);
  }
  const run = env => f.run({ PATH: bin, FIXTURE_PACKET: f.packet, FIXTURE_MODE: mode, ...env });
  return { ...f, run, record: () => json(join(f.packet, 'browser-runs.json')) };
}
for (const destination of ['browser-runs.json', 'chromium-browser.log', 'firefox-artifacts.log.txt']) {
  test(`runner refuses existing ${destination} before mutating evidence`, () => {
    const f = runnerFixture(); const sentinel = 'original retained bytes\r\n';
    put(join(f.packet, destination), sentinel);
    const result = f.run(); assert.equal(result.status, 1); assert.match(result.stderr, /Refusing to replace retained evidence/);
    assert.equal(readFileSync(join(f.packet, destination), 'utf8'), sentinel);
    if (destination !== 'browser-runs.json') assert.equal(existsSync(join(f.packet, 'browser-runs.json')), false);
    assert.equal(existsSync(join(f.packet, 'launches.jsonl')), false);
  });
}
test('runner persists lane intent before launch and binds both reports despite caller overrides', () => {
  const f = runnerFixture(); const outsideJson = join(f.root, 'outside.json'), outsideXml = join(f.root, 'outside.xml');
  put(outsideJson, 'untouched JSON'); put(outsideXml, 'untouched XML');
  const result = f.run({ FC_E2E_JSON_PATH: outsideJson, FC_E2E_JUNIT_PATH: outsideXml });
  assert.equal(result.status, 0, result.stderr); const record = f.record();
  assert.equal(record.complete, true); assert.equal(record.gatesPassed, true); assert.equal(record.lanes.length, 3);
  const launches = readFileSync(join(f.packet, 'launches.jsonl'), 'utf8').trim().split('\n').map(JSON.parse);
  assert.equal(launches.length, 3);
  for (const { lane, engine } of launches) {
    assert.equal(lane.engine, engine); assert.equal(lane.complete, false); assert.equal(lane.disposition, 'incomplete');
    assert.equal(lane.candidateSha256, record.candidateSha256); assert.equal(lane.variables.FC_A11Y_EVIDENCE_RUN_ID, lane.runId);
    assert.equal(lane.termination, null); assert.ok(lane.command.includes(`--project=${engine}`)); assert.ok(lane.validationCommand);
    assert.equal(lane.variables.FC_E2E_JSON_PATH, join(record.rawRoot, engine, 'results/playwright.json'));
    assert.equal(lane.variables.FC_E2E_JUNIT_PATH, join(record.rawRoot, engine, 'results/junit.xml'));
  }
  assert.equal(readFileSync(outsideJson, 'utf8'), 'untouched JSON'); assert.equal(readFileSync(outsideXml, 'utf8'), 'untouched XML');
});
test('runner records invalid/missing reports as incomplete and continues later engines', () => {
  const f = runnerFixture('mixed'); const result = f.run(); assert.equal(result.status, 1);
  const record = f.record(); assert.equal(record.complete, false); assert.equal(record.gatesPassed, false);
  assert.deepEqual(record.lanes.map(lane => lane.engine), engines);
  for (const lane of record.lanes.slice(0, 2)) {
    assert.equal(lane.disposition, 'incomplete'); assert.equal(lane.reports.json.available, false);
    assert.equal(lane.termination.exitCode, 0); assert.equal(lane.validationTermination.exitCode, 0); assert.ok(lane.finished);
  }
  assert.equal(record.lanes[2].complete, true);
});
test('runner records failed launches and unavailable roots, continuing all engines', () => {
  const f = runnerFixture('good', false); const result = f.run(); assert.equal(result.status, 1);
  const record = f.record(); assert.equal(record.complete, false); assert.equal(record.gatesPassed, false); assert.equal(record.lanes.length, 3);
  for (const lane of record.lanes) {
    assert.equal(lane.disposition, 'incomplete'); assert.match(lane.termination.error, /ENOENT/);
    assert.equal(lane.exitCode, null); assert.equal(lane.bytes.results, null); assert.equal(lane.bytes.html, null);
    assert.ok(lane.incompleteReasons.some(reason => reason.includes('root unavailable'))); assert.ok(lane.command && lane.validationCommand && lane.runId && lane.finished);
  }
});
test('runner records a signal as incomplete and continues remaining engines', () => {
  const f = runnerFixture('signal'); const result = f.run(); assert.equal(result.status, 1);
  const record = f.record(); assert.equal(record.lanes[0].termination.signal, 'SIGTERM'); assert.equal(record.lanes[0].complete, false);
  assert.equal(record.lanes[1].complete, true); assert.equal(record.lanes[2].complete, true); assert.equal(record.complete, false);
});
test('runner distinguishes a complete failed gate from successful verification', () => {
  const f = runnerFixture('gatefail'); const result = f.run(); assert.equal(result.status, 1);
  const record = f.record(); assert.equal(record.complete, true); assert.equal(record.gatesPassed, false);
  for (const lane of record.lanes) { assert.equal(lane.exitCode, 1); assert.equal(lane.validationExitCode, 1); assert.equal(lane.outcomes[0].status, 'unexpected'); }
});
function auditFixture() {
  const f = fixture('audit-relocation.mjs');
  for (const path of runtimePaths) put(join(f.root, path), path.endsWith('.json') ? '{}' : '// runtime fixture\n');
  const runtime = { version: 1, files: Object.fromEntries(runtimePaths.map(path => { const bytes = readFileSync(join(f.root, path)); return [path, { bytes: bytes.length, sha256: digest(bytes) }]; })) };
  put(join(f.packet, 'runtime-inputs.json'), runtime);
  const restored = join(f.root, 'restored');
  const replay = { restored, summary: [] }, map = { candidateSha256: '', lanes: [] }, runs = { candidateSha256: '', rawRoot: join(f.root, 'raw'), complete: true, lanes: [] };
  for (const engine of engines) {
    const runId = `fixture-${engine}`, path = join(runs.rawRoot, engine), stats = { expected: 0, unexpected: 1, skipped: 0, flaky: 0 };
    const spec = { id: `${engine}-visual`, file: 'fixture.spec.ts', title: 'visual baseline fixture', tests: [{ projectName: engine, status: 'unexpected', expectedStatus: 'passed', results: [{ status: 'failed', retry: 0 }] }] };
    const report = { config: { metadata: { a11yEvidenceRunId: runId } }, suites: [{ specs: [spec] }], stats, errors: [] };
    put(join(restored, engine, 'results/playwright.json'), report); put(join(restored, engine, 'results/junit.xml'), '<testsuites/>');
    const outcomes = [{ file: spec.file, title: spec.title, engine, status: 'unexpected', expectedStatus: 'passed', attempts: [{ status: 'failed', retry: 0 }] }];
    map.lanes.push({ key: engine, engine, runId, path, completeGate: true, reportSha256: digest(readFileSync(join(restored, engine, 'results/playwright.json'))), stats, paths: {}, files: {} });
    runs.lanes.push({ engine, runId, variables: { FC_A11Y_EVIDENCE_RUN_ID: runId, FC_A11Y_EVIDENCE_ENGINE: engine, FC_E2E_OUTPUT_DIR: join(path, 'results'), FC_E2E_HTML_REPORT_DIR: join(path, 'html') }, stats, outcomes, bytes: { results: 1, html: 1 }, exitCode: 1, validationExitCode: 1 });
    replay.summary.push({ lane: engine, completeGate: true, outcomesUnchanged: true, requiredStates: 116, errors: [`Ambiguous or unsuccessful Playwright outcome: ${JSON.stringify([engine, spec.id])}`], artifactValidationOutput: `retained ${engine} failure\n`, artifactValidationExitCode: 1 });
    put(join(f.packet, `${engine}-artifacts.log.txt`), `retained ${engine} failure\n`);
  }
  put(join(f.packet, 'fixture-replay.json'), replay);
  const script = "import {readFileSync,writeFileSync} from 'node:fs';import{dirname,join}from'node:path';import{fileURLToPath}from'node:url';const root=dirname(fileURLToPath(import.meta.url));writeFileSync(join(root,'replay-executed'),'yes');console.log(readFileSync(join(root,'fixture-replay.json'),'utf8'));process.exitCode=1;\n";
  put(join(f.packet, 'validate-state-packet.mjs'), script);
  put(join(f.packet, 'candidate.json'), { packetTools: { 'validate-state-packet.mjs': digest(script) } });
  runs.candidateSha256 = map.candidateSha256 = digest(readFileSync(join(f.packet, 'candidate.json')));
  put(join(f.packet, 'browser-runs.json'), runs); put(join(f.packet, 'attachment-path-map.json'), map); put(join(f.packet, 'support-logs.json'), { files: [] });
  return { ...f, runs, map, replay, save: () => { put(join(f.packet, 'browser-runs.json'), runs); put(join(f.packet, 'attachment-path-map.json'), map); put(join(f.packet, 'fixture-replay.json'), replay); } };
}
test('audit preserves complete failed outcomes while verifying all lane identities', () => {
  const f = auditFixture(), result = f.run(); assert.equal(result.status, 0, result.stderr);
  const out = JSON.parse(result.stdout); assert.equal(out.runtimeInputsVerified, 3); assert.deepEqual(out.completeLaneIdentitiesVerified, engines);
  assert.equal(out.overallAccessibilityClassification, 'blocked'); assert.equal(out.rawReplayExitCode, 1);
  assert.deepEqual(out.replay.summary.map(row => row.errors), f.replay.summary.map(row => row.errors));
});
for (const path of runtimePaths) test(`audit rejects runtime mutation before execution: ${path}`, () => {
  const f = auditFixture(); put(join(f.root, path), 'mutated bytes');
  const result = f.run(); assert.equal(result.status, 1); assert.match(result.stderr, /Runtime input digest mismatch/);
  assert.equal(existsSync(join(f.packet, 'replay-executed')), false);
});
const preflightMutations = {
  'three duplicate engine records': f => { f.runs.lanes = [f.runs.lanes[0], f.runs.lanes[0], f.runs.lanes[0]]; },
  'wrong candidate identity': f => { f.runs.candidateSha256 = 'wrong'; },
  'wrong mapping candidate identity': f => { f.map.candidateSha256 = 'wrong'; },
  'wrong mapping run identity': f => { f.map.lanes[0].runId = 'wrong'; },
  'wrong environment run identity': f => { f.runs.lanes[0].variables.FC_A11Y_EVIDENCE_RUN_ID = 'wrong'; },
  'wrong environment engine identity': f => { f.runs.lanes[0].variables.FC_A11Y_EVIDENCE_ENGINE = 'firefox'; },
  'duplicate complete mapping': f => { f.map.lanes.push(f.map.lanes[0]); },
};
for (const [label, mutate] of Object.entries(preflightMutations)) test(`audit rejects ${label} before execution`, () => {
  const f = auditFixture(); mutate(f); f.save(); const result = f.run();
  assert.equal(result.status, 1); assert.match(result.stderr, /identity mismatch|Exactly one/);
  assert.equal(existsSync(join(f.packet, 'replay-executed')), false);
});
for (const [label, mutate] of Object.entries({
  'duplicate complete replay row': f => { f.replay.summary.push(f.replay.summary[0]); },
  'missing complete replay row': f => { f.replay.summary.pop(); },
  'unaccounted retained row': f => { f.map.lanes.push({ key: 'history', completeGate: false }); },
  'wrong report run identity': f => {
    const path = join(f.replay.restored, 'chromium/results/playwright.json'), report = json(path);
    report.config.metadata.a11yEvidenceRunId = 'wrong'; put(path, report); f.map.lanes[0].reportSha256 = digest(readFileSync(path));
  },
  'wrong report engine identity': f => {
    const path = join(f.replay.restored, 'chromium/results/playwright.json'), report = json(path);
    report.suites[0].specs[0].tests[0].projectName = 'firefox'; put(path, report); f.map.lanes[0].reportSha256 = digest(readFileSync(path));
  },
  'wrong recorded report outcomes': f => { f.runs.lanes[0].outcomes[0].attempts[0].status = 'passed'; },
})) test(`audit rejects ${label} without accepting original failures as success`, () => {
  const f = auditFixture(); mutate(f); f.save(); const result = f.run();
  assert.equal(result.status, 1); assert.match(result.stderr, /Replay must account|identity mismatch|outcomes mismatch/);
});
