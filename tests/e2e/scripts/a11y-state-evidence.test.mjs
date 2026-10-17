import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync, renameSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import specimenManifest from '../specimens/frontcomposer-specimen-manifest.json' with { type: 'json' };
import { requiredStates, validateStateEvidence } from './a11y-state-evidence.mjs';

function matrix(run) {
  const root = mkdtempSync(join(tmpdir(), 'ae-evidence-test-'));
  try {
    const files = [], snapshots = [];
    for (const state of requiredStates) {
      const sample = { owner: 0, paint: 'Fixture text', ratio: 7, minimum: 4.5 }, edge = { ...sample, minimum: 3 };
      const file = join(root, `ae-state-${state}.json`), snapshot = join(root, `system-state-${state}.png`);
      writeFileSync(file, JSON.stringify({ testId: 'fixture-test', attempt: 0, engine: 'chromium', runId: 'fixture-run', state, viewport: { width: state.startsWith('rail-') ? 1600 : 320, height: 900 }, theme: state.endsWith('-dark') ? 'Dark' : 'Light', themePaint: { rgb: state.endsWith('-dark') ? [41,41,41] : [255,255,255] }, text: [sample], systemText: [sample], icon: [edge], systemIcon: [edge], targets: [{ target: '#fixture', width: 24, height: 24, box: { left: 0, top: 0, right: 24, bottom: 24 }, conforming: true, exception: null, failure: null }], boundaries: [[edge]], systemBoundaries: [[edge]], cues: [[edge]], systemCues: [[edge]], systemSnapshot: `system-state-${state}.png`, systemFocus: { mode: 'forced-colors', status: 'measured', target: 'BUTTON#fixture', requireIndicator: true, box: { left: 10, top: 10, right: 34, bottom: 34 }, failures: [] }, forcedColors: true, forcedColorsPaint: true, reducedMotion: true }));
      writeFileSync(snapshot, 'Fixture image identity'); files.push(file); snapshots.push(snapshot);
    }
    const report = { config: { metadata: { a11yEvidenceRunId: 'fixture-run' } }, suites: [{ specs: [{ id: 'fixture-test', tests: ['chromium','firefox','webkit'].map(projectName => ({ projectName, expectedStatus: 'passed', status: 'expected', results: [{ retry: 0, status: 'passed', attachments: files.map((path, i) => ({ path, name: `state-${requiredStates[i]}`, contentType: 'application/json' })) }] })) }] }] };
    run({ report, root, files, snapshots, all: files.concat(snapshots), read: i => JSON.parse(readFileSync(files[i], 'utf8')), update: (i, patch) => writeFileSync(files[i], JSON.stringify({ ...JSON.parse(readFileSync(files[i], 'utf8')), ...patch })) });
  } finally { rmSync(root, { recursive: true, force: true }); }
}

test('rejects another run when a run is expected', () => matrix(({ report, all, files, update }) => {
  for (let i = 0; i < files.length; i++) update(i, { runId: 'stale-run' });
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium', expectedRun: 'fixture-run' }).join('\n'), /Unexpected engine\/run/);
}));

test('requires all owned state, cue, contrast and actual mode evidence', () => matrix(({ report, files, all, read, update }) => {
  assert.deepEqual(validateStateEvidence(all, { report, expectedEngine: 'chromium', expectedRun: 'fixture-run' }), []);
  assert.match(validateStateEvidence(all.filter(file => file !== files[0]), { report }).join('\n'), /Missing state evidence/);
  const saved = read(0);
  for (const [patch, expected] of [
    [{ systemCues: [] }, /Invalid system state cue/],
    [{ boundaries: [[{ owner: 0, ratio: 1 }, { owner: 1, ratio: 21 }]] }, /Invalid boundary/],
    [{ text: [] }, /Empty text/], [{ systemText: [{ paint: 'Fail', ratio: 1, minimum: 4.5 }] }, /Failed systemText/],
    [{ forcedColors: false }, /Missing actual media-mode/], [{ icon: [] }, /Empty required icon/],
    [{ systemFocus: undefined }, /Invalid forced-color focus geometry/],
    [{ systemFocus: { ...saved.systemFocus, failures: ['viewport clips target or focus ring'] } }, /Invalid forced-color focus geometry/],
  ]) { update(0, { ...saved, ...patch }); assert.match(validateStateEvidence(all, { report }).join('\n'), expected); }
}));

test('target evidence requires real finite dimensions, consistent conformity and evidenced exceptions', () => matrix(({ report, all, update }) => {
  for (const target of [
    { target: '#tiny', width: 1, height: 1, conforming: false, failure: null },
    { target: '#tiny', width: 1, height: 1, conforming: true, failure: null },
    { target: '#tiny', width: 1, height: 1, conforming: false, failure: null, exception: { kind: 'Spacing' } },
    { target: '#bad', width: null, height: 24, conforming: true, failure: null },
    { target: '#bad', width: 24, height: 24, conforming: true, failure: false },
  ]) { update(0, { targets: [target] }); assert.match(validateStateEvidence(all, { report }).join('\n'), /Invalid target inventory/); }
  update(0, { targets: [{ target: '#small', width: 12, height: 12, box: { left: 0, top: 0, right: 12, bottom: 12 }, conforming: false, failure: null, exception: { selector: '#small', kind: 'Spacing', rationale: 'Measured isolated circle', keyboardPath: 'Tab', evidence: 'Fixture target inventory' } }] });
  assert.deepEqual(validateStateEvidence(all, { report }), []);
}));

