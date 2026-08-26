import assert from 'node:assert/strict';
import { mkdtemp, mkdir, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';

import { validateEpic9Artifacts } from './validate-epic9-artifacts.mjs';

const CANDIDATE = '1234567890abcdef1234567890abcdef12345678';
const BASE_URL = 'https://localhost:43210';
const EXACT_KEY = 'counter-e9-1234567890';

const validFixture = () => ({
  metadata: {
    schemaVersion: 1,
    story: '9.8',
    candidateCommit: CANDIDATE,
    workingTreeDirty: false,
    evidenceMode: 'final',
    baseUrl: BASE_URL,
    counterWebResource: 'counter-web-proof',
    startedAtUtc: '2026-08-27T12:00:00Z',
    startMode: 'isolated-build',
    toolVersions: { aspire: '13.4.6', dotnet: '10.0.302', node: 'v24.0.0' },
    commands: ['start', 'wait', 'describe', 'test', 'logs', 'stop', 'validate'],
  },
  preflight: [],
  postflight: [],
  apphostStart: {
    appHostPath: '/repo/src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj',
    appHostPid: 1234,
    dashboardUrl: '[REDACTED]',
  },
  describe: {
    resources: [{ name: 'counter-web-proof', urls: [{ name: 'https', url: BASE_URL }] }],
  },
  logs: { logs: [] },
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
      exactKeyWasAbsentBeforeDispatch: true,
      exactKeyMatchedAllDispatches: true,
      tenantScopePresent: true,
      userScopePresent: true,
      firstWinsVisibleIndicatorCount: 1,
      firstWinsIndicatorCopyRetained: true,
      indicatorRole: 'status',
      indicatorAriaLive: 'polite',
      indicatorAriaLabel: 'New item added outside current filters',
      createIndicatorCopy: 'New item. It may not match current filters yet.',
      firstWinsIndicatorCopy: 'New item. It may not match current filters yet.',
      localizedAnnouncementsNonEmpty: true,
      materializedCountAfterCreate: 41,
      materializedCountAfterOverlappingUpdates: 44,
      materializedCountAfterLaterUpdate: 52,
      createIndicatorDismissedByMaterialization: true,
      overlappingUpdateIndicatorDismissedByMaterialization: true,
      laterUpdateIndicatorDismissedByMaterialization: true,
      indicatorDismissedByMaterialization: true,
    },
  },
  failedStart: undefined,
  junit: '<testsuites tests="1" failures="0" skipped="0" errors="0"><testsuite /></testsuites>\n',
  html: '\n\n<!DOCTYPE html><html><body>passed</body></html>\n',
  screenshot: Buffer.from([137, 80, 78, 71, 13, 10, 26, 10, 1]),
  trace: Buffer.from('PK\u0003\u0004fixture'),
});

const writeFixture = async (fixture) => {
  const root = await mkdtemp(join(tmpdir(), 'fc-epic9-validator-'));
  const resultRoot = join(root, 'playwright-results', 'epic-9-proof');
  await mkdir(resultRoot, { recursive: true });
  await mkdir(join(root, 'playwright-report'), { recursive: true });
  await writeFile(join(root, 'runtime-metadata.json'), `${JSON.stringify(fixture.metadata)}\n`);
  await writeFile(join(root, 'apphost-preflight.json'), `${JSON.stringify(fixture.preflight)}\n`);
  await writeFile(join(root, 'apphost-postflight.json'), `${JSON.stringify(fixture.postflight)}\n`);
  await writeFile(join(root, 'apphost-start.json'), `${JSON.stringify(fixture.apphostStart)}\n`);
  await writeFile(join(root, 'counter-web-describe.json'), `${JSON.stringify(fixture.describe)}\n`);
  await writeFile(join(root, 'counter-web-logs.redacted.json'), `${JSON.stringify(fixture.logs)}\n`);
  await writeFile(join(root, 'junit.xml'), fixture.junit);
  await writeFile(join(root, 'playwright-report', 'index.html'), fixture.html);
  await writeFile(join(resultRoot, 'epic-9-command-evidence.json'), `${JSON.stringify(fixture.evidence)}\n`);
  if (fixture.screenshot !== undefined) {
    await writeFile(join(resultRoot, 'epic-9-live-acceptance.png'), fixture.screenshot);
  }
  await writeFile(join(resultRoot, 'trace.zip'), fixture.trace);
  if (fixture.failedStart !== undefined) {
    await writeFile(join(root, 'apphost-start.failed.json'), fixture.failedStart);
  }
  return root;
};

