import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { expectFieldValue, fieldEditorByLabel, fillFieldByLabel } from '../helpers/fluent-fields.js';

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
    await fillField(form, 'Description', 'QA most recently edited description');
    await waitForConfiguredAbandonmentThreshold(page);

    await counterBreadcrumbLink(page).click();

    const warning = abandonmentWarning(page);
    await expect(warning).toBeVisible();
    await expect(warning).not.toHaveAttribute('role', /alert|status/u);
    await expect(warning).not.toHaveAttribute('aria-live', /.+/u);
    await expect(page.getByTestId('fc-form-abandonment-stay')).toBeFocused();
    await expect(page).toHaveURL(FULL_PAGE_ROUTE);
    await expectFieldValue(form, 'Name', 'QA unsaved counter');
    await expectFieldValue(form, 'Description', 'QA most recently edited description');

    await page.getByTestId('fc-form-abandonment-stay').click();
    await expect(warning).toHaveCount(0);
    await expect(page).toHaveURL(FULL_PAGE_ROUTE);
    await expectFieldValue(form, 'Name', 'QA unsaved counter');
    await expectFieldValue(form, 'Description', 'QA most recently edited description');
    await expect(fieldEditorByLabel(form, 'Description')).toBeFocused();

    await counterBreadcrumbLink(page).click();
    await expect(warning).toBeVisible();
    await page.getByTestId('fc-form-abandonment-leave').focus();
    await page.getByTestId('fc-form-abandonment-leave').press('Escape');
    await expect(warning).toHaveCount(0);
    await expect(page).toHaveURL(FULL_PAGE_ROUTE);
    await expectFieldValue(form, 'Name', 'QA unsaved counter');
    await expectFieldValue(form, 'Description', 'QA most recently edited description');
    await expect(fieldEditorByLabel(form, 'Description')).toBeFocused();

    await counterBreadcrumbLink(page).click();
    await expect(warning).toBeVisible();
    await page.getByTestId('fc-form-abandonment-leave').click();

    await expect(page).toHaveURL(/\/counter$/);
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
    await expect(abandonmentWarning(page)).toHaveCount(0);
  });

  test('deferred edited-origin return preserves a reopened warning action', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await gotoConfigureCounter(page);

    const keepsWarningFocus = await page.evaluate(async (focusModulePath) => {
      const focus = await import(focusModulePath) as {
        captureEditedOrigin: (root: HTMLElement, fieldName: string, sequence: number) => boolean;
        restoreEditedOrigin: (root: HTMLElement) => void;
      };
      const root = document.querySelector<HTMLElement>('[data-fc-abandonment-root]');
      if (!root) throw new Error('The generated abandonment root was not rendered.');
      if (!focus.captureEditedOrigin(root, 'Description', 1000)) throw new Error('The edited origin was not captured.');

      const card = document.createElement('div');
      const warning = document.createElement('section');
      warning.setAttribute('data-testid', 'fc-form-abandonment-warning');
      const stay = document.createElement('button');
      stay.type = 'button';
      stay.textContent = 'Stay';
      warning.append(stay);
      card.append(warning);
      root.before(card);
      stay.focus();
      focus.restoreEditedOrigin(root);
      await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
      return document.activeElement === stay;
    }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');

    expect(keepsWarningFocus).toBe(true);
  });

  test('a sibling guard warning does not suppress this form edited-origin return', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await gotoConfigureCounter(page);

    await page.evaluate(async (focusModulePath) => {
      const focus = await import(focusModulePath) as {
        captureEditedOrigin: (root: HTMLElement, fieldName: string, sequence: number) => boolean;
        restoreEditedOrigin: (root: HTMLElement) => void;
      };
      const root = document.querySelector<HTMLElement>('[data-fc-abandonment-root]');
      if (!root) throw new Error('The generated abandonment root was not rendered.');
      if (!focus.captureEditedOrigin(root, 'Description', 1000)) throw new Error('The edited origin was not captured.');

      const siblingRoot = document.createElement('div');
      siblingRoot.setAttribute('data-fc-abandonment-root', '');
      const card = document.createElement('div');
      const warning = document.createElement('section');
      warning.setAttribute('data-testid', 'fc-form-abandonment-warning');
      const stay = document.createElement('button');
      stay.type = 'button';
      stay.textContent = 'Sibling Stay';
      warning.append(stay);
      card.append(warning);
      root.after(card, siblingRoot);
      stay.focus();
      focus.restoreEditedOrigin(root);
      await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
    }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');

    await expect(fieldEditorByLabel(configureCounterForm(page), 'Description')).toBeFocused();
  });

  test('removed edited control restores focus to the marked full-page command heading', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoConfigureCounter(page);
    const form = configureCounterForm(page);
    const editor = fieldEditorByLabel(form, 'Name');
    await editor.focus();

    await page.evaluate(async (focusModulePath) => {
      const focus = await import(focusModulePath) as {
        captureEditedOrigin: (root: HTMLElement, fieldName: string) => void;
        restoreEditedOrigin: () => void;
      };
      const root = document.querySelector<HTMLElement>('[data-fc-abandonment-root]');
      if (!root || !(document.activeElement instanceof HTMLElement)) {
        throw new Error('The generated abandonment form did not expose an editable origin.');
      }

      focus.captureEditedOrigin(root, 'Name');
      const active = document.activeElement;
      const fieldHost = active.closest('fluent-text-input') ?? active;
      fieldHost.remove();
      focus.restoreEditedOrigin();
      await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
    }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');

    const formHeading = page.locator('h1[data-fc-form-heading="true"]');
    await expect(formHeading).toHaveText('Configure Counter');
    await expect(formHeading).toBeFocused();
  });

  test('sequenced capture ignores a stale earlier edit after focus moves to a warning action', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoConfigureCounter(page);
    const form = configureCounterForm(page);
    const staleCaptureAccepted = await page.evaluate(async (focusModulePath) => {
      const focus = await import(focusModulePath) as {
        captureEditedOrigin: (root: HTMLElement, fieldName: string, sequence: number) => boolean;
        restoreEditedOrigin: (root?: HTMLElement) => void;
      };
      const root = document.querySelector<HTMLElement>('[data-fc-abandonment-root]');
      if (!root) {
        throw new Error('The generated abandonment root was not rendered.');
      }

      focus.captureEditedOrigin(root, 'Name', 1);
      focus.captureEditedOrigin(root, 'Description', 2);
      document.querySelector<HTMLElement>('[data-testid="fc-settings-button"]')?.focus();
      const staleAccepted = focus.captureEditedOrigin(root, 'Name', 1);
      focus.restoreEditedOrigin(root);
      await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
      return staleAccepted;
    }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');

    expect(staleCaptureAccepted).toBe(false);
    await expect(fieldEditorByLabel(form, 'Description')).toBeFocused();
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
