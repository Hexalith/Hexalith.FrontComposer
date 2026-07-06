import { expect, test } from '../fixtures/index.js';
import { expectNoBlockingAxeViolations } from '../helpers/a11y.js';
import { PageToolbarSpecimenPage } from '../page-objects/page-toolbar-specimen.page.js';

test.describe('Story 8.6: reusable page toolbar @p1 @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });

    await page.setViewportSize({ width: 1280, height: 900 });
  });

  test('search, filters, view menu, actions, and tabs are reachable end-to-end', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    const specimen = new PageToolbarSpecimenPage(page);
    await specimen.goto();

    await expect(specimen.toolbar).toBeVisible();
    await expect(specimen.toolbar).toHaveAttribute('role', 'toolbar');
    await expect(specimen.toolbar).toHaveAttribute('aria-label', 'Orders page tools');

    await expect(specimen.searchInput).toHaveValue('open');
    await specimen.searchInput.fill('urgent');
    await specimen.searchInput.blur();
    await expect(specimen.searchState).toContainText('Current search: urgent');

    await expect(specimen.filterPopover).toHaveAttribute('opened', 'false');
    await expect(specimen.filterTrigger).toHaveAttribute('aria-expanded', 'false');
    await specimen.filterTrigger.click();
    await expect(specimen.filterTrigger).toHaveAttribute('aria-expanded', 'true');
    await expect(specimen.filterPopover).toHaveAttribute('opened', 'true');
    await expect(specimen.filterContent).toBeVisible();
    await expect(specimen.filterContent).toContainText('Status: Active');

    await specimen.viewTrigger.click();
    await expect(specimen.viewDensityItem).toBeVisible();

    await expect(specimen.refreshState).toContainText('Refresh count: 0');
    await specimen.refreshButton.click();
    await expect(specimen.refreshState).toContainText('Refresh count: 1');

    await expect(specimen.tabs).toBeVisible();
    await expect(specimen.activeTabState).toContainText('Active tab: summary');
    await specimen.activityTab.click();
    await expect(specimen.activeTabState).toContainText('Active tab: activity');
  });

  test('toolbar wraps without overlap on narrow viewports', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    const specimen = new PageToolbarSpecimenPage(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await specimen.goto();

    await expect(specimen.toolbar).toBeVisible();
    await expect(specimen.searchInput).toBeVisible();
    await expect(specimen.filterTrigger).toBeVisible();
    await expect(specimen.viewTrigger).toBeVisible();
    await expect(specimen.refreshButton).toBeVisible();
    await expect(specimen.tabs).toBeVisible();

    const boxes = await Promise.all([
      specimen.searchInput.boundingBox(),
      specimen.filterTrigger.boundingBox(),
      specimen.viewTrigger.boundingBox(),
      specimen.refreshButton.boundingBox(),
    ]);

    for (const box of boxes) {
      expect(box).not.toBeNull();
      expect(box!.x).toBeGreaterThanOrEqual(0);
      expect(box!.x + box!.width).toBeLessThanOrEqual(390);
    }
  });

  test('toolbar specimen passes the blocking axe gate', async ({ page, tenant }, testInfo) => {
    expect(tenant.tenantId).toBeTruthy();

    const specimen = new PageToolbarSpecimenPage(page);
    await specimen.goto();

    await expectNoBlockingAxeViolations(page, {
      route: 'Story 8.6 page toolbar',
      include: ['[data-testid="fc-page-toolbar-specimen"]'],
      requiredSelectors: [
        '[data-testid="fc-page-toolbar"]',
        '[data-testid="fc-page-toolbar-search"]',
        '[data-testid="fc-page-toolbar-filter-trigger"]',
        '[data-testid="fc-page-toolbar-view-trigger"]',
        '[data-testid="fc-page-toolbar-tabs"]',
      ],
      artifactPath: testInfo.outputPath('axe-story-8-6-page-toolbar.json'),
    });
  });
});
