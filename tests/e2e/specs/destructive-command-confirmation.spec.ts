import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
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

    await page.getByTestId('fc-destructive-cancel').click();

    await expect(dialog).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'idle');
    await expect(form.getByTestId('fc-confirmed')).toHaveCount(0);

    await submitDestructiveCommand(form);
    await expect(dialog).toBeVisible();

    await dialog.focus();
    await page.keyboard.press('Escape');

    await expect(dialog).toHaveCount(0);
    await lifecycle.expectState(COMMAND_ID, 'idle');
    await expect(form.getByRole('button', { name: ACTION_LABEL })).toBeEnabled();
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
    await fillDestructiveFields(form, '', 'QA story 4.1 validation blocks dialog');
    await submitDestructiveCommand(form);

    await expect(page.getByText('The Record Id field is required.')).toBeVisible();
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
  await form.getByLabel('Record Id').fill(recordId);
  await form.getByLabel('Record Id').blur();
  await form.getByLabel('Reason').fill(reason);
  await form.getByLabel('Reason').blur();
};

const submitDestructiveCommand = async (form: Locator): Promise<void> => {
  await form.getByRole('button', { name: ACTION_LABEL }).click();
};
