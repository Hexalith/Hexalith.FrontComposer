import { createHash } from 'node:crypto';
import { lstat, readFile, readdir } from 'node:fs/promises';
import { dirname, isAbsolute, relative, resolve, sep } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const VIEW_KEY = 'Counter:Counter.Domain.CounterProjection';
const INDICATOR_COPY = 'New item. It may not match current filters yet.';
const INDICATOR_ARIA_LABEL = 'New item added outside current filters';
const TEST_FILE = 'epic-9-fresh-row-acceptance.spec.ts';
const TEST_SUITE_TITLE = 'Epic 9 composed and live acceptance';
const TEST_TITLE = 'generated create and update converge through the indicator into an already-rendered grid';
const SHA_PATTERN = /^[0-9a-f]{40}$/u;
const CHECKSUM_PATTERN = /^([0-9a-f]{64})  ([^\r\n]+)$/u;
const UTC_TIMESTAMP_PATTERN = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$/u;
const SENSITIVE_KEY_PATTERN = /authorization|cookie|password|secret|token|headers|dashboard.*url/iu;
const REDACTED = '[REDACTED]';
const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const APPHOST_RELATIVE = 'src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj';
const EVENTSTORE_ASPIRE_RELATIVE = 'references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj';
const INITIAL_START_COMMAND = `aspire start --apphost ${APPHOST_RELATIVE} --isolated --non-interactive --format Json --nologo`;
const FALLBACK_DEPENDENCY_BUILD_COMMAND = `dotnet build ${EVENTSTORE_ASPIRE_RELATIVE} --configuration Debug -m:1 -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false`;
const FALLBACK_APPHOST_BUILD_COMMAND = `dotnet build ${APPHOST_RELATIVE} --configuration Debug -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false`;
const FALLBACK_START_COMMAND = `aspire start --apphost ${APPHOST_RELATIVE} --isolated --no-build --non-interactive --format Json --nologo`;
const COMMON_COMMANDS = [
  `aspire wait counter-web --status up --timeout 180 --apphost ${APPHOST_RELATIVE} --non-interactive --nologo`,
  `aspire describe counter-web --apphost ${APPHOST_RELATIVE} --format Json --non-interactive --nologo`,
  'npm run test:epic-9',
  `aspire logs counter-web --apphost ${APPHOST_RELATIVE} --format Json --tail 1000 --non-interactive --nologo`,
  `aspire stop --apphost ${APPHOST_RELATIVE} --non-interactive --nologo`,
  'generate complete sorted checksums.sha256',
];
const VALIDATE_FINAL_COMMAND = 'npm run validate:epic-9-artifacts -- <artifact-root> --candidate <candidate>';
const VALIDATE_DEVELOPMENT_COMMAND = `${VALIDATE_FINAL_COMMAND} --allow-dirty`;

const assertClaim = (condition, message) => {
  if (!condition) throw new Error(message);
};

const toRelativePath = (root, path) => relative(root, path).split(sep).join('/');

const walkFiles = async (root, directory = root) => {
  const entries = await readdir(directory, { withFileTypes: true });
  entries.sort((left, right) => Buffer.compare(Buffer.from(left.name), Buffer.from(right.name)));
  const files = [];
  for (const entry of entries) {
    const path = resolve(directory, entry.name);
    const relativePath = toRelativePath(root, path);
    const file = await lstat(path);
    assertClaim(!file.isSymbolicLink(), `Epic 9 artifacts must not contain symlinks: ${relativePath}`);
    if (file.isDirectory()) {
      files.push(...await walkFiles(root, path));
    } else {
      assertClaim(file.isFile(), `Epic 9 artifact is not a regular file: ${relativePath}`);
      files.push({ path, relativePath, size: file.size });
    }
  }
  return files;
};

const requireFile = (filesByPath, relativePath) => {
  const file = filesByPath.get(relativePath);
  assertClaim(file && file.size > 0, `Epic 9 artifact is empty or missing: ${relativePath}`);
  return file.path;
};

const assertRootArtifactName = (files, name) => {
  const matches = files.filter((file) => file.relativePath.split('/').at(-1) === name);
  assertClaim(
    matches.length <= 1 && (matches.length === 0 || matches[0].relativePath === name),
    `Reserved Epic 9 artifact name must appear only at the artifact root: ${name}`,
  );
};

