import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdtemp, mkdir, readFile, readdir, rm, symlink, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, relative, resolve, sep } from 'node:path';
import test from 'node:test';

import { validateEpic9Artifacts } from './validate-epic9-artifacts.mjs';

const CANDIDATE = '1234567890abcdef1234567890abcdef12345678';
const BASE_URL = 'https://localhost:43210';
const EXACT_KEY = 'counter-e9-1234567890';
const APPHOST_RELATIVE = 'src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj';
const INITIAL_START_COMMAND = `aspire start --apphost ${APPHOST_RELATIVE} --isolated --non-interactive --format Json --nologo`;
const FALLBACK_BUILD_COMMAND = `dotnet build ${APPHOST_RELATIVE} --configuration Debug -m:1 -p:BuildProjectReferences=false -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false`;
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
const DIRECT_COMMANDS = [INITIAL_START_COMMAND, ...COMMON_COMMANDS, VALIDATE_FINAL_COMMAND];
const FALLBACK_COMMANDS = [INITIAL_START_COMMAND, FALLBACK_BUILD_COMMAND, FALLBACK_START_COMMAND, ...COMMON_COMMANDS, VALIDATE_FINAL_COMMAND];

const pngFixture = (width = 1280, height = 720) => {
  const png = Buffer.alloc(24);
  Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]).copy(png, 0);
  png.writeUInt32BE(13, 8);
  Buffer.from('IHDR').copy(png, 12);
  png.writeUInt32BE(width, 16);
  png.writeUInt32BE(height, 20);
  return png;
};

const validFixture = () => ({
  metadata: {
    schemaVersion: 1,
    story: '9.8',
    candidateCommit: CANDIDATE,
    workingTreeDirty: false,
    evidenceMode: 'final',
    baseUrl: BASE_URL,
    counterWebResource: 'counter-web-proof',
    counterWebLogResource: 'counter-web',
    startedAtUtc: '2026-08-27T12:00:00Z',
    startMode: 'isolated-build',
    toolVersions: { aspire: '13.4.6', dotnet: '10.0.302', node: 'v24.0.0' },
    commands: [...DIRECT_COMMANDS],
  },
  preflight: [],
  postflight: [],
  apphostStart: {
    appHostPath: `/repo/${APPHOST_RELATIVE}`,
    appHostPid: 1234,
    dashboardUrl: '[REDACTED]',
  },
  describe: {
    resources: [{ name: 'counter-web-proof', urls: [{ name: 'https', url: BASE_URL }] }],
  },
  logs: {
    logs: [
      { resourceName: 'counter-web', content: 'Now listening on: https://localhost:43210', isError: false },
    ],
  },
  evidence: {
    schemaVersion: 1,
    story: '9.8',
    candidateCommit: CANDIDATE,
    baseUrl: BASE_URL,
    tenantScope: 'counter-demo',
    userScope: 'demo-user',
    uiLanguage: 'en',
    viewKey: 'Counter:Counter.Domain.CounterProjection',
    exactTargetKey: EXACT_KEY,
    dispatchedCommands: [
      { commandType: 'Counter.Domain.CreateCounterCommand', counterId: EXACT_KEY, initialValue: '[REDACTED]' },
      { commandType: 'Counter.Domain.UpdateCounterCommand', counterId: EXACT_KEY, amount: '[REDACTED]' },
      { commandType: 'Counter.Domain.UpdateCounterCommand', counterId: EXACT_KEY, amount: '[REDACTED]' },
      { commandType: 'Counter.Domain.UpdateCounterCommand', counterId: EXACT_KEY, amount: '[REDACTED]' },
    ],
    observed: {
      gridWasRenderedBeforeDispatch: true,
      exactKeyCountBeforeDispatch: 0,
      exactKeyMatchedDispatchCount: 4,
      tenantScopeLength: 'counter-demo'.length,
      userScopeLength: 'demo-user'.length,
      createIndicatorVisibleCount: 1,
      indicatorRole: 'status',
      indicatorAriaLive: 'polite',
      indicatorAriaLabel: 'New item added outside current filters',
      createIndicatorCopy: 'New item. It may not match current filters yet.',
      catchUpCaptured: 1,
      catchUpPublished: 1,
      catchUpReceived: 1,
      materializedCountAfterCreate: 41,
      createIndicatorCountAfterMaterialization: 0,
      overlapIndicatorCountBeforeSecondDispatch: 1,
      materializedCountBeforeSecondDispatch: 41,
      overlapIndicatorCountAfterSecondDispatch: 1,
      overlapIndicatorElementRetained: true,
      overlapIndicatorCopyBeforeSecondDispatch: 'New item. It may not match current filters yet.',
      overlapIndicatorCopyAfterSecondDispatch: 'New item. It may not match current filters yet.',
      materializedCountAfterOverlappingUpdates: 44,
      overlappingIndicatorCountAfterMaterialization: 0,
      laterUpdateIndicatorVisibleCount: 1,
      materializedCountAfterLaterUpdate: 52,
      laterUpdateIndicatorCountAfterMaterialization: 0,
    },
  },
  failedStart: undefined,
  serializedBuild: undefined,
  junit: '<testsuites tests="1" failures="0" skipped="0" errors="0"><testsuite name="epic-9-fresh-row-acceptance.spec.ts" hostname="chromium" tests="1" failures="0" skipped="0" errors="0"><testcase name="Epic 9 composed and live acceptance › generated create and update converge through the indicator into an already-rendered grid" classname="epic-9-fresh-row-acceptance.spec.ts"></testcase></testsuite></testsuites>\n',
  html: '<!DOCTYPE html><html><head><title>Playwright Test Report</title></head><body><div id="root"></div><template id="playwrightReportBase64">data</template></body></html>\n',
  screenshot: pngFixture(),
  trace: Buffer.from('PK\u0003\u0004fixture/0-trace.trace\u0000fixture/0-trace.network\u0000'),
});

