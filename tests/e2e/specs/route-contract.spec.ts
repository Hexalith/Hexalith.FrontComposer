import { expect, test } from '../fixtures/index.js';
import { ShellPage } from '../page-objects/shell.page.js';

test.describe('Story 11.7: generated command and module route contract', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });
  });

  test('module workspace palette activation reaches the generated full-page command', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await page.setViewportSize({ width: 1920, height: 900 });
    await page.goto('/counter');
    await expect(page).toHaveURL(/\/counter$/);
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();

    const shell = new ShellPage(page);
    await shell.shellRoot.waitFor();
    await shell.counterCategory.click();
    await expect(shell.counterProjectionItem).toBeVisible();
    await expect(shell.counterProjectionItem).toHaveAttribute('data-href', '/counter/counter-projection');
    await shell.counterProjectionItem.click();
    await expect(page).toHaveURL(/\/counter\/counter-projection$/);
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();

    await page.goto('/counter');
    await expect(page).toHaveURL(/\/counter$/);

    await page.getByTestId('fc-palette-trigger').click();
    await page.getByRole('searchbox').pressSequentially('Configure');
    const configureCounter = page.getByTestId('fc-palette-option').filter({ hasText: 'ConfigureCounterCommand' });
    await expect(configureCounter).toBeVisible();
    await configureCounter.click();

    await expect(page).toHaveURL(/\/commands\/Counter\/ConfigureCounterCommand$/);
    await expect(page.locator('.fc-command-form[aria-label="Configure Counter command form"]')).toBeVisible();
  });
});
