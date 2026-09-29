import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { fieldEditorByLabel, fillFieldByLabel, waitForGeneratedFormReady } from '../helpers/fluent-fields.js';
import { getSpecimenRoute } from '../helpers/specimen-manifest.js';

const COMMAND_ID = 'purge-specimen-record';
const FORM_LABEL = 'Purge Specimen Record command form';
const ACTION_LABEL = 'Purge Specimen Record';

test.describe('Story 4.1: destructive command confirmation', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
  });

  test('cancel and Escape keep the destructive command idle and reusable', async ({ page, lifecycle, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);

    const form = destructiveForm(page);
    await expect(form).toBeVisible();
    await lifecycle.expectState(COMMAND_ID, 'idle');

    await submitDestructiveCommand(form);

    const dialog = destructiveDialog(page);
    await expect(dialog).toBeVisible();
    await expect(dialog).toContainText('This specimen record is used by visual and accessibility evidence.');
    await expect(page.getByRole('heading', { name: 'Purge specimen record?' })).toBeVisible();
    await expect(page.getByTestId('fc-destructive-cancel')).toBeFocused();
    const modal = page.getByRole('alertdialog');
    await expect(modal).toHaveAttribute('aria-labelledby', 'fc-destructive-dialog-title');
    await expect(modal).toHaveAttribute(
      'aria-description',
      'This specimen record is used by visual and accessibility evidence.',
    );
    await expect(modal).toHaveAttribute('aria-modal', 'true');
    await expect(modal).toHaveAccessibleName('Purge specimen record?');
    await expect(modal).toHaveAccessibleDescription(
      'This specimen record is used by visual and accessibility evidence.',
    );

    // Cross both action-list boundaries, not merely the adjacent action path.
    await page.keyboard.press('Shift+Tab');
    await expect(page.getByTestId('fc-destructive-confirm')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('fc-destructive-cancel')).toBeFocused();

    await page.getByTestId('fc-destructive-cancel').click();

    await expect(dialog).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'idle');
    await expect(form.getByTestId('fc-confirmed')).toHaveCount(0);
    await expect(form.getByRole('button', { name: ACTION_LABEL })).toBeFocused();

    await submitDestructiveCommand(form);
    await expect(dialog).toBeVisible();

    await dialog.focus();
    await page.keyboard.press('Escape');

    await expect(dialog).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'idle');
    await expect(form.getByRole('button', { name: ACTION_LABEL })).toBeEnabled();
    await expect(form.getByRole('button', { name: ACTION_LABEL })).toBeFocused();
  });

  test('confirmation is required before dispatch and reaches terminal feedback once confirmed', async ({
    page,
    lifecycle,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);

    const form = destructiveForm(page);
    await fillDestructiveFields(form, 'FC-1002', 'QA story 4.1 confirmed dispatch');
    await submitDestructiveCommand(form);

    await lifecycle.expectState(COMMAND_ID, 'idle');

    const dialog = destructiveDialog(page);
    await expect(dialog).toBeVisible();
    await expect(page.getByTestId('fc-destructive-confirm')).toHaveText(ACTION_LABEL);

    await page.getByTestId('fc-destructive-confirm').click();

    await expect(dialog).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'confirmed');
    await expect(form.getByTestId('fc-confirmed')).toBeVisible();
    await expect(form.getByRole('button', { name: ACTION_LABEL })).toBeEnabled();
  });

  test('validation failure and rapid clicks cannot bypass the destructive gate', async ({ page, lifecycle, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);

    const form = destructiveForm(page);
    const group = form.locator('fieldset[data-fc-field-group="Purge details"]');
    await expect(group).toHaveCount(1);
    await expect(group).toHaveAccessibleName('Purge details');
    const groupedFields = group.locator('[data-fc-validation-field="true"]');
    await expect(groupedFields).toHaveCount(2);
    await expect(groupedFields.nth(0)).toHaveAttribute('name', 'RecordId');
    await expect(groupedFields.nth(1)).toHaveAttribute('name', 'Reason');

    // Retention stays unchosen, so the required nullable enum select is invalid beside Record Id.
    await fillDestructiveFields(form, '', 'QA story 4.1 validation blocks dialog');
    await submitDestructiveCommand(form, { chooseRetention: false });

    const summary = page.getByTestId('fc-validation-summary');
    await expect(summary.getByText('The Record Id field is required.')).toBeVisible();
    await expect(summary).toBeFocused();
    // BH3-17 — the default message already names its field, so the link does not repeat the label.
    await expect(summary.locator('[data-fc-validation-target]')).toHaveText([
      'The Record Id field is required.',
      'The Retention field is required.',
    ]);

    // Story 13.3 VR-01 / BH3-08 / BH3-09 — the Chromium accessibility tree of each focusable textbox
    // reports the invalid state and is described by its Fluent-rendered description and error; each
    // error is visible exactly once.
    if (test.info().project.name === 'chromium') {
      await expect.poll(async () => axField(page, `.fc-command-form[aria-label="${FORM_LABEL}"] fluent-text-input[name="RecordId"]`), { timeout: 10_000 }).toMatchObject({
        invalid: 'true',
        description: expect.stringMatching(/Record to purge\.[\s\S]*The Record Id field is required\./u),
      });
      await expect.poll(async () => axField(page, `.fc-command-form[aria-label="${FORM_LABEL}"] fluent-text-input[name="Reason"]`), { timeout: 10_000 }).toMatchObject({
        invalid: undefined,
        description: 'Why this purge is required.',
      });
      // Story 13.3 VG7-02 — a Fluent select's focusable control is the combobox slotted into the light
      // DOM, not a shadow input; it too reports the invalid state and is described by its error.
      await expect.poll(async () => axField(page, `.fc-command-form[aria-label="${FORM_LABEL}"] fluent-dropdown[name="Retention"]`), { timeout: 10_000 }).toMatchObject({
        role: 'combobox',
        invalid: 'true',
        description: expect.stringMatching(/How long the purged record stays recoverable\.[\s\S]*The Retention field is required\./u),
      });
    }
    const recordField = form.locator('fluent-field', { has: page.locator('label[slot="label"]:text-is("Record Id")') });
    await expect(recordField.locator('.fluent-validation-message')).toHaveCount(1);
    await expect(recordField.getByText('The Record Id field is required.', { exact: true })).toHaveCount(1);
    await expect(recordField.locator('[slot="message"] .fc-command-field-description')).toHaveText('Record to purge.');
    const retentionField = form.locator('fluent-field', { has: page.locator('label[slot="label"]:text-is("Retention")') });
    await expect(retentionField.locator('.fluent-validation-message')).toHaveCount(1);
    await expect(retentionField.getByText('The Retention field is required.', { exact: true })).toHaveCount(1);
    await summary.locator('[data-fc-validation-target]').first().click();
    await expect(fieldEditorByLabel(form, 'Record Id')).toBeFocused();
    if (test.info().project.name === 'chromium') {
      // A Fluent select forwards focus to the combobox it slots into its light DOM; the summary link
      // must count that as reaching the select, not fall back to another field.
      await summary.locator('[data-fc-validation-target]').nth(1).click();
      await expect(form.locator('fluent-dropdown[name="Retention"] > [slot="control"]')).toBeFocused();
    }
    await expect(destructiveDialog(page)).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'idle');

    await fillDestructiveFields(form, 'FC-1002', 'QA story 4.1 rapid submit gate');
    await chooseRetention(form);
    // A corrected select drops its invalid state and its single error.
    if (test.info().project.name === 'chromium') {
      await expect.poll(async () => axField(page, `.fc-command-form[aria-label="${FORM_LABEL}"] fluent-dropdown[name="Retention"]`), { timeout: 10_000 }).toMatchObject({
        invalid: undefined,
        description: 'How long the purged record stays recoverable.',
      });
    }
    await expect(retentionField.locator('.fluent-validation-message')).toHaveCount(0);
    await form.getByRole('button', { name: ACTION_LABEL }).dblclick();

    await expect(destructiveDialog(page)).toHaveCount(1);

    await page.getByTestId('fc-destructive-confirm').click();

    await expect(destructiveDialog(page)).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'confirmed');
    await expect(form.getByTestId('fc-confirmed')).toBeVisible();
  });

  test('a real settings modal prevents opening a nested destructive dialog', async ({ page, lifecycle, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);
    const form = destructiveForm(page);
    // Open the real settings modal only once the circuit is interactive, so the click is not lost. A
    // valid form makes the programmatic press reach the confirmation gate, not client validation.
    await waitForGeneratedFormReady(form);
    await chooseRetention(form);
    await page.getByTestId('fc-settings-button').click();
    const settings = page.getByTestId('fc-settings-dialog');
    await expect(settings).toBeVisible();
    await expect(page.locator('#fc-settings-heading')).toBeFocused();

    await form.getByRole('button', { name: ACTION_LABEL }).evaluate((button) => {
      (button as HTMLElement).click();
    });

    await expect(settings).toBeVisible();
    await expect(destructiveDialog(page)).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'idle');
    await page.getByTestId('fc-settings-done').click();
    await expect(settings).toHaveCount(0);
  });

  test('an opening overlay intent reserves the modal slot only until it expires', async ({ page, lifecycle, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);

    const reservation = await page.evaluate(async (focusModulePath) => {
      const focus = await import(focusModulePath) as {
        captureOverlayOrigin: (testId?: string | null, preserveExisting?: boolean) => boolean;
      };
      const intentWindow = window as unknown as { __fcOverlayOpenIntent: unknown; __fcOverlayOrigin: unknown };

      // A second capture before the first overlay receives focus must be refused (no nested modal).
      intentWindow.__fcOverlayOpenIntent = null;
      const firstCapture = focus.captureOverlayOrigin();
      const racingCapture = focus.captureOverlayOrigin();

      // A press that never opened its overlay must not refuse every later confirmation.
      intentWindow.__fcOverlayOpenIntent = { origin: null, moved: false, watchFocus: false, createdAt: Date.now() - 60_000 };
      const afterStaleIntent = focus.captureOverlayOrigin();

      // Story 13.3 BH2-15 / VG2-07 — shell overlays that ask to preserve an existing origin keep the
      // keyboard tracker's intent (it carries no createdAt) and its `moved` state untouched.
      const trackerOrigin = document.querySelector('h1');
      const trackerIntent = { origin: trackerOrigin, moved: true, watchFocus: true };
      intentWindow.__fcOverlayOrigin = trackerOrigin;
      intentWindow.__fcOverlayOpenIntent = trackerIntent;
      const preservedCapture = focus.captureOverlayOrigin('fc-palette-trigger', true);
      const trackerPreserved = intentWindow.__fcOverlayOpenIntent === trackerIntent
        && intentWindow.__fcOverlayOrigin === trackerOrigin
        && trackerIntent.moved === true;

      intentWindow.__fcOverlayOrigin = null;
      intentWindow.__fcOverlayOpenIntent = { origin: null, moved: false, watchFocus: false, createdAt: Date.now() - 60_000 };
      return { firstCapture, racingCapture, afterStaleIntent, preservedCapture, trackerPreserved };
    }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');

    expect(reservation).toEqual({
      firstCapture: true,
      racingCapture: false,
      afterStaleIntent: true,
      preservedCapture: true,
      trackerPreserved: true,
    });

    // The expired intent left behind above still lets the real destructive flow open exactly one dialog.
    const form = destructiveForm(page);
    await submitDestructiveCommand(form);
    const dialog = destructiveDialog(page);
    await expect(dialog).toBeVisible();
    await expect(dialog).toHaveCount(1);
    await expect(page.getByTestId('fc-destructive-cancel')).toBeFocused();
    await page.getByTestId('fc-destructive-cancel').click();
    await expect(dialog).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'idle');
    await expect(form.getByRole('button', { name: ACTION_LABEL })).toBeFocused();
  });

  test('validation focus fallbacks use the next invalid target and survive a missing summary', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);

    const outcome = await page.evaluate(async (focusModulePath) => {
      const focus = await import(focusModulePath) as {
        focusValidationOutcome: (summaryId: string) => void;
        focusValidationTarget: (summaryId: string, targetId: string) => boolean;
      };
      const fixture = document.createElement('div');
      fixture.innerHTML = `
        <div data-fc-command-form="true" data-fc-validation-summary-id="fallback-summary">
          <section id="fallback-summary" tabindex="-1">
            <a data-fc-validation-target="removed-input">Removed field</a>
            <a data-fc-validation-target="next-invalid-input">Next field</a>
          </section>
          <button id="first-invalid-input" data-fc-validation-field data-fc-invalid="true">First invalid field</button>
          <button id="next-invalid-input" data-fc-validation-field>Next invalid field</button>
        </div>`;
      document.body.append(fixture);

      focus.focusValidationTarget('fallback-summary', 'removed-input');
      const missingTargetFallback = document.activeElement?.id;

      document.getElementById('fallback-summary')?.remove();
      focus.focusValidationOutcome('fallback-summary');
      await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
      const missingSummaryFallback = document.activeElement?.id;
      fixture.remove();

      return { missingTargetFallback, missingSummaryFallback };
    }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');

    expect(outcome.missingTargetFallback).toBe('next-invalid-input');
    expect(outcome.missingSummaryFallback).toBe('first-invalid-input');
  });

  test('removed destructive invoker restores focus to the route heading', async ({ page, lifecycle, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoTypeSpecimen(page);

    const form = destructiveForm(page);
    await submitDestructiveCommand(form);
    const dialog = destructiveDialog(page);
    await expect(dialog).toBeVisible();

    await form.getByRole('button', { name: ACTION_LABEL }).evaluate((invoker) => invoker.remove());
    await page.getByTestId('fc-destructive-cancel').click();

    await expect(dialog).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'idle');
    await expect(page.getByRole('heading', {
      name: 'Type, theme, density, lifecycle, and navigation specimen',
    })).toBeFocused();
  });
});

