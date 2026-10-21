import { expect, test } from '../fixtures/index.js';
import { CounterPage } from '../page-objects/counter.page.js';
import type { Page } from '@playwright/test';

async function setShellKeyFilter(page: Page, enabled: boolean): Promise<void> {
  await page.locator('.fc-shell-root').evaluate(async (element, register) => {
    const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
    const keyboard = await import(modulePath);
    if (register) keyboard.registerShellKeyFilter(element);
    else keyboard.unregisterShellKeyFilter(element);
  }, enabled);
}

async function observeTabDefault(page: Page): Promise<void> {
  await page.evaluate(() => {
    document.addEventListener('keydown', (event) => {
      if (event.key === 'Tab') {
        document.documentElement.setAttribute('data-dw91-tab-prevented', String(event.defaultPrevented));
      }
    });
  });
}

test.describe('Forward skip access after route announcement', () => {
  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 900 });
    await new CounterPage(page).goto();
    await expect(new CounterPage(page).heading).toBeFocused();
  });

  test('cold-load heading announcement leaves both skip links forward reachable', async ({ page }) => {
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to navigation', exact: true })).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.locator('#fc-nav:visible')).toBeFocused();
  });

  test('client navigation announces its heading then allows bypass to main content', async ({ page }) => {
    await page.evaluate(() => document.documentElement.setAttribute('data-dw91-document', 'original'));
    await page.getByTestId('fc-nav-context-Specimens').click();
    await expect(page).toHaveURL(/\/specimens$/);
    await expect(page.getByRole('heading', { name: 'Specimens', exact: true, level: 1 })).toBeFocused();
    await expect(page.locator('html')).toHaveAttribute('data-dw91-document', 'original');
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(page.locator('#fc-main-content')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('tab', { name: 'Overview', exact: true })).toBeFocused();
  });

  test('reverse Tab retains native traversal from the announced heading', async ({ page }) => {
    await setShellKeyFilter(page, false);
    await page.keyboard.press('Shift+Tab');
    const nativeDestination = await page.evaluateHandle(() => document.activeElement);
    await new CounterPage(page).heading.focus();
    await setShellKeyFilter(page, true);
    await observeTabDefault(page);
    await page.keyboard.press('Shift+Tab');
    await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');
    await expect.poll(() => page.evaluate((expected) => document.activeElement === expected, nativeDestination)).toBe(true);
    await nativeDestination.dispose();
  });

  for (const key of ['Shift', 'ArrowDown', 'Escape']) {
    test(`${key} without a focus move preserves forward skip access`, async ({ page }) => {
      await page.keyboard.press(key);
      await expect(new CounterPage(page).heading).toBeFocused();
      await page.keyboard.press('Tab');
      await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
    });
  }

  test('a deliberate pointer action on the heading retains native forward traversal', async ({ page }) => {
    await observeTabDefault(page);
    await new CounterPage(page).heading.click();
    await page.keyboard.press('Tab');
    // Firefox may naturally restart sequential traversal at the first link after clicking an
    // already-focused tabindex=-1 heading. Prove the shell left the browser's decision intact.
    await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');
  });

  test('unregister and repeated register preserve the shell keyboard lifecycle', async ({ page }) => {
    await observeTabDefault(page);
    for (let cycle = 0; cycle < 2; cycle += 1) {
      await setShellKeyFilter(page, false);
      await new CounterPage(page).heading.focus();
      await page.keyboard.press('Tab');
      await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');

      await new CounterPage(page).heading.focus();
      await setShellKeyFilter(page, true);
      await setShellKeyFilter(page, true);
      await page.keyboard.press('Tab');
      await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).toBeFocused();
    }
  });

  test('a heading outside main content does not arm the registered shell skip link', async ({ page }) => {
    await observeTabDefault(page);
    await page.getByRole('banner').evaluate((banner) => {
      const heading = document.createElement('h1');
      heading.tabIndex = -1;
      heading.textContent = 'Unrelated banner heading';
      banner.append(heading);
      const button = document.createElement('button');
      button.textContent = 'Banner continuation';
      banner.append(button);
      heading.focus();
    });
    await page.keyboard.press('Tab');
    await expect(page.getByRole('button', { name: 'Banner continuation', exact: true })).toBeFocused();
    await expect(page.locator('html')).toHaveAttribute('data-dw91-tab-prevented', 'false');
  });

  test('opening a shell dialog preserves its own Tab flow', async ({ page }) => {
    await page.getByTestId('fc-settings-button').click();
    await expect(page.locator('#fc-settings-heading')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('link', { name: 'Skip to content', exact: true })).not.toBeFocused();
    await expect.poll(() => page.evaluate(() =>
      document.activeElement?.closest('fluent-dialog, dialog, [aria-modal="true"]') !== null)).toBe(true);
  });
});
