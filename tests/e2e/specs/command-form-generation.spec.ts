import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { fieldByLabel, fillFieldByLabel } from '../helpers/fluent-fields.js';

const COMMAND_FORM = '.fc-command-form';
const CONFIGURE_FIELD = (name: string): string =>
  `.fc-command-form[aria-label="Configure Counter command form"] fluent-text-input[name="${name}"]`;
// Fluent editors expose their label on the light-DOM host and on the inner shadow input; the host
// comes first in document order, so `.first()` asserts the rendered editor itself.

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
    await expect(fieldByLabel(compactForm, 'Amount').first()).toBeVisible();
    await expect(fieldByLabel(compactForm, 'Note').first()).toBeVisible();
    await expect(fieldByLabel(compactForm, 'Effective Date').first()).toBeVisible();
    await expectFrameworkIdentityHidden(compactForm);

    await page.getByRole('button', { name: 'Increment', exact: true }).click();
    const inlineForm = commandForm(page, 'Increment command form');
    await expect(inlineForm).toBeVisible();
    await expect(fieldByLabel(inlineForm, 'Amount').first()).toBeVisible();
    await expectFrameworkIdentityHidden(inlineForm);
    await page.getByRole('button', { name: 'Cancel' }).click();

    await page.getByRole('link', { name: 'Configure Counter' }).click();
    await expect(page).toHaveURL(/\/commands\/Counter\/ConfigureCounterCommand/);

    const fullPageForm = commandForm(page, 'Configure Counter command form');
    await expect(fullPageForm).toBeVisible();
    for (const label of ['Name', 'Description', 'Initial Value', 'Max Value', 'Category']) {
      await expect(fieldByLabel(fullPageForm, label).first(), `${label} field is missing`).toBeVisible();
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
    await expect(compactForm.getByTestId('fc-confirmed')).toBeVisible({ timeout: 20_000 });
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
    // Story 13.3 VG2-09 / BH3-09 — the parse error appears once in the editor's Fluent validation
    // message and as one linked summary entry; scope each assertion so neither matches the other.
    const initialValueField = fieldContainer(fullPageForm, 'Initial Value');
    await expect(initialValueField.locator('.fluent-validation-message')).toHaveCount(1);
    await expect(initialValueField.locator('.fluent-validation-message')).toHaveText('Invalid number format.');
    await expect(initialValueField.getByText('Invalid number format.', { exact: true })).toHaveCount(1);
    await expect(fullPageForm.getByTestId('fc-validation-summary')
      .getByRole('link', { name: 'Initial Value: Invalid number format.' })).toBeVisible();
    await expect(page).toHaveURL(/\/commands\/Counter\/ConfigureCounterCommand/);

    await fillField(fullPageForm, 'Initial Value', '1');
    await fullPageForm.getByRole('button', { name: 'Configure Counter' }).click();

    await expect(page).toHaveURL(/\/counter$/);
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
  });

  test('invalid generated editors expose invalid state and their error on the focusable control', async ({
    page,
    browserName,
    tenant,
  }) => {
    test.skip(browserName !== 'chromium', 'Chromium accessibility-tree evidence (Story 13.3 BH3-08).');
    expect(tenant.tenantId).toBeTruthy();

    await gotoCounter(page);
    await page.getByRole('link', { name: 'Configure Counter' }).click();
    const fullPageForm = commandForm(page, 'Configure Counter command form');
    await fillField(fullPageForm, 'Name', 'QA accessibility tree');
    await fillField(fullPageForm, 'Description', 'Accessibility tree evidence');
    await fillField(fullPageForm, 'Initial Value', 'not-a-number');
    await fillField(fullPageForm, 'Max Value', '10');
    await fillField(fullPageForm, 'Category', 'QA');
    await fullPageForm.getByRole('button', { name: 'Configure Counter' }).click();
    await expect(fullPageForm.getByTestId('fc-validation-summary')).toBeFocused();

    // VR-01 — host ARIA never crosses the Fluent shadow boundary, so the Chromium accessibility tree of
    // the focusable textbox itself must report the invalid state and describe it with its error.
    await expect.poll(async () => axField(page, CONFIGURE_FIELD('InitialValue')), { timeout: 10_000 }).toMatchObject({
      invalid: 'true',
      description: expect.stringContaining('Invalid number format.'),
    });
    await expect.poll(async () => (await axField(page, CONFIGURE_FIELD('MaxValue'))).invalid).toBeUndefined();

    // A corrected field drops its invalid state and its single error.
    await fillField(fullPageForm, 'Initial Value', '1');
    await expect.poll(async () => axField(page, CONFIGURE_FIELD('InitialValue'))).toMatchObject({ invalid: undefined });
    await expect(fieldContainer(fullPageForm, 'Initial Value').locator('.fluent-validation-message')).toHaveCount(0);
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

    await page.getByRole('button', { name: 'Increment', exact: true }).click();
    const inlinePopover = page.locator('.inline-section .fc-popover');
    await expect(inlinePopover).toBeVisible();
    await expect(inlinePopover.locator(COMMAND_FORM)).toHaveAttribute('aria-label', 'Increment command form');
    await expect(fieldByLabel(inlinePopover, 'Amount').first()).toBeVisible();
    await expectFrameworkIdentityHidden(inlinePopover);
    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(inlinePopover).not.toBeVisible();

    const compactSection = page.locator('.command-section');
    const compactCard = compactSection.locator('.fc-expand-in-row');
    await expect(compactCard).toBeVisible();
    await expect(compactCard.locator(COMMAND_FORM)).toHaveAttribute('aria-label', 'Batch Increment command form');
    await expect(fieldByLabel(compactCard, 'Amount').first()).toBeVisible();
    await expect(fieldByLabel(compactCard, 'Note').first()).toBeVisible();
    await expect(fieldByLabel(compactCard, 'Effective Date').first()).toBeVisible();
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
      await expect(fieldByLabel(fullPageForm, label).first(), `${label} field is missing`).toBeVisible();
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
    await expect(compactForm.getByTestId('fc-confirmed')).toBeVisible({ timeout: 20_000 });
    await expect(compactForm.getByTestId('fc-idempotent')).toHaveCount(0);
    await expectFrameworkIdentityHidden(compactForm);
  });
});

