import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { fillFieldByLabel } from '../helpers/fluent-fields.js';
import { getSpecimenRoute } from '../helpers/specimen-manifest.js';

const ALLOWED_COMMAND_ID = 'policy-allowed-specimen';
const DENIED_COMMAND_ID = 'policy-denied-specimen';
const ALLOWED_FORM_LABEL = 'Policy Allowed Specimen command form';
const DENIED_FORM_LABEL = 'Policy Denied Specimen command form';
const ALLOWED_ACTION_LABEL = 'Policy Allowed Specimen';
const DENIED_ACTION_LABEL = 'Policy Denied Specimen';
const COMMAND_FORM = '.fc-command-form';

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

    await gotoTypeSpecimen(page);

    const specimen = page.getByTestId('fc-policy-command-specimen');
    await expect(specimen).toBeVisible();
    await expect(specimen).toContainText('Permission required');
    await expect(specimen).toContainText(`You do not have permission to ${DENIED_ACTION_LABEL}.`);
    await expect(policyForm(page, DENIED_FORM_LABEL)).toHaveCount(0);
    await expect(page.getByTestId(`fc-lifecycle-${DENIED_COMMAND_ID}`)).toHaveCount(0);
    await expect(specimen).not.toContainText('Specimens.PolicyDenied');
    await expect(specimen).not.toContainText('PolicyDeniedSpecimenCommand');
  });
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
