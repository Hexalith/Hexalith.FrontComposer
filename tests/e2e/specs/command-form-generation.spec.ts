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
    await expectFrameworkIdentityHidden(compactForm);

    await page.getByRole('button', { name: 'Increment' }).click();
    const inlineForm = commandForm(page, 'Increment command form');
    await expect(inlineForm).toBeVisible();
    await expect(inlineForm.getByLabel('Amount')).toBeVisible();
    await expectFrameworkIdentityHidden(inlineForm);
    await page.getByRole('button', { name: 'Cancel' }).click();

    await page.getByRole('link', { name: 'Configure Counter' }).click();
    await expect(page).toHaveURL(/\/commands\/Counter\/ConfigureCounterCommand/);

    const fullPageForm = commandForm(page, 'Configure Counter command form');
    await expect(fullPageForm).toBeVisible();
    for (const label of ['Name', 'Description', 'Initial Value', 'Max Value', 'Category']) {
      await expect(fullPageForm.getByLabel(label), `${label} field is missing`).toBeVisible();
    }
    await expectFrameworkIdentityHidden(fullPageForm);
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

test.describe('Story 3.2: command form density rule', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
  });

  test('non-derivable field count selects inline, compact inline, and full-page surfaces', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoCounter(page);

    await expect(page.locator('.inline-section .fc-expand-in-row')).toHaveCount(0);
    await expect(page.locator('.inline-section [aria-label="breadcrumb"]')).toHaveCount(0);

    await page.getByRole('button', { name: 'Increment' }).click();
    const inlinePopover = page.locator('.inline-section .fc-popover');
    await expect(inlinePopover).toBeVisible();
    await expect(inlinePopover.locator(COMMAND_FORM)).toHaveAttribute('aria-label', 'Increment command form');
    await expect(inlinePopover.getByLabel('Amount')).toBeVisible();
    await expectFrameworkIdentityHidden(inlinePopover);
    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(inlinePopover).not.toBeVisible();

    const compactSection = page.locator('.command-section');
    const compactCard = compactSection.locator('.fc-expand-in-row');
    await expect(compactCard).toBeVisible();
    await expect(compactCard.locator(COMMAND_FORM)).toHaveAttribute('aria-label', 'Batch Increment command form');
    await expect(compactCard.getByLabel('Amount')).toBeVisible();
    await expect(compactCard.getByLabel('Note')).toBeVisible();
    await expect(compactCard.getByLabel('Effective Date')).toBeVisible();
    await expectFrameworkIdentityHidden(compactCard);
    await expect(compactSection.locator('[aria-label="breadcrumb"]')).toHaveCount(0);

    await page.getByRole('link', { name: 'Configure Counter' }).click();
    await expect(page).toHaveURL(/\/commands\/Counter\/ConfigureCounterCommand/);
    const breadcrumb = page.getByRole('navigation', { name: 'breadcrumb' });
    await expect(breadcrumb).toBeVisible();
    const counterReturnLink = breadcrumb.getByRole('link', { name: /counter/i });
    await expect(counterReturnLink).toHaveAttribute('href', /\/counter/);
    await expect(page.locator('.fc-expand-in-row')).toHaveCount(0);

    const fullPageForm = commandForm(page, 'Configure Counter command form');
    await expect(fullPageForm).toBeVisible();
    for (const label of ['Name', 'Description', 'Initial Value', 'Max Value', 'Category']) {
      await expect(fullPageForm.getByLabel(label), `${label} field is missing`).toBeVisible();
    }
    await expectFrameworkIdentityHidden(fullPageForm);

    await counterReturnLink.click();
    await expect(page).toHaveURL(/\/counter$/);
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
  });
});

test.describe('Story 3.3: FC-CMD pending identity and correlation contract', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
  });

  test('generated compact form keeps identity framework-owned while command reaches pending confirmation', async ({
    page,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoCounter(page);

    const compactForm = commandForm(page, 'Batch Increment command form');
    await expect(compactForm).toBeVisible();
    await expectFrameworkIdentityHidden(compactForm);

    await fillField(compactForm, 'Amount', '3');
    await fillField(compactForm, 'Note', 'QA story 3.3 identity contract');

    await compactForm.getByRole('button', { name: 'Batch Increment' }).click();

    await expect(compactForm.getByText(/Submitting/u)).toBeVisible();
    await expect(compactForm.getByTestId('fc-confirmed')).toBeVisible();
    await expect(compactForm.getByTestId('fc-idempotent')).toHaveCount(0);
    await expectFrameworkIdentityHidden(compactForm);
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

const expectFrameworkIdentityHidden = async (root: Locator): Promise<void> => {
  for (const label of ['MessageId', 'CorrelationId', 'TenantId', 'UserId']) {
    await expect(root.getByLabel(label, { exact: true })).toHaveCount(0);
  }
};