const requireUniqueExactName = (files, name, parentPrefix) => {
  const matches = files.filter((file) => {
    const segments = file.relativePath.split('/');
    return segments.at(-1) === name && file.relativePath.startsWith(`${parentPrefix}/`);
  });
  assertClaim(matches.length === 1, `Epic 9 artifact must contain exactly one ${name}; found ${matches.length}.`);
  assertClaim(matches[0].size > 0, `Epic 9 artifact is empty: ${matches[0].relativePath}`);
  return matches[0].path;
};

const readJson = async (path, label) => {
  try {
    return JSON.parse(await readFile(path, 'utf8'));
  } catch (error) {
    throw new Error(`${label} is not valid JSON.`, { cause: error });
  }
};

const requireNonEmptyString = (value, claim) => {
  assertClaim(typeof value === 'string' && value.trim().length > 0, `${claim} must be a non-empty string.`);
};

const assertSensitiveValuesRedacted = (value, artifactLabel, path = '$') => {
  if (Array.isArray(value)) {
    value.forEach((item, index) => assertSensitiveValuesRedacted(item, artifactLabel, `${path}[${index}]`));
    return;
  }
  if (value === null || typeof value !== 'object') return;
  for (const [key, nested] of Object.entries(value)) {
    if (SENSITIVE_KEY_PATTERN.test(key)) {
      assertClaim(nested === REDACTED, `${artifactLabel} contains an unredacted sensitive value at ${path}.${key}.`);
    } else {
      assertSensitiveValuesRedacted(nested, artifactLabel, `${path}.${key}`);
    }
  }
};

const expectedCommands = (startMode, evidenceMode) => {
  const validationCommand = evidenceMode === 'development'
    ? VALIDATE_DEVELOPMENT_COMMAND
    : VALIDATE_FINAL_COMMAND;
  return startMode === 'isolated-build'
    ? [INITIAL_START_COMMAND, ...COMMON_COMMANDS, validationCommand]
    : [INITIAL_START_COMMAND, FALLBACK_DEPENDENCY_BUILD_COMMAND, FALLBACK_APPHOST_BUILD_COMMAND,
        FALLBACK_START_COMMAND, ...COMMON_COMMANDS, validationCommand];
};

const assertExactArray = (actual, expected, claim) => {
  assertClaim(Array.isArray(actual), `${claim} must be an array.`);
  assertClaim(
    actual.length === expected.length && actual.every((value, index) => value === expected[index]),
    `${claim} does not equal the exact expected command sequence.`,
  );
};

const byteSorted = (values) => [...values].sort((left, right) => Buffer.compare(Buffer.from(left), Buffer.from(right)));

const validateChecksumManifest = async (artifactRoot, files, manifestPath) => {
  const manifest = await readFile(manifestPath, 'utf8');
  assertClaim(manifest.endsWith('\n'), 'checksums.sha256 must end with a newline.');
  const lines = manifest.slice(0, -1).split('\n');
  assertClaim(lines.length > 0 && lines.every((line) => line.length > 0), 'checksums.sha256 contains a blank entry.');
  const entries = lines.map((line) => {
    const match = line.match(CHECKSUM_PATTERN);
    assertClaim(match, `checksums.sha256 contains an invalid entry: ${line}`);
    const relativePath = match[2];
    assertClaim(
      !isAbsolute(relativePath)
        && !relativePath.startsWith('./')
        && !relativePath.split('/').includes('..')
        && !relativePath.includes('\\'),
      `checksums.sha256 contains an unsafe or non-canonical path: ${relativePath}`,
    );
    return { digest: match[1], relativePath };
  });
  const recordedPaths = entries.map((entry) => entry.relativePath);
  assertClaim(new Set(recordedPaths).size === recordedPaths.length, 'checksums.sha256 contains duplicate paths.');
  assertClaim(
    recordedPaths.every((value, index) => value === byteSorted(recordedPaths)[index]),
    'checksums.sha256 entries must be sorted by relative path.',
  );
  const expectedPaths = byteSorted(
    files.filter((file) => file.relativePath !== 'checksums.sha256').map((file) => file.relativePath),
  );
  assertClaim(
    recordedPaths.length === expectedPaths.length
      && recordedPaths.every((value, index) => value === expectedPaths[index]),
    'checksums.sha256 must list every regular artifact exactly once and no unlisted artifact may exist.',
  );
  for (const entry of entries) {
    const content = await readFile(resolve(artifactRoot, entry.relativePath));
    const actualDigest = createHash('sha256').update(content).digest('hex');
    assertClaim(actualDigest === entry.digest, `checksums.sha256 mismatch for ${entry.relativePath}.`);
  }
};

