import { fileURLToPath } from 'node:url';

import { expect, test } from '../fixtures/index.js';
import { spawnOwnedProcess, stopOwnedProcess, waitForOwnedServer, type OwnedProcess } from '../helpers/owned-process.js';

const DEVELOPMENT_BASE_URL = process.env.FC_E2E_STORY_6_4_DEVELOPMENT_BASE_URL ?? 'http://127.0.0.1:5084';
const NON_DEVELOPMENT_BASE_URL = process.env.FC_E2E_STORY_6_4_NON_DEVELOPMENT_BASE_URL ?? 'http://127.0.0.1:5085';
const SERVER_READY_TIMEOUT_MS = 120_000;
const COUNTER_PROJECT_DIRECTORY = new URL('../../../samples/Counter/Counter.Web/', import.meta.url);
const COUNTER_DEBUG_ASSEMBLY = fileURLToPath(new URL('bin/Debug/net10.0/Counter.Web.dll', COUNTER_PROJECT_DIRECTORY));

const ownedProcesses: OwnedProcess[] = [];

test.describe('Story 6.4: override accessibility safety diagnostics', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'Dedicated host coverage runs once in Chromium.');
  test.describe.configure({ mode: 'serial' });

  // Build once outside the assertion tests. Register ownership and cleanup before any startup await.
  test.afterAll(async () => {
    await Promise.all(ownedProcesses.map(stopOwnedProcess));
  });

  test.beforeAll(async () => {
    test.setTimeout(SERVER_READY_TIMEOUT_MS);
    const deadline = Date.now() + SERVER_READY_TIMEOUT_MS - 10_000;
    if (!process.env.FC_E2E_STORY_6_4_DEVELOPMENT_BASE_URL || !process.env.FC_E2E_STORY_6_4_NON_DEVELOPMENT_BASE_URL) {
      await buildDebugCounter(deadline);
    }
    await startCounterHost(DEVELOPMENT_BASE_URL, 'Development', deadline);
    await startCounterHost(NON_DEVELOPMENT_BASE_URL, 'Test', deadline);
  });

  test('development debug host renders sanitized contract mismatch diagnostics through the panel', async ({ page }) => {
    await page.goto(new URL('/counter', DEVELOPMENT_BASE_URL).href);
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    const panel = page.locator('[data-fc-diagnostic="HFC1041"]');
    await expect(panel).toBeVisible();
    await expect(panel).toHaveAttribute('role', 'alert');
    await expect(panel).toHaveAttribute('data-fc-customization-level', 'Level3');
    await expect(panel).toHaveAttribute('data-fc-projection', 'Counter.Domain.CounterProjection');
    await expect(panel).toHaveAttribute('data-fc-component', 'Counter.Web.Components.Slots.CounterCountSlot');
    await expect(panel).toHaveAttribute('data-fc-role', '<any>');
    await expect(panel).toHaveAttribute('data-fc-field', 'Count');
    await expect(panel).toContainText('A customization contract mismatch was rejected during startup hydration.');
    await expect(panel).toContainText('installed contract version 1.0.0');
    await expect(panel).toContainText('declared contract version 2.0.0');
    await expect(panel).toContainText('MajorMismatch');
    await expect(panel).toContainText('The descriptor is skipped, so the generated framework path remains available.');
    await expect(panel.getByRole('link', { name: 'Diagnostic documentation' })).toHaveAttribute(
      'href',
      'https://hexalith.github.io/FrontComposer/diagnostics/HFC1041',
    );
  });

  test('non-development debug host suppresses the mismatch panel even when a rejection is recorded', async ({ page }) => {
    await page.goto(new URL('/counter', NON_DEVELOPMENT_BASE_URL).href);
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
    await expect(page.locator('[data-fc-diagnostic="HFC1041"]')).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Diagnostic documentation' })).toHaveCount(0);
  });
});

const buildDebugCounter = async (deadline: number): Promise<void> => {
  const build = spawnOwnedProcess(
    'dotnet',
    ['build', 'Counter.Web.csproj', '--configuration', 'Debug', '--disable-build-servers', '-m:1'],
    { cwd: COUNTER_PROJECT_DIRECTORY, env: process.env },
    deadline - Date.now(),
  );
  ownedProcesses.push(build);
  const exitCode = await new Promise<number | null>((resolve, reject) => {
    build.child.once('error', reject);
    build.child.once('close', resolve);
  });
  if (build.timedOut || exitCode !== 0) {
    throw new Error(`Counter Story 6.4 Debug build failed with code ${exitCode}, timed out: ${build.timedOut}.\n${build.output}`);
  }
};

const startCounterHost = async (
  baseUrl: string,
  environmentName: 'Development' | 'Test',
  deadline: number,
): Promise<void> => {
  if (process.env.FC_E2E_STORY_6_4_DEVELOPMENT_BASE_URL && environmentName === 'Development') return;
  if (process.env.FC_E2E_STORY_6_4_NON_DEVELOPMENT_BASE_URL && environmentName === 'Test') return;
  const host = spawnOwnedProcess('dotnet', [COUNTER_DEBUG_ASSEMBLY, '--urls', baseUrl], {
    cwd: COUNTER_PROJECT_DIRECTORY,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: environmentName,
      DOTNET_ENVIRONMENT: environmentName,
      Hexalith__FrontComposer__E2E__SeedContractMismatch: 'true',
      Hexalith__FrontComposer__Specimens__Enabled: '',
      Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs: '200',
    },
  });
  ownedProcesses.push(host);
  await waitForOwnedServer(host, baseUrl, deadline);
};
