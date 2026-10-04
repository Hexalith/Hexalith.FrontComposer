import { fileURLToPath } from 'node:url';

import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { submissionTest } from '../fixtures/command-submission.fixture.js';
import { expectFieldValue, fieldEditorByLabel, fillFieldByLabel } from '../helpers/fluent-fields.js';
import { spawnOwnedProcess, stopOwnedProcess, waitForOwnedServer, type OwnedProcess } from '../helpers/owned-process.js';
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

test.describe('Story 13.3: submit-time denial focus', () => {
  submissionTest.use({ dispatchForbiddenEnabled: true });

  submissionTest('dispatch denial replaces editors with one focused non-live heading', async ({ page, lifecycle }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
      const focusLog: string[] = [];
      (window as unknown as { __fcSubmitDenialFocus: string[] }).__fcSubmitDenialFocus = focusLog;
      document.addEventListener('focusin', (event) => {
        if (event.target instanceof HTMLElement && event.target.id.endsWith('-authorization-heading')) {
          focusLog.push(event.target.id);
        }
      });
    });
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const form = policyForm(page, REJECTION_FORM_LABEL);
    await fillField(form, 'Amount', '3');
    await fillField(form, 'Note', 'QA dispatch denial focus');
    const submit = form.getByRole('button', { name: REJECTION_ACTION_LABEL, exact: true });
    await submit.focus();
    await page.keyboard.press('Enter');

    const card = page.locator('section[data-fc-authorization-denied="true"]');
    const heading = card.getByRole('heading', { name: 'Permission required', exact: true });
    await expect(heading).toBeFocused();
    await expect(form.locator('[data-fc-validation-field]')).toHaveCount(0);
    await expect(form.getByRole('button', { name: REJECTION_ACTION_LABEL, exact: true })).toHaveCount(0);
    await expect(card).toHaveAttribute('role', 'group');
    await expect(card).toHaveAttribute('aria-labelledby', (await heading.getAttribute('id'))!);
    await expect(card).not.toHaveAttribute('aria-live');
    await expect(heading).not.toHaveAttribute('aria-live');
    await expect(card.locator('[aria-live], [role="alert"], [role="status"]')).toHaveCount(0);
    await expect(card).toContainText('You do not have permission to Batch Increment.');
    // BH17-03 — focus is the only speech path, so the focused heading is described by the reason.
    await expect(heading).toHaveAccessibleDescription('You do not have permission to Batch Increment.');
    await expect(card).not.toContainText('Specimen backend denial');
    await expect(card).not.toContainText('This specimen command was denied at dispatch.');
    await lifecycle.expectState(REJECTION_COMMAND_ID, 'idle');
    expect(await page.evaluate(() => (window as unknown as { __fcSubmitDenialFocus: string[] }).__fcSubmitDenialFocus))
      .toEqual([await heading.getAttribute('id')]);
  });
});

