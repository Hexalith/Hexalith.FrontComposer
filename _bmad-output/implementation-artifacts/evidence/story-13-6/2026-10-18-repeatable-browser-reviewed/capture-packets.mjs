// Losslessly retain every browser attempt, including intermediate failures.
import { createHash } from 'node:crypto';
import { copyFileSync, existsSync, mkdirSync, readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(import.meta.url));
const digest = bytes => createHash('sha256').update(bytes).digest('hex');
const walk = path => readdirSync(path, { withFileTypes: true }).flatMap(entry => entry.isDirectory() ? walk(join(path, entry.name)) : [join(path, entry.name)]);
const runs = JSON.parse(readFileSync(join(root, 'browser-runs.json')));
const lanes = runs.lanes.map(lane => ({ key: lane.engine, path: join(runs.rawRoot, lane.engine), engine: lane.engine, runId: lane.runId, completeGate: true }));
const map = { version: 1, candidateSha256: digest(readFileSync(join(root, 'candidate.json'))), lanes: [] };
for (const lane of lanes) {
  const reportPath = join(lane.path, 'results/playwright.json');
  const report = existsSync(reportPath) ? JSON.parse(readFileSync(reportPath)) : null;
  const paths = {};
  const visit = suite => {
    for (const spec of suite.specs ?? []) for (const test of spec.tests ?? []) for (const attempt of test.results ?? []) for (const attachment of attempt.attachments ?? []) {
      if (!attachment.path || paths[attachment.path]) continue;
      let retained = relative(lane.path, attachment.path);
      const bytes = readFileSync(attachment.path);
      if (retained.startsWith('..') || resolve(lane.path, retained) !== attachment.path) {
        retained = `external/${digest(bytes)}`;
        mkdirSync(join(lane.path, 'external'), { recursive: true });
        copyFileSync(attachment.path, join(lane.path, retained));
      }
      paths[attachment.path] = { path: retained, sha256: digest(bytes) };
    }
    for (const child of suite.suites ?? []) visit(child);
  };
  if (report) visit(report);
  const files = Object.fromEntries(walk(lane.path).map(file => [relative(lane.path, file), { sha256: digest(readFileSync(file)), bytes: statSync(file).size }]));
  const archive = `${lane.key}.tar.gz`;
  if (existsSync(join(root, archive))) throw new Error(`Refusing to replace retained archive: ${archive}`);
  const captured = spawnSync('tar', ['-czf', join(root, archive), '-C', lane.path, '.'], { encoding: 'utf8' });
  if (captured.status !== 0) throw new Error(captured.stderr);
  const entry = { ...lane, archive, archiveSha256: digest(readFileSync(join(root, archive))), files, paths, reportSha256: report ? files['results/playwright.json'].sha256 : null, stats: report?.stats ?? null };
  map.lanes.push(entry);
  console.log(`${lane.key}: ${Object.keys(files).length} files, ${Object.keys(paths).length} attachment paths`);
}
writeFileSync(join(root, 'attachment-path-map.json'), JSON.stringify(map, null, 2) + '\n');