const elements = (markup, element) => [...markup.matchAll(new RegExp(`<${element}\\b([^>]*)>`, 'gu'))];

const attributeValue = (elementMatch, element, attribute) => {
  const attributeMatch = elementMatch[1].match(new RegExp(`\\b${attribute}="([^"]*)"`, 'u'));
  assertClaim(attributeMatch, `${element}.${attribute} is missing from JUnit evidence.`);
  return attributeMatch[1];
};

const assertJUnitZeroCounts = (elementMatch, element) => {
  assertClaim(attributeValue(elementMatch, element, 'tests') === '1', `${element} must report tests=1.`);
  for (const attribute of ['failures', 'errors', 'skipped']) {
    assertClaim(attributeValue(elementMatch, element, attribute) === '0', `${element} must report ${attribute}=0.`);
  }
};

const validateJUnit = async (junitPath) => {
  const junit = await readFile(junitPath, 'utf8');
  const roots = elements(junit, 'testsuites');
  const suites = elements(junit, 'testsuite');
  const cases = elements(junit, 'testcase');
  assertClaim(roots.length === 1, 'JUnit evidence must contain exactly one testsuites root.');
  assertClaim(suites.length === 1, 'JUnit evidence must contain exactly one testsuite.');
  assertClaim(cases.length === 1, 'JUnit evidence must contain exactly one testcase.');
  assertJUnitZeroCounts(roots[0], 'testsuites');
  assertJUnitZeroCounts(suites[0], 'testsuite');
  assertClaim(attributeValue(suites[0], 'testsuite', 'name') === TEST_FILE, 'JUnit suite name must identify the Epic 9 spec.');
  assertClaim(attributeValue(suites[0], 'testsuite', 'hostname') === 'chromium', 'JUnit suite hostname/project must equal chromium.');
  assertClaim(attributeValue(cases[0], 'testcase', 'classname') === TEST_FILE, 'JUnit testcase classname must identify the Epic 9 spec.');
  const caseName = attributeValue(cases[0], 'testcase', 'name');
  assertClaim(
    caseName.includes(TEST_SUITE_TITLE) && caseName.includes(TEST_TITLE),
    'JUnit testcase name must contain the Epic 9 composed/live test title.',
  );
};

