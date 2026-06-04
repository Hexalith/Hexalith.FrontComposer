import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';

const COMMAND_FORM = '.fc-command-form';

test.describe('Story 3.1: generated command forms', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
  });

  test('counter sample exposes generated inline, compact, and full-page command forms', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoCounter(page);

    const compactForm = commandForm(page, 'Batch Increment command form');
    await expect(compactForm).toBeVisible();
    await expect(compactForm.getByLabel('Amount')).toBeVisible();
    await expect(compactForm.getByLabel('Note')).toBeVisible();
    await expect(compactForm.getByLabel('Effective Date')).toBeVisible();
    await expect(compactForm.getByLabel('MessageId')).toHaveCount(0);
    await expect(compactForm.getByLabel('TenantId')).toHaveCount(0);

    await page.getByRole('button', { name: 'Increment' }).click();
    const inlineForm = commandForm(page, 'Increment command form');
    await expect(inlineForm).toBeVisible();
    await expect(inlineForm.getByLabel('Amount')).toBeVisible();
    await expect(inlineForm.getByLabel('TenantId')).toHaveCount(0);
    await page.getByRole('button', { name: 'Cancel' }).click();

    await page.getByRole('link', { name: 'Configure Counter' }).click();
    await expect(page).toHaveURL(/\/commands\/Counter\/ConfigureCounterCommand/);

    const fullPageForm = commandForm(page, 'Configure Counter command form');
    await expect(fullPageForm).toBeVisible();
    for (const label of ['Name', 'Description', 'Initial Value', 'Max Value', 'Category']) {
      await expect(fullPageForm.getByLabel(label), `${label} field is missing`).toBeVisible();
    }
    await expect(fullPageForm.getByLabel('MessageId')).toHaveCount(0);
    await expect(fullPageForm.getByLabel('TenantId')).toHaveCount(0);
  });

  test('compact generated form submits and reaches confirmed lifecycle feedback', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoCounter(page);

    const compactForm = commandForm(page, 'Batch Increment command form');
    await fillField(compactForm, 'Amount', '2');
    await fillField(compactForm, 'Note', 'QA generated e2e command form');

    await compactForm.getByRole('button', { name: 'Batch Increment' }).click();

    await expect(compactForm.getByText(/Submitting/u)).toBeVisible();
    await expect(compactForm.getByTestId('fc-confirmed')).toBeVisible();
    await expect(compactForm.getByRole('button', { name: 'Batch Increment' })).toBeEnabled();
  });

  test('full-page generated form blocks invalid numbers then submits after correction', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoCounter(page);
    await page.getByRole('link', { name: 'Configure Counter' }).click();

    const fullPageForm = commandForm(page, 'Configure Counter command form');
    await fillField(fullPageForm, 'Name', 'QA Counter');
    await fillField(fullPageForm, 'Description', 'Generated command form e2e coverage');
    await fillField(fullPageForm, 'Initial Value', 'not-a-number');
    await fillField(fullPageForm, 'Max Value', '10');
    await fillField(fullPageForm, 'Category', 'QA');

    await fullPageForm.getByRole('button', { name: 'Configure Counter' }).click();
    await expect(fullPageForm.getByText('Invalid number format.')).toBeVisible();
    await expect(page).toHaveURL(/\/commands\/Counter\/ConfigureCounterCommand/);

    await fillField(fullPageForm, 'Initial Value', '1');
    await fullPageForm.getByRole('button', { name: 'Configure Counter' }).click();

    await expect(page).toHaveURL(/\/counter$/);
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
  });
});

const gotoCounter = async (page: Page): Promise<void> => {
  await page.goto('/counter');
  await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
};

const commandForm = (page: Page, ariaLabel: string): Locator =>
  page.locator(`${COMMAND_FORM}[aria-label="${ariaLabel}"]`);

const fillField = async (root: Locator, label: string, value: string): Promise<void> => {
  const field = root.getByLabel(label);
  await field.fill(value);
  await field.blur();
};