test.describe('Story 13.3: mapped rejection recovery focus', () => {
  submissionTest.use({ mappedRejectionEnabled: true });

  submissionTest('keyboard dismissal preserves mapped errors and values, then permits a corrected retry', async ({ page, lifecycle, browserName }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const form = policyForm(page, REJECTION_FORM_LABEL);
    await fillField(form, 'Amount', '3');
    await fillField(form, 'Note', 'QA mapped rejection preservation');
    await form.getByRole('button', { name: REJECTION_ACTION_LABEL, exact: true }).click();

    await lifecycle.expectState(REJECTION_COMMAND_ID, 'rejected');
    const summary = form.getByTestId('fc-validation-summary');
    const amount = fieldEditorByLabel(form, 'Amount');
    const error = 'Amount conflicts with the specimen limit.';
    await expect(summary).toBeFocused();
    await expect(summary.locator('[data-fc-validation-target]')).toHaveText(error);
    await expect(summary).not.toHaveAttribute('aria-live');
    await expect(form.locator('.fc-lifecycle-live')).toHaveCount(0);
    await expect(amount).toHaveAttribute('aria-invalid', 'true');
    if (browserName === 'chromium') {
      // ARIA element reflection crosses Fluent's shadow boundary in the native accessibility tree;
      // Playwright's DOM-only description matcher does not follow that relationship.
      await expect.poll(async () => mappedFieldDescription(page)).toBe(error);
    }
    const field = form.locator('.fc-command-field').filter({ has: page.locator('fluent-text-input[name="Amount"]') });
    await expect(field.locator('.fluent-validation-message')).toHaveText(error);
    await expectFieldValue(form, 'Note', 'QA mapped rejection preservation');

    const dismiss = form.getByTestId('fc-rejected-mapped-dismiss');
    await dismiss.focus();
    await page.keyboard.press('Enter');
    await expect(form.getByTestId('fc-rejected-mapped')).toHaveCount(0);
    await expect(amount).toBeFocused();
    await expectFieldValue(form, 'Amount', '3');
    await expectFieldValue(form, 'Note', 'QA mapped rejection preservation');
    await expect(field.locator('.fluent-validation-message')).toHaveText(error);
    await expect(amount).toHaveAttribute('aria-invalid', 'true');
    await expect(summary.locator('[data-fc-validation-target]')).toHaveText(error);

    await fillField(form, 'Amount', '4');
    await expect(field.locator('.fluent-validation-message')).toHaveCount(0);
    await expect(amount).not.toHaveAttribute('aria-invalid');
    await form.getByRole('button', { name: REJECTION_ACTION_LABEL, exact: true }).click();
    await lifecycle.expectState(REJECTION_COMMAND_ID, 'confirmed');
    await expect(form.getByTestId('fc-rejected-mapped')).toHaveCount(0);
    await expect(summary).toHaveCount(0);
    await expectFieldValue(form, 'Note', 'QA mapped rejection preservation');
  });
});

test.describe('Story 13.3: unmapped rejection recovery focus', () => {
  test.skip(({ browserName }) => browserName !== 'chromium', 'Dedicated rejection-host coverage runs once in Chromium.');
  test.describe.configure({ mode: 'serial' });
  test.use({ baseURL: REJECTION_BASE_URL });

  let server: OwnedProcess | undefined;

  test.beforeAll(async () => {
    if (process.env.FC_E2E_STORY_13_3_REJECTION_BASE_URL) {
      return;
    }

    const projectDirectory = new URL('../../../samples/Counter/Counter.Web/', import.meta.url);
    const configuration = process.env.FC_E2E_COMMAND_SUBMISSION_CONFIGURATION === 'Debug' ? 'Debug' : 'Release';
    const assembly = fileURLToPath(new URL(`bin/${configuration}/net10.0/Counter.Web.dll`, projectDirectory));
    server = spawnOwnedProcess('dotnet', [assembly, '--urls', REJECTION_BASE_URL], {
      cwd: projectDirectory,
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
    }, SERVER_READY_TIMEOUT_MS);

    try {
      await waitForOwnedServer(server, REJECTION_BASE_URL, Date.now() + SERVER_READY_TIMEOUT_MS);
    } catch (error) {
      await stopOwnedProcess(server);
      throw error;
    }

    // VG17-O2 — the spawn timeout bounds startup only; the ready host serves both serial tests until afterAll.
    clearTimeout(server.timer);
  });

  test.afterAll(async () => {
    if (server) {
      await stopOwnedProcess(server);
      server = undefined;
    }
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
    // VR-03 / AA17-02 — the first recovery action works without a pointer.
    await editAndRetry.focus();
    await page.keyboard.press('Enter');

    await expect(fieldEditorByLabel(form, 'Amount')).toBeFocused();
  });

  test('unmapped rejection recovery actions work from the keyboard alone', async ({ page, context, lifecycle }) => {
    await context.grantPermissions(['clipboard-read', 'clipboard-write']);
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
    // A prior in-app page gives Return a browser-history entry to go back to.
    await page.goto('/');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const previousUrl = page.url();
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const form = policyForm(page, REJECTION_FORM_LABEL);
    await expect(form).toBeVisible();
    await fillField(form, 'Amount', '4');
    await fillField(form, 'Note', 'QA keyboard recovery');
    const submit = form.getByRole('button', { name: REJECTION_ACTION_LABEL, exact: true });
    await submit.focus();
    await page.keyboard.press('Enter');

    await lifecycle.expectState(REJECTION_COMMAND_ID, 'rejected');

    // Story 13.3 VR-03 / AA5-02 — edit and retry, return, and copy the support-safe reference follow
    // the rejection message in tab order and work without a pointer.
    const editAndRetry = form.getByTestId('fc-rejection-edit-retry');
    const returnAction = form.getByTestId('fc-rejection-return');
    const copyReference = form.getByTestId('fc-rejection-copy-reference');
    await expect(returnAction).toHaveText('Return');
    await expect(copyReference).toHaveText('Copy support reference');
    await editAndRetry.focus();
    await page.keyboard.press('Tab');
    await expect(returnAction).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(copyReference).toBeFocused();

    await page.keyboard.press('Enter');
    await expect(copyReference).toHaveText('Support reference copied');
    await expect(copyReference).toBeFocused();
    // AA27-01 — the bar keeps Fluent's implicit status role but is not live, so the Copy relabel
    // speaks only as the focused button's new name instead of re-reading the whole rejection bar.
    expect(await rejectionBarLiveProperties(page)).toEqual({ role: 'status', live: undefined });
    const copied = await page.evaluate(async () => navigator.clipboard.readText());
    expect(copied).toMatch(/^Error code: \S.*, documentation code: \S.*$/u);
    expect(copied).not.toContain('QA keyboard recovery');
    expect(copied).not.toContain('The specimen change was rejected.');
    await expectFieldValue(form, 'Amount', '4');
    await expectFieldValue(form, 'Note', 'QA keyboard recovery');

    await returnAction.focus();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(previousUrl);
  });
});

