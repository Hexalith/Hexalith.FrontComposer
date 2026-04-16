import { expect, test } from '../fixtures/index.js';
import { CounterPage } from '../page-objects/counter.page.js';
import { expectNoAxeViolations } from '../helpers/a11y.js';

test.describe('smoke: attribute -> generator -> render pipeline', () => {
  test('counter page renders the generated command UI', async ({ page, tenant }) => {
    // Given a tenant-scoped user lands on the Counter app
    expect(tenant.tenantId).toBeTruthy();

    // When the counter page is opened
    const counter = new CounterPage(page);
    await counter.goto();

    // Then the generated command controls are rendered
    await expect(counter.heading).toBeVisible();
    await expect(counter.incrementButton).toBeVisible();
    await expect(counter.decrementButton).toBeVisible();
    await expect(counter.currentValue).toBeVisible();
  });

  test('counter page has no WCAG 2.1 AA violations', async ({ page }) => {
    const counter = new CounterPage(page);
    await counter.goto();
    await expectNoAxeViolations(page);
  });
});
