import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync, renameSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { requiredStates, validateStateEvidence } from './a11y-state-evidence.mjs';

function matrix(run) {
  const root = mkdtempSync(join(tmpdir(), 'ae-evidence-test-'));
  try {
    const files = [], snapshots = [];
    for (const state of requiredStates) {
      const sample = { owner: 0, paint: 'Fixture text', ratio: 7, minimum: 4.5 }, edge = { ...sample, minimum: 3 };
      const file = join(root, `ae-state-${state}.json`), snapshot = join(root, `system-state-${state}.png`);
      writeFileSync(file, JSON.stringify({ engine: 'chromium', runId: 'fixture-run', state, viewport: { width: state.startsWith('rail-') ? 1600 : 320, height: 900 }, theme: state.endsWith('-dark') ? 'Dark' : 'Light', themePaint: { rgb: state.endsWith('-dark') ? [41,41,41] : [255,255,255] }, text: [sample], systemText: [sample], icon: [edge], systemIcon: [edge], targets: [{ target: '#fixture', width: 24, height: 24, conforming: true, exception: null, failure: null }], boundaries: [[edge]], systemBoundaries: [[edge]], cues: [[edge]], systemCues: [[edge]], systemSnapshot: `system-state-${state}.png`, systemFocus: { mode: 'forced-colors', status: 'measured', target: 'BUTTON#fixture', requireIndicator: true, box: { left: 10, top: 10, right: 34, bottom: 34 }, failures: [] }, forcedColors: true, forcedColorsPaint: true, reducedMotion: true }));
      writeFileSync(snapshot, 'Fixture image identity'); files.push(file); snapshots.push(snapshot);
    }
    run({ root, files, snapshots, all: files.concat(snapshots), read: i => JSON.parse(readFileSync(files[i], 'utf8')), update: (i, patch) => writeFileSync(files[i], JSON.stringify({ ...JSON.parse(readFileSync(files[i], 'utf8')), ...patch })) });
  } finally { rmSync(root, { recursive: true, force: true }); }
}

test('requires all owned state, cue, contrast and actual mode evidence', () => matrix(({ files, all, read, update }) => {
  assert.deepEqual(validateStateEvidence(all, { expectedEngine: 'chromium', expectedRun: 'fixture-run' }), []);
  assert.match(validateStateEvidence(all.filter(file => file !== files[0])).join('\n'), /Missing state evidence/);
  const saved = read(0);
  for (const [patch, expected] of [
    [{ systemCues: [] }, /Invalid system state cue/],
    [{ boundaries: [[{ owner: 0, ratio: 1 }, { owner: 1, ratio: 21 }]] }, /Invalid boundary/],
    [{ text: [] }, /Empty text/], [{ systemText: [{ paint: 'Fail', ratio: 1, minimum: 4.5 }] }, /Failed systemText/],
    [{ forcedColors: false }, /Missing actual media-mode/], [{ icon: [] }, /Empty required icon/],
    [{ systemFocus: undefined }, /Invalid forced-color focus geometry/],
    [{ systemFocus: { ...saved.systemFocus, failures: ['viewport clips target or focus ring'] } }, /Invalid forced-color focus geometry/],
  ]) { update(0, { ...saved, ...patch }); assert.match(validateStateEvidence(all).join('\n'), expected); }
}));

test('target evidence requires real finite dimensions, consistent conformity and evidenced exceptions', () => matrix(({ all, update }) => {
  for (const target of [
    { target: '#tiny', width: 1, height: 1, conforming: false, failure: null },
    { target: '#tiny', width: 1, height: 1, conforming: true, failure: null },
    { target: '#tiny', width: 1, height: 1, conforming: false, failure: null, exception: { kind: 'Spacing' } },
    { target: '#bad', width: null, height: 24, conforming: true, failure: null },
    { target: '#bad', width: 24, height: 24, conforming: true, failure: false },
  ]) { update(0, { targets: [target] }); assert.match(validateStateEvidence(all).join('\n'), /Invalid target inventory/); }
  update(0, { targets: [{ target: '#small', width: 12, height: 12, conforming: false, failure: null, exception: { selector: '#small', kind: 'Spacing', rationale: 'Measured isolated circle', keyboardPath: 'Tab', evidence: 'Fixture target inventory' } }] });
  assert.deepEqual(validateStateEvidence(all), []);
}));

test('never pools partial engine/run matrices or another owners snapshot', () => matrix(({ root, all, files, snapshots, update }) => {
  for (let i = 58; i < files.length; i++) update(i, { engine: 'firefox' });
  assert.match(validateStateEvidence(all).join('\n'), /Missing state evidence/);
  assert.match(validateStateEvidence(all, { expectedEngine: 'chromium' }).join('\n'), /Unexpected engine\/run/);
  for (let i = 58; i < files.length; i++) update(i, { engine: 'chromium', runId: 'other-run' });
  assert.match(validateStateEvidence(all).join('\n'), /Missing state evidence/);
  for (let i = 58; i < files.length; i++) update(i, { runId: 'fixture-run' });
  const foreign = join(root, 'other-owner'); mkdirSync(foreign); const moved = join(foreign, `system-state-${requiredStates[0]}.png`); renameSync(snapshots[0], moved);
  assert.match(validateStateEvidence(all.filter(file => file !== snapshots[0]).concat(moved)).join('\n'), /Missing system-state snapshot/);
}));

test('WebKit capability attribution never weakens the mandatory Chromium artifact policy', () => matrix(({ all, files, update }) => {
  for (let i = 0; i < files.length; i++) update(i, { engine: 'webkit', forcedColorsPaint: false, systemText: [], systemIcon: [], systemBoundaries: [], systemCues: [], systemSnapshot: null, forcedColorsStatus: 'unsupported authored-color replacement; media rules only' });
  assert.match(validateStateEvidence(all).join('\n'), /Missing actual forced palette paint/);
  const capability = { engine: 'webkit', authoredColorReplacement: false, forced: { forcedColors: true } };
  assert.deepEqual(validateStateEvidence(all, { expectedEngine: 'webkit', unsupportedForcedColorsCapability: capability }), []);
  assert.match(validateStateEvidence(all, { expectedEngine: 'chromium', unsupportedForcedColorsCapability: capability }).join('\n'), /Unexpected engine\/run/);
}));
