import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';

import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { fillFieldByLabel } from '../helpers/fluent-fields.js';

const COMMAND_ID = 'batch-increment';
const CONTRACT_PATH = '../../_bmad-output/contracts/fc-cmd-retry-degraded-state-contract-2026-06-05.md';
const COMMAND_FORM = '.fc-command-form[aria-label="Batch Increment command form"]';

test.describe('Story 4.5: retry and degraded-state handling', () => {
  test('FC-RETRY contract pins retry budget, taxonomy, and non-goals', async () => {
    const contract = await readFile(resolve(process.cwd(), CONTRACT_PATH), 'utf8');

    for (const expected of [
      'one retry after the initial attempt',
      'deterministic 250 ms delay',
      'Every attempt reuses the same `MessageId`',
      'HTTP `408`, `502`, `503`, and `504`',
      'Non-retryable outcomes include `400`, `401`, `403`, `404`, `409`, `429`',
      'Accepted commands are never re-dispatched',
      '`TimeoutActionThresholdMs`',
      '`PendingCommandPollingIntervalMs`',
      '`MaxPendingCommandPollingDurationMs`',
      'reset to `Idle`, preserve entered values',
      '`FcPendingCommandSummary` lists active pending entries',
      'aria-live="polite"',
      'No queueing, batching, background re-dispatch after acceptance',
      'automatic retry for the non-EventStore Stub dispatcher',
    ]) {
      expect(contract).toContain(expected);
    }
  });

  test('accepted slow command surfaces degraded UI while pending summary stays bounded and honest', async ({
    page,
    lifecycle,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });

    await gotoCounter(page);

    const form = batchIncrementForm(page);
    await fillFieldByLabel(form, 'Amount', '2');
    await fillFieldByLabel(form, 'Note', 'QA story 4.5 pending summary redaction token=secret tenant=demo-tenant');
    await form.getByRole('button', { name: 'Batch Increment' }).click();

    await lifecycle.expectState(COMMAND_ID, 'syncing');

    const summary = page.getByTestId('fc-pending-command-summary');
    await expect(summary).toBeVisible();
    await expect(summary).toHaveAttribute('aria-live', 'polite');
    await expect(summary).toContainText('1 pending, 0 confirmed, 0 rejected, 0 needs review');
    await expect(summary).toContainText('is still pending. EventStore accepted it and FrontComposer is checking status.');
    await expect(summary).not.toContainText('token=secret');
    await expect(summary).not.toContainText(tenant.tenantId);

    const actionPrompt = form.getByTestId('fc-action-prompt');
    await expect(actionPrompt).toBeVisible({ timeout: 8_000 });
    await expect(actionPrompt).toContainText("Action needed: the system hasn't confirmed your submission");
    await expect(summary).toContainText('1 pending, 0 confirmed, 0 rejected, 0 needs review');
    await lifecycle.expectState(COMMAND_ID, 'syncing');

    await lifecycle.expectState(COMMAND_ID, 'confirmed');
    await expect(actionPrompt).toHaveCount(0);
    await expect(summary).toContainText('0 pending, 1 confirmed, 0 rejected, 0 needs review');
    await expect(form.getByTestId('fc-confirmed')).toBeVisible();
  });
});

const gotoCounter = async (page: Page): Promise<void> => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
};

const batchIncrementForm = (page: Page): Locator => page.locator(COMMAND_FORM);
