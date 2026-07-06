import { expect, test } from '../fixtures/index.js';
import { getSpecimenRoute, type SpecimenRoute } from '../helpers/specimen-manifest.js';

test.describe('FC-TBL generated table contract', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
  });

  test('type specimen exposes generated grid envelopes, fields, status chips, and detail regions', async ({ page }) => {
    await gotoSpecimen(page, getSpecimenRoute('type'));

    await assertGeneratedGridContract(page.getByTestId('fc-badge-grid'), {
      expectedFieldKeys: ['Status'],
      expectedMinimumRowsForField: 6,
    });
    await assertGeneratedGridContract(page.getByTestId('fc-generated-counter-grid'), {
      expectedFieldKeys: ['Count'],
      expectedMinimumRowsForField: 1,
    });

    const statusChips = page.getByTestId('fc-badge-grid').getByTestId('fc-status-filter-chips');
    await expect(statusChips).toBeVisible();
    await expect(statusChips).toHaveAttribute('role', 'group');

    const expectedStatusSlots = ['Neutral', 'Info', 'Success', 'Warning', 'Danger', 'Accent'];
    for (const slot of expectedStatusSlots) {
      await expect(statusChips.locator(`[data-fc-status-chip="${slot}"]`), `${slot} chip is missing`).toBeVisible();
    }

    await expect(statusChips.locator('[data-fc-status-chip]')).toHaveCount(expectedStatusSlots.length);
    await expect(page.getByTestId('fc-badge-grid').locator('.fc-expand-in-row-detail')).toHaveAttribute('role', 'region');
    await expect(page.getByTestId('fc-generated-counter-grid').locator('.fc-expand-in-row-detail')).toHaveAttribute('role', 'region');
  });

  test('status filter chips toggle active state without removing the frozen field contract', async ({ page }) => {
    await gotoSpecimen(page, getSpecimenRoute('type'));

    const badgeGrid = page.getByTestId('fc-badge-grid');
    const warningChip = badgeGrid.locator('[data-fc-status-chip="Warning"]');

    await expect(warningChip).toHaveAttribute('aria-pressed', 'false');
    await warningChip.click();
    await expect(warningChip).toHaveAttribute('aria-pressed', 'true');
    await expect(warningChip).toHaveAttribute('data-fc-chip-active', 'true');
    await expect(badgeGrid.locator("[data-fc-field='Status']")).toHaveCount(1);
  });

  test('data-formatting specimen preserves generated column field keys and formatted cell values', async ({ page }) => {
    await gotoSpecimen(page, getSpecimenRoute('data-formatting'));

    const formattingGrid = page.getByTestId('fc-generated-formatting-grid');
    await assertGeneratedGridContract(formattingGrid, {
      expectedFieldKeys: [
        'TotalOrders',
        'SubmittedAt',
        'LastSync',
        'Budget',
        'LifecycleState',
        'OpaquePayload',
      ],
      expectedMinimumRowsForField: 1,
    });

    await expect(formattingGrid.locator("[data-fc-field='TotalOrders']").first()).toContainText(/12[\s\u202f]345,67/u);
    await expect(formattingGrid.locator("[data-fc-field='Budget']").first()).toContainText(/1[\s\u202f]234,50/u);
    await expect(formattingGrid.locator("[data-fc-field='OpaquePayload']").first()).toBeVisible();
    await expect(formattingGrid.locator('.fc-expand-in-row-detail')).toHaveAttribute('role', 'region');
  });
});

const gotoSpecimen = async (page: import('@playwright/test').Page, route: SpecimenRoute): Promise<void> => {
  await page.goto(route.path);
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await expect(page.locator(route.readySelector), `${route.path} missing ready marker`).toBeVisible();
  await expect.poll(() => page.evaluate(() => document.body.dataset.fcDensity)).toBe('compact');
};

const assertGeneratedGridContract = async (
  root: import('@playwright/test').Locator,
  options: { expectedFieldKeys: string[]; expectedMinimumRowsForField: number },
): Promise<void> => {
  const gridHost = root.locator('[data-fc-datagrid]');
  await expect(gridHost).toHaveCount(1);
  await expect(gridHost).toHaveAttribute('data-fc-datagrid', /.+:.+/);
  await expect(gridHost).toHaveClass(/(^|\s)fc-projection-grid(\s|$)/);
  await expect
    .poll(() => gridHost.evaluate((element) => getComputedStyle(element).getPropertyValue('--fc-spacing-unit').trim()))
    .toBe('2px');
  await expect(gridHost.locator('table, [role="grid"]').first()).toBeVisible();

  for (const fieldKey of options.expectedFieldKeys) {
    const cells = root.locator(`[data-fc-field="${fieldKey}"]`);
    await expect(cells, `${fieldKey} generated field contract is missing`).toHaveCount(options.expectedMinimumRowsForField);
  }
};
