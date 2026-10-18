import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, rmSync, renameSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { basename, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { crc32, deflateSync, inflateSync } from 'node:zlib';
import specimenManifest from '../specimens/frontcomposer-specimen-manifest.json' with { type: 'json' };
import { requiredStates, validateStateEvidence } from './a11y-state-evidence.mjs';

// Genuine 1x1 RGBA PNGs with different pixels and equal encoded lengths.
const fixturePng = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR4AWMAgv8AAQQBAP8H9UQAAAAASUVORK5CYII=', 'base64');
const alternatePng = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR4AWP4DwQACfsD/c8LaHIAAAAASUVORK5CYII=', 'base64');

function pngChunk(type, data) {
  const chunk = Buffer.alloc(data.length + 12);
  chunk.writeUInt32BE(data.length);
  chunk.write(type, 4, 'ascii'); data.copy(chunk, 8);
  chunk.writeUInt32BE(crc32(chunk.subarray(4, -4)), chunk.length - 4);
  return chunk;
}

function rewriteChunk(png, type, edit) {
  for (let offset = 8; offset < png.length;) {
    const length = png.readUInt32BE(offset), end = offset + 12 + length;
    if (png.toString('ascii', offset + 4, offset + 8) === type) {
      const data = edit(Buffer.from(png.subarray(offset + 8, end - 4)));
      return Buffer.concat([png.subarray(0, offset), pngChunk(type, data), png.subarray(end)]);
    }
    offset = end;
  }
  throw new Error(`Fixture PNG has no ${type} chunk`);
}

function matrix(run) {
  const root = mkdtempSync(join(tmpdir(), 'ae-evidence-test-'));
  try {
    const files = [], snapshots = [];
    const attached = join(root, 'attachments'); mkdirSync(attached);
    for (const [index, state] of requiredStates.entries()) {
      const sample = { owner: 0, paint: 'Fixture text', ratio: 7, minimum: 4.5 }, edge = { ...sample, minimum: 3 };
      const file = join(root, `ae-state-${state}.json`), snapshot = join(root, `system-state-${state}.png`);
      writeFileSync(file, JSON.stringify({ testId: 'fixture-test', attempt: 0, engine: 'chromium', runId: 'fixture-run', state, viewport: { width: state.startsWith('rail-') ? 1600 : 320, height: 900 }, theme: state.endsWith('-dark') ? 'Dark' : 'Light', themePaint: { rgb: state.endsWith('-dark') ? [41,41,41] : [255,255,255] }, text: [sample], systemText: [sample], icon: [edge], systemIcon: [edge], targets: [{ target: '#fixture', width: 24, height: 24, box: { left: 0, top: 0, right: 24, bottom: 24 }, conforming: true, exception: null, failure: null }], boundaries: [[edge]], systemBoundaries: [[edge]], cues: [[edge]], systemCues: [[edge]], systemSnapshot: `system-state-${state}.png`, systemFocus: { mode: 'forced-colors', status: 'measured', target: 'BUTTON#fixture', requireIndicator: true, box: { left: 10, top: 10, right: 34, bottom: 34 }, failures: [] }, forcedColors: true, forcedColorsPaint: true, reducedMotion: true }));
      writeFileSync(snapshot, index % 2 ? alternatePng : fixturePng);
      files.push(file); snapshots.push(snapshot);
    }
    const engineImages = Object.fromEntries(['chromium', 'firefox', 'webkit'].map(engine => {
      const dir = join(attached, engine); mkdirSync(dir);
      return [engine, snapshots.map(snapshot => { const path = join(dir, basename(snapshot)); writeFileSync(path, readFileSync(snapshot)); return path; })];
    }));
    const imageAttachments = engineImages.chromium;
    const report = { config: { metadata: { a11yEvidenceRunId: 'fixture-run' } }, suites: [{ specs: [{ id: 'fixture-test', tests: ['chromium','firefox','webkit'].map(projectName => ({ projectName, expectedStatus: 'passed', status: 'expected', results: [{ retry: 0, status: 'passed', attachments: files.flatMap((path, i) => [{ path, name: `state-${requiredStates[i]}`, contentType: 'application/json' }, { path: engineImages[projectName][i], name: `system-state-${requiredStates[i]}`, contentType: 'image/png' }]) }] })) }] }] };
    run({ report, root, files, snapshots, imageAttachments, all: files.concat(snapshots), read: i => JSON.parse(readFileSync(files[i], 'utf8')), update: (i, patch) => writeFileSync(files[i], JSON.stringify({ ...JSON.parse(readFileSync(files[i], 'utf8')), ...patch })) });
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

test('WebKit capability attribution exempts absent images without weakening Chromium policy', () => matrix(({ report, root, all, files, snapshots, update }) => {
  for (const snapshot of snapshots) rmSync(snapshot);
  for (const owner of report.suites[0].specs[0].tests) {
    for (const attachment of owner.results[0].attachments.filter(item => item.contentType === 'image/png')) rmSync(attachment.path);
    owner.results[0].attachments = owner.results[0].attachments.filter(item => item.contentType !== 'image/png');
  }
  all = all.filter(path => !path.endsWith('.png'));
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
  const failedFiles = files.map(file => { const target = join(failed, basename(file)); writeFileSync(target, readFileSync(file)); return target; });
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

test('system screenshots require one readable image attachment from their authoritative attempt', () => matrix(({ report, all, imageAttachments, root }) => {
  const attempt = report.suites[0].specs[0].tests[0].results[0];
  const name = `system-state-${requiredStates[0]}`;
  const original = attempt.attachments.find(attachment => attachment.name === name);
  const attachments = attempt.attachments;
  for (const [label, replacements] of [
    ['missing', []],
    ['duplicate', [original, { ...original }]],
    ['wrong MIME', [{ ...original, contentType: 'application/octet-stream' }]],
    ['pathless', [{ ...original, path: undefined }]],
    ['inline body without path', [{ ...original, path: undefined, body: fixturePng.toString('base64') }]],
    ['missing file', [{ ...original, path: join(root, 'missing.png') }]],
    ['directory', [{ ...original, path: root }]],
  ]) {
    attempt.attachments = attachments.filter(attachment => attachment.name !== name).concat(replacements);
    assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /System-state snapshot does not match authoritative attempt attachment/, label);
  }
  attempt.attachments = attachments;
  assert.equal(fixturePng.length, alternatePng.length, 'The replacement must defeat a length-only comparison');
  writeFileSync(imageAttachments[0], alternatePng);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /System-state snapshot does not match authoritative attempt attachment/);
  writeFileSync(imageAttachments[0], fixturePng);
  assert.deepEqual(validateStateEvidence(all, { report, expectedEngine: 'chromium' }), []);
}));

test('a changed or unreadable state-local screenshot cannot reuse its original report attachment', () => matrix(({ report, all, snapshots }) => {
  writeFileSync(snapshots[0], alternatePng);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /System-state snapshot does not match authoritative attempt attachment/);
  rmSync(snapshots[0]);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /System-state snapshot does not match authoritative attempt attachment/);
  mkdirSync(snapshots[0]);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /System-state snapshot does not match authoritative attempt attachment/);
}));

test('a failed retry or different test cannot provide a successful attempts missing screenshot', () => matrix(({ report, all, files, update }) => {
  const owner = report.suites[0].specs[0].tests[0];
  const name = `system-state-${requiredStates[0]}`;
  const attachments = owner.results[0].attachments;
  const image = attachments.find(attachment => attachment.name === name);
  owner.status = 'flaky';
  owner.results = [{ retry: 0, status: 'failed', attachments }, { retry: 1, status: 'passed', attachments: attachments.filter(attachment => attachment.name !== name) }];
  for (let i = 0; i < files.length; i++) update(i, { attempt: 1 });
  report.suites[0].specs.push({ id: 'foreign-test', tests: [{ projectName: 'chromium', expectedStatus: 'passed', status: 'expected', results: [{ retry: 0, status: 'passed', attachments: [image] }] }] });
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /System-state snapshot does not match authoritative attempt attachment/);
  owner.results[1].attachments.push(image);
  assert.deepEqual(validateStateEvidence(all, { report, expectedEngine: 'chromium' }), []);
}));

