import { fileURLToPath } from 'node:url';

import { expect, test } from '../fixtures/index.js';
import { fillFieldByLabel } from '../helpers/fluent-fields.js';
import { spawnOwnedProcess, stopOwnedProcess, waitForOwnedServer, type OwnedProcess } from '../helpers/owned-process.js';
import { CounterPage } from '../page-objects/counter.page.js';

const externalDevelopmentUrl = process.env.FC_E2E_STORY_6_4_DEVELOPMENT_BASE_URL;
const externalNonDevelopmentUrl = process.env.FC_E2E_STORY_6_4_NON_DEVELOPMENT_BASE_URL;
const externalFaultUrl = process.env.FC_E2E_STORY_6_4_FAULT_BASE_URL;
const DEVELOPMENT_BASE_URL = externalDevelopmentUrl ?? 'http://127.0.0.1:5084';
const NON_DEVELOPMENT_BASE_URL = externalNonDevelopmentUrl ?? 'http://127.0.0.1:5085';
const FAULT_BASE_URL = externalFaultUrl ?? 'http://127.0.0.1:5098';
const SERVER_READY_TIMEOUT_MS = 120_000;
const COUNTER_PROJECT_DIRECTORY = new URL('../../../samples/Counter/Counter.Web/', import.meta.url);
const COUNTER_DEBUG_ASSEMBLY = fileURLToPath(new URL('bin/Debug/net11.0/Counter.Web.dll', COUNTER_PROJECT_DIRECTORY));

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
    if (!externalDevelopmentUrl || !externalNonDevelopmentUrl || !externalFaultUrl) {
      await buildDebugCounter(deadline, true);
      await buildDebugCounter(deadline, false);
    }
    await Promise.all([
      externalDevelopmentUrl || startCounterHost(DEVELOPMENT_BASE_URL, 'Development', deadline, false),
      externalNonDevelopmentUrl || startCounterHost(NON_DEVELOPMENT_BASE_URL, 'Test', deadline, false),
      externalFaultUrl || startCounterHost(FAULT_BASE_URL, 'Development', deadline, true),
    ]);
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

    await expect(new CounterPage(page).heading).toBeVisible();
    await expect(page.locator('[data-fc-diagnostic="HFC1041"]')).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Diagnostic documentation' })).toHaveCount(0);
  });

  test('an override render fault leaves the lower-level view and Counter controls usable', async ({ page }) => {
    await page.goto(new URL('/counter', FAULT_BASE_URL).href);
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    const form = page.locator('.fc-command-form[aria-label="Batch Increment command form"]');
    await fillFieldByLabel(form, 'Amount', '2');
    await fillFieldByLabel(form, 'Note', 'Counter replacement fault fixture');
    const submit = form.getByRole('button', { name: 'Batch Increment' });
    await submit.click();
    await expect(page.locator('[data-testid="fc-lifecycle-batch-increment"]')).toHaveAttribute(
      'data-lifecycle-state', 'confirmed',
    );

    await page.getByRole('tab', { name: 'Projection', exact: true }).click();
    const grid = page.locator('[data-fc-datagrid]');
    const fault = grid.locator('[data-fc-diagnostic="HFC2121"]');
    await expect(fault).toBeVisible();
    await expect(fault).toHaveAttribute('data-fc-customization-level', 'Level4');
    await expect(fault).not.toContainText('Intentional Counter override fixture fault.');
    await expect(grid.locator('.counter-cards')).toBeVisible();
    await expect(grid.getByText('counter-1', { exact: true })).toBeVisible();
    await expect(grid.locator('.counter-count-slot strong')).toHaveText('2');

    await page.getByRole('tab', { name: 'Overview', exact: true }).click();
    await fillFieldByLabel(form, 'Amount', '2');
    await fillFieldByLabel(form, 'Note', 'Counter command after the render fault');
    await submit.click();
    await page.getByRole('tab', { name: 'Projection', exact: true }).click();
    await expect(grid.locator('.counter-count-slot strong')).toHaveText('4');
  });
});

const buildDebugCounter = async (deadline: number, accessibilityFixture: boolean): Promise<void> => {
  const args = ['build', 'Counter.Web.csproj', '--configuration', 'Debug', '--disable-build-servers', '-m:1'];
  if (accessibilityFixture) {
    args.push('-p:DefineConstants=COUNTER_ACCESSIBILITY_WARNING_FIXTURE', '-p:WarningsNotAsErrors=HFC1050');
  }
  const build = spawnOwnedProcess(
    'dotnet',
    args,
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
  if (accessibilityFixture) {
    expect(build.output).toMatch(/UnnamedCounterButton\.cs.*warning HFC1050/);
  }
};

const startCounterHost = async (
  baseUrl: string,
  environmentName: 'Development' | 'Test',
  deadline: number,
  renderFault: boolean,
): Promise<void> => {
  const host = spawnOwnedProcess('dotnet', [COUNTER_DEBUG_ASSEMBLY, '--urls', baseUrl], {
    cwd: COUNTER_PROJECT_DIRECTORY,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: environmentName,
      DOTNET_ENVIRONMENT: environmentName,
      Hexalith__FrontComposer__E2E__SeedContractMismatch: String(!renderFault),
      Hexalith__FrontComposer__E2E__SeedOverrideRenderFault: String(renderFault),
      Hexalith__FrontComposer__Specimens__Enabled: '',
      Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs: '200',
    },
  });
  ownedProcesses.push(host);
  await waitForOwnedServer(host, baseUrl, deadline);
};
