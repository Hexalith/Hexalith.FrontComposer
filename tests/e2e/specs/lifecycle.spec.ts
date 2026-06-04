import { expect, test } from '../fixtures/index.js';

test.describe('Story 3.4: command lifecycle UI', () => {
  test('generated compact command surfaces submitting -> acknowledged -> syncing -> confirmed', async ({
    page,
    lifecycle,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    await page.goto('/counter');
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();

    const commandId = 'batch-increment';
    const indicator = lifecycle.locator(commandId);
    await expect(indicator).toBeVisible();
    await lifecycle.expectState(commandId, 'idle');

    const form = page.locator('.fc-command-form[aria-label="Batch Increment command form"]');
    await expect(form).toBeVisible();
    await form.getByLabel('Amount').fill('2');
    await form.getByLabel('Note').fill('QA story 3.4 browser lifecycle');
    await form.getByRole('button', { name: 'Batch Increment' }).click();

    await lifecycle.expectState(commandId, 'submitting');
    await lifecycle.expectState(commandId, 'acknowledged');
    await expect(form.getByTestId('fc-acknowledged')).toBeVisible();
    await lifecycle.expectState(commandId, 'syncing');

    const terminal = await lifecycle.waitForTerminal(commandId);
    expect(terminal).toBe('confirmed');
    await expect(form.getByTestId('fc-confirmed')).toBeVisible();
    await expect(form).toBeVisible();
  });
});
