import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { expectFieldValue, fillFieldByLabel } from '../helpers/fluent-fields.js';

const COMMAND_FORM = '.fc-command-form';
const FULL_PAGE_ROUTE = /\/commands\/Counter\/ConfigureCounterCommand/;
const FORM_LABEL = 'Configure Counter command form';
const FORM_ABANDONMENT_THRESHOLD_SECONDS = 5;
const SERVER_CLOCK_SLOP_MS = 1500;

test.describe('Story 4.2: unsaved full-page command form abandonment guard', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
  });

  test('clean generated full-page command form navigates without warning', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoConfigureCounter(page);

    await counterBreadcrumbLink(page).click();

    await expect(abandonmentWarning(page)).toHaveCount(0);
    await expect(page).toHaveURL(/\/counter$/);
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
  });

  test('dirty generated full-page command form below threshold navigates without warning', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoConfigureCounter(page);
    const form = configureCounterForm(page);
    await fillField(form, 'Name', 'QA below threshold');

    await counterBreadcrumbLink(page).click();

    await expect(abandonmentWarning(page)).toHaveCount(0);
    await expect(page).toHaveURL(/\/counter$/);
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
  });

  test('dirty generated full-page command form after threshold supports stay, Escape, and leave', async ({
    page,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoConfigureCounter(page);
    const form = configureCounterForm(page);
    await fillField(form, 'Name', 'QA unsaved counter');
    await waitForConfiguredAbandonmentThreshold(page);

    await counterBreadcrumbLink(page).click();

    const warning = abandonmentWarning(page);
    await expect(warning).toBeVisible();
    await expect(page).toHaveURL(FULL_PAGE_ROUTE);
    await expectFieldValue(form, 'Name', 'QA unsaved counter');

    await page.getByTestId('fc-form-abandonment-stay').click();
    await expect(warning).toHaveCount(0);
    await expect(page).toHaveURL(FULL_PAGE_ROUTE);
    await expectFieldValue(form, 'Name', 'QA unsaved counter');

    await counterBreadcrumbLink(page).click();
    await expect(warning).toBeVisible();
    await page.getByTestId('fc-form-abandonment-stay').press('Escape');
    await expect(warning).toHaveCount(0);
    await expect(page).toHaveURL(FULL_PAGE_ROUTE);
    await expectFieldValue(form, 'Name', 'QA unsaved counter');

    await counterBreadcrumbLink(page).click();
    await expect(warning).toBeVisible();
    await page.getByTestId('fc-form-abandonment-leave').click();

    await expect(page).toHaveURL(/\/counter$/);
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
    await expect(abandonmentWarning(page)).toHaveCount(0);
  });
});

const gotoConfigureCounter = async (page: Page): Promise<void> => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();

  await page.getByRole('link', { name: 'Configure Counter' }).click();
  await expect(page).toHaveURL(FULL_PAGE_ROUTE);
  await expect(configureCounterForm(page)).toBeVisible();
};

const configureCounterForm = (page: Page): Locator =>
  page.locator(`${COMMAND_FORM}[aria-label="${FORM_LABEL}"]`);

const counterBreadcrumbLink = (page: Page): Locator =>
  page.getByRole('navigation', { name: 'breadcrumb' }).getByRole('link', { name: /counter/i });

const abandonmentWarning = (page: Page): Locator =>
  page.getByTestId('fc-form-abandonment-warning');

const fillField = async (root: Locator, label: string, value: string): Promise<void> => {
  await fillFieldByLabel(root, label, value);
};

const waitForConfiguredAbandonmentThreshold = async (page: Page): Promise<void> => {
  const deadline = await page.evaluate(
    (thresholdMs) => performance.now() + thresholdMs,
    FORM_ABANDONMENT_THRESHOLD_SECONDS * 1000 + SERVER_CLOCK_SLOP_MS);

  await page.waitForFunction(
    (target) => performance.now() >= target,
    deadline,
    { timeout: FORM_ABANDONMENT_THRESHOLD_SECONDS * 1000 + SERVER_CLOCK_SLOP_MS + 2000 });
};
