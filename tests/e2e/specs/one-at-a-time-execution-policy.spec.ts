import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';

import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { expectFieldValue, fillFieldByLabel } from '../helpers/fluent-fields.js';

const BATCH_COMMAND_ID = 'batch-increment';
const INCREMENT_COMMAND_ID = 'increment';
const CONTRACT_PATH = '../../_bmad-output/contracts/fc-cnc-one-at-a-time-execution-policy-2026-06-04.md';
const COMMAND_FORM = '.fc-command-form';

test.describe('Story 4.3: one-at-a-time execution policy', () => {
  test('FC-CNC contract records block-not-queue v1 semantics', async () => {
    const contract = await readFile(resolve(process.cwd(), CONTRACT_PATH), 'utf8');

    for (const expected of [
      'one-at-a-time',
      'per Shell circuit/user scope',
      'block and reject the later local submit',
      'must not create a client-side queue',
      'fast-follow scope',
      'PendingCommandStateService',
      'Confirmed',
      'Rejected',
      'IdempotentConfirmed',
      'NeedsReview',
    ]) {
      expect(contract).toContain(expected);
    }
  });

  test('accepted pending command blocks another generated form until terminal confirmation', async ({
    page,
    lifecycle,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoCounter(page);

    const batchForm = commandForm(page, 'Batch Increment command form');
    await fillField(batchForm, 'Amount', '2');
    await fillField(batchForm, 'Note', 'QA story 4.3 first command');
    await batchForm.getByRole('button', { name: 'Batch Increment' }).click();

    await lifecycle.expectState(BATCH_COMMAND_ID, 'syncing');

    await page.getByRole('button', { name: 'Increment' }).click();
    const incrementForm = commandForm(page, 'Increment command form');
    await expect(incrementForm).toBeVisible();
    await fillField(incrementForm, 'Amount', '7');
    await incrementForm.getByRole('button', { name: 'Increment' }).click();

    await expect(incrementForm).toContainText('Command already in progress');
    await expect(incrementForm).toContainText('A command is still waiting for confirmation.');
    await expect(incrementForm).not.toContainText(/queued|retried|submitted/iu);
    await expectFieldValue(incrementForm, 'Amount', '7');
    await expect(incrementForm.getByRole('button', { name: 'Increment' })).toBeEnabled();
    await lifecycle.expectState(INCREMENT_COMMAND_ID, 'idle');
    await lifecycle.expectState(BATCH_COMMAND_ID, 'syncing');

    await lifecycle.expectState(BATCH_COMMAND_ID, 'confirmed');

    await incrementForm.getByRole('button', { name: 'Increment' }).click();
    await expect(incrementForm.getByText(/Submitting/u)).toBeVisible();
    await lifecycle.expectState(INCREMENT_COMMAND_ID, 'confirmed');
    await expect(incrementForm.getByTestId('fc-confirmed')).toBeVisible();
  });
});

const gotoCounter = async (page: Page): Promise<void> => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
};

const commandForm = (page: Page, ariaLabel: string): Locator =>
  page.locator(`${COMMAND_FORM}[aria-label="${ariaLabel}"]`);

const fillField = async (root: Locator, label: string, value: string): Promise<void> => {
  await fillFieldByLabel(root, label, value);
};
