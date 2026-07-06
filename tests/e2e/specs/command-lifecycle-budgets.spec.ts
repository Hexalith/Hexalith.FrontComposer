import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';

import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';

const COMMAND_ID = 'batch-increment';
const CONTRACT_PATH = '../../_bmad-output/contracts/fc-cmd-command-budget-contract-2026-06-04.md';

test.describe('Story 3.6: command lifecycle budgets', () => {
  test('budget contract records the v1 defaults and explicit retry budget', async () => {
    const contract = await readFile(resolve(process.cwd(), CONTRACT_PATH), 'utf8');

    for (const expected of [
      'SyncPulseThresholdMs = 300',
      'StillSyncingThresholdMs = 2_000',
      'TimeoutActionThresholdMs = 10_000',
      'PendingCommandPollingIntervalMs = 1_000',
      'MaxPendingCommandPollingDurationMs = 120_000',
      'MaxPendingCommandPollingPerTick = 25',
      'MaxPendingCommandEntries = 100',
      'Automatic client retry budget | `0`',
    ]) {
      expect(contract).toContain(expected);
    }

    expect(contract).toContain('Retry-After: 1');
    expect(contract).toContain('resolves it to `PendingCommandStatus.NeedsReview`');
  });

  test.describe('browser workflow', () => {
    test.beforeEach(async ({ page }) => {
      await page.addInitScript(() => {
        window.localStorage.clear();
        window.sessionStorage.clear();
      });
    });

    test('degraded action prompt is non-terminal and later terminal confirmation replaces it', async ({
      page,
      lifecycle,
      tenant,
    }) => {
      expect(tenant.tenantId).toBeTruthy();

      await gotoCounter(page);

      const form = commandForm(page);
      await form.getByLabel('Amount').fill('2');
      await form.getByLabel('Note').fill('QA story 3.6 degraded browser budget');
      await form.getByRole('button', { name: 'Batch Increment' }).click();

      await lifecycle.expectState(COMMAND_ID, 'syncing');

      const actionPrompt = form.getByTestId('fc-action-prompt');
      await expect(actionPrompt).toBeVisible({ timeout: 8_000 });
      await expect(actionPrompt).toContainText("Action needed: the system hasn't confirmed your submission");
      await expect(actionPrompt.getByRole('button', { name: 'Start over' })).toBeVisible();
      await expect(lifecycle.locator(COMMAND_ID)).toHaveAttribute('data-lifecycle-state', 'syncing');
      await expect(form).toBeVisible();

      await expect(form.getByTestId('fc-confirmed')).toBeVisible({ timeout: 5_000 });
      await lifecycle.expectState(COMMAND_ID, 'confirmed');
      await expect(actionPrompt).toHaveCount(0);
      await expect(form).toBeVisible();
    });
  });
});

const gotoCounter = async (page: Page): Promise<void> => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
};

const commandForm = (page: Page): Locator =>
  page.locator('.fc-command-form[aria-label="Batch Increment command form"]');
