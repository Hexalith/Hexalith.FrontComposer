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

    await fillDestructiveFields(form, '', 'QA story 4.1 validation blocks dialog');
    await submitDestructiveCommand(form);

    const summary = page.getByTestId('fc-validation-summary');
    await expect(summary.getByText('The Record Id field is required.')).toBeVisible();
    await expect(summary).toBeFocused();
    await summary.locator('[data-fc-validation-target]').click();
    await expect(fieldEditorByLabel(form, 'Record Id')).toBeFocused();
    await expect(destructiveDialog(page)).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'idle');

    await fillDestructiveFields(form, 'FC-1002', 'QA story 4.1 rapid submit gate');
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
          <button id="first-invalid-input" data-fc-validation-field aria-invalid="true">First invalid field</button>
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

const submitDestructiveCommand = async (form: Locator): Promise<void> => {
  await waitForGeneratedFormReady(form);
  await form.getByRole('button', { name: ACTION_LABEL }).click();
};