test('never pools partial engine/run matrices or another owners snapshot', () => matrix(({ report, root, all, files, snapshots, update }) => {
  assert.equal(files.length % 2, 0);
  for (let i = files.length / 2; i < files.length; i++) update(i, { engine: 'firefox' });
  assert.match(validateStateEvidence(all, { report }).join('\n'), /Missing state evidence/);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /Unexpected engine\/run/);
  for (let i = files.length / 2; i < files.length; i++) update(i, { engine: 'chromium', runId: 'other-run' });
  assert.match(validateStateEvidence(all, { report }).join('\n'), /Missing state evidence/);
  for (let i = files.length / 2; i < files.length; i++) update(i, { runId: 'fixture-run' });
  const foreign = join(root, 'other-owner'); mkdirSync(foreign); const moved = join(foreign, `system-state-${requiredStates[0]}.png`); renameSync(snapshots[0], moved);
  assert.match(validateStateEvidence(all.filter(file => file !== snapshots[0]).concat(moved), { report }).join('\n'), /Missing system-state snapshot/);
}));

test('WebKit capability attribution never weakens the mandatory Chromium artifact policy', () => matrix(({ report, root, all, files, update }) => {
  for (let i = 0; i < files.length; i++) update(i, { engine: 'webkit', forcedColorsPaint: false, systemText: [], systemIcon: [], systemBoundaries: [], systemCues: [], systemSnapshot: null, forcedColorsStatus: 'unsupported authored-color replacement; media rules only' });
  assert.match(validateStateEvidence(all, { report }).join('\n'), /Missing actual forced palette paint/);
  const capability = { engine: 'webkit', runId: 'fixture-run', testId: 'fixture-test', attempt: 0, authoredColorReplacement: false, forced: { forcedColors: true } };
  const path = join(root, 'forced-colors-capability-webkit.json');
  writeFileSync(path, JSON.stringify(capability));
  report.suites[0].specs[0].tests[2].results[0].attachments.push({ path, name: 'forced-color-capability', contentType: 'application/json' });
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'webkit', unsupportedForcedColorsCapability: { ...capability, runId: 'stale-run' } }).join('\n'), /Unbound forced-color capability/);
  assert.deepEqual(validateStateEvidence(all, { report, expectedEngine: 'webkit', unsupportedForcedColorsCapability: capability }), []);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium', unsupportedForcedColorsCapability: capability }).join('\n'), /Unexpected engine\/run/);
}));


test('successful retry replaces the failed attempt without fragment pooling', () => matrix(({ report, root, all, files, update }) => {
  const test = report.suites[0].specs[0].tests[0];
  const attachments = test.results[0].attachments;
  test.status = 'flaky'; test.results = [{ retry: 0, status: 'failed' }, { retry: 1, status: 'passed', attachments }];
  assert.match(validateStateEvidence(all, { report }).join('\n'), /Missing state evidence/);
  const failed = join(root, 'failed-attempt'); mkdirSync(failed);
  const failedFiles = files.map(file => { const target = join(failed, file.split('/').at(-1)); writeFileSync(target, readFileSync(file)); return target; });
  for (let i = 0; i < files.length; i++) update(i, { attempt: 1 });
  assert.deepEqual(validateStateEvidence(all.concat(failedFiles), { report }), []);
  update(0, { attempt: 0 });
  assert.match(validateStateEvidence(all.concat(failedFiles), { report }).join('\n'), /Missing state evidence/);
  update(0, { attempt: 1 }); test.results[1].status = 'failed'; test.status = 'unexpected';
  assert.match(validateStateEvidence(all, { report }).join('\n'), /unsuccessful Playwright outcome/);
}));

test('missing or mixed authoritative reports cannot certify a matrix', () => matrix(({ report, all }) => {
  assert.match(validateStateEvidence(all).join('\n'), /Missing authoritative/);
  report.config.metadata.a11yEvidenceRunId = 'other-run';
  assert.match(validateStateEvidence(all, { report }).join('\n'), /report ownership/);
}));


