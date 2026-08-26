import { access, readFile, readdir } from 'node:fs/promises';
import { resolve } from 'node:path';

const artifactRoot = resolve(process.argv[2] ?? '../../artifacts/epic-9');

const walk = async (directory) => {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(async (entry) => {
    const path = resolve(directory, entry.name);
    return entry.isDirectory() ? walk(path) : [path];
  }));
  return nested.flat();
};

const requireFile = async (relativePath) => {
  const path = resolve(artifactRoot, relativePath);
  await access(path);
  return path;
};

const metadataPath = await requireFile('runtime-metadata.json');
const apphostStartPath = await requireFile('apphost-start.json');
const describePath = await requireFile('counter-web-describe.json');
const logsPath = await requireFile('counter-web-logs.redacted.json');
await requireFile('junit.xml');
await requireFile('playwright-report/index.html');

const allFiles = await walk(artifactRoot);
const requiredSuffixes = [
  'epic-9-command-evidence.json',
  'epic-9-live-acceptance.png',
  'trace.zip',
];
for (const suffix of requiredSuffixes) {
  if (!allFiles.some((path) => path.endsWith(suffix))) {
    throw new Error(`Epic 9 artifact is missing: ${suffix}`);
  }
}

const metadata = JSON.parse(await readFile(metadataPath, 'utf8'));
for (const field of ['candidateCommit', 'baseUrl', 'counterWebResource', 'startedAtUtc']) {
  if (typeof metadata[field] !== 'string' || metadata[field].length === 0) {
    throw new Error(`runtime-metadata.json is missing ${field}`);
  }
}

const evidencePath = allFiles.find((path) => path.endsWith('epic-9-command-evidence.json'));
const evidence = JSON.parse(await readFile(evidencePath, 'utf8'));
if (evidence.candidateCommit !== metadata.candidateCommit || evidence.baseUrl !== metadata.baseUrl) {
  throw new Error('Browser evidence does not match the AppHost candidate commit and discovered endpoint.');
}
if (evidence.observed?.indicatorDismissedByMaterialization !== true
    || evidence.observed?.firstWinsVisibleIndicatorCount !== 1) {
  throw new Error('Browser evidence is missing dismissal or first-wins proof.');
}

const sensitiveFieldPattern = /"[^"]*(?:authorization|cookie|password|secret|token|headers|dashboard[^"]*url)[^"]*"\s*:\s*"(?!\[REDACTED\])[^"\r\n]+"/iu;
for (const artifactPath of [apphostStartPath, describePath, logsPath]) {
  const artifact = await readFile(artifactPath, 'utf8');
  if (sensitiveFieldPattern.test(artifact)) {
    throw new Error(`Structured artifact still contains a sensitive field value: ${artifactPath}`);
  }
}
const failedStartPath = allFiles.find((path) => path.endsWith('apphost-start.failed.json'));
if (failedStartPath) {
  const failedStart = await readFile(failedStartPath, 'utf8');
  if (/login\?t=(?!\[REDACTED\])[^\s"&]+/iu.test(failedStart)) {
    throw new Error('Failed AppHost startup artifact still contains a dashboard login token.');
  }
}

console.log(`Validated Epic 9 proof artifacts at ${artifactRoot}`);
