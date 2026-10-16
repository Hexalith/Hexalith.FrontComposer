import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, writeFileSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { requiredStates, validateStateEvidence } from './a11y-state-evidence.mjs';

test('requires every themed state with real contrast and mode samples', () => {
  const root = mkdtempSync(join(tmpdir(), 'ae-evidence-test-'));
  try {
    const files = requiredStates.map(state => {
      const file = join(root, `ae-state-${state}.json`);
      const sample = { paint: 'Fixture text', ratio: 7, minimum: 4.5 };
      writeFileSync(file, JSON.stringify({ state, theme: state.endsWith('-dark') ? 'Dark' : 'Light', themePaint: { rgb: state.endsWith('-dark') ? [41,41,41] : [255,255,255] }, text: [sample], systemText: [sample], icon: [sample], targets: [{ failure: null }], boundaries: [[sample]], cues: [[sample]], forcedColors: true, forcedColorsPaint: true, reducedMotion: true }));
      return file;
    });
    assert.deepEqual(validateStateEvidence(files), []);
    assert.match(validateStateEvidence(files.slice(1)).join('\n'), /Missing state evidence/);
    const state = requiredStates[0];
    const saved = JSON.parse(readFileSync(files[0], 'utf8'));
    writeFileSync(files[0], JSON.stringify({ ...saved, engine: 'webkit', forcedColorsPaint: false, systemText: [], forcedColorsStatus: 'unsupported authored-color replacement; media rules only' }));
    assert.match(validateStateEvidence(files).join('\n'), /Missing actual forced palette paint/);
    assert.deepEqual(validateStateEvidence(files, { unsupportedForcedColorsCapability: { engine: 'webkit', authoredColorReplacement: false, forced: { forcedColors: true } } }), []);
    writeFileSync(files[0], JSON.stringify({ state, text: [], systemText: [{ paint: 'Fail', ratio: 1, minimum: 4.5 }], icon: [{ ratio: 1 }], forcedColors: false }));
    const errors = validateStateEvidence(files).join('\n');
    for (const expected of ['Empty text', 'Failed systemText', 'Missing actual media-mode', 'Invalid icon', 'Mismatched actual theme']) assert.ok(errors.includes(expected), errors);
    writeFileSync(files[0], JSON.stringify({ state, icon: [] }));
    assert.match(validateStateEvidence(files).join('\n'), /Empty required icon/);
  } finally { rmSync(root, { recursive: true, force: true }); }
});
