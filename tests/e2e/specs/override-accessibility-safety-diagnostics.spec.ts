import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process';

import { expect, test } from '../fixtures/index.js';

const DEVELOPMENT_BASE_URL = process.env.FC_E2E_STORY_6_4_DEVELOPMENT_BASE_URL ?? 'http://127.0.0.1:5084';
const NON_DEVELOPMENT_BASE_URL = process.env.FC_E2E_STORY_6_4_NON_DEVELOPMENT_BASE_URL ?? 'http://127.0.0.1:5085';
const SERVER_READY_TIMEOUT_MS = 120_000;

test.describe('Story 6.4: override accessibility safety diagnostics', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'Dedicated host coverage runs once in Chromium.');
  test.describe.configure({ mode: 'serial' });

  test('development debug host renders sanitized contract mismatch diagnostics through the panel', async ({ page }) => {
    const host = await startCounterHost(DEVELOPMENT_BASE_URL, 'Development');
    try {
      await page.goto(DEVELOPMENT_BASE_URL);

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
    } finally {
      await stopCounterHost(host);
    }
  });

  test('non-development debug host suppresses the mismatch panel even when a rejection is recorded', async ({ page }) => {
    const host = await startCounterHost(NON_DEVELOPMENT_BASE_URL, 'Test');
    try {
      await page.goto(NON_DEVELOPMENT_BASE_URL);

      await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
      await expect(page.locator('[data-fc-diagnostic="HFC1041"]')).toHaveCount(0);
      await expect(page.getByRole('link', { name: 'Diagnostic documentation' })).toHaveCount(0);
    } finally {
      await stopCounterHost(host);
    }
  });
});

type CounterHost = {
  process?: ChildProcessWithoutNullStreams;
  output: string;
};

const startCounterHost = async (baseUrl: string, environmentName: 'Development' | 'Test'): Promise<CounterHost> => {
  if (process.env.FC_E2E_STORY_6_4_DEVELOPMENT_BASE_URL && environmentName === 'Development') {
    return { output: '' };
  }

  if (process.env.FC_E2E_STORY_6_4_NON_DEVELOPMENT_BASE_URL && environmentName === 'Test') {
    return { output: '' };
  }

  const host: CounterHost = { output: '' };
  host.process = spawn(
    'dotnet',
    [
      'run',
      '--project',
      '../../samples/Counter/Counter.Web/Counter.Web.csproj',
      '--configuration',
      'Debug',
      '--no-launch-profile',
      '--urls',
      baseUrl,
    ],
    {
      cwd: new URL('..', import.meta.url),
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: environmentName,
        DOTNET_ENVIRONMENT: environmentName,
        Hexalith__FrontComposer__E2E__SeedContractMismatch: 'true',
        Hexalith__FrontComposer__Specimens__Enabled: '',
        Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs: '200',
      },
    },
  );
  host.process.stdout.on('data', (chunk: Buffer) => appendServerOutput(host, chunk));
  host.process.stderr.on('data', (chunk: Buffer) => appendServerOutput(host, chunk));

  try {
    await waitForServerReady(host, baseUrl);
  } catch (error) {
    host.process.kill('SIGTERM');
    throw error;
  }

  return host;
};

const stopCounterHost = async (host: CounterHost): Promise<void> => {
  if (!host.process) {
    return;
  }

  host.process.kill('SIGTERM');
  await new Promise<void>((resolve) => {
    host.process?.once('exit', () => resolve());
    setTimeout(resolve, 5_000).unref();
  });
  host.process = undefined;
};

const waitForServerReady = async (host: CounterHost, baseUrl: string): Promise<void> => {
  const deadline = Date.now() + SERVER_READY_TIMEOUT_MS;
  let lastError: unknown;

  while (Date.now() < deadline) {
    if (host.process?.exitCode !== null) {
      throw new Error(
        `Counter Story 6.4 host exited before it became ready with code ${host.process?.exitCode}.\n${host.output}`,
      );
    }

    try {
      const response = await fetch(baseUrl);
      if (response.ok) {
        return;
      }
    } catch (error) {
      lastError = error;
    }

    await new Promise((resolve) => setTimeout(resolve, 500));
  }

  throw new Error(
    `Counter Story 6.4 host did not become ready at ${baseUrl}. Last error: ${String(lastError)}\n${host.output}`,
  );
};

const appendServerOutput = (host: CounterHost, chunk: Buffer): void => {
  host.output = `${host.output}${chunk.toString('utf8')}`.slice(-8_000);
};