// Reads the Chromium accessibility tree (not the DOM) for the focusable control of one generated
// editor, found from the editor host selector: the input inside a text-like editor's shadow root, or
// the control a Fluent select slots into the light DOM.
const axField = async (
  page: Page,
  hostSelector: string,
): Promise<{ role?: string; invalid?: string; description?: string }> => {
  const session = await page.context().newCDPSession(page);
  try {
    const { result } = await session.send('Runtime.evaluate', {
      expression: `(() => {
        const host = document.querySelector(${JSON.stringify(hostSelector)});
        return host?.shadowRoot?.querySelector('input, textarea')
          ?? host?.querySelector(':scope > [slot="control"]')
          ?? host;
      })()`,
    }) as { result: { objectId?: string } };
    if (!result.objectId) return {};
    const { nodes } = await session.send('Accessibility.getPartialAXTree', {
      objectId: result.objectId,
      fetchRelatives: false,
    }) as {
      nodes: Array<{
        role?: { value?: string };
        description?: { value?: string };
        properties?: Array<{ name: string; value?: { value?: unknown } }>;
      }>;
    };
    const node = nodes[0];
    const invalid = node?.properties?.find((property) => property.name === 'invalid')?.value?.value;
    return {
      role: node?.role?.value,
      invalid: invalid === undefined || invalid === 'false' ? undefined : String(invalid),
      description: node?.description?.value,
    };
  }
  finally {
    await session.detach();
  }
};

