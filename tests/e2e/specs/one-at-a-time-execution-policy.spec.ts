import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';

import type { Locator, Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { expectFieldValue, fieldEditorByLabel, fillFieldByLabel } from '../helpers/fluent-fields.js';

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

    await page.getByRole('button', { name: 'Increment', exact: true }).click();
    const incrementForm = commandForm(page, 'Increment command form');
    await expect(incrementForm).toBeVisible();
    await fillField(incrementForm, 'Amount', '7');
    const attemptedSubmit = incrementForm.getByRole('button', { name: 'Increment', exact: true });
    // Story 13.3 BH2-06 — the polite AM-20 node exists before the first blocked attempt, so its
    // first update is announced; record every text it shows to prove one speech per attempt.
    const blockedStatus = incrementForm.getByTestId('fc-command-blocked-status');
    await expect(blockedStatus).toHaveCount(1);
    await expect(blockedStatus).toHaveText('');
    await blockedStatus.evaluate((node) => {
      const texts: string[] = [];
      (window as unknown as { __fcBlockedTexts: string[] }).__fcBlockedTexts = texts;
      new MutationObserver(() => texts.push(node.textContent ?? '')).observe(node, {
        characterData: true,
        childList: true,
        subtree: true,
      });
    });
    await attemptedSubmit.click();

    await expect(incrementForm).toContainText('This command did not run. Another command is already in progress.');
    await expect(incrementForm.getByRole('status')).toHaveCount(1);
    await expect(blockedStatus).toHaveAttribute('aria-live', 'polite');
    await expect(blockedStatus).toHaveAttribute('aria-atomic', 'true');
    await expect(blockedStatus).toContainText('This command did not run. Another command is already in progress.');
    await expect(incrementForm).not.toContainText(/queued|retried|submitted/iu);
    await expectFieldValue(incrementForm, 'Amount', '7');
    await expect(attemptedSubmit).toBeEnabled();
    await expect(attemptedSubmit).toBeFocused();
    await expect(page.getByText('This command did not run. Another command is already in progress.', { exact: true })).toHaveCount(1);
    await lifecycle.expectState(INCREMENT_COMMAND_ID, 'idle');
    await lifecycle.expectState(BATCH_COMMAND_ID, 'syncing');

    // An identical repeat is announced again: the node clears, then sets the same exact copy.
    await attemptedSubmit.press('Enter');
    await expect.poll(async () => page.evaluate(() => (window as unknown as { __fcBlockedTexts: string[] }).__fcBlockedTexts
      .filter((text) => text === 'This command did not run. Another command is already in progress.').length))
      .toBe(2);
    const blockedTexts = await page.evaluate(() => (window as unknown as { __fcBlockedTexts: string[] }).__fcBlockedTexts);
    expect(blockedTexts.filter((text) => text === '').length).toBeGreaterThanOrEqual(1);
    await expect(attemptedSubmit).toBeFocused();
    await expectFieldValue(incrementForm, 'Amount', '7');
    await lifecycle.expectState(INCREMENT_COMMAND_ID, 'idle');

    // Story 13.3 BH3-05 — Enter in a field is an attempt from inside the form: focus stays in that
    // field instead of being pulled to the submit button, and the attempt is still announced once.
    const amountEditor = fieldEditorByLabel(incrementForm, 'Amount');
    await amountEditor.focus();
    await amountEditor.press('Enter');
    await expect.poll(async () => page.evaluate(() => (window as unknown as { __fcBlockedTexts: string[] }).__fcBlockedTexts
      .filter((text) => text === 'This command did not run. Another command is already in progress.').length))
      .toBe(3);
    await expect(amountEditor).toBeFocused();
    await expectFieldValue(incrementForm, 'Amount', '7');
    await lifecycle.expectState(INCREMENT_COMMAND_ID, 'idle');

    await incrementForm.getByRole('button', { name: 'View active command' }).click();
    await expect(lifecycle.locator(BATCH_COMMAND_ID).locator('[data-fc-lifecycle-heading]')).toBeFocused();

    await lifecycle.expectState(BATCH_COMMAND_ID, 'confirmed');

    // Story 13.3 BH3-04 — the focused active-lifecycle heading unmounts when its command settles;
    // focus moves to the owning form's first editable control and never falls to the document body.
    await expect(lifecycle.locator(BATCH_COMMAND_ID).locator('[data-fc-lifecycle-heading]')).toHaveCount(0);
    await expect(fieldEditorByLabel(batchForm, 'Amount')).toBeFocused();
    expect(await page.evaluate(() => document.activeElement === document.body)).toBe(false);

    await attemptedSubmit.focus();
    await attemptedSubmit.press('Enter');
    await expect(incrementForm.getByText(/Submitting/u)).toBeVisible();
    await lifecycle.expectState(INCREMENT_COMMAND_ID, 'confirmed');
    await expect(page.getByText('IncrementCommand: confirmed.', { exact: true })).toBeVisible();
    await expect(incrementForm).toBeHidden();
  });
});

test.describe('Story 13.3: View active command availability', () => {
  test('a hidden active lifecycle offers no View active command target', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    await gotoCounter(page);

    // Story 13.3 BH3-07 — a lifecycle inside a hidden zero-field form or a closed popover cannot take
    // focus, so it must not count as available; the same heading counts once it is rendered.
    const outcome = await page.evaluate(async (focusModulePath) => {
      const focus = await import(focusModulePath) as {
        hasActiveLifecycle: () => boolean;
        focusActiveLifecycle: () => boolean;
      };
      const fixture = document.createElement('div');
      fixture.innerHTML = `
        <div data-fc-command-form="true">
          <div data-fc-active-lifecycle="true" style="display:none">
            <h2 data-fc-lifecycle-heading tabindex="-1">Hidden command status</h2>
          </div>
        </div>`;
      document.body.append(fixture);
      const hiddenAvailable = focus.hasActiveLifecycle();
      const hiddenFocused = focus.focusActiveLifecycle();
      (fixture.querySelector('[data-fc-active-lifecycle]') as HTMLElement).style.display = '';
      const shownAvailable = focus.hasActiveLifecycle();
      const shownFocused = focus.focusActiveLifecycle();
      fixture.remove();
      return { hiddenAvailable, hiddenFocused, shownAvailable, shownFocused };
    }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');

    expect(outcome).toEqual({ hiddenAvailable: false, hiddenFocused: false, shownAvailable: true, shownFocused: true });
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
