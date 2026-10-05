import { expect, test } from '../fixtures/index.js';
import { fillFieldByLabel } from '../helpers/fluent-fields.js';

test.describe('Story 13.4: projection and command announcements', () => {
  test('browser offline event reaches the shared projection status', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    const status = page.getByTestId('fc-projection-connection-status');
    await expect.poll(async () => {
      await page.evaluate(() => window.dispatchEvent(new Event('offline')));
      return status.textContent();
    }).toContain('You are offline');
    await expect(status).toContainText('You are offline');
    await expect(page.getByTestId('fc-surface-status').filter({ hasText: 'You are offline' })).toHaveCount(1);

    await page.evaluate(() => window.dispatchEvent(new Event('online')));
    await expect(status).not.toContainText('You are offline');
  });

  test('a command keeps one polite status and closes with canonical confirmation', async ({ page, lifecycle, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    const form = page.locator('.fc-command-form[aria-label="Batch Increment command form"]');
    const wrapper = lifecycle.locator('batch-increment');
    const status = wrapper.getByTestId('fc-surface-status');
    await expect(status).toHaveCount(1);
    await expect(status).toHaveAttribute('aria-live', 'polite');
    await expect(status).toHaveAttribute('aria-atomic', 'true');

    await fillFieldByLabel(form, 'Amount', '2');
    await fillFieldByLabel(form, 'Note', 'QA story 13.4 status');
    await form.getByRole('button', { name: 'Batch Increment' }).click();

    const terminal = await lifecycle.waitForTerminal('batch-increment');
    expect(terminal).toBe('confirmed');
    await expect(status).toHaveText('Command confirmed.');
    await expect(wrapper.locator('[role="alert"], [aria-live="assertive"]')).toHaveCount(0);
    await expect(status).toHaveCount(1);
  });

  test('a hidden expanded row focuses the described Clear filter button once', async ({ page }) => {
    await page.goto('/__frontcomposer/specimens/type?announcementFocus=true');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    const fixture = page.getByTestId('fc-announcement-focus-fixture');
    await fixture.getByTestId('fc-focus-hide-detail').click();
    const clearFilter = fixture.getByTestId('fc-expanded-row-hidden-banner-clear');
    await expect(clearFilter).toBeFocused();
    const descriptionId = await clearFilter.getAttribute('aria-describedby');
    expect(descriptionId).toBeTruthy();
    await expect(fixture.locator(`#${descriptionId}`)).toContainText('hidden');

    const laterResult = fixture.getByTestId('fc-focus-repeat-result');
    await laterResult.click();
    await expect(laterResult).toBeFocused();
    await expect(clearFilter).toBeVisible();
  });

  test('a blocked scope focuses its replacement heading', async ({ page }) => {
    await page.goto('/__frontcomposer/specimens/type?announcementFocus=true');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();

    const fixture = page.getByTestId('fc-announcement-focus-fixture');
    await fixture.getByTestId('fc-focus-block-scope').click();
    const heading = fixture.getByTestId('fc-scope-blocked').getByRole('heading', { level: 1 });
    await expect(heading).toBeFocused();
  });
});