const listFiles = async (root, directory = root) => {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) files.push(...await listFiles(root, path));
    else if (entry.isFile()) files.push(relative(root, path).split(sep).join('/'));
  }
  return files;
};

const writeManifest = async (root) => {
  const files = (await listFiles(root)).filter((path) => path !== 'checksums.sha256').sort();
  const lines = [];
  for (const path of files) {
    const digest = createHash('sha256').update(await readFile(join(root, path))).digest('hex');
    lines.push(`${digest}  ${path}`);
  }
  await writeFile(join(root, 'checksums.sha256'), `${lines.join('\n')}\n`);
};

const writeFixture = async (fixture) => {
  const root = await mkdtemp(join(tmpdir(), 'fc-epic9-validator-'));
  const resultRoot = join(root, 'playwright-results', 'epic-9-proof');
  await mkdir(resultRoot, { recursive: true });
  await mkdir(join(root, 'playwright-report'), { recursive: true });
  await writeFile(join(root, 'runtime-metadata.json'), `${JSON.stringify(fixture.metadata)}\n`);
  await writeFile(join(root, 'apphost-preflight.json'), `${JSON.stringify(fixture.preflight)}\n`);
  await writeFile(join(root, 'apphost-postflight.json'), `${JSON.stringify(fixture.postflight)}\n`);
  await writeFile(join(root, 'apphost-start.json'), `${JSON.stringify(fixture.apphostStart)}\n`);
  await writeFile(join(root, 'counter-web-wait.log'), 'counter-web is up\n');
  await writeFile(join(root, 'counter-web-describe.json'), `${JSON.stringify(fixture.describe)}\n`);
  await writeFile(join(root, 'counter-web-logs.redacted.json'), `${JSON.stringify(fixture.logs)}\n`);
  await writeFile(join(root, 'junit.xml'), fixture.junit);
  await writeFile(join(root, 'playwright-report', 'index.html'), fixture.html);
  await writeFile(join(resultRoot, 'epic-9-command-evidence.json'), `${JSON.stringify(fixture.evidence)}\n`);
  await writeFile(join(resultRoot, 'epic-9-live-acceptance.png'), fixture.screenshot);
  await writeFile(join(resultRoot, 'trace.zip'), fixture.trace);
  if (fixture.failedStart !== undefined) {
    await writeFile(join(root, 'apphost-start.failed.json'), fixture.failedStart);
  }
  if (fixture.serializedBuild !== undefined) {
    await writeFile(join(root, 'apphost-serialized-build.log'), fixture.serializedBuild);
  }
  await writeManifest(root);
  return root;
};