test('exchanged state images and another engines attachments cannot certify a screenshot', () => matrix(({ report, all, imageAttachments }) => {
  const first = readFileSync(imageAttachments[0]), second = readFileSync(imageAttachments[1]);
  assert.notDeepEqual(first, second);
  writeFileSync(imageAttachments[0], second); writeFileSync(imageAttachments[1], first);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /Image bytes differ/);
  writeFileSync(imageAttachments[0], first); writeFileSync(imageAttachments[1], second);
  const attempt = report.suites[0].specs[0].tests[0].results[0];
  const attachments = attempt.attachments;
  attempt.attachments = attachments.filter(attachment => attachment.name !== `system-state-${requiredStates[0]}`);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /Expected one image\/png attachment/);
  attempt.attachments = attachments;
  assert.deepEqual(validateStateEvidence(all, { report, expectedEngine: 'chromium' }), []);
}));

test('retained failed-attempt pixels cannot replace the successful retry image', () => matrix(({ report, all, files, snapshots, imageAttachments, root, update }) => {
  const owner = report.suites[0].specs[0].tests[0];
  const name = `system-state-${requiredStates[0]}`;
  const oldImage = join(root, 'failed-image.png'); writeFileSync(oldImage, fixturePng);
  const attachments = owner.results[0].attachments;
  owner.status = 'flaky';
  owner.results = [{ retry: 0, status: 'failed', attachments: attachments.map(attachment => attachment.name === name ? { ...attachment, path: oldImage } : attachment) }, { retry: 1, status: 'passed', attachments }];
  for (let i = 0; i < files.length; i++) update(i, { attempt: 1 });
  writeFileSync(imageAttachments[0], alternatePng);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /Image bytes differ/);
  writeFileSync(snapshots[0], alternatePng);
  assert.deepEqual(validateStateEvidence(all, { report, expectedEngine: 'chromium' }), []);
}));

