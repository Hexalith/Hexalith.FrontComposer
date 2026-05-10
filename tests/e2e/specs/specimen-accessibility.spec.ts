import { expect, test } from '../fixtures/index.js';
import { expectNoBlockingAxeViolations } from '../helpers/a11y.js';
import {
  getSpecimenRoute,
  specimenManifest,
  validateSpecimenManifest,
  type SpecimenRoute,
} from '../helpers/specimen-manifest.js';

const consoleErrorsByPage = new WeakMap<import('@playwright/test').Page, string[]>();
const unexpectedRequestsByPage = new WeakMap<import('@playwright/test').Page, string[]>();

test.describe('FrontComposer accessibility and visual specimens', () => {
  test.use({
    locale: 'fr-FR',
    timezoneId: 'UTC',
    deviceScaleFactor: 1,
  });

  test.beforeEach(async ({ page }) => {
    const consoleErrors: string[] = [];
    const unexpectedRequests: string[] = [];

    page.on('console', (message) => {
      if (message.type() === 'error') {
        if (message.text() === 'Failed to load resource: the server responded with a status of 404 (Not Found)') {
          return;
        }

        consoleErrors.push(message.text());
      }
    });

    page.on('request', (request) => {
      const url = new URL(request.url());
      const path = url.pathname;
      const allowed = path.startsWith('/_framework/')
        || path.startsWith('/_content/')
        || path.startsWith('/_blazor')
        || path.startsWith('/css/')
        || path.startsWith('/js/')
        || path.startsWith('/__frontcomposer/specimens/')
        || path === '/Counter.Web.styles.css'
        || path === '/'
        || path === '/favicon.ico';

      if (!allowed) {
        unexpectedRequests.push(`${request.method()} ${url.pathname}`);
      }
    });

    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });

    await page.setViewportSize({ width: 1280, height: 900 });
    await page.emulateMedia({ reducedMotion: 'reduce' });

    test.info().attachments.push({
      name: 'specimen-console-and-network-guard',
      contentType: 'text/plain',
      body: Buffer.from('Console and network guards are active for specimen routes.'),
    });

    consoleErrorsByPage.set(page, consoleErrors);
    unexpectedRequestsByPage.set(page, unexpectedRequests);
  });

  test.afterEach(async ({ page }) => {
    const consoleErrors = consoleErrorsByPage.get(page) ?? [];
    const unexpectedRequests = unexpectedRequestsByPage.get(page) ?? [];
    expect(consoleErrors, `Unhandled browser console errors:\n${consoleErrors.join('\n')}`).toEqual([]);
    expect(unexpectedRequests, `Unexpected network calls:\n${unexpectedRequests.join('\n')}`).toEqual([]);
  });

  test('manifest names owned nonblank specimen routes and exactly six visual combinations', () => {
    validateSpecimenManifest();
  });

  for (const route of specimenManifest.routes) {
    test(`${route.name} specimen is nonblank and passes blocking axe gate`, async ({ page }, testInfo) => {
      await gotoSpecimen(page, route);
      await expectNoBlockingAxeViolations(page, {
        route: route.path,
        include: [route.landmarkRoot],
        requiredSelectors: [route.readySelector, ...route.requiredSections],
        artifactPath: testInfo.outputPath(`axe-${route.name}.json`),
      });
    });
  }

  test('type specimen renders required shell, density, badge, lifecycle, detail, and nav surfaces', async ({ page }) => {
    const route = getSpecimenRoute('type');
    await gotoSpecimen(page, route);

    await expect(page.getByTestId('fc-type-slot-display')).toContainText('FrontComposer Display');
    await expect(page.getByTestId('fc-token-theme-light')).toBeVisible();
    await expect(page.getByTestId('fc-token-theme-dark')).toBeVisible();
    await expect(page.getByTestId('fc-density-state-compact')).toBeVisible();
    await expect(page.getByTestId('fc-density-state-comfortable')).toBeVisible();
    await expect(page.getByTestId('fc-density-state-roomy')).toBeVisible();
    await expect(page.getByTestId('fc-status-badge')).toHaveCount(6);
    await expect(page.getByTestId('fc-lifecycle-idle')).toContainText('Ready to submit');
    await expect(page.getByTestId('fc-lifecycle-confirmed-rejected')).toContainText('Terminal confirmation');
    await expect(page.getByTestId('fc-expanded-detail')).toContainText('fc-correlation-0002');
    await expect(page.getByRole('navigation', { name: 'Specimen multi-level navigation' })).toBeVisible();
  });

  test('data-formatting specimen renders deterministic text and accessible names', async ({ page }) => {
    const route = getSpecimenRoute('data-formatting');
    await gotoSpecimen(page, route);

    await expect(page.getByTestId('fc-data-formatting-specimen')).toHaveAttribute('data-culture', 'fr-FR');
    await expect(page.getByTestId('fc-format-row-locale-number')).toContainText('12 345,67');
    await expect(page.getByTestId('fc-format-row-absolute-timestamp')).toContainText('15/01/2026 13:45 UTC');
    await expect(page.getByTestId('fc-format-row-relative-timestamp')).toContainText('3 hours ago');
    await expect(page.getByTestId('fc-format-row-null-value')).toContainText('—');
    await expect(page.getByTestId('fc-format-row-currency')).toContainText('1 234,50 EUR');
    await expect(page.getByTestId('fc-format-row-boolean')).toContainText('Yes');
    await expect(page.getByLabel('Budget: 1 234,50 EUR')).toBeVisible();
  });

  test('keyboard flow reaches skip link, controls, grid, command form, detail, and nav without traps', async ({ page }) => {
    const route = getSpecimenRoute('type');
    await gotoSpecimen(page, route);

    await page.getByTestId('fc-specimen-skip').focus();
    await expect(page.locator(':focus')).toHaveAttribute('data-testid', 'fc-specimen-skip');

    const expected = [
      'fc-theme-control-light',
      'fc-theme-control-dark',
      'fc-density-control-compact',
      'fc-density-control-comfortable',
      'fc-density-control-roomy',
      'fc-command-input',
      'fc-command-number',
      'fc-command-submit',
      'fc-command-cancel',
      'fc-expanded-detail-summary',
      'fc-nav-foundation',
      'fc-nav-group-generated',
    ];

    for (const testId of expected) {
      await page.keyboard.press('Tab');
      await expect(page.locator(':focus'), `Expected focus to reach ${testId}`).toHaveAttribute('data-testid', testId);
      await expect(page.locator(':focus')).toBeInViewport();
    }

    await page.keyboard.press('Space');
    await expect(page.getByTestId('fc-nav-group-generated')).toHaveAttribute('aria-expanded', 'true');
    await page.keyboard.press('Escape');
    await expect(page.locator(':focus')).toBeVisible();
  });

  test('focus-visible indicator remains visible over lifecycle visuals', async ({ page }, testInfo) => {
    const route = getSpecimenRoute('type');
    await gotoSpecimen(page, route);
    await page.getByTestId('fc-command-submit').focus();

    const outline = await page.getByTestId('fc-command-submit').evaluate((element) => getComputedStyle(element).outlineStyle);
    expect(outline).not.toBe('none');
    await page.screenshot({ path: testInfo.outputPath('focus-type-command-submit.png'), fullPage: false });
  });

  test('forced-colors and reduced-motion states are active and perceivable', async ({ browser }) => {
    const context = await browser.newContext({
      baseURL: process.env.BASE_URL ?? 'http://127.0.0.1:5070',
      forcedColors: 'active',
      reducedMotion: 'reduce',
      ignoreHTTPSErrors: true,
    });
    const page = await context.newPage();

    const route = getSpecimenRoute('type');
    await gotoSpecimen(page, route);

    await expect(page.locator(route.readySelector)).toBeVisible();
    await expect(page.getByTestId('fc-badge-grid').getByText('Warning', { exact: true })).toBeVisible();
    await expect(page.getByTestId('fc-lifecycle-confirmed-rejected')).toContainText('rejection');
    expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches)).toBe(true);
    expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);

    await page.getByTestId('fc-command-submit').focus();
    const outlineColor = await page.getByTestId('fc-command-submit').evaluate((element) => getComputedStyle(element).outlineColor);
    expect(outlineColor).toBeTruthy();
    await context.close();
  });

  for (const scale of [1, 2, 4] as const) {
    test(`zoom and reflow keep critical controls reachable at ${scale * 100}%`, async ({ page }) => {
      const route = getSpecimenRoute('type');
      await page.setViewportSize({ width: Math.floor(1280 / scale), height: 900 });
      await gotoSpecimen(page, route);

      await page.getByTestId('fc-command-submit').scrollIntoViewIfNeeded();
      await expect(page.getByTestId('fc-command-submit')).toBeInViewport();
      await page.getByTestId('fc-multi-level-nav').scrollIntoViewIfNeeded();
      await expect(page.getByTestId('fc-multi-level-nav')).toBeInViewport();
      const horizontalOverflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(horizontalOverflow).toBeLessThanOrEqual(24);
    });
  }

  for (const combination of specimenManifest.themeDensityCombinations) {
    test(`visual baseline ${combination.theme} ${combination.density}`, async ({ page }) => {
      await page.goto(`/__frontcomposer/specimens/type?theme=${combination.theme}&density=${combination.density}`);
      await expect(page.getByTestId('fc-type-specimen')).toBeVisible();
      await expect(page).toHaveScreenshot(combination.artifact, {
        fullPage: true,
        animations: 'disabled',
      });
    });
  }

  test('production-style route exposure fails closed when specimen host configuration is absent', async ({ request }) => {
    const response = await request.get('/__frontcomposer/specimens/type?productionSmoke=true', {
      headers: { 'X-FrontComposer-Specimen-Expected': 'disabled-by-default' },
      failOnStatusCode: false,
    });

    if (process.env.HEXALITH_SPECIMEN_PRODUCTION_SMOKE === '1') {
      expect(response.status()).toBeGreaterThanOrEqual(400);
    } else {
      expect([200, 404]).toContain(response.status());
    }
  });
});

const gotoSpecimen = async (page: import('@playwright/test').Page, route: SpecimenRoute): Promise<void> => {
  await page.goto(route.path);
  await expect(page.locator(route.readySelector), `${route.path} missing ready marker`).toBeVisible();
  for (const selector of route.requiredSections) {
    await expect(page.locator(selector), `${route.path} missing ${selector}`).toBeVisible();
  }
};
