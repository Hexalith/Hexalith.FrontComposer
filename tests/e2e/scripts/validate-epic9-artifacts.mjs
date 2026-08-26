import { readFile, readdir, stat } from 'node:fs/promises';
import { dirname, isAbsolute, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const VIEW_KEY = 'Counter:Counter.Domain.CounterProjection';
const INDICATOR_COPY = 'New item. It may not match current filters yet.';
const INDICATOR_ARIA_LABEL = 'New item added outside current filters';
const SHA_PATTERN = /^[0-9a-f]{40}$/u;
const SENSITIVE_KEY_PATTERN = /authorization|cookie|password|secret|token|headers|dashboard.*url/iu;
const REDACTED = '[REDACTED]';
const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');

const walk = async (directory) => {
  const entries = await readdir(directory, { withFileTypes: true });
  const nested = await Promise.all(entries.map(async (entry) => {
    const path = resolve(directory, entry.name);
    return entry.isDirectory() ? walk(path) : [path];
  }));
  return nested.flat();
};

const assertClaim = (condition, message) => {
  if (!condition) {
    throw new Error(message);
  }
};

const requireFile = async (artifactRoot, relativePath) => {
  const path = resolve(artifactRoot, relativePath);
  const file = await stat(path);
  assertClaim(file.isFile() && file.size > 0, `Epic 9 artifact is empty or not a file: ${relativePath}`);
  return path;
};

const requireUniqueSuffix = (allFiles, suffix) => {
  const matches = allFiles.filter((path) => path.endsWith(suffix));
  assertClaim(matches.length === 1, `Epic 9 artifact must contain exactly one ${suffix}; found ${matches.length}.`);
  return matches[0];
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
  if (value === null || typeof value !== 'object') {
    return;
  }
  for (const [key, nested] of Object.entries(value)) {
    if (SENSITIVE_KEY_PATTERN.test(key)) {
      assertClaim(nested === REDACTED, `${artifactLabel} contains an unredacted sensitive value at ${path}.${key}.`);
    } else {
      assertSensitiveValuesRedacted(nested, artifactLabel, `${path}.${key}`);
    }
  }
};

const attributeValue = (markup, element, attribute) => {
  const elementMatch = markup.match(new RegExp(`<${element}\\b([^>]*)>`, 'u'));
  assertClaim(elementMatch, `${element} is missing from JUnit evidence.`);
  const attributeMatch = elementMatch[1].match(new RegExp(`\\b${attribute}="([^"]+)"`, 'u'));
  assertClaim(attributeMatch, `${element}.${attribute} is missing from JUnit evidence.`);
  return attributeMatch[1];
};

export const validateEpic9Artifacts = async (
  artifactRootInput,
  { allowDirty = false, expectedCandidate } = {},
) => {
  const artifactRoot = isAbsolute(artifactRootInput)
    ? resolve(artifactRootInput)
    : resolve(REPOSITORY_ROOT, artifactRootInput);
  const metadataPath = await requireFile(artifactRoot, 'runtime-metadata.json');
  const preflightPath = await requireFile(artifactRoot, 'apphost-preflight.json');
  const postflightPath = await requireFile(artifactRoot, 'apphost-postflight.json');
  const apphostStartPath = await requireFile(artifactRoot, 'apphost-start.json');
  const describePath = await requireFile(artifactRoot, 'counter-web-describe.json');
  const logsPath = await requireFile(artifactRoot, 'counter-web-logs.redacted.json');
  const junitPath = await requireFile(artifactRoot, 'junit.xml');
  const htmlReportPath = await requireFile(artifactRoot, 'playwright-report/index.html');

  const allFiles = await walk(artifactRoot);
  const evidencePath = requireUniqueSuffix(allFiles, 'epic-9-command-evidence.json');
  const screenshotPath = requireUniqueSuffix(allFiles, 'epic-9-live-acceptance.png');
  const tracePath = requireUniqueSuffix(allFiles, 'trace.zip');

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
  requireNonEmptyString(metadata.startedAtUtc, 'runtime-metadata.json.startedAtUtc');
  assertClaim(Number.isFinite(Date.parse(metadata.startedAtUtc)), 'runtime-metadata.json.startedAtUtc must be an ISO timestamp.');
  assertClaim(
    ['isolated-build', 'isolated-no-build-after-serialized-build'].includes(metadata.startMode),
    'runtime-metadata.json.startMode is not a supported isolated start mode.',
  );
  for (const tool of ['aspire', 'dotnet', 'node']) {
    requireNonEmptyString(metadata.toolVersions?.[tool], `runtime-metadata.json.toolVersions.${tool}`);
  }
  assertClaim(Array.isArray(metadata.commands) && metadata.commands.length >= 7, 'runtime-metadata.json.commands is incomplete.');

  const preflight = await readJson(preflightPath, 'apphost-preflight.json');
  assertClaim(Array.isArray(preflight), 'apphost-preflight.json must contain the Aspire process list.');
  assertClaim(!JSON.stringify(preflight).includes('Hexalith.FrontComposer.AppHost'), 'AppHost preflight shows an existing FrontComposer run.');
  const postflight = await readJson(postflightPath, 'apphost-postflight.json');
  assertClaim(Array.isArray(postflight), 'apphost-postflight.json must contain the Aspire process list.');
  assertClaim(!JSON.stringify(postflight).includes('Hexalith.FrontComposer.AppHost'), 'AppHost postflight shows the proof AppHost is still running.');
  const apphostStart = await readJson(apphostStartPath, 'apphost-start.json');
  assertClaim(Number.isInteger(apphostStart.appHostPid) && apphostStart.appHostPid > 0, 'apphost-start.json.appHostPid must be a positive integer.');
  assertClaim(
    typeof apphostStart.appHostPath === 'string'
      && apphostStart.appHostPath.endsWith('/src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj'),
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
  ]) {
    assertSensitiveValuesRedacted(artifact, label);
  }

  const describedResource = describe.resources?.find?.((resource) => resource.name === metadata.counterWebResource);
  assertClaim(describedResource, 'counter-web-describe.json does not contain the recorded counter-web resource.');
  const describedUrls = describedResource.urls?.map?.((entry) => entry.url) ?? [];
  assertClaim(describedUrls.includes(metadata.baseUrl), 'counter-web-describe.json does not contain the recorded endpoint.');

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

  assertClaim(Array.isArray(evidence.dispatchedCommands), 'Browser evidence dispatchedCommands must be an array.');
  assertClaim(evidence.dispatchedCommands.length === 4, 'Browser evidence must record the create and three update dispatches.');
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
  const requiredTrueClaims = [
    'gridWasRenderedBeforeDispatch',
    'exactKeyWasAbsentBeforeDispatch',
    'exactKeyMatchedAllDispatches',
    'tenantScopePresent',
    'userScopePresent',
    'firstWinsIndicatorCopyRetained',
    'localizedAnnouncementsNonEmpty',
    'createIndicatorDismissedByMaterialization',
    'overlappingUpdateIndicatorDismissedByMaterialization',
    'laterUpdateIndicatorDismissedByMaterialization',
    'indicatorDismissedByMaterialization',
  ];
  for (const claim of requiredTrueClaims) {
    assertClaim(observed?.[claim] === true, `Browser evidence observed.${claim} must equal true.`);
  }
  assertClaim(observed.firstWinsVisibleIndicatorCount === 1, 'Browser evidence first-wins visible count must equal 1.');
  assertClaim(observed.indicatorRole === 'status', 'Browser evidence indicator role must equal status.');
  assertClaim(observed.indicatorAriaLive === 'polite', 'Browser evidence indicator aria-live must equal polite.');
  assertClaim(observed.indicatorAriaLabel === INDICATOR_ARIA_LABEL, 'Browser evidence indicator aria-label is not localized copy.');
  assertClaim(observed.createIndicatorCopy === INDICATOR_COPY, 'Browser evidence create announcement is not localized copy.');
  assertClaim(observed.firstWinsIndicatorCopy === INDICATOR_COPY, 'Browser evidence first-wins announcement is not localized copy.');
  assertClaim(observed.materializedCountAfterCreate === 41, 'Browser evidence create count must equal 41.');
  assertClaim(observed.materializedCountAfterOverlappingUpdates === 44, 'Browser evidence overlapping-update count must equal 44.');
  assertClaim(observed.materializedCountAfterLaterUpdate === 52, 'Browser evidence later-update count must equal 52.');

  const junit = await readFile(junitPath, 'utf8');
  assertClaim(attributeValue(junit, 'testsuites', 'tests') === '1', 'JUnit evidence must record exactly one test.');
  assertClaim(attributeValue(junit, 'testsuites', 'failures') === '0', 'JUnit evidence records a failure.');
  assertClaim(attributeValue(junit, 'testsuites', 'errors') === '0', 'JUnit evidence records an error.');
  const htmlReport = await readFile(htmlReportPath, 'utf8');
  assertClaim(/^\s*<!DOCTYPE html>/iu.test(htmlReport), 'Playwright HTML report is not an HTML document.');
  const screenshot = await readFile(screenshotPath);
  assertClaim(screenshot.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])), 'Epic 9 screenshot is not a PNG file.');
  const trace = await readFile(tracePath);
  assertClaim(trace.subarray(0, 2).equals(Buffer.from('PK')), 'Epic 9 trace is not a ZIP file.');

  const failedStartPath = allFiles.find((path) => path.endsWith('apphost-start.failed.json'));
  if (failedStartPath) {
    const failedStart = await readFile(failedStartPath, 'utf8');
    assertClaim(!/login\?t=(?!\[REDACTED\])[^\s"&]+/iu.test(failedStart), 'Failed AppHost startup artifact contains a dashboard login token.');
  }

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

const isMain = process.argv[1]
  && pathToFileURL(resolve(process.argv[1])).href === import.meta.url;
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