const gotoCounter = async (page: Page): Promise<void> => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
};

const commandForm = (page: Page, ariaLabel: string): Locator =>
  page.locator(`${COMMAND_FORM}[aria-label="${ariaLabel}"]`);

const fillField = async (root: Locator, label: string, value: string): Promise<void> => {
  await fillFieldByLabel(root, label, value);
};

// The Fluent field that wraps one generated editor: its label, editor, and messages.
const fieldContainer = (root: Locator, label: string): Locator =>
  root.locator('fluent-field', { has: root.page().locator(`label[slot="label"]:text-is("${label}")`) });

// Reads the Chromium accessibility tree (not the DOM) for the focusable control of one generated
// editor: the input inside the Fluent editor's shadow root, found from the editor host selector.
const axField = async (page: Page, hostSelector: string): Promise<{ invalid?: string; description?: string }> => {
  const session = await page.context().newCDPSession(page);
  try {
    const { result } = await session.send('Runtime.evaluate', {
      expression: `(() => {
        const host = document.querySelector(${JSON.stringify(hostSelector)});
        return host?.shadowRoot?.querySelector('input, textarea') ?? host;
      })()`,
    }) as { result: { objectId?: string } };
    if (!result.objectId) return {};
    const { nodes } = await session.send('Accessibility.getPartialAXTree', {
      objectId: result.objectId,
      fetchRelatives: false,
    }) as {
      nodes: Array<{
        description?: { value?: string };
        properties?: Array<{ name: string; value?: { value?: unknown } }>;
      }>;
    };
    const node = nodes[0];
    const invalid = node?.properties?.find((property) => property.name === 'invalid')?.value?.value;
    return {
      invalid: invalid === undefined || invalid === 'false' ? undefined : String(invalid),
      description: node?.description?.value,
    };
  }
  finally {
    await session.detach();
  }
};

const expectFrameworkIdentityHidden = async (root: Locator): Promise<void> => {
  for (const label of ['MessageId', 'CorrelationId', 'TenantId', 'UserId']) {
    await expect(root.getByLabel(label, { exact: true })).toHaveCount(0);
  }
};
