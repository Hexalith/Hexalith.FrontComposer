import { writeFile } from 'node:fs/promises';

import { expect, test } from '../fixtures/index.js';

const VIEW_KEY = 'Counter:Counter.Domain.CounterProjection';
const INDICATOR_COPY = 'New item. It may not match current filters yet.';
const INDICATOR_ARIA_LABEL = 'New item added outside current filters';

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
    expect(tenant.userId).toBeTruthy();
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const html = page.locator('html');
    await expect(html).toHaveAttribute('lang', 'en');
    await expect(grid).toBeVisible();
    await expect(catchUp).toHaveCount(1);
    await expect(grid.getByText(exactKey, { exact: true })).toHaveCount(0);
    const uiLanguage = (await html.getAttribute('lang'))?.trim() ?? '';
    const gridWasRenderedBeforeDispatch = await grid.isVisible();
    const exactKeyCountBeforeDispatch = await grid.getByText(exactKey, { exact: true }).count();

    await createForm.getByLabel('Counter key').fill(exactKey);
    await createForm.getByLabel('Initial Value').fill('41');
    await createForm.getByRole('button', { name: 'Create Counter', exact: true }).click();

    await expect(indicator).toHaveCount(1);
    await expect(indicator).toHaveAttribute('role', 'status');
    await expect(indicator).toHaveAttribute('aria-live', 'polite');
    await expect(indicator).toHaveAttribute('aria-label', INDICATOR_ARIA_LABEL);
    await expect(indicator).toHaveText(INDICATOR_COPY);
    const createAnnouncement = (await indicator.textContent())?.trim() ?? '';
    const createAriaLabel = (await indicator.getAttribute('aria-label'))?.trim() ?? '';
    const indicatorRole = (await indicator.getAttribute('role'))?.trim() ?? '';
    const indicatorAriaLive = (await indicator.getAttribute('aria-live'))?.trim() ?? '';
    const createIndicatorVisibleCount = await indicator.count();
    expect(createAnnouncement).toBe(INDICATOR_COPY);
    expect(createAriaLabel).toBe(INDICATOR_ARIA_LABEL);
    await expect(catchUp).toHaveAttribute('data-captured', '1');
    await expect(catchUp).toHaveAttribute('data-published', '1');
    await expect(catchUp).toHaveAttribute('data-received', '1');
    const catchUpCaptured = Number(await catchUp.getAttribute('data-captured'));
    const catchUpPublished = Number(await catchUp.getAttribute('data-published'));
    const catchUpReceived = Number(await catchUp.getAttribute('data-received'));
    const createdRow = grid.getByRole('group').filter({ hasText: exactKey });
    const createdCount = createdRow.locator('strong');
    await expect(createdRow).toBeVisible();
    await expect(createdCount).toHaveText('41');
    const materializedCountAfterCreate = Number((await createdCount.textContent())?.trim());
    await expect(indicator).toHaveCount(0);
    const createIndicatorCountAfterMaterialization = await indicator.count();

    // Two provider-resolved updates reach the same target before the first projection refresh.
    // The live DOM can prove that one localized indicator remains visible through the overlap;
    // internal composite-key provenance remains in the bUnit composition proof.
    await updateForm.getByLabel('Counter key').fill(exactKey);
    await updateForm.getByLabel('Amount').fill('1');
    await updateForm.getByRole('button', { name: 'Update Counter', exact: true }).click();
    await expect(indicator).toHaveCount(1);
    const firstUpdateAnnouncement = (await indicator.textContent())?.trim() ?? '';
    expect(firstUpdateAnnouncement).toBe(INDICATOR_COPY);
    const firstUpdateIndicatorElement = await indicator.elementHandle();
    expect(firstUpdateIndicatorElement).not.toBeNull();
    const overlapIndicatorCountBeforeSecondDispatch = await indicator.count();
    await updateForm.getByLabel('Amount').fill('2');
    await expect(createdCount).toHaveText('41');
    const materializedCountBeforeSecondDispatch = Number((await createdCount.textContent())?.trim());
    await updateForm.getByRole('button', { name: 'Update Counter', exact: true }).click();
    await expect(indicator).toHaveCount(1);
    await expect(indicator).toHaveText(firstUpdateAnnouncement);
    const secondUpdateIndicatorElement = await indicator.elementHandle();
    expect(secondUpdateIndicatorElement).not.toBeNull();
    if (!firstUpdateIndicatorElement || !secondUpdateIndicatorElement) {
      throw new Error('The overlapping update indicator element was not retained for DOM correlation.');
    }
    const overlapIndicatorElementRetained = await firstUpdateIndicatorElement.evaluate(
      (element, observedElement) => element.isConnected && element === observedElement,
      secondUpdateIndicatorElement,
    );
    expect(overlapIndicatorElementRetained).toBe(true);
    const overlapIndicatorCountAfterSecondDispatch = await indicator.count();
    const overlapIndicatorCopyAfterSecondDispatch = (await indicator.textContent())?.trim() ?? '';
    await expect(createdCount).toHaveText('44');
    const materializedCountAfterOverlappingUpdates = Number((await createdCount.textContent())?.trim());
    await expect(indicator).toHaveCount(0);
    const overlappingIndicatorCountAfterMaterialization = await indicator.count();

    // A later update proves the provider path against a row that was absent before dispatch and
    // is now already rendered. Its projection refresh dismisses the fresh-row announcement.
    await updateForm.getByLabel('Counter key').fill(exactKey);
    await updateForm.getByLabel('Amount').fill('8');
    await updateForm.getByRole('button', { name: 'Update Counter', exact: true }).click();
    await expect(indicator).toHaveCount(1);
    const laterUpdateIndicatorVisibleCount = await indicator.count();
    await expect(createdCount).toHaveText('52');
    const materializedCountAfterLaterUpdate = Number((await createdCount.textContent())?.trim());
    await expect(indicator).toHaveCount(0);
    const laterUpdateIndicatorCountAfterMaterialization = await indicator.count();

    await page.screenshot({ path: screenshotPath, fullPage: true });
    const dispatchedCommands = [
      { commandType: 'Counter.Domain.CreateCounterCommand', counterId: exactKey, initialValue: '[REDACTED]' },
      { commandType: 'Counter.Domain.UpdateCounterCommand', counterId: exactKey, amount: '[REDACTED]' },
      { commandType: 'Counter.Domain.UpdateCounterCommand', counterId: exactKey, amount: '[REDACTED]' },
      { commandType: 'Counter.Domain.UpdateCounterCommand', counterId: exactKey, amount: '[REDACTED]' },
    ];
    const evidence = {
      schemaVersion: 1,
      story: '9.8',
      candidateCommit: process.env.FC_E2E_CANDIDATE_COMMIT ?? 'unknown',
      baseUrl: process.env.BASE_URL ?? 'unknown',
      tenantScope: tenant.tenantId,
      userScope: tenant.userId,
      uiLanguage,
      viewKey: VIEW_KEY,
      exactTargetKey: exactKey,
      dispatchedCommands,
      observed: {
        gridWasRenderedBeforeDispatch,
        exactKeyCountBeforeDispatch,
        exactKeyMatchedDispatchCount: dispatchedCommands.filter((command) => command.counterId === exactKey).length,
        tenantScopeLength: tenant.tenantId.length,
        userScopeLength: tenant.userId.length,
        createIndicatorVisibleCount,
        indicatorRole,
        indicatorAriaLive,
        indicatorAriaLabel: createAriaLabel,
        createIndicatorCopy: createAnnouncement,
        catchUpCaptured,
        catchUpPublished,
        catchUpReceived,
        materializedCountAfterCreate,
        createIndicatorCountAfterMaterialization,
        overlapIndicatorCountBeforeSecondDispatch,
        materializedCountBeforeSecondDispatch,
        overlapIndicatorCountAfterSecondDispatch,
        overlapIndicatorElementRetained,
        overlapIndicatorCopyBeforeSecondDispatch: firstUpdateAnnouncement,
        overlapIndicatorCopyAfterSecondDispatch,
        materializedCountAfterOverlappingUpdates,
        overlappingIndicatorCountAfterMaterialization,
        laterUpdateIndicatorVisibleCount,
        materializedCountAfterLaterUpdate,
        laterUpdateIndicatorCountAfterMaterialization,
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