const createFixture = async (t, mutate = () => {}) => {
  const fixture = validFixture();
  mutate(fixture);
  const root = await writeFixture(fixture);
  t.after(() => rm(root, { recursive: true, force: true }));
  return root;
};

const runFixture = async (t, mutate = () => {}, options = { expectedCandidate: CANDIDATE }) => {
  const root = await createFixture(t, mutate);
  return validateEpic9Artifacts(root, options);
};

const useFallback = (fixture) => {
  fixture.metadata.startMode = 'isolated-no-build-after-serialized-build';
  fixture.metadata.commands = [...FALLBACK_COMMANDS];
  fixture.failedStart = 'initial start failed with login?t=[REDACTED]\n';
  fixture.serializedBuild = 'serialized build succeeded\n';
};

test('Epic 9 artifact validator accepts correlated direct, fallback, and development bundles', async (t) => {
  const finalResult = await runFixture(t);
  assert.equal(finalResult.candidateCommit, CANDIDATE);
  assert.equal(finalResult.baseUrl, BASE_URL);

  const fallbackResult = await runFixture(t, useFallback);
  assert.equal(fallbackResult.candidateCommit, CANDIDATE);

  const developmentResult = await runFixture(
    t,
    (fixture) => {
      fixture.metadata.workingTreeDirty = true;
      fixture.metadata.evidenceMode = 'development';
      fixture.metadata.commands = [...DIRECT_COMMANDS.slice(0, -1), VALIDATE_DEVELOPMENT_COMMAND];
    },
    { allowDirty: true, expectedCandidate: CANDIDATE },
  );
  assert.equal(developmentResult.candidateCommit, CANDIDATE);
});