const gotoTypeSpecimen = async (page: Page): Promise<void> => {
  const route = getSpecimenRoute('type');
  await page.goto(route.path);
  await expect(page.locator(route.readySelector)).toBeVisible();
  await expect(page.getByTestId('fc-destructive-command-specimen')).toBeVisible();
};

const destructiveForm = (page: Page): Locator =>
  page.locator(`.fc-command-form[aria-label="${FORM_LABEL}"]`);

const destructiveDialog = (page: Page): Locator => page.getByTestId('fc-destructive-dialog');

const fillDestructiveFields = async (form: Locator, recordId: string, reason: string): Promise<void> => {
  await fillFieldByLabel(form, 'Record Id', recordId);
  await fillFieldByLabel(form, 'Reason', reason);
};

// The required Retention select starts unchosen; a valid purge picks a retention first.
const chooseRetention = async (form: Locator, option = 'Thirty Days'): Promise<void> => {
  await waitForGeneratedFormReady(form);
  const control = form.locator('fluent-dropdown[name="Retention"] > [slot="control"]');
  if ((await control.textContent())?.trim() === option) return;
  await control.click();
  await form.locator('fluent-dropdown[name="Retention"] fluent-option', { hasText: option }).click();
  await expect(control).toHaveText(option);
};

const submitDestructiveCommand = async (
  form: Locator,
  { chooseRetention: withRetention = true }: { chooseRetention?: boolean } = {},
): Promise<void> => {
  await waitForGeneratedFormReady(form);
  if (withRetention) await chooseRetention(form);
  await form.getByRole('button', { name: ACTION_LABEL }).click();
};
