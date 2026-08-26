import { writeFile } from 'node:fs/promises';

import { expect, test } from '../fixtures/index.js';

const VIEW_KEY = 'Counter:Counter.Domain.CounterProjection';

test.describe('Epic 9 composed and live acceptance', () => {
  test('generated create and update converge through the indicator into an already-rendered grid', async ({
    page,
    tenant,
  }, testInfo) => {
    const exactKey = `counter-e9-${Date.now()}`;
    const createForm = page.locator('.fc-command-form[aria-label="Create Counter command form"]');
    const updateForm = page.locator('.fc-command-form[aria-label="Update Counter command form"]');
    const grid = page.locator(`[data-fc-datagrid="${VIEW_KEY}"]`);
    const indicator = page.getByTestId('fc-new-item-indicator');
    const catchUp = page.getByTestId('epic-9-catch-up');
    const screenshotPath = testInfo.outputPath('epic-9-live-acceptance.png');
    const evidencePath = testInfo.outputPath('epic-9-command-evidence.json');

    expect(tenant.tenantId).toBeTruthy();
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await expect(grid).toBeVisible();
    await expect(catchUp).toHaveCount(1);
    await expect(grid.getByText(exactKey, { exact: true })).toHaveCount(0);

    await createForm.getByLabel('Counter key').fill(exactKey);
    await createForm.getByLabel('Initial Value').fill('41');
    await createForm.getByRole('button', { name: 'Create Counter', exact: true }).click();

    await expect(indicator).toHaveCount(1);
    await expect(indicator).toHaveAttribute('role', 'status');
    await expect(indicator).toHaveAttribute('aria-live', 'polite');
    const createAnnouncement = (await indicator.textContent())?.trim() ?? '';
    await expect(catchUp).toHaveAttribute('data-captured', '1');
    await expect(catchUp).toHaveAttribute('data-published', '1');
    await expect(catchUp).toHaveAttribute('data-received', '1');
    const createdRow = grid.getByRole('group').filter({ hasText: exactKey });
    await expect(createdRow).toBeVisible();
    await expect(createdRow).toContainText('41');
    await expect(indicator).toHaveCount(0);

    // Two provider-resolved updates reach the same target before the first projection refresh.
    // The one visible announcement and unchanged copy prove composite-key first-wins without
    // direct indicator-state access.
    await updateForm.getByLabel('Counter key').fill(exactKey);
    await updateForm.getByLabel('Amount').fill('1');
    await updateForm.getByRole('button', { name: 'Update Counter', exact: true }).click();
    await expect(indicator).toHaveCount(1);
    const firstUpdateAnnouncement = (await indicator.textContent())?.trim() ?? '';
    await updateForm.getByLabel('Amount').fill('2');
    await updateForm.getByRole('button', { name: 'Update Counter', exact: true }).click();
    await expect(indicator).toHaveCount(1);
    await expect(indicator).toHaveText(firstUpdateAnnouncement);
    await expect(createdRow).toContainText('44');
    await expect(indicator).toHaveCount(0);

    // A later update proves the provider path against a row that was absent before dispatch and
    // is now already rendered. Its projection refresh dismisses the fresh-row announcement.
    await updateForm.getByLabel('Counter key').fill(exactKey);
    await updateForm.getByLabel('Amount').fill('8');
    await updateForm.getByRole('button', { name: 'Update Counter', exact: true }).click();
    await expect(indicator).toHaveCount(1);
    await expect(createdRow).toContainText('52');
    await expect(indicator).toHaveCount(0);

    await page.screenshot({ path: screenshotPath, fullPage: true });
    const evidence = {
      schemaVersion: 1,
      story: '9.8',
      candidateCommit: process.env.FC_E2E_CANDIDATE_COMMIT ?? 'unknown',
      baseUrl: process.env.BASE_URL ?? 'unknown',
      tenantScope: tenant.tenantId,
      userScope: tenant.userId,
      viewKey: VIEW_KEY,
      exactTargetKey: exactKey,
      dispatchedCommands: [
        { commandType: 'Counter.Domain.CreateCounterCommand', counterId: exactKey, initialValue: '[REDACTED]' },
        { commandType: 'Counter.Domain.UpdateCounterCommand', counterId: exactKey, amount: '[REDACTED]' },
      ],
      observed: {
        gridWasRenderedBeforeDispatch: true,
        exactKeyWasAbsentBeforeDispatch: true,
        firstWinsVisibleIndicatorCount: 1,
        indicatorRole: 'status',
        indicatorAriaLive: 'polite',
        createIndicatorCopy: createAnnouncement,
        firstWinsIndicatorCopy: firstUpdateAnnouncement,
        materializedCountAfterCreate: 41,
        materializedCountAfterOverlappingUpdates: 44,
        materializedCountAfterLaterUpdate: 52,
        indicatorDismissedByMaterialization: true,
      },
    };
    await writeFile(evidencePath, `${JSON.stringify(evidence, null, 2)}\n`, 'utf8');
    await testInfo.attach('epic-9-command-evidence', {
      path: evidencePath,
      contentType: 'application/json',
    });
    await testInfo.attach('epic-9-live-acceptance', {
      path: screenshotPath,
      contentType: 'image/png',
    });
  });
});