const assertNoDashboardLoginToken = async (files) => {
  const lifecycleFiles = files.filter((file) => !file.relativePath.includes('/') && /\.(?:json|log)$/u.test(file.relativePath));
  for (const file of lifecycleFiles) {
    const text = await readFile(file.path, 'utf8');
    for (const match of text.matchAll(/login\?t=([^\s"'&<>]+)/giu)) {
      assertClaim(match[1] === REDACTED, `${file.relativePath} contains an unredacted dashboard login token.`);
    }
  }
};

export const validateEpic9Artifacts = async (
  artifactRootInput,
  { allowDirty = false, expectedCandidate } = {},
) => {
  const artifactRoot = isAbsolute(artifactRootInput)
    ? resolve(artifactRootInput)
    : resolve(REPOSITORY_ROOT, artifactRootInput);
  const files = await walkFiles(artifactRoot);
  const filesByPath = new Map(files.map((file) => [file.relativePath, file]));
  assertClaim(filesByPath.size === files.length, 'Epic 9 artifact paths are not unique.');
  for (const name of [
    'checksums.sha256',
    'runtime-metadata.json',
    'apphost-preflight.json',
    'apphost-postflight.json',
    'apphost-start.json',
    'apphost-start.failed.json',
    'apphost-serialized-build.log',
    'counter-web-wait.log',
    'counter-web-describe.json',
    'counter-web-logs.redacted.json',
    'junit.xml',
  ]) assertRootArtifactName(files, name);

  const manifestPath = requireFile(filesByPath, 'checksums.sha256');
  await validateChecksumManifest(artifactRoot, files, manifestPath);
  const metadataPath = requireFile(filesByPath, 'runtime-metadata.json');
  const preflightPath = requireFile(filesByPath, 'apphost-preflight.json');
  const postflightPath = requireFile(filesByPath, 'apphost-postflight.json');
  const apphostStartPath = requireFile(filesByPath, 'apphost-start.json');
  const describePath = requireFile(filesByPath, 'counter-web-describe.json');
  const logsPath = requireFile(filesByPath, 'counter-web-logs.redacted.json');
  const junitPath = requireFile(filesByPath, 'junit.xml');
  const htmlReportPath = requireFile(filesByPath, 'playwright-report/index.html');
  requireFile(filesByPath, 'counter-web-wait.log');

  const evidencePath = requireUniqueExactName(files, 'epic-9-command-evidence.json', 'playwright-results');
  const screenshotPath = requireUniqueExactName(files, 'epic-9-live-acceptance.png', 'playwright-results');
  const tracePath = requireUniqueExactName(files, 'trace.zip', 'playwright-results');

  const metadata = await readJson(metadataPath, 'runtime-metadata.json');
  assertClaim(metadata.schemaVersion === 1, 'runtime-metadata.json.schemaVersion must equal 1.');
  assertClaim(metadata.story === '9.8', 'runtime-metadata.json.story must equal 9.8.');
  assertClaim(SHA_PATTERN.test(metadata.candidateCommit), 'runtime-metadata.json.candidateCommit must be a full lowercase commit SHA.');
  assertClaim(typeof metadata.workingTreeDirty === 'boolean', 'runtime-metadata.json.workingTreeDirty must be a boolean.');
  assertClaim(allowDirty || metadata.workingTreeDirty === false, 'Final Epic 9 evidence cannot come from a dirty working tree.');
  if (expectedCandidate !== undefined) {
    assertClaim(SHA_PATTERN.test(expectedCandidate), 'The expected candidate must be a full lowercase commit SHA.');
    assertClaim(metadata.candidateCommit === expectedCandidate, 'Runtime metadata does not match the expected candidate commit.');
  }
  assertClaim(metadata.evidenceMode === (allowDirty ? 'development' : 'final'), 'runtime-metadata.json.evidenceMode does not match validation mode.');
  requireNonEmptyString(metadata.baseUrl, 'runtime-metadata.json.baseUrl');
  assertClaim(/^https?:\/\/[^\s]+$/u.test(metadata.baseUrl), 'runtime-metadata.json.baseUrl must be an HTTP(S) endpoint.');
  requireNonEmptyString(metadata.counterWebResource, 'runtime-metadata.json.counterWebResource');
  requireNonEmptyString(metadata.counterWebLogResource, 'runtime-metadata.json.counterWebLogResource');
  requireNonEmptyString(metadata.startedAtUtc, 'runtime-metadata.json.startedAtUtc');
  assertClaim(UTC_TIMESTAMP_PATTERN.test(metadata.startedAtUtc), 'runtime-metadata.json.startedAtUtc must use strict UTC YYYY-MM-DDTHH:mm:ssZ.');
  assertClaim(
    Number.isFinite(Date.parse(metadata.startedAtUtc))
      && new Date(metadata.startedAtUtc).toISOString().replace('.000Z', 'Z') === metadata.startedAtUtc,
    'runtime-metadata.json.startedAtUtc is not a valid UTC instant.',
  );
  assertClaim(
    ['isolated-build', 'isolated-no-build-after-serialized-build'].includes(metadata.startMode),
    'runtime-metadata.json.startMode is not a supported isolated start mode.',
  );
  for (const tool of ['aspire', 'dotnet', 'node']) {
    requireNonEmptyString(metadata.toolVersions?.[tool], `runtime-metadata.json.toolVersions.${tool}`);
  }
  assertExactArray(metadata.commands, expectedCommands(metadata.startMode, metadata.evidenceMode), 'runtime-metadata.json.commands');

  const failedStart = filesByPath.get('apphost-start.failed.json');
  const serializedBuild = filesByPath.get('apphost-serialized-build.log');
  if (metadata.startMode === 'isolated-no-build-after-serialized-build') {
    assertClaim(failedStart?.size > 0, 'Fallback mode requires non-empty apphost-start.failed.json.');
    assertClaim(serializedBuild?.size > 0, 'Fallback mode requires non-empty apphost-serialized-build.log.');
  } else {
    assertClaim(!failedStart && !serializedBuild, 'isolated-build must reject fallback-only startup artifacts.');
  }

  const preflight = await readJson(preflightPath, 'apphost-preflight.json');
  assertClaim(Array.isArray(preflight), 'apphost-preflight.json must contain the Aspire process list.');
  assertClaim(!JSON.stringify(preflight).includes('Hexalith.FrontComposer.AppHost'), 'AppHost preflight shows an existing FrontComposer run.');
  const postflight = await readJson(postflightPath, 'apphost-postflight.json');
  assertClaim(Array.isArray(postflight), 'apphost-postflight.json must contain the Aspire process list.');
  assertClaim(!JSON.stringify(postflight).includes('Hexalith.FrontComposer.AppHost'), 'AppHost postflight shows the proof AppHost is still running.');
  const apphostStart = await readJson(apphostStartPath, 'apphost-start.json');
  assertClaim(Number.isInteger(apphostStart.appHostPid) && apphostStart.appHostPid > 0, 'apphost-start.json.appHostPid must be a positive integer.');
  assertClaim(
    typeof apphostStart.appHostPath === 'string' && apphostStart.appHostPath.endsWith(`/${APPHOST_RELATIVE}`),
    'apphost-start.json.appHostPath does not identify the FrontComposer AppHost.',
  );
  const describe = await readJson(describePath, 'counter-web-describe.json');
  const logs = await readJson(logsPath, 'counter-web-logs.redacted.json');
  for (const [label, artifact] of [
    ['runtime-metadata.json', metadata],
    ['apphost-preflight.json', preflight],
    ['apphost-postflight.json', postflight],
    ['apphost-start.json', apphostStart],
    ['counter-web-describe.json', describe],
    ['counter-web-logs.redacted.json', logs],
  ]) assertSensitiveValuesRedacted(artifact, label);
  await assertNoDashboardLoginToken(files);

  const describedResource = describe.resources?.find?.((resource) => resource.name === metadata.counterWebResource);
  assertClaim(describedResource, 'counter-web-describe.json does not contain the recorded counter-web resource.');
  const describedUrls = describedResource.urls?.map?.((entry) => entry.url) ?? [];
  assertClaim(describedUrls.includes(metadata.baseUrl), 'counter-web-describe.json does not contain the recorded endpoint.');
  assertClaim(Array.isArray(logs.logs) && logs.logs.length > 0, 'counter-web-logs.redacted.json.logs must be non-empty.');
  for (const [index, entry] of logs.logs.entries()) {
    assertClaim(entry.resourceName === metadata.counterWebLogResource, `Counter log ${index} does not identify the recorded counter resource.`);
    requireNonEmptyString(entry.content, `Counter log ${index}.content`);
  }
  assertClaim(logs.logs.some((entry) => entry.content.includes('Now listening on')), 'Counter logs must include a Now listening on event.');

  const evidence = await readJson(evidencePath, 'epic-9-command-evidence.json');
  assertSensitiveValuesRedacted(evidence, 'epic-9-command-evidence.json');
  assertClaim(evidence.schemaVersion === 1, 'Browser evidence schemaVersion must equal 1.');
  assertClaim(evidence.story === '9.8', 'Browser evidence story must equal 9.8.');
  assertClaim(evidence.candidateCommit === metadata.candidateCommit, 'Browser evidence candidate does not match runtime metadata.');
  assertClaim(evidence.baseUrl === metadata.baseUrl, 'Browser evidence endpoint does not match runtime metadata.');
  requireNonEmptyString(evidence.tenantScope, 'Browser evidence tenantScope');
  requireNonEmptyString(evidence.userScope, 'Browser evidence userScope');
  assertClaim(evidence.uiLanguage === 'en', 'Browser evidence uiLanguage must equal en.');
  assertClaim(evidence.viewKey === VIEW_KEY, 'Browser evidence viewKey is not the Counter generated-grid lane.');
  assertClaim(/^counter-e9-[0-9]+$/u.test(evidence.exactTargetKey), 'Browser evidence exactTargetKey is not a fresh Epic 9 key.');

  assertClaim(Array.isArray(evidence.dispatchedCommands) && evidence.dispatchedCommands.length === 4, 'Browser evidence must record the create and three update dispatches.');
  const expectedCommandTypes = [
    'Counter.Domain.CreateCounterCommand',
    'Counter.Domain.UpdateCounterCommand',
    'Counter.Domain.UpdateCounterCommand',
    'Counter.Domain.UpdateCounterCommand',
  ];
  evidence.dispatchedCommands.forEach((command, index) => {
    assertClaim(command.commandType === expectedCommandTypes[index], `Browser evidence command ${index} has the wrong type.`);
    assertClaim(command.counterId === evidence.exactTargetKey, `Browser evidence command ${index} does not target the exact key.`);
    const payloadField = index === 0 ? 'initialValue' : 'amount';
    assertClaim(command[payloadField] === REDACTED, `Browser evidence command ${index} payload is not redacted.`);
  });

  const observed = evidence.observed;
  assertClaim(observed?.gridWasRenderedBeforeDispatch === true, 'Browser evidence must observe the rendered grid before dispatch.');
  assertClaim(observed.exactKeyCountBeforeDispatch === 0, 'Browser evidence exact key must be absent before dispatch.');
  assertClaim(observed.exactKeyMatchedDispatchCount === evidence.dispatchedCommands.length, 'Browser evidence exact key must match every dispatch.');
  assertClaim(observed.tenantScopeLength === evidence.tenantScope.length && observed.tenantScopeLength > 0, 'Browser evidence tenant scope length is not measured correctly.');
  assertClaim(observed.userScopeLength === evidence.userScope.length && observed.userScopeLength > 0, 'Browser evidence user scope length is not measured correctly.');
  assertClaim(observed.createIndicatorVisibleCount === 1, 'Browser evidence create indicator visible count must equal 1.');
  assertClaim(observed.indicatorRole === 'status', 'Browser evidence indicator role must equal status.');
  assertClaim(observed.indicatorAriaLive === 'polite', 'Browser evidence indicator aria-live must equal polite.');
  assertClaim(observed.indicatorAriaLabel === INDICATOR_ARIA_LABEL, 'Browser evidence indicator aria-label is not localized copy.');
  assertClaim(observed.createIndicatorCopy === INDICATOR_COPY, 'Browser evidence create announcement is not localized copy.');
  for (const stage of ['catchUpCaptured', 'catchUpPublished', 'catchUpReceived']) {
    assertClaim(observed[stage] === 1, `Browser evidence observed.${stage} must equal the DOM counter 1.`);
  }
  assertClaim(observed.materializedCountAfterCreate === 41, 'Browser evidence create count must equal 41.');
  assertClaim(observed.createIndicatorCountAfterMaterialization === 0, 'Browser evidence create indicator must be dismissed by materialization.');
  assertClaim(observed.overlapIndicatorCountBeforeSecondDispatch === 1, 'Browser evidence must observe one indicator before the overlapping dispatch.');
  assertClaim(observed.materializedCountBeforeSecondDispatch === 41, 'Browser evidence row count must remain 41 immediately before the second dispatch.');
  assertClaim(observed.overlapIndicatorCountAfterSecondDispatch === 1, 'Browser evidence must observe one indicator after the overlapping dispatch.');
  assertClaim(observed.overlapIndicatorElementRetained === true, 'Browser evidence must retain the same connected indicator element through the second observation.');
  assertClaim(observed.overlapIndicatorCopyBeforeSecondDispatch === INDICATOR_COPY, 'Browser evidence overlap announcement before dispatch is not localized copy.');
  assertClaim(observed.overlapIndicatorCopyAfterSecondDispatch === INDICATOR_COPY, 'Browser evidence overlap announcement after dispatch is not localized copy.');
  assertClaim(observed.materializedCountAfterOverlappingUpdates === 44, 'Browser evidence overlapping-update count must equal 44.');
  assertClaim(observed.overlappingIndicatorCountAfterMaterialization === 0, 'Browser evidence overlapping indicator must be dismissed by materialization.');
  assertClaim(observed.laterUpdateIndicatorVisibleCount === 1, 'Browser evidence later-update indicator visible count must equal 1.');
  assertClaim(observed.materializedCountAfterLaterUpdate === 52, 'Browser evidence later-update count must equal 52.');
  assertClaim(observed.laterUpdateIndicatorCountAfterMaterialization === 0, 'Browser evidence later-update indicator must be dismissed by materialization.');

  await validateJUnit(junitPath);
  const htmlReport = await readFile(htmlReportPath, 'utf8');
  assertClaim(/^\s*<!DOCTYPE html>/iu.test(htmlReport), 'Playwright HTML report is not an HTML document.');
  assertClaim(/<title>\s*Playwright Test Report\s*<\/title>/iu.test(htmlReport), 'Playwright HTML report title is missing or incorrect.');
  assertClaim(/<div id=['"]root['"]><\/div>/iu.test(htmlReport) && /id=['"]playwrightReportBase64['"]/iu.test(htmlReport), 'Playwright HTML report payload markers are missing.');
  const screenshot = await readFile(screenshotPath);
  assertClaim(screenshot.length >= 24 && screenshot.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])), 'Epic 9 screenshot is not a PNG file.');
  assertClaim(screenshot.subarray(12, 16).equals(Buffer.from('IHDR')), 'Epic 9 screenshot is missing a PNG IHDR chunk.');
  assertClaim(screenshot.readUInt32BE(16) > 0 && screenshot.readUInt32BE(20) > 0, 'Epic 9 screenshot dimensions must be greater than zero.');
  const trace = await readFile(tracePath);
  assertClaim(trace.length >= 4 && trace.subarray(0, 4).equals(Buffer.from([0x50, 0x4b, 0x03, 0x04])), 'Epic 9 trace is not a ZIP file.');
  const traceIndex = trace.toString('latin1');
  assertClaim(/(?:^|[^a-z])\d+-trace\.trace(?:[^a-z]|$)/iu.test(traceIndex), 'Epic 9 trace ZIP is missing a Playwright trace stream.');
  assertClaim(/(?:^|[^a-z])\d+-trace\.network(?:[^a-z]|$)/iu.test(traceIndex), 'Epic 9 trace ZIP is missing a Playwright network stream.');

  return { artifactRoot, candidateCommit: metadata.candidateCommit, baseUrl: metadata.baseUrl };
};

const parseArguments = (arguments_) => {
  let artifactRoot = 'artifacts/epic-9';
  let allowDirty = false;
  let expectedCandidate;
  let sawArtifactRoot = false;
  for (let index = 0; index < arguments_.length; index += 1) {
    const argument = arguments_[index];
    if (argument === '--allow-dirty') {
      allowDirty = true;
    } else if (argument === '--candidate') {
      expectedCandidate = arguments_[index + 1];
      assertClaim(expectedCandidate !== undefined, '--candidate requires a value.');
      index += 1;
    } else if (argument.startsWith('--')) {
      throw new Error(`Unknown option: ${argument}`);
    } else {
      assertClaim(!sawArtifactRoot, 'Only one artifact root may be supplied.');
      artifactRoot = argument;
      sawArtifactRoot = true;
    }
  }
  return { artifactRoot, allowDirty, expectedCandidate };
};

const isMain = process.argv[1] && pathToFileURL(resolve(process.argv[1])).href === import.meta.url;
if (isMain) {
  try {
    const { artifactRoot, allowDirty, expectedCandidate } = parseArguments(process.argv.slice(2));
    const result = await validateEpic9Artifacts(artifactRoot, { allowDirty, expectedCandidate });
    console.log(`Validated Epic 9 proof artifacts at ${result.artifactRoot}`);
  } catch (error) {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  }
}
