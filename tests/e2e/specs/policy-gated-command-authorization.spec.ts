import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process';

import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { expectFieldValue, fieldEditorByLabel, fillFieldByLabel } from '../helpers/fluent-fields.js';
import { getSpecimenRoute } from '../helpers/specimen-manifest.js';

const ALLOWED_COMMAND_ID = 'policy-allowed-specimen';
const DENIED_COMMAND_ID = 'policy-denied-specimen';
const ALLOWED_FORM_LABEL = 'Policy Allowed Specimen command form';
const DENIED_FORM_LABEL = 'Policy Denied Specimen command form';
const ALLOWED_ACTION_LABEL = 'Policy Allowed Specimen';
const DENIED_ACTION_LABEL = 'Policy Denied Specimen';
const COMMAND_FORM = '.fc-command-form';
const REJECTION_COMMAND_ID = 'batch-increment';
const REJECTION_FORM_LABEL = 'Batch Increment command form';
const REJECTION_ACTION_LABEL = 'Batch Increment';
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
    await page.addInitScript(() => {
      const focusLog: string[] = [];
      (window as unknown as { __fcAuthorizationHeadingFocus: string[] }).__fcAuthorizationHeadingFocus = focusLog;
      document.addEventListener('focusin', (event) => {
        const target = event.target instanceof Element ? event.target : null;
        if (target?.id.startsWith('fc-command-authorization-') || target?.id.endsWith('-authorization-heading')) {
          focusLog.push(target.id);
        }
      }, true);
    });

    await gotoTypeSpecimen(page);

    const specimen = page.getByTestId('fc-policy-command-specimen');
    await expect(specimen).toBeVisible();
    await expect(specimen).toContainText('Permission required');
    await expect(specimen).toContainText(`You do not have permission to ${DENIED_ACTION_LABEL}.`);
    const denialHeading = specimen.locator(
      '[id^="fc-command-authorization-Counter-Specimens-Domain-PolicyDeniedSpecimenCommand-"]',
    );
    await expect(denialHeading).toBeVisible();
    await expect(denialHeading).toHaveAttribute('tabindex', '-1');
    // Story 13.3 BH2-04 / FM-01 — a renderer denied on page load is not an operator activation:
    // its replacement renders silently and no denied heading ever takes focus from the route.
    await page.waitForTimeout(1_000);
    await expect(denialHeading).not.toBeFocused();
    expect(await page.evaluate(() => (window as unknown as { __fcAuthorizationHeadingFocus: string[] }).__fcAuthorizationHeadingFocus))
      .toEqual([]);
    await expect(denialHeading).not.toHaveAttribute('role', /alert|status/u);
    await expect(denialHeading).not.toHaveAttribute('aria-live', /.+/u);
    await expect(policyForm(page, DENIED_FORM_LABEL)).toHaveCount(0);
    await expect(page.getByTestId(`fc-lifecycle-${DENIED_COMMAND_ID}`)).toHaveCount(0);
    await expect(specimen).not.toContainText('Specimens.PolicyDenied');
    await expect(specimen).not.toContainText('PolicyDeniedSpecimenCommand');
  });

  test('a background denial focuses its heading only when the replaced form held focus', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);

    // Story 13.3 VG3-01 — execute the real replacement check in the browser. The Counter host cannot
    // revoke a policy mid-session, so the replacement is simulated in the DOM the way a background
    // authorization refresh renders it: the focused editor disappears and a denial heading appears.
    const outcome = await page.evaluate(async (focusModulePath) => {
      const focus = await import(focusModulePath) as {
        captureFocusBeforeReplacement: (key: string) => boolean;
        focusReplacementHeading: (key: string, headingId: string) => boolean;
      };
      const fixture = document.createElement('div');
      fixture.innerHTML = `
        <div id="replaced-form"><input id="replaced-editor" aria-label="Replaced editor"></div>
        <div id="silent-form"><input id="silent-editor" aria-label="Silent editor"></div>
        <button id="elsewhere" type="button">Elsewhere</button>`;
      document.body.append(fixture);

      // Focus inside the form that the denial replaces: the denial heading takes focus.
      (document.getElementById('replaced-editor') as HTMLElement).focus();
      const armed = focus.captureFocusBeforeReplacement('replaced-heading');
      (document.getElementById('replaced-form') as HTMLElement).innerHTML =
        '<section><h2 id="replaced-heading" tabindex="-1">Permission required</h2></section>';
      const replacedFocused = focus.focusReplacementHeading('replaced-heading', 'replaced-heading');
      const afterReplacement = document.activeElement?.id;

      // Focus elsewhere on the page: the replacement renders silently and leaves focus alone.
      (document.getElementById('elsewhere') as HTMLElement).focus();
      focus.captureFocusBeforeReplacement('silent-heading');
      (document.getElementById('silent-form') as HTMLElement).innerHTML =
        '<section><h2 id="silent-heading" tabindex="-1">Permission required</h2></section>';
      const silentFocused = focus.focusReplacementHeading('silent-heading', 'silent-heading');
      const afterSilent = document.activeElement?.id;

      // Without a capture (an initial denial), the heading never takes focus.
      const uncapturedFocused = focus.focusReplacementHeading('never-captured', 'silent-heading');
      fixture.remove();
      return { armed, replacedFocused, afterReplacement, silentFocused, afterSilent, uncapturedFocused };
    }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');

    expect(outcome).toEqual({
      armed: true,
      replacedFocused: true,
      afterReplacement: 'replaced-heading',
      silentFocused: false,
      afterSilent: 'elsewhere',
      uncapturedFocused: false,
    });
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
    // An unprotected generated form keeps this recovery proof independent of the host's
    // authentication provider; the rejection comes from the authoritative command service.
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const form = policyForm(page, REJECTION_FORM_LABEL);
    await expect(form).toBeVisible();
    await fillField(form, 'Amount', '3');
    await fillField(form, 'Note', 'QA unmapped rejection focus');
    const submit = form.getByRole('button', { name: REJECTION_ACTION_LABEL, exact: true });
    await submit.click();

    await lifecycle.expectState(REJECTION_COMMAND_ID, 'rejected');
    // An unmapped rejection invents no field errors and keeps its lifecycle recovery path.
    await expect(form.getByTestId('fc-validation-summary')).toHaveCount(0);
    await expect(form).toContainText('The specimen change was rejected.');
    await expectFieldValue(form, 'Amount', '3');
    await expectFieldValue(form, 'Note', 'QA unmapped rejection focus');

    const editAndRetry = form.getByTestId('fc-rejection-edit-retry');
    await expect(editAndRetry).toBeVisible();
    await editAndRetry.click();

    await expect(fieldEditorByLabel(form, 'Amount')).toBeFocused();
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