test('Epic 9 artifact validator rejects semantic contradictions and weak evidence', async (t) => {
  const cases = [
    ['dirty final evidence', (fixture) => { fixture.metadata.workingTreeDirty = true; }, /dirty working tree/u],
    ['wrong evidence mode', (fixture) => { fixture.metadata.evidenceMode = 'development'; }, /evidenceMode/u],
    ['non-strict UTC timestamp', (fixture) => { fixture.metadata.startedAtUtc = '2026-08-27T12:00:00.000Z'; }, /strict UTC/u],
    ['invalid UTC timestamp', (fixture) => { fixture.metadata.startedAtUtc = '2026-99-27T12:00:00Z'; }, /valid UTC instant/u],
    ['extra command', (fixture) => { fixture.metadata.commands.push('extra'); }, /exact expected command/u],
    ['reordered command', (fixture) => { fixture.metadata.commands.reverse(); }, /exact expected command/u],
    ['fallback artifacts in direct mode', (fixture) => { fixture.failedStart = 'failed\n'; }, /fallback-only/u],
    ['fallback missing failed start', (fixture) => { useFallback(fixture); fixture.failedStart = undefined; }, /requires non-empty apphost-start.failed/u],
    ['fallback missing serialized build', (fixture) => { useFallback(fixture); fixture.serializedBuild = undefined; }, /requires non-empty apphost-serialized-build/u],
    ['stale browser candidate', (fixture) => { fixture.evidence.candidateCommit = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'; }, /candidate/u],
    ['contradictory browser endpoint', (fixture) => { fixture.evidence.baseUrl = 'https://localhost:1'; }, /endpoint/u],
    ['contradictory describe endpoint', (fixture) => { fixture.describe.resources[0].urls[0].url = 'https://localhost:2'; }, /recorded endpoint/u],
    ['empty logs', (fixture) => { fixture.logs.logs = []; }, /logs must be non-empty/u],
    ['wrong log resource', (fixture) => { fixture.logs.logs[0].resourceName = 'other'; }, /recorded counter resource/u],
    ['no listening event', (fixture) => { fixture.logs.logs[0].content = 'Application started'; }, /Now listening on/u],
    ['existing FrontComposer AppHost', (fixture) => { fixture.preflight = [{ appHostPath: 'Hexalith.FrontComposer.AppHost' }]; }, /existing FrontComposer/u],
    ['unclean AppHost postflight', (fixture) => { fixture.postflight = [{ appHostPath: 'Hexalith.FrontComposer.AppHost' }]; }, /proof AppHost is still running/u],
    ['unredacted structured secret', (fixture) => { fixture.logs.authorization = 'Bearer secret'; }, /unredacted sensitive/u],
    ['unredacted command payload', (fixture) => { fixture.evidence.dispatchedCommands[1].amount = '2'; }, /payload is not redacted/u],
    ['unredacted failed-start token', (fixture) => { useFallback(fixture); fixture.failedStart = 'https://localhost/login?t=secret'; }, /dashboard login token/u],
    ['unredacted build token', (fixture) => { useFallback(fixture); fixture.serializedBuild = 'https://localhost/login?t=secret'; }, /dashboard login token/u],
    ['wrong exact target key', (fixture) => { fixture.evidence.exactTargetKey = 'seeded-key'; }, /exactTargetKey/u],
    ['wrong command target', (fixture) => { fixture.evidence.dispatchedCommands[2].counterId = 'other-key'; }, /exact key/u],
    ['root JUnit skipped', (fixture) => { fixture.junit = fixture.junit.replace('skipped="0"', 'skipped="1"'); }, /testsuites must report skipped=0/u],
    ['suite JUnit failures', (fixture) => { fixture.junit = fixture.junit.replace('<testsuite name=', '<testsuite failures="1" name='); }, /testsuite must report failures=0/u],
    ['wrong JUnit project', (fixture) => { fixture.junit = fixture.junit.replace('hostname="chromium"', 'hostname="firefox"'); }, /hostname\/project/u],
    ['wrong JUnit classname', (fixture) => { fixture.junit = fixture.junit.replace('classname="epic-9-fresh-row-acceptance.spec.ts"', 'classname="other.spec.ts"'); }, /classname/u],
    ['wrong JUnit test title', (fixture) => { fixture.junit = fixture.junit.replace('Epic 9 composed and live acceptance', 'Other suite'); }, /composed\/live test title/u],
    ['extra JUnit testcase', (fixture) => { fixture.junit = fixture.junit.replace('</testsuite>', '<testcase name="extra" classname="extra"></testcase></testsuite>'); }, /exactly one testcase/u],
    ['weak HTML title', (fixture) => { fixture.html = fixture.html.replace('Playwright Test Report', 'Report'); }, /report title/u],
    ['missing HTML payload', (fixture) => { fixture.html = fixture.html.replace('playwrightReportBase64', 'other'); }, /payload markers/u],
    ['zero-width PNG', (fixture) => { fixture.screenshot = pngFixture(0, 720); }, /dimensions/u],
    ['invalid PNG', (fixture) => { fixture.screenshot = Buffer.from('not-png'); }, /not a PNG/u],
    ['missing trace stream', (fixture) => { fixture.trace = Buffer.from('PK\u0003\u0004fixture/0-trace.network'); }, /trace stream/u],
    ['missing network stream', (fixture) => { fixture.trace = Buffer.from('PK\u0003\u0004fixture/0-trace.trace'); }, /network stream/u],
  ];
  for (const [name, mutate, errorPattern] of cases) {
    await t.test(name, async (subtest) => {
      await assert.rejects(runFixture(subtest, mutate), errorPattern);
    });
  }
});

test('Epic 9 artifact validator rejects invalid measured browser observations', async (t) => {
  const cases = [
    ['gridWasRenderedBeforeDispatch', false],
    ['exactKeyCountBeforeDispatch', 1],
    ['exactKeyMatchedDispatchCount', 3],
    ['tenantScopeLength', 0],
    ['userScopeLength', 0],
    ['createIndicatorVisibleCount', 0],
    ['indicatorRole', 'alert'],
    ['indicatorAriaLive', 'assertive'],
    ['indicatorAriaLabel', ''],
    ['createIndicatorCopy', ''],
    ['catchUpCaptured', 0],
    ['catchUpPublished', 0],
    ['catchUpReceived', 0],
    ['materializedCountAfterCreate', 40],
    ['createIndicatorCountAfterMaterialization', 1],
    ['overlapIndicatorCountBeforeSecondDispatch', 0],
    ['materializedCountBeforeSecondDispatch', 42],
    ['overlapIndicatorCountAfterSecondDispatch', 0],
    ['overlapIndicatorElementRetained', false],
    ['overlapIndicatorCopyBeforeSecondDispatch', ''],
    ['overlapIndicatorCopyAfterSecondDispatch', ''],
    ['materializedCountAfterOverlappingUpdates', 43],
    ['overlappingIndicatorCountAfterMaterialization', 1],
    ['laterUpdateIndicatorVisibleCount', 0],
    ['materializedCountAfterLaterUpdate', 51],
    ['laterUpdateIndicatorCountAfterMaterialization', 1],
  ];
  for (const [claim, invalidValue] of cases) {
    await t.test(claim, async (subtest) => {
      await assert.rejects(runFixture(subtest, (fixture) => {
        fixture.evidence.observed[claim] = invalidValue;
      }));
    });
  }
});

test('Epic 9 artifact validator verifies a complete sorted checksum manifest and rejects symlinks', async (t) => {
  await t.test('checksum mismatch', async (subtest) => {
    const root = await createFixture(subtest);
    await writeFile(join(root, 'runtime-metadata.json'), '{}\n');
    await assert.rejects(validateEpic9Artifacts(root), /checksum.*mismatch/iu);
  });

  await t.test('unlisted file', async (subtest) => {
    const root = await createFixture(subtest);
    await writeFile(join(root, 'unlisted.txt'), 'unlisted\n');
    await assert.rejects(validateEpic9Artifacts(root), /list every regular artifact/u);
  });

  await t.test('missing listed file', async (subtest) => {
    const root = await createFixture(subtest);
    await rm(join(root, 'counter-web-wait.log'));
    await assert.rejects(validateEpic9Artifacts(root), /list every regular artifact/u);
  });

  await t.test('unsorted manifest', async (subtest) => {
    const root = await createFixture(subtest);
    const manifestPath = join(root, 'checksums.sha256');
    const lines = (await readFile(manifestPath, 'utf8')).trimEnd().split('\n').reverse();
    await writeFile(manifestPath, `${lines.join('\n')}\n`);
    await assert.rejects(validateEpic9Artifacts(root), /must be sorted/u);
  });

  await t.test('missing manifest entry', async (subtest) => {
    const root = await createFixture(subtest);
    const manifestPath = join(root, 'checksums.sha256');
    const lines = (await readFile(manifestPath, 'utf8')).trimEnd().split('\n');
    await writeFile(manifestPath, `${lines.slice(1).join('\n')}\n`);
    await assert.rejects(validateEpic9Artifacts(root), /list every regular artifact/u);
  });

  await t.test('artifact symlink', async (subtest) => {
    const root = await createFixture(subtest);
    await symlink(join(root, 'runtime-metadata.json'), join(root, 'metadata-link.json'));
    await assert.rejects(validateEpic9Artifacts(root), /must not contain symlinks/u);
  });

  await t.test('reserved artifact name outside root', async (subtest) => {
    const root = await createFixture(subtest);
    const nested = join(root, 'playwright-results', 'nested');
    await mkdir(nested, { recursive: true });
    await writeFile(join(nested, 'apphost-start.failed.json'), 'nested impostor\n');
    await writeManifest(root);
    await assert.rejects(validateEpic9Artifacts(root), /only at the artifact root/u);
  });
});