test('empty or malformed screenshot evidence returns a validation error', () => matrix(({ report, all, snapshots, imageAttachments, update }) => {
  writeFileSync(snapshots[0], Buffer.alloc(0)); writeFileSync(imageAttachments[0], Buffer.alloc(0));
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /Missing system-state snapshot/);
  writeFileSync(snapshots[0], fixturePng); writeFileSync(imageAttachments[0], fixturePng);
  for (const systemSnapshot of [42, {}, [], null]) {
    update(0, { systemSnapshot });
    assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /Missing system-state snapshot/);
  }
}));

test('decodes genuine RGB, RGBA, grayscale, indexed, 16-bit and Adam7 screenshots', () => matrix(({ report, all, snapshots, imageAttachments }) => {
  for (const [label, base64] of [
    ['RGB8', 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGP4z8AAAAMBAQDJ/pLvAAAAAElFTkSuQmCC'],
    ['grayscale1', 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQAAAAA3bvkkAAAACklEQVR4nGNoAAAAggCBd81ytgAAAABJRU5ErkJggg=='],
    ['grayscale16', 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAAAAABq7kcWAAAAC0lEQVR4nGP4/x8AAwAB//wl3FEAAAAASUVORK5CYII='],
    ['RGBA16', 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABEAYAAABPhRjKAAAAEUlEQVR4nGP4/5+BgYHh/38AEfcD/Z36ZyYAAAAASUVORK5CYII='],
    ['indexed1', 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABAQMAAAAl21bKAAAAA1BMVEX/AAAZ4gk3AAAACklEQVR4nGNgAAAAAgABSK+kcQAAAABJRU5ErkJggg=='],
    ['grayscale-alpha8', 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNo+A8AAgIBgG5WixMAAAAASUVORK5CYII='],
    ['packed grayscale2', 'iVBORw0KGgoAAAANSUhEUgAAAAMAAAABAgAAAAB0O1PJAAAACklEQVR4nGNwAAAAQgBBKTf07wAAAABJRU5ErkJggg=='],
    ['packed grayscale4', 'iVBORw0KGgoAAAANSUhEUgAAAAMAAAABBAAAAAD7e6ZpAAAAC0lEQVR4nGP40AAAAmMBcc99mVEAAAAASUVORK5CYII='],
    // 8x8 transparent pixels, all seven Adam7 passes, 271 filtered bytes.
    ['Adam7', 'iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAAGzCI4dAAAADUlEQVR4nGNgGAVIAAABDwABP/8fGgAAAABJRU5ErkJggg=='],
    ['Adam7 1x1', 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAFoEvQfAAAAC0lEQVR4nGNgAAIAAAUAAXpeqz8AAAAASUVORK5CYII='],
    ['Adam7 1x9', 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAAJCAYAAAGEQXZyAAAADElEQVR4nGNgIAEAAAAtAAHF18CGAAAAAElFTkSuQmCC'],
    ['Adam7 9x5', 'iVBORw0KGgoAAAANSUhEUgAAAAkAAAAFCAYAAAHgVPb9AAAADElEQVR4nGNgGNIAAAC/AAH3Q/0IAAAAAElFTkSuQmCC'],
  ]) {
    const bytes = Buffer.from(base64, 'base64');
    writeFileSync(snapshots[0], bytes); writeFileSync(imageAttachments[0], bytes);
    assert.deepEqual(validateStateEvidence(all, { report, expectedEngine: 'chromium' }), [], label);
  }
}));

test('matching corrupt PNG bytes cannot certify a system screenshot', () => matrix(({ report, all, snapshots, imageAttachments }) => {
  const badCrc = Buffer.from(fixturePng); badCrc[29] ^= 1;
  const corruptStream = rewriteChunk(fixturePng, 'IDAT', data => { data[0] ^= 255; return data; });
  const badAdler = rewriteChunk(fixturePng, 'IDAT', data => { data[data.length - 1] ^= 1; return data; });
  const truncatedStream = rewriteChunk(fixturePng, 'IDAT', data => data.subarray(0, -1));
  const extraPixels = rewriteChunk(fixturePng, 'IDAT', data => deflateSync(Buffer.concat([inflateSync(data), Buffer.from([0])])));
  const shortPixels = rewriteChunk(fixturePng, 'IDAT', data => deflateSync(inflateSync(data).subarray(0, -1)));
  const zeroWidth = rewriteChunk(fixturePng, 'IHDR', data => { data.writeUInt32BE(0); return data; });
  const zeroHeight = rewriteChunk(fixturePng, 'IHDR', data => { data.writeUInt32BE(0, 4); return data; });
  const hugeDimensions = rewriteChunk(fixturePng, 'IHDR', data => { data.writeUInt32BE(100_000); data.writeUInt32BE(100_000, 4); return data; });
  const excessivePixels = rewriteChunk(fixturePng, 'IHDR', data => { data.writeUInt32BE(4096); data.writeUInt32BE(4097, 4); return data; });
  const trailingCompressed = rewriteChunk(fixturePng, 'IDAT', data => Buffer.concat([data, Buffer.from([0])]));
  const invalidFilter = rewriteChunk(fixturePng, 'IDAT', () => deflateSync(Buffer.from([5, 0, 0, 0, 0])));
  const duplicateHeader = Buffer.concat([fixturePng.subarray(0, 33), fixturePng.subarray(8)]);
  const missingData = Buffer.concat([fixturePng.subarray(0, 33), fixturePng.subarray(-12)]);
  for (const [label, bytes] of [
    ['non-PNG', Buffer.from('matching text is not an image')],
    ['signature only', fixturePng.subarray(0, 8)],
    ['truncated header', fixturePng.subarray(0, 20)],
    ['truncated image data', fixturePng.subarray(0, 50)],
    ['missing IEND', fixturePng.subarray(0, -12)],
    ['truncated IEND', fixturePng.subarray(0, -1)],
    ['bad chunk CRC', badCrc], ['bad zlib header', corruptStream],
    ['bad zlib checksum with valid chunk CRC', badAdler],
    ['truncated zlib footer with valid chunk CRC', truncatedStream],
    ['extra decompressed pixels', extraPixels], ['missing decompressed pixels', shortPixels],
    ['zero width', zeroWidth], ['zero height', zeroHeight],
    ['oversized dimensions', hugeDimensions], ['pixel allocation limit', excessivePixels],
    ['trailing compressed bytes', trailingCompressed], ['invalid row filter', invalidFilter],
    ['duplicate IHDR', duplicateHeader], ['missing IDAT', missingData],
    ['trailing bytes', Buffer.concat([fixturePng, Buffer.from([0])])],
  ]) {
    // Corrupt both independent copies: byte ownership must pass before decoding fails.
    writeFileSync(snapshots[0], bytes); writeFileSync(imageAttachments[0], bytes);
    const errors = validateStateEvidence(all, { report, expectedEngine: 'chromium' });
    assert.equal(errors.length, 1, `${label}: ${errors.join('\n')}`);
    assert.match(errors[0], /Invalid system-state PNG/, label);
    assert.ok(errors[0].includes(requiredStates[0]), `${label}: diagnostic must identify its state`);
  }
  writeFileSync(snapshots[0], fixturePng); writeFileSync(imageAttachments[0], fixturePng);
  assert.deepEqual(validateStateEvidence(all, { report, expectedEngine: 'chromium' }), []);
}));

test('invalid PNG header formats cannot reach pixel decoding', () => matrix(({ report, all, snapshots, imageAttachments }) => {
  for (const [label, index, value] of [
    ['bit depth', 8, 3], ['color type', 9, 1], ['compression', 10, 1],
    ['filter method', 11, 1], ['interlace', 12, 2],
  ]) {
    const bytes = rewriteChunk(fixturePng, 'IHDR', data => { data[index] = value; return data; });
    writeFileSync(snapshots[0], bytes); writeFileSync(imageAttachments[0], bytes);
    const errors = validateStateEvidence(all, { report, expectedEngine: 'chromium' });
    assert.equal(errors.length, 1, label);
    assert.match(errors[0], /Invalid system-state PNG.*Unsupported PNG header/, label);
  }
  for (const [label, color, depth] of [['RGB1', 2, 1], ['indexed16', 3, 16]]) {
    const bytes = rewriteChunk(fixturePng, 'IHDR', data => { data[8] = depth; data[9] = color; return data; });
    writeFileSync(snapshots[0], bytes); writeFileSync(imageAttachments[0], bytes);
    assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /Invalid system-state PNG.*Unsupported PNG header/, label);
  }
}));

test('expanded PNG byte budget applies before inflation even within the pixel limit', () => matrix(({ report, all, snapshots, imageAttachments }) => {
  // This valid-CRC header requests 128 MiB of filtered RGBA16 data. The small
  // original IDAT must never reach inflation: the resource diagnostic comes first.
  const bytes = rewriteChunk(fixturePng, 'IHDR', data => { data.writeUInt32BE(4096); data.writeUInt32BE(4096, 4); data[8] = 16; return data; });
  writeFileSync(snapshots[0], bytes); writeFileSync(imageAttachments[0], bytes);
  assert.match(validateStateEvidence(all, { report, expectedEngine: 'chromium' }).join('\n'), /Invalid system-state PNG.*expanded image data exceeds the 33554432-byte decoding limit/);
}));

test('accepts consecutive and empty IDAT chunks but rejects interrupted image data', () => matrix(({ report, all, snapshots, imageAttachments }) => {
  const data = fixturePng.subarray(41, 41 + fixturePng.readUInt32BE(33));
  const header = fixturePng.subarray(0, 33), end = fixturePng.subarray(-12);
  for (const [label, chunks, valid] of [
    ['consecutive', [pngChunk('IDAT', data.subarray(0, 3)), pngChunk('IDAT', data.subarray(3))], true],
    ['empty', [pngChunk('IDAT', Buffer.alloc(0)), pngChunk('IDAT', data), pngChunk('IDAT', Buffer.alloc(0))], true],
    ['interrupted', [pngChunk('IDAT', data.subarray(0, 3)), pngChunk('tEXt', Buffer.from('Comment\0Fixture')), pngChunk('IDAT', data.subarray(3))], false],
  ]) {
    const bytes = Buffer.concat([header, ...chunks, end]);
    writeFileSync(snapshots[0], bytes); writeFileSync(imageAttachments[0], bytes);
    const errors = validateStateEvidence(all, { report, expectedEngine: 'chromium' });
    if (valid) assert.deepEqual(errors, [], label);
    else assert.match(errors.join('\n'), /Invalid system-state PNG.*image data must be contiguous/, label);
  }
}));

test('Adam7 screenshots require exactly the complete filtered pixel data', () => matrix(({ report, all, snapshots, imageAttachments }) => {
  const interlaced = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAAGzCI4dAAAADUlEQVR4nGNgGAVIAAABDwABP/8fGgAAAABJRU5ErkJggg==', 'base64');
  for (const [label, length] of [['missing byte', 270], ['extra byte', 272]]) {
    const bytes = rewriteChunk(interlaced, 'IDAT', () => deflateSync(Buffer.alloc(length)));
    writeFileSync(snapshots[0], bytes); writeFileSync(imageAttachments[0], bytes);
    const errors = validateStateEvidence(all, { report, expectedEngine: 'chromium' });
    assert.equal(errors.length, 1, label);
    assert.match(errors[0], /Invalid system-state PNG/, label);
  }
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
  const result = spawnSync(process.execPath, [fileURLToPath(new URL('./validate-a11y-artifacts.mjs', import.meta.url))], { encoding: 'utf8', env: { ...process.env, FC_E2E_OUTPUT_DIR: root, FC_E2E_HTML_REPORT_DIR: html, FC_E2E_JSON_PATH: reportPath, FC_A11Y_EVIDENCE_ENGINE: 'webkit', FC_A11Y_EVIDENCE_RUN_ID: 'fixture-run' } });
  assert.equal(result.status, 0, result.stdout + result.stderr);
  assert.match(result.stdout, /artifact validation passed/);
}));