const runFixture = async (t, mutate = () => {}, options = { expectedCandidate: CANDIDATE }) => {
  const fixture = validFixture();
  mutate(fixture);
  const root = await writeFixture(fixture);
  t.after(() => rm(root, { recursive: true, force: true }));
  return validateEpic9Artifacts(root, options);
};

test('Epic 9 artifact validator accepts complete correlated final and development bundles', async (t) => {
  const finalResult = await runFixture(t);
  assert.equal(finalResult.candidateCommit, CANDIDATE);
  assert.equal(finalResult.baseUrl, BASE_URL);

  const developmentResult = await runFixture(
    t,
    (fixture) => {
      fixture.metadata.workingTreeDirty = true;
      fixture.metadata.evidenceMode = 'development';
    },
    { allowDirty: true, expectedCandidate: CANDIDATE },
  );
  assert.equal(developmentResult.candidateCommit, CANDIDATE);
});

test('Epic 9 artifact validator rejects missing, contradictory, stale, or sensitive evidence', async (t) => {
  const cases = [
    ['dirty final evidence', (fixture) => { fixture.metadata.workingTreeDirty = true; }, /dirty working tree/u],
    ['wrong evidence mode', (fixture) => { fixture.metadata.evidenceMode = 'development'; }, /evidenceMode/u],
    ['stale browser candidate', (fixture) => { fixture.evidence.candidateCommit = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'; }, /candidate/u],
    ['contradictory browser endpoint', (fixture) => { fixture.evidence.baseUrl = 'https://localhost:1'; }, /endpoint/u],
    ['contradictory describe endpoint', (fixture) => { fixture.describe.resources[0].urls[0].url = 'https://localhost:2'; }, /recorded endpoint/u],
    ['existing FrontComposer AppHost', (fixture) => { fixture.preflight = [{ appHostPath: 'Hexalith.FrontComposer.AppHost' }]; }, /existing FrontComposer/u],
    ['unclean AppHost postflight', (fixture) => { fixture.postflight = [{ appHostPath: 'Hexalith.FrontComposer.AppHost' }]; }, /proof AppHost is still running/u],
    ['unredacted structured secret', (fixture) => { fixture.logs.authorization = 'Bearer secret'; }, /unredacted sensitive/u],
    ['unredacted command payload', (fixture) => { fixture.evidence.dispatchedCommands[1].amount = '2'; }, /payload is not redacted/u],
    ['unredacted failed-start token', (fixture) => { fixture.failedStart = 'https://localhost/login?t=secret'; }, /dashboard login token/u],
    ['wrong exact target key', (fixture) => { fixture.evidence.exactTargetKey = 'seeded-key'; }, /exactTargetKey/u],
    ['wrong command target', (fixture) => { fixture.evidence.dispatchedCommands[2].counterId = 'other-key'; }, /exact key/u],
    ['failed JUnit result', (fixture) => { fixture.junit = '<testsuites tests="1" failures="1" errors="0" />'; }, /records a failure/u],
    ['missing screenshot', (fixture) => { fixture.screenshot = undefined; }, /exactly one epic-9-live-acceptance.png/u],
    ['invalid screenshot type', (fixture) => { fixture.screenshot = Buffer.from('not-png'); }, /not a PNG/u],
    ['invalid trace type', (fixture) => { fixture.trace = Buffer.from('not-zip'); }, /not a ZIP/u],
  ];
  for (const [name, mutate, errorPattern] of cases) {
    await t.test(name, async (subtest) => {
      await assert.rejects(runFixture(subtest, mutate), errorPattern);
    });
  }

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
    await t.test(`required observed claim ${claim}`, async (subtest) => {
      await assert.rejects(runFixture(subtest, (fixture) => {
        fixture.evidence.observed[claim] = false;
      }), new RegExp(claim, 'u'));
    });
  }

  const exactObservedClaims = [
    ['firstWinsVisibleIndicatorCount', 2],
    ['indicatorRole', 'alert'],
    ['indicatorAriaLive', 'assertive'],
    ['indicatorAriaLabel', ''],
    ['createIndicatorCopy', ''],
    ['firstWinsIndicatorCopy', ''],
    ['materializedCountAfterCreate', 40],
    ['materializedCountAfterOverlappingUpdates', 43],
    ['materializedCountAfterLaterUpdate', 51],
  ];
  for (const [claim, invalidValue] of exactObservedClaims) {
    await t.test(`exact observed claim ${claim}`, async (subtest) => {
      await assert.rejects(runFixture(subtest, (fixture) => {
        fixture.evidence.observed[claim] = invalidValue;
      }));
    });
  }
});
