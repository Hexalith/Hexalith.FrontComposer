import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process';

import { expect, test } from '../fixtures/index.js';
import { fillFieldByLabel } from '../helpers/fluent-fields.js';

const LEVEL4_BASE_URL = process.env.FC_E2E_LEVEL4_BASE_URL ?? 'http://127.0.0.1:5082';
const SERVER_READY_TIMEOUT_MS = 120_000;

let server: ChildProcessWithoutNullStreams | undefined;
let serverOutput = '';

test.describe('Story 6.3: Level 4 full-view overrides', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'Dedicated host coverage runs once in Chromium.');
  test.use({ baseURL: LEVEL4_BASE_URL });

  test.beforeAll(async () => {
    if (process.env.FC_E2E_LEVEL4_BASE_URL) {
      return;
    }

    serverOutput = '';
    server = spawn(
      'dotnet',
      [
        'run',
        '--project',
        '../../samples/Counter/Counter.Web/Counter.Web.csproj',
        '--configuration',
        'Release',
        '--no-build',
        '--no-launch-profile',
        '--urls',
        LEVEL4_BASE_URL,
      ],
      {
        cwd: new URL('..', import.meta.url),
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: 'Test',
          Hexalith__FrontComposer__Specimens__Enabled: '',
          Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs: '200',
        },
      },
    );
    server.stdout.on('data', appendServerOutput);
    server.stderr.on('data', appendServerOutput);

    try {
      await waitForServerReady(LEVEL4_BASE_URL);
    } catch (error) {
      server.kill('SIGTERM');
      throw error;
    }
  });

  test.afterAll(async () => {
    if (!server) {
      return;
    }

    server.kill('SIGTERM');
    await new Promise<void>((resolve) => {
      server?.once('exit', () => resolve());
      setTimeout(resolve, 5_000).unref();
    });
    server = undefined;
  });

  test('registered full-view replacement owns the projection body and delegates generated fields explicitly', async ({
    page,
  }) => {
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();

    const form = page.locator('.fc-command-form[aria-label="Batch Increment command form"]');
    await expect(form).toBeVisible();
    await fillFieldByLabel(form, 'Amount', '2');
    await fillFieldByLabel(form, 'Note', 'Story 6.3 Level 4 full-view override e2e');
    await form.getByRole('button', { name: 'Batch Increment' }).click();

    await expect(page.locator('[data-testid="fc-lifecycle-batch-increment"]')).toHaveAttribute(
      'data-lifecycle-state',
      'confirmed',
    );

    const replacement = page.locator('section[aria-labelledby="counter-full-view-heading"]');
    await expect(replacement).toBeVisible();
    await expect(replacement.locator('#counter-full-view-heading')).toBeVisible();
    await expect(replacement.getByText('counter-1')).toBeVisible();

    const countSlot = replacement.locator('.counter-count-slot');
    await expect(countSlot).toHaveCount(1);
    await expect(countSlot.getByText('Count')).toBeVisible();
    await expect(countSlot.locator('strong')).toHaveText('2');

    await expect(replacement.getByLabel(/Last changed for counter-1/u)).toBeVisible();
    await expect(page.locator('.fc-selected-template')).toHaveCount(0);
    await expect(page.locator('fluent-data-grid')).toHaveCount(0);
  });
});

const waitForServerReady = async (baseUrl: string): Promise<void> => {
  const deadline = Date.now() + SERVER_READY_TIMEOUT_MS;
  let lastError: unknown;

  while (Date.now() < deadline) {
    if (server?.exitCode !== null) {
      throw new Error(
        `Counter Level 4 host exited before it became ready with code ${server?.exitCode}.\n${serverOutput}`,
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
    `Counter Level 4 host did not become ready at ${baseUrl}. Last error: ${String(lastError)}\n${serverOutput}`,
  );
};

const appendServerOutput = (chunk: Buffer): void => {
  serverOutput = `${serverOutput}${chunk.toString('utf8')}`.slice(-8_000);
};
