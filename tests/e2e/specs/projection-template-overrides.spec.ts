import { expect, test } from '../fixtures/index.js';
import { getSpecimenRoute, type SpecimenRoute } from '../helpers/specimen-manifest.js';

test.describe('Level 2 projection-template overrides', () => {
  test('type specimen renders registered projection templates before generated default bodies', async ({ page }) => {
    await gotoSpecimen(page, getSpecimenRoute('type'));

    const badgeGrid = page.getByTestId('fc-badge-grid');
    const counterGrid = page.getByTestId('fc-generated-counter-grid');

    await assertProjectionTemplateRendered(badgeGrid, ['Status']);
    await assertProjectionTemplateRendered(counterGrid, ['Count']);
    await expect(page.locator('.fc-specimen-generated-template')).toHaveCount(2);
  });

  test('data-formatting specimen keeps generated field renderers inside the registered template', async ({ page }) => {
    await gotoSpecimen(page, getSpecimenRoute('data-formatting'));

    await assertProjectionTemplateRendered(page.getByTestId('fc-generated-formatting-grid'), [
      'TotalOrders',
      'SubmittedAt',
      'LastSync',
      'Budget',
      'LifecycleState',
      'OpaquePayload',
    ]);
  });
});

const gotoSpecimen = async (page: import('@playwright/test').Page, route: SpecimenRoute): Promise<void> => {
  await page.goto(route.path);
  await expect(page.locator(route.readySelector), `${route.path} missing ready marker`).toBeVisible();
};

const assertProjectionTemplateRendered = async (
  projectionRoot: import('@playwright/test').Locator,
  expectedFieldKeys: string[],
): Promise<void> => {
  const template = projectionRoot.locator('.fc-specimen-generated-template');
  await expect(template).toHaveCount(1);
  await expect(template).toBeVisible();

  for (const fieldKey of expectedFieldKeys) {
    await expect(
      template.locator(`[data-fc-field="${fieldKey}"]`).first(),
      `${fieldKey} field is missing inside the Level 2 template`,
    ).toBeVisible();
  }
};
