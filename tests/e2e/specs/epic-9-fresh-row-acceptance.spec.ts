import { writeFile } from 'node:fs/promises';

import { expect, test } from '../fixtures/index.js';
import { fillFieldByLabel } from '../helpers/fluent-fields.js';

const VIEW_KEY = 'Counter:Counter.Domain.CounterProjection';
const INTERACTIVE_READY_TIMEOUT_MS = 30_000;

test.describe('Epic 9 composed and live acceptance', () => {
  test.skip(!process.env.PLAYWRIGHT_SKIP_WEBSERVER,
    'Exact AM-21 counts need the Aspire live-proof timing; the stub server splits the overlapping updates.');

  test('generated create and update converge through the indicator into an already-rendered grid', async ({
    page,
    tenant,
  }, testInfo) => {
    const exactKey = `counter-e9-${Date.now()}`;
    const indicatorCopy = `Updated row: ${exactKey}.`;
    const createForm = page.locator('.fc-command-form[aria-label="Create Counter command form"]');
    const updateForm = page.locator('.fc-command-form[aria-label="Update Counter command form"]');
    const grid = page.locator(`[data-fc-datagrid="${VIEW_KEY}"]`);
    const indicator = page.getByTestId('fc-new-item-indicator');
    const catchUp = page.getByTestId('epic-9-catch-up');
    const showOverview = async () => {
      const tab = page.getByRole('tab', { name: 'Overview', exact: true });
      await tab.click();
      await expect(tab).toHaveAttribute('aria-selected', 'true');
      await expect(createForm).toBeVisible();
      await expect(updateForm).toBeVisible();
    };
    const showProjection = async () => {
      const tab = page.getByRole('tab', { name: 'Projection', exact: true });
      await tab.click();
      await expect(tab).toHaveAttribute('aria-selected', 'true');
      await expect(grid).toBeVisible();
    };
    const screenshotPath = testInfo.outputPath('epic-9-live-acceptance.png');
    const evidencePath = testInfo.outputPath('epic-9-command-evidence.json');

    expect(tenant.tenantId).toBeTruthy();
    expect(tenant.userId).toBeTruthy();
    await page.goto('/counter');
    await expect(page.locator('.fc-shell-root[data-fc-interactive="true"]')).toBeVisible({
      timeout: INTERACTIVE_READY_TIMEOUT_MS,
    });
    const html = page.locator('html');
    await expect(html).toHaveAttribute('lang', 'en');
    await showProjection();
    await expect(catchUp).toHaveCount(1);
    await expect(grid.getByText(exactKey, { exact: true })).toHaveCount(0);
    const uiLanguage = (await html.getAttribute('lang'))?.trim() ?? '';
    const gridWasRenderedBeforeDispatch = await grid.isVisible();
    const exactKeyCountBeforeDispatch = await grid.getByText(exactKey, { exact: true }).count();

    await page.evaluate(({ copy, viewKey }) => {
      const state = window as Window & { freshRowSpeech?: string[] };
      state.freshRowSpeech = [];
      const revisions = new WeakMap<Element, string>();
      const statusNodes = () => document.querySelector(`[data-fc-datagrid="${viewKey}"]`)
        ?.parentElement?.querySelectorAll('[data-testid="fc-surface-status"]') ?? [];
      for (const node of statusNodes()) {
        const revision = node.querySelector('[data-message-revision]')?.getAttribute('data-message-revision');
        if (revision) revisions.set(node, revision);
      }
      new MutationObserver(() => {
        for (const node of statusNodes()) {
          const revision = node.querySelector('[data-message-revision]')?.getAttribute('data-message-revision');
          const changed = revision && revisions.get(node) !== revision;
          if (revision) revisions.set(node, revision);
          // Exposure alone must not count a revision that was already observed while hidden.
          if (!changed || node.getAttribute('aria-live') !== 'polite' || node.closest('[hidden], [inert], [aria-hidden="true"]')
            || node.getClientRects().length === 0
            || getComputedStyle(node).visibility !== 'visible') continue;
          if (node.textContent?.trim() === copy) state.freshRowSpeech!.push(copy);
        }
      }).observe(document.body, { subtree: true, childList: true, characterData: true, attributes: true });
    }, { copy: indicatorCopy, viewKey: VIEW_KEY });
    const speechCount = () => page.evaluate(() =>
      (window as Window & { freshRowSpeech?: string[] }).freshRowSpeech?.length ?? 0);

    await showOverview();
    await fillFieldByLabel(createForm, 'Counter key', exactKey);
    await fillFieldByLabel(createForm, 'Initial Value', '41');
    await createForm.getByRole('button', { name: 'Create Counter', exact: true }).click();
    // The grid was mounted above and remains retained in its inactive panel.
    await expect(indicator).toHaveCount(1);
    expect(await speechCount()).toBe(0);
    const hiddenBeforeActivationAnnouncementCount = await speechCount();

    await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
    await showProjection();
    await expect(indicator).toBeVisible();
    await expect(indicator).toHaveCount(1);
    await expect(indicator).not.toHaveAttribute('role');
    await expect(indicator).not.toHaveAttribute('aria-live');
    await expect(indicator).not.toHaveAttribute('aria-label');
    await expect(indicator).toHaveText(indicatorCopy);
    const cueModes = await indicator.evaluate((cue) => {
      const style = getComputedStyle(cue);
      const systemProbe = document.createElement('span');
      systemProbe.style.color = 'CanvasText';
      systemProbe.style.backgroundColor = 'Canvas';
      document.body.append(systemProbe);
      const systemColor = getComputedStyle(systemProbe).color;
      const systemBackground = getComputedStyle(systemProbe).backgroundColor;
      systemProbe.remove();
      const icon = cue.querySelector('[aria-hidden="true"]');
      const svg = icon?.matches('svg') ? icon : icon?.querySelector('svg');
      const effectiveOpacity = (element: Element) => {
        let opacity = 1;
        for (let current: Element | null = element; current && cue.contains(current); current = current.parentElement) {
          opacity *= Number(getComputedStyle(current).opacity);
        }
        return opacity;
      };
      const paintedColor = (color: string) => color !== 'none' && color !== 'transparent'
        && color !== systemBackground && !/^rgba\([^)]*,\s*0(?:\.0+)?\)$/.test(color);
      const displayed = (element: Element) => {
        for (let current: Element | null = element; current && cue.contains(current); current = current.parentElement) {
          const currentStyle = getComputedStyle(current);
          if (currentStyle.display === 'none' || currentStyle.visibility !== 'visible') return false;
        }
        return true;
      };
      const shapes = svg ? Array.from(svg.querySelectorAll<SVGElement>('path, circle, rect, ellipse, line, polyline, polygon')) : [];
      const iconVisible = () => !!icon && icon.getBoundingClientRect().width > 0
        && icon.getBoundingClientRect().height > 0 && displayed(icon) && effectiveOpacity(icon) > 0
        && shapes.some((shape) => {
          const paint = getComputedStyle(shape);
          return displayed(shape) && effectiveOpacity(shape) > 0
            && ((paintedColor(paint.fill) && Number(paint.fillOpacity) > 0)
              || (paintedColor(paint.stroke) && Number(paint.strokeOpacity) > 0));
        });
      const indicatorIconVisible = iconVisible();
      const rejectsHiddenShapes = (property: string, value: string) => {
        const styles = shapes.map((shape) => shape.getAttribute('style'));
        try {
          for (const shape of shapes) shape.style.setProperty(property, value);
          return !iconVisible();
        } finally {
          shapes.forEach((shape, index) => {
            const original = styles[index];
            if (original === null) shape.removeAttribute('style');
            else shape.setAttribute('style', original!);
          });
        }
      };
      return {
        indicatorSystemBorder: style.borderInlineStartColor === systemColor
          && parseFloat(style.borderInlineStartWidth) > 0 && style.borderInlineStartStyle === 'solid',
        indicatorIconVisible,
        indicatorHiddenShapesRejected: rejectsHiddenShapes('display', 'none'),
        indicatorInvisibleShapesRejected: rejectsHiddenShapes('visibility', 'hidden'),
        indicatorForcedColors: matchMedia('(forced-colors: active)').matches,
        indicatorReducedMotion: matchMedia('(prefers-reduced-motion: reduce)').matches,
        indicatorBackgroundImage: style.backgroundImage,
        indicatorAnimationName: style.animationName,
        indicatorTransitionDuration: style.transitionDuration,
      };
    });
    expect(cueModes).toEqual({ indicatorSystemBorder: true, indicatorIconVisible: true,
      indicatorHiddenShapesRejected: true, indicatorInvisibleShapesRejected: true,
      indicatorForcedColors: true, indicatorReducedMotion: true,
      indicatorBackgroundImage: 'none', indicatorAnimationName: 'none', indicatorTransitionDuration: '0s' });
    await page.emulateMedia({ forcedColors: 'none', reducedMotion: 'no-preference' });
    const createAnnouncement = (await indicator.textContent())?.trim() ?? '';
    const createAriaLabel = (await indicator.getAttribute('aria-label'))?.trim() ?? '';
    const indicatorRole = (await indicator.getAttribute('role'))?.trim() ?? '';
    const indicatorAriaLive = (await indicator.getAttribute('aria-live'))?.trim() ?? '';
    const createIndicatorVisibleCount = await indicator.count();
    expect(createAnnouncement).toBe(indicatorCopy);
    expect(createAriaLabel).toBe('');
    const sharedStatus = grid.locator('..').locator('[data-testid="fc-surface-status"][aria-live="polite"]');
    await expect(sharedStatus).toHaveCount(1);
    await expect(sharedStatus).toBeVisible();
    await expect(sharedStatus).toHaveText(indicatorCopy);
    const sharedPoliteNodeCount = await sharedStatus.count();
    await expect.poll(speechCount).toBe(1);
    const createAnnouncementCount = await speechCount();
    await expect(catchUp).toHaveAttribute('data-captured', '1');
    await expect(catchUp).toHaveAttribute('data-published', '1');
    await expect(catchUp).toHaveAttribute('data-received', '1');
    const catchUpCaptured = Number(await catchUp.getAttribute('data-captured'));
    const catchUpPublished = Number(await catchUp.getAttribute('data-published'));
    const catchUpReceived = Number(await catchUp.getAttribute('data-received'));
    const createdRow = grid.getByRole('row').or(grid.getByRole('group')).filter({ hasText: exactKey });
    const createdCount = createdRow.locator('[data-fc-field="Count"], .counter-count-slot strong');
    await expect(createdCount).toHaveCount(1);
    await expect(createdRow).toBeVisible();
    await expect(createdCount).toHaveText('41');
    const materializedCountAfterCreate = Number((await createdCount.textContent())?.trim());
    await expect(indicator).toHaveCount(0);
    const createIndicatorCountAfterMaterialization = await indicator.count();

    // Two provider-resolved updates reach the same target before the first projection refresh.
    // The live DOM can prove that one localized indicator remains visible through the overlap;
    // internal composite-key provenance remains in the bUnit composition proof.
    await showOverview();
    await fillFieldByLabel(updateForm, 'Counter key', exactKey);
    await fillFieldByLabel(updateForm, 'Amount', '1');
    await updateForm.getByRole('button', { name: 'Update Counter', exact: true }).click();
    await showProjection();
    await expect(indicator).toBeVisible();
    await expect(indicator).toHaveCount(1);
    const firstUpdateAnnouncement = (await indicator.textContent())?.trim() ?? '';
    expect(firstUpdateAnnouncement).toBe(indicatorCopy);
    await expect.poll(speechCount).toBe(2);
    const speechBeforeTabSwitch = await speechCount();
    await showOverview();
    await showProjection();
    await expect(indicator).toBeVisible();
    expect(await speechCount()).toBe(speechBeforeTabSwitch);
    const tabSwitchAnnouncementCount = (await speechCount()) - speechBeforeTabSwitch;
    const firstUpdateIndicatorElement = await indicator.elementHandle();
    expect(firstUpdateIndicatorElement).not.toBeNull();
    const overlapIndicatorCountBeforeSecondDispatch = await indicator.count();
    await showOverview();
    await fillFieldByLabel(updateForm, 'Amount', '2');
    await showProjection();
    await expect(createdCount).toHaveText('41');
    const materializedCountBeforeSecondDispatch = Number((await createdCount.textContent())?.trim());
    await showOverview();
    await updateForm.getByRole('button', { name: 'Update Counter', exact: true }).click();
    await showProjection();
    await expect(indicator).toBeVisible();
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
    expect(await speechCount()).toBe(2);
    const duplicateAnnouncementCount = (await speechCount()) - speechBeforeTabSwitch;
    const overlapIndicatorCountAfterSecondDispatch = await indicator.count();
    const overlapIndicatorCopyAfterSecondDispatch = (await indicator.textContent())?.trim() ?? '';
    await expect(catchUp).toHaveAttribute('data-published', '3');
    await expect(createdCount).toHaveText('44');
    const materializedCountAfterOverlappingUpdates = Number((await createdCount.textContent())?.trim());
    await expect(indicator).toHaveCount(0);
    const overlappingIndicatorCountAfterMaterialization = await indicator.count();

    // A later update proves the provider path against a row that was absent before dispatch and
    // is now already rendered. Its projection refresh dismisses the fresh-row announcement.
    await showOverview();
    await fillFieldByLabel(updateForm, 'Counter key', exactKey);
    await fillFieldByLabel(updateForm, 'Amount', '8');
    await updateForm.getByRole('button', { name: 'Update Counter', exact: true }).click();
    await showProjection();
    await expect(indicator).toBeVisible();
    await expect(indicator).toHaveCount(1);
    await expect.poll(speechCount).toBe(3);
    const laterUpdateIndicatorVisibleCount = await indicator.count();
    await expect(catchUp).toHaveAttribute('data-published', '4');
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
      schemaVersion: 2,
      story: '13.5',
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
        sharedPoliteNodeCount,
        createAnnouncementCount,
        hiddenBeforeActivationAnnouncementCount,
        firstActivationAnnouncementCount: createAnnouncementCount,
        ...cueModes,
        tabSwitchAnnouncementCount,
        duplicateAnnouncementCount,
        freshRowAnnouncementCount: await speechCount(),
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
