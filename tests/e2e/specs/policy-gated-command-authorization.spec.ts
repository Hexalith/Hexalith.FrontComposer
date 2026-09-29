import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process';

import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { fieldEditorByLabel, fillFieldByLabel } from '../helpers/fluent-fields.js';
import { getSpecimenRoute } from '../helpers/specimen-manifest.js';

const ALLOWED_COMMAND_ID = 'policy-allowed-specimen';
const DENIED_COMMAND_ID = 'policy-denied-specimen';
const ALLOWED_FORM_LABEL = 'Policy Allowed Specimen command form';
const DENIED_FORM_LABEL = 'Policy Denied Specimen command form';
const ALLOWED_ACTION_LABEL = 'Policy Allowed Specimen';
const DENIED_ACTION_LABEL = 'Policy Denied Specimen';
const COMMAND_FORM = '.fc-command-form';
const REJECTION_BASE_URL = process.env.FC_E2E_STORY_13_3_REJECTION_BASE_URL ?? 'http://127.0.0.1:5086';
const SERVER_READY_TIMEOUT_MS = 120_000;

test.describe('Story 4.4: policy-gated command authorization', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
  });

  test('allowed protected specimen command renders the form and dispatches after authorization', async ({
    page,
    lifecycle,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);

    const form = policyForm(page, ALLOWED_FORM_LABEL);
    await expect(form).toBeVisible();
    await lifecycle.expectState(ALLOWED_COMMAND_ID, 'idle');

    await fillField(form, 'Record Id', 'FC-AUTH-ALLOW-001');
    await fillField(form, 'Reason', 'QA story 4.4 allowed dispatch');
    await form.getByRole('button', { name: ALLOWED_ACTION_LABEL }).click();

    await expect(form.getByText(/Submitting/u)).toBeVisible();
    await lifecycle.expectState(ALLOWED_COMMAND_ID, 'confirmed');
    await expect(form.getByTestId('fc-confirmed')).toBeVisible();
  });

  test('denied protected specimen command fails closed without leaking policy metadata', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);

    const specimen = page.getByTestId('fc-policy-command-specimen');
    await expect(specimen).toBeVisible();
    await expect(specimen).toContainText('Permission required');
    await expect(specimen).toContainText(`You do not have permission to ${DENIED_ACTION_LABEL}.`);
    const denialHeading = specimen.locator(
      '[id^="fc-command-authorization-Counter-Specimens-Domain-PolicyDeniedSpecimenCommand-"]',
    );
    await expect(denialHeading).toBeFocused();
    await expect(denialHeading).not.toHaveAttribute('role', /alert|status/u);
    await expect(denialHeading).not.toHaveAttribute('aria-live', /.+/u);
    await expect(policyForm(page, DENIED_FORM_LABEL)).toHaveCount(0);
    await expect(page.getByTestId(`fc-lifecycle-${DENIED_COMMAND_ID}`)).toHaveCount(0);
    await expect(specimen).not.toContainText('Specimens.PolicyDenied');
    await expect(specimen).not.toContainText('PolicyDeniedSpecimenCommand');
  });
});

test.describe('Story 13.3: unmapped rejection recovery focus', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'Dedicated rejection-host coverage runs once in Chromium.');
  test.describe.configure({ mode: 'serial' });
  test.use({ baseURL: REJECTION_BASE_URL });

  let server: ChildProcessWithoutNullStreams | undefined;
  let serverOutput = '';

  test.beforeAll(async () => {
    if (process.env.FC_E2E_STORY_13_3_REJECTION_BASE_URL) {
      return;
    }

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
        REJECTION_BASE_URL,
      ],
      {
        cwd: new URL('..', import.meta.url),
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: 'Development',
          DOTNET_ENVIRONMENT: 'Development',
          Hexalith__FrontComposer__Specimens__Enabled: 'true',
          Hexalith__FrontComposer__StubCommandService__AcknowledgeDelayMs: '0',
          Hexalith__FrontComposer__StubCommandService__SimulateRejection: 'true',
          Hexalith__FrontComposer__StubCommandService__RejectionReason: 'The specimen change was rejected.',
          Hexalith__FrontComposer__StubCommandService__RejectionResolution: 'Edit the values and retry.',
        },
      },
    );
    server.stdout.on('data', appendServerOutput);
    server.stderr.on('data', appendServerOutput);

    try {
      await waitForServerReady(REJECTION_BASE_URL);
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
    await new Promise<void>((resolveExit) => {
      server?.once('exit', () => resolveExit());
      setTimeout(resolveExit, 5_000).unref();
    });
    server = undefined;
  });

  test('Edit and retry moves real browser focus to the first generated editor', async ({ page, lifecycle }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
    await gotoTypeSpecimen(page);
    const form = policyForm(page, ALLOWED_FORM_LABEL);
    await fillField(form, 'Record Id', 'FC-REJECT-001');
    await fillField(form, 'Reason', 'QA unmapped rejection focus');
    await form.getByRole('button', { name: ALLOWED_ACTION_LABEL }).click();

    await lifecycle.expectState(ALLOWED_COMMAND_ID, 'rejected');
    const editAndRetry = form.getByTestId('fc-rejection-edit-retry');
    await expect(editAndRetry).toBeVisible();
    await editAndRetry.click();

    await expect(fieldEditorByLabel(form, 'Record Id')).toBeFocused();
    await expect(form.getByTestId('fc-validation-summary')).toHaveCount(0);
    await expect(form).toContainText('The specimen change was rejected.');
  });

  const appendServerOutput = (chunk: Buffer): void => {
    serverOutput = `${serverOutput}${chunk.toString('utf8')}`.slice(-8_000);
  };

  const waitForServerReady = async (baseUrl: string): Promise<void> => {
    const deadline = Date.now() + SERVER_READY_TIMEOUT_MS;
    let lastError: unknown;

    while (Date.now() < deadline) {
      if (server?.exitCode !== null) {
        throw new Error(`Counter Story 13.3 rejection host exited before readiness.\n${serverOutput}`);
      }

      try {
        const response = await fetch(baseUrl);
        if (response.ok) {
          return;
        }
      } catch (error) {
        lastError = error;
      }

      await new Promise((resolveWait) => setTimeout(resolveWait, 500));
    }

    throw new Error(
      `Counter Story 13.3 rejection host did not become ready. Last error: ${String(lastError)}\n${serverOutput}`,
    );
  };
});

const gotoTypeSpecimen = async (page: Page): Promise<void> => {
  const route = getSpecimenRoute('type');
  await page.goto(route.path);
  await expect(page.locator(route.readySelector)).toBeVisible();
  await expect(page.getByTestId('fc-policy-command-specimen')).toBeVisible();
};

const policyForm = (page: Page, ariaLabel: string): Locator =>
  page.locator(`${COMMAND_FORM}[aria-label="${ariaLabel}"]`);

const fillField = async (root: Locator, label: string, value: string): Promise<void> => {
  await fillFieldByLabel(root, label, value);
};