const gotoTypeSpecimen = async (page: Page): Promise<void> => {
  const route = getSpecimenRoute('type');
  await page.goto(route.path);
  await expect(page.locator(route.readySelector)).toBeVisible();
  await expect(page.getByTestId('fc-policy-command-specimen')).toBeVisible();
};

const mappedFieldDescription = async (page: Page): Promise<string | undefined> => {
  const session = await page.context().newCDPSession(page);
  try {
    const { result } = await session.send('Runtime.evaluate', {
      expression: `document.querySelector('.fc-command-form[aria-label="${REJECTION_FORM_LABEL}"] fluent-text-input[name="Amount"]')?.shadowRoot?.querySelector('input')`,
    });
    if (!result.objectId) return undefined;
    const { nodes } = await session.send('Accessibility.getPartialAXTree', {
      objectId: result.objectId,
      fetchRelatives: false,
    });
    return nodes[0]?.description?.value;
  } finally {
    await session.detach();
  }
};

const rejectionBarLiveProperties = async (page: Page): Promise<{ role: string | undefined; live: string | undefined }> => {
  const session = await page.context().newCDPSession(page);
  try {
    const { result } = await session.send('Runtime.evaluate', {
      expression: `document.querySelector('.fc-command-form[aria-label="${REJECTION_FORM_LABEL}"] [data-testid="fc-rejected"]')`,
    });
    if (!result.objectId) return { role: undefined, live: undefined };
    const { nodes } = await session.send('Accessibility.getPartialAXTree', {
      objectId: result.objectId,
      fetchRelatives: false,
    });
    const live = nodes[0]?.properties?.find((property) => property.name === 'live')?.value.value;
    return { role: nodes[0]?.role?.value, live: typeof live === 'string' ? live : undefined };
  } finally {
    await session.detach();
  }
};

const policyForm = (page: Page, ariaLabel: string): Locator =>
  page.locator(`${COMMAND_FORM}[aria-label="${ariaLabel}"]`);

const fillField = async (root: Locator, label: string, value: string): Promise<void> => {
  await fillFieldByLabel(root, label, value);
};