test('target geometry mirrors stale, spacing and essential exception applicability', () => matrix(({ all, report, update }) => {
  const exception = { selector: '#small', kind: 'Spacing', rationale: 'Measured circle', keyboardPath: 'Tab', evidence: 'Fixture' };
  const small = { target: '#small', width: 12, height: 12, box: { left: 0, top: 0, right: 12, bottom: 12 }, conforming: false, exception, failure: null };
  update(0, { targets: [small, { ...small, target: '#neighbor', box: { left: 15, top: 0, right: 27, bottom: 12 } }] });
  assert.match(validateStateEvidence(all, { report }).join('\n'), /Invalid target inventory/);
  update(0, { targets: [{ ...small, exception: { ...exception, kind: 'Essential', essentialReason: 'Fixed presentation is required' } }] });
  assert.match(validateStateEvidence(all, { report }).join('\n'), /Invalid target inventory/);
  update(0, { targets: [{ ...small, exception: { ...exception, kind: 'Essential', essentialReason: 'Fixed presentation is required' }, traits: { essentialIdentified: true } }] });
  assert.deepEqual(validateStateEvidence(all, { report }), []);
  update(0, { targets: [{ ...small, width: 24, height: 24, box: { left: 0, top: 0, right: 24, bottom: 24 }, conforming: true }] });
  assert.match(validateStateEvidence(all, { report }).join('\n'), /Invalid target inventory/);
}));


test('state payload must match the final successful attempt attachment', () => matrix(({ report, all, files }) => {
  const attachments = report.suites[0].specs[0].tests[0].results[0].attachments;
  attachments[0].path = files[1];
  assert.match(validateStateEvidence(all, { report }).join('\n'), /authoritative attempt attachment/);
  attachments[0].path = files[0];
  attachments.push({ ...attachments[0] });
  assert.match(validateStateEvidence(all, { report }).join('\n'), /authoritative attempt attachment/);
}));


test('expected unrelated skips supply no state authority and cannot fill a skipped owner', () => matrix(({ report, all }) => {
  report.suites[0].specs.push({ id: 'existing-platform-skip', tests: [{ projectName: 'chromium', expectedStatus: 'skipped', status: 'skipped', results: [{ retry: 0, status: 'skipped' }] }] });
  assert.deepEqual(validateStateEvidence(all, { report, expectedEngine: 'chromium' }), []);
  const owner = report.suites[0].specs[0].tests[0];
  owner.expectedStatus = owner.status = owner.results[0].status = 'skipped';
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /Unbound Playwright attempt|Missing state evidence/);
}));


test('runner, setup and teardown errors invalidate an otherwise passing report', () => matrix(({ report, all }) => {
  for (const message of ['runner error', 'setup error', 'teardown error']) {
    report.errors = [{ message }];
    assert.match(validateStateEvidence(all, { report }).join('\n'), /runner\/setup\/teardown errors/);
  }
}));

test('CLI resolves capability from the successful retry while retaining the failed file', () => matrix(({ report, root, files, update }) => {
  const owner = report.suites[0].specs[0].tests[2];
  owner.status = 'flaky'; owner.results = [{ retry: 0, status: 'failed' }, { ...owner.results[0], retry: 1 }];
  for (let i = 0; i < files.length; i++) update(i, { engine: 'webkit', attempt: 1, forcedColorsPaint: false, systemText: [], systemIcon: [], systemBoundaries: [], systemCues: [], systemSnapshot: null, forcedColorsStatus: 'unsupported authored-color replacement; media rules only' });
  const paths = ['a-failed', 'z-final'].map(name => { const dir = join(root, name); mkdirSync(dir); return join(dir, 'forced-colors-capability-webkit.json'); });
  const capability = { engine: 'webkit', runId: 'fixture-run', testId: 'capability-test', attempt: 1, authoredColorReplacement: false, forced: { forcedColors: true } };
  writeFileSync(paths[0], JSON.stringify({ ...capability, attempt: 0 }));
  writeFileSync(paths[1], JSON.stringify(capability));
  report.suites[0].specs.push({ id: 'capability-test', tests: [{ projectName: 'webkit', expectedStatus: 'passed', status: 'flaky', results: paths.map((path, retry) => ({ retry, status: retry ? 'passed' : 'failed', attachments: [{ path, name: 'forced-color-capability', contentType: 'application/json' }] })) }] });
  const html = join(root, 'html'); mkdirSync(html); writeFileSync(join(html, 'index.html'), 'Fixture report');
  writeFileSync(join(root, 'junit.xml'), 'Fixture outcomes');
  const reportPath = join(root, 'playwright.json'); writeFileSync(reportPath, JSON.stringify(report));
  for (const artifact of specimenManifest.routes.flatMap(route => route.expectedArtifacts).filter(name => name.startsWith('axe-') || name.startsWith('focus-'))) writeFileSync(join(root, artifact), '{}');
  const result = spawnSync(process.execPath, [new URL('./validate-a11y-artifacts.mjs', import.meta.url).pathname], { encoding: 'utf8', env: { ...process.env, FC_E2E_OUTPUT_DIR: root, FC_E2E_HTML_REPORT_DIR: html, FC_E2E_JSON_PATH: reportPath, FC_A11Y_EVIDENCE_ENGINE: 'webkit', FC_A11Y_EVIDENCE_RUN_ID: 'fixture-run' } });
  assert.equal(result.status, 0, result.stdout + result.stderr);
  assert.match(result.stdout, /artifact validation passed/);
}));
