import { FORCED_COLORS_MEDIA_ONLY_STATUS } from '../scripts/a11y-state-evidence.mjs';
import { PageToolbarSpecimenPage } from '../page-objects/page-toolbar-specimen.page.js';
import { fillFieldByLabel } from '../helpers/fluent-fields.js';
import { writeFile } from 'node:fs/promises';
import { expect, test } from '../fixtures/index.js';
import type { Locator, Page, TestInfo } from '@playwright/test';
import { applyTextSpacing, focusGeometry, expectFocusVisible, expectNoPageOverflow, expectNoTextClipping, expectSurfaceContrast, expectTargetSizes, measureSurfaceContrast, screenshotPreservingScroll, settlePaint } from '../helpers/responsive-accessibility.js';
import { expectNoBlockingAxeViolations } from '../helpers/a11y.js';
import { LIFECYCLE_STATES } from '../fixtures/lifecycle.fixture.js';

const scenarioPath = '/__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact';
const outcomes = ['validation', 'returned-rejection', 'exception-rejection', 'forbidden', 'unavailable', 'blank-unavailable', 'rate-limited', 'blank-rate-limited', 'transport', 'admission', 'policy'] as const;

async function ready(page: Page, path: string, theme: string) {
  await page.goto(path);
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.getByTitle('Change theme').first().click();
  await page.getByRole('menuitem', { name: theme === 'dark' ? 'Dark' : 'Light', exact: true }).click();
  await page.setViewportSize({ width: 320, height: 900 });
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await applyTextSpacing(page);
  await settlePaint(page);
}

type VisibleSurfaceInspection = { phase: string; retainedFocus?: Locator };

async function evidence(page: Page, root: Locator, state: string, info: TestInfo, icons?: Locator, inspection?: VisibleSurfaceInspection, refreshTransientState?: (rerender: boolean) => Promise<void>) {
  // These explicitly opted-in journeys inspect distant paint without moving operator
  // focus. Each mode measures that focus while visible, before its own root scroll.
  const inspectBeforeRootScroll = async (mode: 'normal' | 'forced-colors') => {
    expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches)).toBe(mode === 'forced-colors');
    const activeElement = await page.evaluateHandle(() => document.activeElement);
    try {
      const focused = inspection!.retainedFocus ?? page.locator(':focus').first();
      const active = await page.evaluate(() => document.activeElement?.tagName ?? 'BODY');
      const operatorFocused = !['BODY','HTML'].includes(active) && await focused.count() > 0;
      if (inspection!.retainedFocus) await expect(focused).toBeFocused();
      let result;
      if (operatorFocused) {
        await focused.scrollIntoViewIfNeeded();
        await expect(focused).toBeFocused();
        await settlePaint(page);
        const requireIndicator = await focused.evaluate(element => element.matches(':focus-visible') || Boolean(element.shadowRoot?.querySelector(':focus-visible')));
        const geometry = await focusGeometry(focused, { requireIndicator });
        expect(geometry.failures, `Visible ${mode} focus before root scroll: ${state} ${JSON.stringify(geometry)}`).toEqual([]);
        const contrast = requireIndicator ? await expectSurfaceContrast(focused, 'focus') : null;
        result = { mode, status: 'measured', phase: inspection!.phase, ...geometry, contrast };
      } else result = { mode, status: 'none', phase: inspection!.phase, target: active };
      await root.scrollIntoViewIfNeeded();
      expect(await activeElement.evaluate(element => document.activeElement === element), `Root inspection retains activeElement: ${state} ${mode}`).toBe(true);
      if (inspection!.retainedFocus) await expect(focused).toBeFocused();
      return { ...result, activeElementRetainedAfterRootScroll: true };
    } finally { await activeElement.dispose(); }
  };

  const measureCurrentState = async <T>(measure: () => Promise<T>): Promise<T> => {
    if (refreshTransientState) await refreshTransientState(true);
    const result = await measure();
    if (refreshTransientState) await refreshTransientState(false);
    return result;
  };
  await expect(root).toBeVisible();
  await measureCurrentState(() => expectNoPageOverflow(page));
  await measureCurrentState(() => expectNoTextClipping(page));
  await settlePaint(page);
  const theme = state.endsWith('-dark') ? 'Dark' : 'Light';
  await expect(page.getByTitle('Change theme').first().locator('span[aria-hidden="true"]')).toHaveText(theme);
  const themePaint = await page.locator('.fc-shell-root').evaluate(element => {
    const css = getComputedStyle(element), background = css.getPropertyValue('--colorNeutralBackground1').trim();
    const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
    const context = canvas.getContext('2d')!; context.fillStyle = background; context.fillRect(0, 0, 1, 1);
    return { background, rgb: [...context.getImageData(0, 0, 1, 1).data].slice(0, 3) };
  });
  expect(themePaint.rgb.reduce((sum, channel) => sum + channel, 0) < 384, 'Actual Fluent theme paint').toBe(theme === 'Dark');
  const reducedMotion = await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches);
  expect(reducedMotion).toBe(true);
  const normalFocus = inspection ? await inspectBeforeRootScroll('normal') : undefined;
  const collectCues = async () => {
    const cues = [];
    if (state.startsWith('generated-validation-')) cues.push(await expectSurfaceContrast(root, 'focus'));
    if (state.startsWith('fresh-presentation-')) cues.push(await expectSurfaceContrast(root, 'border'));
    if (state.startsWith('generated-missing-lost-focus-')) cues.push(await expectSurfaceContrast(root, 'focus'));
    if (state.startsWith('tabs-active-')) cues.push(await expectSurfaceContrast(root.locator('[aria-selected="true"]'), 'focus'));
    if (state.startsWith('palette-selected-')) cues.push(await expectSurfaceContrast(root.locator('.fc-palette-option-selected'), 'cue'));
    if (state.startsWith('destructive-dialog-')) cues.push(await expectSurfaceContrast(page.getByTestId('fc-destructive-cancel'), 'focus'));
    if (state.startsWith('abandonment-')) cues.push(await expectSurfaceContrast(page.getByTestId('fc-form-abandonment-stay'), 'focus'));
    if (state.startsWith('rail-')) cues.push(await expectSurfaceContrast(page.getByTestId('fc-shell-navigation').getByTestId('fc-navigation-rail').locator('[aria-current="page"]'), 'border'));
    if (state.startsWith('settings-')) cues.push(await expectSurfaceContrast(root.locator('fluent-radio:focus'), 'focus'));
    if (state.startsWith('theme-menu-')) cues.push(await expectSurfaceContrast(root.locator('[role="menuitem"]:focus'), 'focus'));
    return cues;
  };
  const cues = await collectCues();
  const exceptions: import('../helpers/responsive-accessibility.js').TargetException[] = state.startsWith('settings-')
    ? ['Compact', 'Comfortable', 'Roomy'].map(value => ({ selector: `fluent-radio[name="fc-density-select"][value="${value}"]`, kind: 'Spacing' as const, rationale: 'Fluent density radio keeps its native visual size with a clear 24px spacing circle.', keyboardPath: 'Settings → Density; Tab and arrows', evidence: `ae-state-${state}.json target inventory` }))
    : state.startsWith('abandonment-') ? [{ selector: 'nav[aria-label="breadcrumb"] > a', kind: 'Spacing', rationale: 'The full-page breadcrumb link is isolated from all neighboring targets; its 24px circle is measured against the complete inventory.', keyboardPath: 'Full-page form → Counter breadcrumb (Shift+Tab)', evidence: `ae-state-${state}.json target inventory` }] : [];
  const targets = await measureCurrentState(() => expectTargetSizes(page, exceptions));
  const text = await measureCurrentState(() => expectSurfaceContrast(root));
  const paintedIcons = await measureCurrentState(() => measureSurfaceContrast(icons ?? root, 'icon'));
  const icon = paintedIcons.length ? await measureCurrentState(() => expectSurfaceContrast(icons ?? root, 'icon')) : [];
  const collectBoundaries = async () => {
    const boundaries = [];
    for (const control of await root.locator('fluent-text-input:not([disabled]),fluent-number-input:not([disabled]),fluent-dropdown:not([disabled]),fluent-combobox:not([disabled])').all()) {
      if (!await control.isVisible()) continue;
      // isVisible includes controls fully outside a scrollport. Native intersection
      // geometry excludes only those whose boundaries currently paint no pixels.
      const painted = await control.evaluate(node => new Promise<boolean>(resolve => {
        const observer = new IntersectionObserver(([entry]) => {
          observer.disconnect();
          resolve(entry.intersectionRect.width > 0 && entry.intersectionRect.height > 0);
        });
        observer.observe(node);
      }));
      if (painted) boundaries.push(await expectSurfaceContrast(control, 'border'));
    }
    return boundaries;
  };
  const boundaries = await measureCurrentState(collectBoundaries);
  if (refreshTransientState) expect(icon.length, `Required normal ${state} icon samples`).toBeGreaterThan(0);
  if (state.startsWith('lifecycle-rejected-')) expect(icon.length, 'Required normal rejected-state icon samples').toBeGreaterThan(0);
  if (state.startsWith('policy-denied-')) expect(boundaries.length, 'Required normal policy control-boundary samples').toBeGreaterThan(0);
  await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
  await settlePaint(page);
  const inspectedSystemFocus = inspection ? await inspectBeforeRootScroll('forced-colors') : undefined;
  const forcedColors = await page.evaluate(() => matchMedia('(forced-colors: active)').matches);
  const forcedColorsPaint = await page.evaluate(() => {
    const probe = document.createElement('div');
    probe.style.cssText = 'position:fixed;left:-10000px;color:rgb(255,0,0);background:rgb(0,255,0);forced-color-adjust:auto';
    document.body.append(probe);
    const css = getComputedStyle(probe), changed = css.color !== 'rgb(255, 0, 0)' && css.backgroundColor !== 'rgb(0, 255, 0)';
    probe.remove(); return changed;
  });
  const systemText = forcedColorsPaint ? await measureCurrentState(() => expectSurfaceContrast(root)) : [];
  const systemIcon = forcedColorsPaint && icon.length ? await measureCurrentState(() => expectSurfaceContrast(icons ?? root, 'icon')) : [];
  const systemBoundaries = forcedColorsPaint ? await measureCurrentState(collectBoundaries) : [];
  const systemCues = forcedColorsPaint ? await measureCurrentState(collectCues) : [];
  if (forcedColorsPaint) {
    if (refreshTransientState) expect(systemIcon.length, `Required forced-color ${state} icon samples`).toBeGreaterThan(0);
    if (state.startsWith('lifecycle-rejected-')) expect(systemIcon.length, 'Required forced-color rejected-state icon samples').toBeGreaterThan(0);
    if (state.startsWith('policy-denied-')) expect(systemBoundaries.length, 'Required forced-color policy control-boundary samples').toBeGreaterThan(0);
  }
  const forcedColorsStatus = forcedColorsPaint ? 'painted system palette' : FORCED_COLORS_MEDIA_ONLY_STATUS;
  if (!forcedColorsPaint) expect(info.project.name, 'Only the probed WebKit engine lacks forced palette emulation').toBe('webkit');
  await expect(root).toBeVisible();
  await measureCurrentState(() => expectNoPageOverflow(page));
  await measureCurrentState(() => expectNoTextClipping(page));
  const focused = page.locator(':focus').first();
  const active = await page.evaluate(() => document.activeElement?.tagName ?? 'BODY');
  const operatorFocused = !['BODY','HTML'].includes(active) && await focused.count() > 0;
  const systemFocus = inspectedSystemFocus ?? (operatorFocused
    ? { mode: 'forced-colors', status: 'measured', ...await focusGeometry(focused, { requireIndicator: await focused.evaluate(element => element.matches(':focus-visible') || Boolean(element.shadowRoot?.querySelector(':focus-visible'))) }) }
    : { mode: 'forced-colors', status: 'none', target: active });
  if ('failures' in systemFocus) expect(systemFocus.failures, `Forced-color focused geometry: ${state} ${JSON.stringify(systemFocus)}`).toEqual([]);
  const systemSnapshot = forcedColorsPaint ? `system-state-${state}.png` : null;
  if (systemSnapshot) {
    const path = info.outputPath(systemSnapshot);
    await measureCurrentState(() => screenshotPreservingScroll(root, { path }));
    await info.attach(`system-state-${state}`, { path, contentType: 'image/png' });
  }
  await page.emulateMedia({ forcedColors: 'none' });
  await settlePaint(page);
  if (refreshTransientState) await refreshTransientState(true);
  const path = info.outputPath(`ae-state-${state}.json`);
  await writeFile(path, JSON.stringify({ testId: info.testId, attempt: info.retry, state, engine: info.project.name, runId: process.env.FC_A11Y_EVIDENCE_RUN_ID, viewport: page.viewportSize(), theme, themePaint, ...(refreshTransientState ? { transientStateVerifiedAtEachMeasurement: true } : {}), targets, ...(inspection ? { normalFocus, rootPaintPhase: 'after-root-scroll-with-retained-active-element', fixtureOptions: info.config.metadata.testHostOptions } : {}), text, icon, boundaries, cues, decorativeIconExemptions: ['.fc-empty-state-body__icon: redundant illustration accompanying complete visible empty-state text', '.fluent-skeleton: decorative loading skeleton accompanying aria-busy and its accessible name'], forcedColors, forcedColorsPaint, forcedColorsStatus, systemText, systemIcon, systemBoundaries, systemCues, systemFocus, systemSnapshot, reducedMotion }, null, 2));
  await info.attach(`state-${state}`, { path, contentType: 'application/json' });
}

for (const theme of ['light', 'dark']) {
  test.describe(`AE complete state matrix ${theme}`, () => {
    test.beforeEach(async ({ page }) => {
      await page.addInitScript(() => { localStorage.clear(); sessionStorage.clear(); });
    });

    for (const outcome of outcomes) {
      test(`generated zero-field ${outcome}: safe feedback, focus, owner, next attempt`, async ({ page }, info) => {
        await ready(page, `${scenarioPath}&theme=${theme}`, theme);
        const root = page.getByTestId('fc-ae-scenarios');
        await root.getByTestId(`fc-ae-select-${outcome}`).click();
        const trigger = root.getByRole('button', { name: 'Run accessibility specimen', exact: true });
        await trigger.focus();
        await page.keyboard.press('Shift+Tab');
        await page.keyboard.press('Tab');
        await page.keyboard.press('Enter');
        const feedback = root.getByTestId('fc-zero-field-outcome');
        await expect(feedback).toBeVisible();
        await expect(feedback).toHaveAttribute('aria-live', 'off');
        await expect(trigger).toBeFocused();
        await expect(feedback).not.toHaveText(/^\s*$/u);
        await expect(feedback.locator('a,button,fluent-button')).toHaveCount(0);
        if (outcome.includes('rate-limited')) await expect(feedback).toContainText('Retry after 17 seconds.');
        const copy = (await feedback.innerText()).trim();
        await expect(root.getByTestId('fc-surface-status').filter({ hasText: outcome.includes('rejection') ? /rejected/i : copy })).toHaveCount(1);
        await expect(root.locator('[role="alert"],[aria-live="assertive"]')).toHaveCount(0);
        await feedback.scrollIntoViewIfNeeded();
        await evidence(page, feedback, `zero-${outcome}-${theme}`, info);
        await expectFocusVisible(trigger);
        const focusContrast = await expectSurfaceContrast(trigger, 'focus');
        // No automatic pre-accept retry: the independent dispatch counter stays at one (zero for denials).
        await page.waitForTimeout(350);
        await root.getByTestId('fc-ae-select-accepted').click();
        await expect(root.getByTestId('fc-ae-command-probe')).toHaveAttribute('data-dispatch-count', ['admission', 'policy'].includes(outcome) ? '0' : '1');
        await expect(feedback).toHaveText(copy);
        await trigger.click();
        await expect(feedback).toHaveCount(0);
        await expect(page.locator('.fc-pending-command-summary')).toBeVisible();
        if (outcome === 'validation') {
          await expect(trigger).toBeFocused();
          // Inspect the global accepted summary separately after proving trigger retention.
          await page.locator('#fc-main-content h1').first().focus();
          await expect(page.locator('#fc-main-content h1').first()).toBeFocused();
          await page.locator('.fc-pending-command-summary').scrollIntoViewIfNeeded();
          await evidence(page, page.locator('.fc-pending-command-summary'), `zero-accepted-${theme}`, info);
        }
        await info.attach(`zero-field-${outcome}-${theme}.json`, { body: JSON.stringify({ outcome, theme, copy, focusContrast, preAcceptRetries: 0, observedOwnerCount: 1, heardSpeech: 'not observed' }), contentType: 'application/json' });
      });
    }

    test('generated zero-field wrapped feedback clears the complete trigger focus ring', async ({ page }, info) => {
      await ready(page, `${scenarioPath}&theme=${theme}`, theme);
      const root = page.getByTestId('fc-ae-scenarios');
      await root.getByTestId('fc-ae-select-validation').click();
      const trigger = root.getByRole('button', { name: 'Run accessibility specimen', exact: true });
      await trigger.focus();
      await page.keyboard.press('Shift+Tab');
      await page.keyboard.press('Tab');
      await page.keyboard.press('Enter');
      const feedback = root.getByTestId('fc-zero-field-outcome');
      await expect(feedback).toBeVisible();
      await expect(trigger).toBeFocused();
      await trigger.scrollIntoViewIfNeeded();
      await settlePaint(page);
      const geometry = await focusGeometry(trigger);
      const layout = await feedback.evaluate(feedback => {
        const bounds = feedback.getBoundingClientRect();
        return { left: bounds.left, top: bounds.top, right: bounds.right, bottom: bounds.bottom };
      });
      const triggerBottom = await trigger.evaluate(element => element.getBoundingClientRect().bottom);
      expect(layout.top, 'Feedback is on a wrapped row below its trigger').toBeGreaterThanOrEqual(triggerBottom);
      expect(layout.top, 'Wrapped feedback clears the complete exterior focus ring').toBeGreaterThan(geometry.box.bottom);
      expect(geometry.failures, 'The retained trigger and its focus ring remain fully visible').toEqual([]);
      await info.attach(`zero-field-wrapped-feedback-${theme}`, { body: JSON.stringify({ theme, triggerBottom, feedback: layout, focus: geometry }), contentType: 'application/json' });
    });

    test('real generated detail removal preserves external focus and deduplicates completed results', async ({ page }, info) => {
      await ready(page, `${scenarioPath}&theme=${theme}`, theme);
      const grid = page.getByTestId('fc-generated-counter-grid');
      const controls = page.getByTestId('fc-ae-scenarios');
      await controls.getByTestId('fc-ae-rows-two').click();
      await expect(grid.locator('tbody tr.fluent-data-grid-row')).toHaveCount(2);
      await grid.getByRole('gridcell', { name: 'FC-AE-1', exact: true }).click();
      const detail = grid.locator('.fc-expand-in-row-detail');
      await expect(detail).toContainText('FC-AE-1');
      const external = controls.getByTestId('fc-ae-rows-two');
      await external.focus();
      await controls.getByTestId('fc-ae-rows-missing').dispatchEvent('click');
      const explanation = grid.locator('.fc-expanded-row-hidden-banner-text');
      await expect(explanation).toContainText('not in the current results');
      await expect(external).toBeFocused();
      await expect(grid.getByTestId('fc-expanded-row-hidden-banner-clear')).toHaveCount(0);
      await expect(grid.getByTestId('fc-surface-status')).toContainText('not in the current results');
      const revision = await grid.locator('[data-message-revision]').getAttribute('data-message-revision');
      await controls.getByTestId('fc-ae-rows-repeat').dispatchEvent('click');
      await expect(grid.locator('[data-message-revision]')).toHaveAttribute('data-message-revision', revision!);
      await expect(external).toBeFocused();
      await evidence(page, explanation, `generated-missing-external-${theme}`, info, undefined, { phase: 'retained-external-focus-before-root-scroll', retainedFocus: external });
    });

    test('real generated detail loss focuses its neutral non-live explanation', async ({ page }, info) => {
      await ready(page, `${scenarioPath}&theme=${theme}`, theme);
      const grid = page.getByTestId('fc-generated-counter-grid');
      const controls = page.getByTestId('fc-ae-scenarios');
      await controls.getByTestId('fc-ae-rows-two').click();
      await grid.getByRole('gridcell', { name: 'FC-AE-1', exact: true }).click();
      const detail = grid.locator('.fc-expand-in-row-detail');
      await expect(detail).toContainText('FC-AE-1');
      const detailControl = detail.locator('a,button,fluent-button,[tabindex="0"]').first();
      await detailControl.focus();
      await expect(detailControl).toBeFocused();
      await controls.getByTestId('fc-ae-rows-missing').dispatchEvent('click');
      const explanation = grid.locator('.fc-expanded-row-hidden-banner-text');
      await expect(explanation).toBeFocused();
      await expect(explanation).toContainText('not in the current results');
      await expect(grid.getByTestId('fc-surface-status')).not.toContainText('not in the current results');
      await expect(explanation.locator('xpath=..')).toHaveAttribute('aria-live', 'off');
      await evidence(page, explanation, `generated-missing-lost-focus-${theme}`, info);
      await expectFocusVisible(explanation);
    });

    for (const [stateIndex, state] of LIFECYCLE_STATES.entries()) {
      test(`actual lifecycle wrapper ${state} retains immediate text and system-color cues`, async ({ page }, info) => {
        test.setTimeout(120_000);
        await ready(page, `${scenarioPath}&theme=${theme}`, theme);
        const root = page.getByTestId('fc-ae-scenarios');
        const wrapper = page.getByTestId('fc-lifecycle-ae-state');
        // Keep the original transition history; each state owns its complete audit budget.
        for (const precedingState of LIFECYCLE_STATES.slice(0, stateIndex + 1)) {
          await root.getByTestId(`fc-ae-lifecycle-${precedingState.replaceAll('-', '')}`).click();
          await expect(wrapper).toHaveAttribute('data-lifecycle-state', precedingState);
        }
        const transient = state === 'confirmed' || state === 'idempotent-confirmed';
        const refresh = transient ? async (rerender: boolean) => {
          if (rerender) await root.getByTestId(`fc-ae-lifecycle-${state.replaceAll('-', '')}`).click();
          await expect(wrapper).toHaveAttribute('data-lifecycle-state', state);
          await expect(wrapper.getByTestId(state === 'confirmed' ? 'fc-confirmed' : 'fc-idempotent')).toBeVisible();
        } : undefined;
        await evidence(page, wrapper, `lifecycle-${state}-${theme}`, info, undefined, state === 'rejected' ? { phase: 'operator-focus-before-rejected-lifecycle-scroll' } : undefined, refresh);
        await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
        await expect(wrapper).toContainText('Observable lifecycle specimen');
        const adjustment = await wrapper.evaluate(element => getComputedStyle(element).getPropertyValue('forced-color-adjust'));
        if (adjustment) expect(adjustment).toBe('auto');
        else expect(info.project.name, 'Unsupported forced palette replacement remains an explicit WebKit capability limit').toBe('webkit');
        const animations = await wrapper.evaluate(element => [element, ...element.querySelectorAll('.fc-lifecycle-pulse,.fc-lifecycle-still-syncing')].map(node => ({ animation: getComputedStyle(node).animationName, transition: getComputedStyle(node).transitionDuration })));
        expect(animations.every(paint => paint.animation === 'none' && paint.transition.split(',').every(value => parseFloat(value) === 0))).toBe(true);
        await page.emulateMedia({ forcedColors: 'none' });
        if (stateIndex === LIFECYCLE_STATES.length - 1) await page.screenshot({ path: info.outputPath(`lifecycle-matrix-${theme}.png`) });
      });
    }

    test('narrow lifecycle rejection keeps its native Dismiss target reachable after text spacing', async ({ page }, info) => {
      await ready(page, `${scenarioPath}&theme=${theme}`, theme);
      await page.getByTestId('fc-ae-lifecycle-rejected').click();
      const wrapper = page.getByTestId('fc-lifecycle-ae-state');
      await expect(wrapper).toHaveAttribute('data-lifecycle-state', 'rejected');
      const bar = wrapper.locator('fluent-message-bar');
      const dismiss = bar.getByRole('button', { name: 'Hide this message', exact: true });
      await dismiss.scrollIntoViewIfNeeded();
      const geometry = await bar.evaluate(element => ({
        bar: element.getBoundingClientRect().toJSON(),
        body: element.shadowRoot!.querySelector('.content')!.getBoundingClientRect().toJSON(),
        actions: element.shadowRoot!.querySelector('.actions')!.getBoundingClientRect().toJSON(),
        dismiss: element.querySelector('[slot="dismiss"]')!.getBoundingClientRect().toJSON(),
      }));
      expect(geometry.dismiss.width).toBeGreaterThanOrEqual(24);
      expect(geometry.dismiss.height).toBeGreaterThanOrEqual(24);
      expect(geometry.dismiss.left).toBeGreaterThanOrEqual(geometry.bar.left);
      expect(geometry.dismiss.right).toBeLessThanOrEqual(geometry.bar.right);
      expect(geometry.actions.top).toBeGreaterThanOrEqual(geometry.body.bottom);
      await expectNoTextClipping(page);
      await expectNoPageOverflow(page);
      await expectTargetSizes(page);
      await dismiss.click();
      await expect(dismiss).not.toBeVisible();
      await info.attach(`native-lifecycle-dismiss-${theme}`, { body: JSON.stringify(geometry), contentType: 'application/json' });
    });

    test('generated projection loaded, detail, loading, empty and failure surfaces', async ({ page }, info) => {
      await ready(page, `${scenarioPath}&theme=${theme}`, theme);
      const grid = page.getByTestId('fc-generated-counter-grid');
      for (const state of ['two', 'loading', 'empty', 'error']) {
        await page.getByTestId(`fc-ae-rows-${state}`).click();
        if (state === 'two') await expect(grid.locator('tbody tr.fluent-data-grid-row')).toHaveCount(2);
        if (state === 'loading') await expect(grid.locator('[aria-busy="true"]').first()).toBeVisible();
        if (state === 'empty') await expect(grid).toContainText(/No .*yet|No data|No items/i);
        if (state === 'error') await expect(grid).toContainText(/could not|failed|try again/i);
        await evidence(page, grid, `projection-${state}-${theme}`, info);
        await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
        await expect(grid).toBeVisible();
        await expectNoPageOverflow(page);
        await page.emulateMedia({ forcedColors: 'none' });
      }
    });

    for (const surface of [
      { name: 'home', path: '/home' },
      { name: 'navigation-tabs', path: '/counter/counter-projection' },
      { name: 'toolbar', path: '/__frontcomposer/specimens/page-toolbar' },
      { name: 'generated-forms', path: '/counter' },
    ]) {
      test(`${surface.name}: composed text, targets, current-state and forced-color cues`, async ({ page }, info) => {
        await ready(page, surface.path, theme);
        await evidence(page, page.locator('#fc-main-content'), `${surface.name}-${theme}`, info);
        const inventory = await expectTargetSizes(page);
        await info.attach(`targets-${surface.name}-${theme}`, { body: JSON.stringify(inventory), contentType: 'application/json' });
        const heading = page.locator('#fc-main-content h1').first();
        const keyboardTarget = page.getByTestId('fc-palette-trigger');
        await keyboardTarget.focus();
        await page.keyboard.press('Shift+Tab');
        await page.keyboard.press('Tab');
        await expectFocusVisible(keyboardTarget);
        await expectSurfaceContrast(keyboardTarget, 'focus');
        await expectNoBlockingAxeViolations(page, { route: surface.path, artifactPath: info.outputPath(`axe-state-${surface.name}-${theme}.json`) });
        await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
        await expect(heading).toBeVisible();
        await expectNoPageOverflow(page);
        const current = page.locator('[aria-current="page"],[aria-selected="true"]');
        if (surface.name === 'navigation-tabs') expect(await current.count()).toBeGreaterThan(0);
        await page.screenshot({ path: info.outputPath(`system-colors-${surface.name}-${theme}.png`) });
      });
    }
  });
}

test('contrast measurement rejects empty, low-contrast, unpainted and unsupported samples', async ({ page }) => {
  await page.setContent('<style>#bad:not(:focus){outline:none!important}</style><main style="color:#aaa;background:#fff"><p id="low">Low text</p><div id="empty"></div><div id="host" style="background:#111;color:#fff"></div><button id="bad" style="border:2px solid #eee;outline:2px solid #eee">Bad focus</button><div id="gradient" style="margin-top:8px;background:linear-gradient(white,black)">Unsupported</div><svg id="icon" width="20" height="20"><path fill="#eee" d="M0 0h20v20H0z"/></svg></main>');
  await expect(expectSurfaceContrast(page.locator('#low'))).rejects.toThrow(/contrast failures/);
  await expect(expectSurfaceContrast(page.locator('#empty'))).rejects.toThrow(/Nonempty/);
  await page.locator('#low').evaluate(element => (element as HTMLElement).style.opacity = '.5');
  await expect(expectSurfaceContrast(page.locator('#low'))).rejects.toThrow(/pixel evidence/);
  await expect(expectSurfaceContrast(page.locator('#gradient'))).rejects.toThrow(/pixel evidence/);
  await expect(expectSurfaceContrast(page.locator('#bad'), 'border')).rejects.toThrow(/contrast failures/);
  await page.locator('#bad').focus();
  await expect(expectSurfaceContrast(page.locator('#bad'), 'focus')).rejects.toThrow(/contrast failures/);
  await expect(expectSurfaceContrast(page.locator('#icon'), 'icon')).rejects.toThrow(/contrast failures/);
  await page.locator('#host').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<p style="color:rgba(255,255,255,.9)">Shadow text</p>'; });
  const samples = await expectSurfaceContrast(page.locator('#host'));
  expect(samples).toHaveLength(1);
  expect(samples[0].background.slice(0, 3)).toEqual([17, 17, 17]);
  expect(await measureSurfaceContrast(page.locator('#empty'), 'icon')).toEqual([]);
});

test('contrast clips a shadow background at its scrollport while rejecting 1.7px of visible overlap', async ({ page }, info) => {
  await page.setContent('<style>body{background:white}#port{position:absolute;left:40px;top:40px;width:222px;height:60px;overflow:hidden}#host{position:relative;left:0}#label{position:absolute;left:40px;top:100px;margin:0;color:black;font:14px/20px sans-serif}</style><div id="port"><div id="host"></div></div><p id="label">Footer label</p>');
  const geometry = await page.locator('#host').evaluate(host => {
    const background = document.createElement('div');
    background.style.cssText = 'position:relative;width:222px;height:32px;background:black';
    host.attachShadow({ mode: 'open' }).append(background);
    const label = document.querySelector('#label')!;
    const range = document.createRange(); range.selectNodeContents(label);
    const text = range.getBoundingClientRect(), port = host.parentElement!.getBoundingClientRect();
    (host as HTMLElement).style.top = `${text.bottom - 1.7 - port.top}px`;
    const candidate = background.getBoundingClientRect();
    return { text: text.toJSON(), port: port.toJSON(), candidate: candidate.toJSON(), rawOverlap: text.bottom - candidate.top };
  });
  expect(geometry.rawOverlap).toBeCloseTo(1.7, 1);
  expect(geometry.candidate.top).toBeGreaterThan(geometry.port.bottom);
  await info.attach('shadow-background-scrollport-geometry', { body: JSON.stringify(geometry), contentType: 'application/json' });
  for (const overflow of ['hidden', 'auto', 'clip']) {
    await page.locator('#port').evaluate((port, value) => { (port as HTMLElement).style.overflow = value; }, overflow);
    const samples = await expectSurfaceContrast(page.locator('#label'));
    expect(samples).toHaveLength(1);
    expect(samples[0].background.slice(0, 3)).toEqual([255, 255, 255]);
    await page.locator('#port').evaluate(port => { (port as HTMLElement).style.overflow = 'visible'; });
    await expect(measureSurfaceContrast(page.locator('#label'))).rejects.toThrow(/Overlapping sibling background/);
  }
  await page.locator('#port').evaluate(port => { (port as HTMLElement).style.overflow = 'hidden'; });
  await page.locator('#host').evaluate((host, top) => { (host as HTMLElement).style.cssText = `position:fixed;left:40px;top:${top}px`; }, geometry.candidate.top);
  await expect(measureSurfaceContrast(page.locator('#label'))).rejects.toThrow(/Overlapping sibling background/);
});

const clippingPaintRegressions = [
  { name: 'scaled ancestor', port: 'position:relative;left:40px;top:40px;width:100px;height:60px;overflow:hidden;transform:scale(2);transform-origin:0 0', background: 'display:block;position:relative;top:40px;width:100px;height:20px', labelTop: 120, tag: 'div' },
  { name: 'overflow clip margin', port: 'position:relative;left:40px;top:40px;width:100px;height:60px;overflow:clip;overflow-clip-margin:30px', background: 'display:block;position:relative;top:70px;width:100px;height:20px', labelTop: 110, tag: 'div' },
  { name: 'inline ancestor', port: 'overflow:hidden', background: 'display:inline-block;position:relative;width:200px;height:20px', labelTop: 0, tag: 'span' },
  { name: 'display contents ancestor', port: 'display:contents;overflow:hidden', background: 'display:block;position:relative;left:40px;top:100px;width:222px;height:32px', labelTop: 110, tag: 'div' },
  { name: 'fractional scrollport', port: 'position:relative;left:40px;top:40px;width:100px;height:60.2px;overflow:hidden', background: 'display:block;position:relative;top:60.1px;width:100px;height:20px', labelTop: 85, tag: 'div' },
  { name: 'expanded negative inset', port: 'position:relative;left:40px;top:40px;width:100px;height:60px;clip-path:inset(-30px)', background: 'display:block;position:relative;top:70px;width:100px;height:20px', labelTop: 110, tag: 'div' },
];
for (const fixture of clippingPaintRegressions) {
  test(`review clipping preserves visible competing paint: ${fixture.name}`, async ({ page }) => {
    await page.setContent(`<style>body{margin:0;background:white}#port{${fixture.port}}#bg{${fixture.background};background:black}#label{position:absolute;left:40px;top:${fixture.labelTop}px;margin:0;color:black;font:14px/20px sans-serif}</style><${fixture.tag} id="port"><span id="bg"></span></${fixture.tag}><p id="label">Footer label</p>`);
    if (fixture.name === 'overflow clip margin') test.skip(!await page.evaluate(() => CSS.supports('overflow-clip-margin', '30px')), 'Engine does not implement expanded overflow clip margins');
    if (fixture.name === 'fractional scrollport') {
      const overlap = await page.locator('#port').evaluate(port => port.getBoundingClientRect().bottom - document.querySelector('#bg')!.getBoundingClientRect().top);
      expect(overlap).toBeGreaterThan(0);
      expect(overlap).toBeLessThan(.2);
    }
    await expect(measureSurfaceContrast(page.locator('#label'))).rejects.toThrow(/pixel evidence/);
    await page.locator('#bg').evaluate(background => background.remove());
    const samples = await expectSurfaceContrast(page.locator('#label'));
    expect(samples).toHaveLength(1);
    expect(samples[0].ratio).toBe(21);
  });
}

test('unrelated controls cannot mask failed focus or boundary contrast', async ({ page }) => {
  await page.setContent('<style>#bad:not(:focus){outline:none!important;box-shadow:none!important}</style><main style="background:white;padding:20px"><button id="bad" style="background:white;border:2px solid #eee;outline:2px solid #eee">Low contrast</button><button id="good" style="margin-left:12px;background:white;border:2px solid black;outline:2px solid black">Readable</button></main>');
  await page.locator('#bad').focus();
  await expect(expectSurfaceContrast(page.locator('main'), 'border')).rejects.toThrow(/contrast failures/);
  await expect(expectSurfaceContrast(page.locator('main'), 'focus')).rejects.toThrow(/contrast failures/);
  await page.locator('#bad').evaluate(element => { element.style.outline = 'none'; element.style.boxShadow = '0 0 0 2px white,0 0 0 4px black'; });
  const ring = await expectSurfaceContrast(page.locator('#bad'), 'focus');
  expect(ring.some(sample => sample.ratio === 1)).toBe(true);
  expect(ring.some(sample => sample.ratio === 21)).toBe(true);
});

test('forced-color state checks reject a removed cue while text and selected semantics survive', async ({ page }) => {
  await page.setContent('<style>button{background:white;color:black;border:2px solid black}@media(forced-colors:active){button{border:none;outline:none!important;box-shadow:none!important}}</style><button role="tab" aria-selected="true">Current tab</button>');
  const selected = page.getByRole('tab');
  await expectSurfaceContrast(selected);
  await expectSurfaceContrast(selected, 'border');
  await page.emulateMedia({ forcedColors: 'active' });
  await expect(selected).toHaveAttribute('aria-selected', 'true');
  await expectSurfaceContrast(selected);
  await expect(expectSurfaceContrast(selected, 'border')).rejects.toThrow(/Nonempty/);
});

for (const theme of ['light', 'dark']) {
  test(`shell chrome and theme menu in ${theme}`, async ({ page }, info) => {
    test.setTimeout(120_000);
    await ready(page, '/counter', theme);
    const shell = page.locator('.fc-shell-root');
    await evidence(page, shell, `shell-chrome-${theme}`, info);
    await page.getByTitle('Change theme').first().click();
    const themeMenu = page.getByRole('menu').first();
    await expect(themeMenu).toHaveAttribute('focusgroup', 'menu');
    await settlePaint(page);
    await themeMenu.getByRole('menuitem', { name: 'Light', exact: true }).focus();
    await page.keyboard.press('ArrowDown');
    const darkItem = themeMenu.getByRole('menuitem', { name: 'Dark', exact: true });
    await expectFocusVisible(darkItem);
    await evidence(page, themeMenu, `theme-menu-${theme}`, info);
    await expectSurfaceContrast(darkItem, 'focus');
    await expectNoBlockingAxeViolations(page, { route: '/counter#theme-menu', artifactPath: info.outputPath(`axe-shell-menu-${theme}.json`) });
    await page.keyboard.press('Escape');
  });

  test(`exact desktop navigation rail transitions in ${theme}`, async ({ page }, info) => {
    test.setTimeout(120_000);
    await ready(page, '/counter', theme);
    const shell = page.locator('.fc-shell-root');
    await page.setViewportSize({ width: 1600, height: 900 });
    const rail = page.getByTestId('fc-shell-navigation').getByTestId('fc-navigation-rail');
    await expect(rail).toHaveAttribute('data-rail-width', '72');
    await expect.poll(async () => (await rail.boundingBox())?.width).toBe(72);
    await expect(rail.locator('[aria-current="page"]')).toHaveCount(1);
    await evidence(page, shell, `rail-labeled-${theme}`, info, rail);
    await page.getByTestId('fc-hamburger-toggle').click();
    await expect(rail).toHaveAttribute('data-rail-width', '48');
    await expect.poll(async () => (await rail.boundingBox())?.width).toBe(48);
    await expect(rail.locator('[aria-current="page"]')).toHaveCount(1);
    await evidence(page, shell, `rail-icons-${theme}`, info, rail);
  });

  test(`active toolbar, tabs, palette and settings states in ${theme}`, async ({ page }, info) => {
    await ready(page, '/__frontcomposer/specimens/page-toolbar', theme);
    const toolbar = new PageToolbarSpecimenPage(page);
    await toolbar.searchInput.focus();
    await page.keyboard.press('Shift+Tab');
    await page.keyboard.press('Tab');
    await expectFocusVisible(toolbar.searchInput);
    await expectSurfaceContrast(page.getByTestId('fc-page-toolbar-search'), 'focus');
    await toolbar.filterTrigger.click();
    await expect(toolbar.filterContent).toBeVisible();
    await evidence(page, toolbar.filterContent, `filter-popover-${theme}`, info);
    await toolbar.filterTrigger.click();
    await toolbar.viewTrigger.click();
    await evidence(page, toolbar.viewDensityItem, `view-menu-${theme}`, info);
    await page.keyboard.press('Escape');
    await toolbar.summaryTab.focus();
    await toolbar.summaryTab.press('ArrowRight');
    await expect(toolbar.activityTab).toHaveAttribute('aria-selected', 'true');
    await expectFocusVisible(toolbar.activityTab);
    await evidence(page, toolbar.tabs, `tabs-active-${theme}`, info);
    await page.getByTestId('fc-palette-trigger').click();
    const palette = page.getByTestId('fc-palette-root');
    await evidence(page, palette, `palette-default-${theme}`, info);
    const search = page.getByTestId('fc-palette-search').locator('input');
    await search.fill('shortcuts');
    const informationalShortcut = palette.locator('.fc-palette-option[aria-disabled="true"]').first();
    await expect(informationalShortcut).toBeVisible();
    await page.emulateMedia({ forcedColors: 'active' });
    if (info.project.name === 'webkit') {
      await info.attach('palette-disabled-forced-palette-capability-limit', { body: 'WebKit does not replace authored colors with system palette paint in this lane.', contentType: 'text/plain' });
    } else {
      const colors = await informationalShortcut.evaluate(element => {
        const system = document.createElement('span');
        system.style.color = 'GrayText';
        element.append(system);
        const expected = getComputedStyle(system).color;
        system.remove();
        return { expected, label: getComputedStyle(element.querySelector('.fc-palette-option-label')!).color, hint: getComputedStyle(element.querySelector('.fc-palette-option-hint')!).color };
      });
      expect(colors.label).toBe(colors.expected);
      expect(colors.hint).toBe(colors.expected);
    }
    await page.emulateMedia({ forcedColors: 'none' });
    await search.fill('counter');
    await expect(palette.getByRole('option')).toHaveText(['CounterProjection', 'ConfigureCounterCommand']);
    const selectedProjection = palette.getByRole('option', { name: 'CounterProjection', exact: true });
    await expect(selectedProjection).toHaveClass(/(^|\s)fc-palette-option-selected(\s|$)/u);
    await expect(selectedProjection).toHaveAttribute('aria-selected', 'true');
    await expect(selectedProjection).toBeInViewport({ ratio: 1 });
    await settlePaint(page);
    await evidence(page, palette, `palette-selected-${theme}`, info);
    await expectSurfaceContrast(palette.locator('.fc-palette-option-selected'), 'cue');
    await search.fill('no-match-accessibility-136');
    await expect(palette.locator('.fc-palette-noresults')).toBeVisible();
    await evidence(page, palette, `palette-empty-${theme}`, info);
    await page.emulateMedia({ forcedColors: 'active' });
    await expect(palette.locator('.fc-palette-noresults')).toContainText(/No/);
    await page.emulateMedia({ forcedColors: 'none' });
    await page.keyboard.press('Escape');
    await expect(palette).toHaveCount(0);
    await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByTestId('fc-settings-button')).toBeFocused();
    await page.keyboard.press('Enter');
    const settings = page.getByTestId('fc-settings-dialog');
    await expect(page.locator('#fc-settings-heading')).toBeFocused();
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');
    const radio = settings.locator('fluent-radio:focus');
    await expectFocusVisible(radio);
    await evidence(page, settings, `settings-${theme}`, info);
    await info.attach(`settings-radio-focus-${theme}`, { body: JSON.stringify(await expectSurfaceContrast(radio, 'focus')), contentType: 'application/json' });
    const exceptions: import('../helpers/responsive-accessibility.js').TargetException[] = ['Compact', 'Comfortable', 'Roomy'].map(value => ({ selector: `fluent-radio[name="fc-density-select"][value="${value}"]`, kind: 'Spacing', rationale: 'Fluent radio with independently measured 24px spacing circle.', keyboardPath: 'Settings → Density; Tab and arrows', evidence: `settings-${theme} target inventory` }));
    const targets = await expectTargetSizes(page, exceptions);
    await info.attach(`settings-targets-${theme}`, { body: JSON.stringify(targets), contentType: 'application/json' });
    await page.emulateMedia({ forcedColors: 'active' });
    await expect(settings).toContainText('Display density');
    await page.keyboard.press('Escape');
    await expect(page.getByTestId('fc-settings-button')).toBeFocused();
  });

  test(`generated validation, policy, destructive and abandonment states in ${theme}`, async ({ page }, info) => {
    await ready(page, `/__frontcomposer/specimens/type?theme=${theme}`, theme);
    const destructive = page.getByTestId('fc-destructive-command-specimen');
    await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).click();
    // Required retention deliberately stays empty: the genuine generated validation summary owns focus.
    const summary = destructive.locator('[data-testid="fc-validation-summary"]');
    await expect(summary).toBeVisible();
    await evidence(page, summary, `generated-validation-${theme}`, info);
    await evidence(page, page.getByTestId('fc-policy-command-specimen'), `policy-denied-${theme}`, info, undefined, { phase: 'operator-focus-before-policy-surface-scroll' });
    const form = destructive.locator('.fc-command-form');
    await form.getByRole('combobox').click();
    await form.getByRole('combobox').press('ArrowDown');
    await form.getByRole('combobox').press('Enter');
    await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).focus();
    await page.keyboard.press('Shift+Tab');
    await page.keyboard.press('Tab');
    await page.keyboard.press('Enter');
    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toBeVisible();
    await evidence(page, dialog, `destructive-dialog-${theme}`, info);
    await expectTargetSizes(page);
    await page.keyboard.press('Escape');

    await ready(page, '/commands/Counter/ConfigureCounterCommand', theme);
    const configure = page.locator('.fc-command-form[aria-label="Configure Counter command form"]');
    await fillFieldByLabel(configure, 'Name', 'Accessibility specimen');
    await page.waitForTimeout(6_500);
    await page.getByRole('link', { name: 'Counter', exact: true }).last().focus();
    await page.keyboard.press('Shift+Tab');
    await page.keyboard.press('Tab');
    await page.keyboard.press('Enter');
    const warning = page.getByTestId('fc-form-abandonment-warning');
    await expect(warning).toBeVisible();
    await expect(page.getByTestId('fc-form-abandonment-stay')).toBeFocused();
    await evidence(page, warning, `abandonment-${theme}`, info);
    await expectFocusVisible(page.getByTestId('fc-form-abandonment-stay'));
    await page.keyboard.press('Escape');
    await expect(warning).toHaveCount(0);
    await expect(configure).toContainText('Name');
  });
}

for (const theme of ['light', 'dark']) {
  const connections = ['reconnecting', 'disconnected', 'connected'] as const;
  for (const [connectionIndex, connection] of connections.entries()) {
    test(`projection ${connection} connection and stale cues in ${theme}`, async ({ page }, info) => {
      await ready(page, `${scenarioPath}&theme=${theme}`, theme);
      const grid = page.getByTestId('fc-generated-counter-grid');
      const notice = page.getByTestId('fc-projection-connection-status');
      // Exercise the same real transitions before auditing this connection state.
      for (const precedingConnection of connections.slice(0, connectionIndex + 1)) {
        await page.getByTestId(`fc-ae-connection-${precedingConnection}`).click();
        await expect(notice).toBeVisible();
      }
      await evidence(page, notice, `connection-${connection}-${theme}`, info, notice, { phase: 'operator-focus-before-projection-surface-scroll' });
      if (connection !== 'connected') {
        await expect(grid.getByTestId('fc-projection-stale-notice')).toBeVisible();
        await evidence(page, grid.getByTestId('fc-projection-stale-notice'), `stale-${connection}-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
      }
    });
  }

  test(`projection slow cap and filter cues in ${theme}`, async ({ page }, info) => {
    await ready(page, `${scenarioPath}&theme=${theme}`, theme);
    const grid = page.getByTestId('fc-generated-counter-grid');
    for (const connection of connections) {
      await page.getByTestId(`fc-ae-connection-${connection}`).click();
      await expect(page.getByTestId('fc-projection-connection-status')).toBeVisible();
    }
    await page.getByTestId('fc-ae-page-start').click();
    const slow = grid.getByTestId('fc-slow-query-notice');
    await expect(slow).toBeVisible({ timeout: 5_000 });
    await evidence(page, slow, `slow-query-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
    await page.getByTestId('fc-ae-page-cap').click();
    await expect(slow).toHaveCount(0);
    const cap = grid.getByTestId('fc-max-items-cap-notice');
    await expect(cap).toBeVisible();
    await evidence(page, cap, `result-limit-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
    await page.getByTestId('fc-ae-filter-empty').click();
    await expect(page.getByTestId('fc-filter-empty-state')).toBeVisible();
    await evidence(page, page.getByTestId('fc-filter-empty-state'), `filter-empty-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
    await page.getByTestId('fc-ae-filter-reset').click();
    await expect(page.getByTestId('fc-filter-empty-state')).toHaveCount(0);
  });
}

for (const theme of ['light', 'dark']) {
  test(`real generated filter-hidden detail stays distinct from missing-item results in ${theme}`, async ({ page }, info) => {
    await ready(page, `${scenarioPath}&theme=${theme}`, theme);
    const grid = page.getByTestId('fc-badge-grid');
    await grid.getByRole('gridcell', { name: 'FC-1000', exact: true }).click();
    await expect(grid.locator('.fc-expand-in-row-detail')).toContainText('FC-1000');
    await grid.locator('[data-fc-status-chip="Success"]').click();
    const clear = grid.getByTestId('fc-expanded-row-hidden-banner-clear');
    await expect(clear).toBeFocused();
    await expect(grid.getByTestId('fc-expanded-row-hidden-banner')).toContainText('hidden');
    await expect(grid.getByTestId('fc-expanded-row-hidden-banner')).not.toContainText('not in the current results');
    await evidence(page, grid.getByTestId('fc-expanded-row-hidden-banner'), `filter-hidden-detail-${theme}`, info);
    await clear.click();
    await expect(grid.locator('.fc-expand-in-row-detail')).toContainText('FC-1000');
  });
}

for (const theme of ['light', 'dark']) {
  test(`delayed generated detail recovery cancels after operator focus and navigation in ${theme}`, async ({ page }) => {
    await page.route('**/fc-focus.js', async route => {
      const response = await route.fetch();
      const source = (await response.text()).replace('export function focusReplacementHeading(key, headingId)', 'function originalFocusReplacementHeading(key, headingId)');
      await route.fulfill({ response, body: source + `
export async function focusReplacementHeading(key, headingId) {
  const gate = window.__aeRecoveryGate;
  if (gate?.hold) {
    gate.entered = true;
    gate.capturedDetached = focusBeforeReplacement.get(key) instanceof HTMLElement && !focusBeforeReplacement.get(key).isConnected;
    await new Promise(resolve => { gate.release = () => { gate.released = true; resolve(); }; });
  }
  if (gate?.hold) gate.captureStillOwned = focusBeforeReplacement.has(key);
  const result = originalFocusReplacementHeading(key, headingId);
  if (gate?.hold) gate.returned = result;
  return result;
}` });
    });
    await ready(page, `${scenarioPath}&theme=${theme}`, theme);
    const grid = page.getByTestId('fc-generated-counter-grid');
    await grid.scrollIntoViewIfNeeded();
    await expect(grid.locator('[data-fc-datagrid]')).toBeVisible();
    await expect(grid.locator('tbody tr.fluent-data-grid-row').first()).toBeVisible();
    await page.getByTestId('fc-ae-rows-two').click();
    // The control above the grid may scroll its virtualized rows out of view.
    // Establish the completed two-row render before entering the focus recovery sequence.
    await grid.scrollIntoViewIfNeeded();
    await expect(grid.locator('tbody tr.fluent-data-grid-row')).toHaveCount(2);
    await expect(grid.getByRole('gridcell', { name: 'FC-AE-2', exact: true })).toBeVisible();
    await grid.getByRole('gridcell', { name: 'FC-AE-1', exact: true }).click();
    const detail = grid.locator('.fc-expand-in-row-detail');
    await expect(detail).toContainText('FC-AE-1');
    await detail.locator('a,button,fluent-button,[tabindex="0"]').first().focus();
    const arm = async () => page.evaluate(() => { (window as typeof window & { __aeRecoveryGate: unknown }).__aeRecoveryGate = { hold: true, entered: false, released: false }; });
    const entered = async () => {
      await expect.poll(() => page.evaluate(() => (window as typeof window & { __aeRecoveryGate: { entered: boolean; capturedDetached?: boolean } }).__aeRecoveryGate)).toMatchObject({ entered: true, capturedDetached: true });
      await expect(detail.locator('a,button,fluent-button,[tabindex="0"]')).toHaveCount(0);
      await expect(grid.locator('.fc-expanded-row-hidden-banner-text')).toBeVisible();
    };
    const release = async () => {
      await page.evaluate(() => (window as typeof window & { __aeRecoveryGate: { release: () => void } }).__aeRecoveryGate.release());
      await expect.poll(() => page.evaluate(() => (window as typeof window & { __aeRecoveryGate: { returned?: boolean; captureStillOwned?: boolean } }).__aeRecoveryGate)).toMatchObject({ returned: false, captureStillOwned: false });
    };
    await arm();
    await page.getByTestId('fc-ae-rows-missing').dispatchEvent('click');
    await entered();
    await page.getByTestId('fc-ae-rows-two').focus();
    await release();
    await expect(page.getByTestId('fc-ae-rows-two')).toBeFocused();
    await page.getByTestId('fc-ae-rows-two').click();
    await expect(detail).toContainText('FC-AE-1');
    await detail.locator('a,button,fluent-button,[tabindex="0"]').first().focus();
    await arm();
    await page.getByTestId('fc-ae-rows-missing').dispatchEvent('click');
    await entered();
    // Keep the entered JS recovery alive across real in-app navigation, then release it.
    await page.evaluate(() => (window as typeof window & { Blazor: { navigateTo: (url: string) => void } }).Blazor.navigateTo('/home'));
    const heading = page.locator('#fc-main-content h1').first();
    await expect(page).toHaveURL(/\/home$/);
    await expect(heading).toBeFocused();
    await release();
    await expect(heading).toBeFocused();
    await expect(page.locator('.fc-expanded-row-hidden-banner-text')).toHaveCount(0);
  });
}

for (const theme of ['light', 'dark']) {
  test(`header theme and account controls retain focus and pointer geometry in ${theme}`, async ({ page }, info) => {
    await ready(page, '/counter', theme);
    const button = page.getByTitle('Change theme').first();
    await button.focus();
    await page.keyboard.press('Shift+Tab');
    await page.keyboard.press('Tab');
    await expect(button).toBeFocused();
    const geometry = await button.evaluate(element => {
      const caret = element.shadowRoot!.querySelector('slot[name="end"] > svg path')!;
      return { button: element.getBoundingClientRect().toJSON(), label: element.querySelector('span[aria-hidden="true"]')!.getBoundingClientRect().toJSON(), caret: caret.getBoundingClientRect().toJSON() };
    });
    expect(geometry.button.width).toBeGreaterThanOrEqual(48);
    for (const paint of [geometry.label, geometry.caret]) {
      expect(paint.left).toBeGreaterThanOrEqual(geometry.button.left);
      expect(paint.right).toBeLessThanOrEqual(geometry.button.right);
      expect(paint.top).toBeGreaterThanOrEqual(geometry.button.top);
      expect(paint.bottom).toBeLessThanOrEqual(geometry.button.bottom);
    }
    await expectFocusVisible(button);
    await expectSurfaceContrast(button, 'focus');
    const avatar = page.locator('#fc-account-avatar');
    const pointer = await avatar.evaluate(element => {
      const box = element.getBoundingClientRect(), center = { x: box.left + box.width / 2, y: box.top + box.height / 2 };
      const misses: { x: number; y: number; hit: string | null }[] = [];
      for (const dx of [-12,-6,0,6,12]) for (const dy of [-12,-6,0,6,12]) {
        const x = center.x + dx, y = center.y + dy;
        let hit = document.elementFromPoint(x,y);
        while (hit?.shadowRoot) { const child = hit.shadowRoot.elementFromPoint(x,y); if (!child || child === hit) break; hit = child; }
        let owned = false;
        for (let node = hit; node; node = node.assignedSlot ?? node.parentElement ?? (node.getRootNode() instanceof ShadowRoot ? (node.getRootNode() as ShadowRoot).host : null)) if (node === element) owned = true;
        if (!owned) misses.push({ x,y,hit:hit?.tagName ?? null });
      }
      return { box:box.toJSON(), square:{left:center.x-12,right:center.x+12,top:center.y-12,bottom:center.y+12}, misses };
    });
    expect(pointer.misses, 'Complete centered 24px square, including every corner, must hit the native avatar').toEqual([]);
    await avatar.click();
    await expect(page.getByTestId('fc-account-user')).toBeVisible();
    await page.keyboard.press('Escape');
    await info.attach(`header-control-geometry-${theme}`, { body: JSON.stringify({ ...geometry,pointer }), contentType: 'application/json' });
  });

  test(`fresh-row presentation retains text boundary and system-color meaning in ${theme}`, async ({ page }, info) => {
    await ready(page, `${scenarioPath}&theme=${theme}`, theme);
    const cue = page.getByTestId('fc-ae-fresh-presentation');
    await expect(cue).toContainText(/new|updated/i);
    await evidence(page, cue, `fresh-presentation-${theme}`, info, undefined, { phase: 'operator-focus-before-fresh-presentation-scroll' });
    await expectSurfaceContrast(cue, 'border');
  });
}

test('paint measurement follows slots, ignores unpainted pseudo borders and sees native modal text through inert ancestors', async ({ page }) => {
  await page.setContent('<style>#edge{border:1px solid #eee;background:white}#edge::after{content:"";border-bottom:2px solid black;transform:scaleX(0);display:block}</style><div id="host" style="color:black;background:black">Slotted readable label</div><div id="edge">Boundary</div><div inert><dialog id="modal"><p>Visible modal text</p></dialog></div>');
  await page.locator('#host').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<style>slot{color:white}</style><slot></slot>'; });
  expect((await expectSurfaceContrast(page.locator('#host')))[0].ratio).toBe(21);
  expect((await expectSurfaceContrast(page.locator('#host').locator('slot')))[0].ratio).toBe(21);
  await expect(expectSurfaceContrast(page.locator('#edge'), 'border')).rejects.toThrow(/contrast failures/);
  await page.addStyleTag({ content: '#edge::after{transform:scaleX(1)}' });
  expect((await expectSurfaceContrast(page.locator('#edge'), 'border')).some(sample => sample.ratio >= 3)).toBe(true);
  await page.locator('#modal').evaluate(element => (element as HTMLDialogElement).showModal());
  await expectSurfaceContrast(page.locator('#modal'));
});


test('validation links keep native same-page href activation when focus enhancement is unavailable', async ({ page }, info) => {
  await page.route('**/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js', async route => {
    const response = await route.fetch();
    const body = (await response.text())
      .replace('export function focusValidationOutcome(', 'export function unavailableFocusValidationOutcome(')
      .replace('export function focusValidationTarget(', 'export function unavailableFocusValidationTarget(')
      + "\nexport function focusValidationOutcome(){throw new Error('Fixed focus-enhancement fixture unavailable');}\nexport function focusValidationTarget(){throw new Error('Fixed focus-enhancement fixture unavailable');}\n";
    await route.fulfill({ response, body });
  });
  await ready(page, '/__frontcomposer/specimens/type', 'light');
  await page.getByTestId('fc-destructive-command-specimen').getByRole('button', { name: 'Purge Specimen Record', exact: true }).click();
  const link = page.getByTestId('fc-validation-summary').getByRole('link', { name: 'The Retention field is required.', exact: true });
  await expect(link).toHaveCount(1);
  const href = await link.getAttribute('href');
  expect(href).toMatch(/__frontcomposer\/specimens\/type#.+-Retention$/u);
  await link.focus();
  await page.keyboard.press('Shift+Tab');
  await page.keyboard.press('Tab');
  await expect(link).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(new URL(href!, await page.locator('base').getAttribute('href').then(base => new URL(base!, page.url()).href)).href);
  await info.attach('native-validation-link-fallback', { body: JSON.stringify({ role: 'link', href, activation: 'keyboard Enter', duplicateKeyboardStops: false }), contentType: 'application/json' });
});

test('G09 forced-color focus geometry rejects a mode-specific clipped ring', async ({ page }) => {
  await page.setContent('<style>button{position:fixed;left:2px;top:40px;width:80px;height:40px;appearance:none;outline:none}button:focus{outline:2px solid black}@media(forced-colors:active){button:focus{outline-width:6px}}</style><button>Save</button>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  await page.emulateMedia({ forcedColors: 'active' });
  expect((await focusGeometry(page.locator('button'))).failures).toContain('viewport clips target or focus ring');
});

for (const theme of ['light', 'dark']) for (const scope of ['tenant', 'user']) {
  test(`held generated recovery cancels after ${scope} scope change in ${theme}`, async ({ page }) => {
    await page.route('**/fc-focus.js', async route => {
      const response = await route.fetch();
      const source = (await response.text()).replace('export function focusReplacementHeading(key, headingId)', 'function originalFocusReplacementHeading(key, headingId)');
      await route.fulfill({ response, body: source + `
export async function focusReplacementHeading(key, headingId) {
  const gate = window.__aeRecoveryGate;
  if (gate?.hold) {
    gate.entered = true;
    gate.capturedDetached = focusBeforeReplacement.get(key) instanceof HTMLElement && !focusBeforeReplacement.get(key).isConnected;
    await new Promise(resolve => { gate.release = () => { gate.released = true; resolve(); }; });
  }
  if (gate?.hold) gate.captureStillOwned = focusBeforeReplacement.has(key);
  const result = originalFocusReplacementHeading(key, headingId);
  if (gate?.hold) gate.returned = result;
  return result;
}` });
    });
    await ready(page, `${scenarioPath}&theme=${theme}`, theme);
    const grid = page.getByTestId('fc-generated-counter-grid');
    await grid.scrollIntoViewIfNeeded();
    await expect(grid.locator('[data-fc-datagrid]')).toBeVisible();
    await expect(grid.locator('tbody tr.fluent-data-grid-row').first()).toBeVisible();
    await page.getByTestId('fc-ae-rows-two').click();
    // The control above the grid may scroll its virtualized rows out of view.
    // Establish the completed two-row render before entering the focus recovery sequence.
    await grid.scrollIntoViewIfNeeded();
    await expect(grid.locator('tbody tr.fluent-data-grid-row')).toHaveCount(2);
    await expect(grid.getByRole('gridcell', { name: 'FC-AE-2', exact: true })).toBeVisible();
    await grid.getByRole('gridcell', { name: 'FC-AE-1', exact: true }).click();
    const detail = grid.locator('.fc-expand-in-row-detail');
    await expect(detail).toContainText('FC-AE-1');
    await detail.locator('a,button,fluent-button,[tabindex="0"]').first().focus();
    const arm = async () => page.evaluate(() => { (window as typeof window & { __aeRecoveryGate: unknown }).__aeRecoveryGate = { hold: true, entered: false, released: false }; });
    const entered = async () => {
      await expect.poll(() => page.evaluate(() => (window as typeof window & { __aeRecoveryGate: { entered: boolean; capturedDetached?: boolean } }).__aeRecoveryGate)).toMatchObject({ entered: true, capturedDetached: true });
      await expect(detail.locator('a,button,fluent-button,[tabindex="0"]')).toHaveCount(0);
      await expect(grid.locator('.fc-expanded-row-hidden-banner-text')).toBeVisible();
    };
    const release = async () => {
      await page.evaluate(() => (window as typeof window & { __aeRecoveryGate: { release: () => void } }).__aeRecoveryGate.release());
      await expect.poll(() => page.evaluate(() => (window as typeof window & { __aeRecoveryGate: { returned?: boolean; captureStillOwned?: boolean } }).__aeRecoveryGate)).toMatchObject({ returned: false, captureStillOwned: false });
    };
    await arm();
    await page.getByTestId('fc-ae-rows-missing').dispatchEvent('click');
    await entered();
    await page.getByTestId(`fc-ae-scope-${scope}`).dispatchEvent('click');
    await expect(page.locator('.fc-expanded-row-hidden-banner-text')).toHaveCount(0);
    const current = page.getByTestId('fc-ae-rows-two');
    await expect(current).toBeVisible();
    await current.focus();
    await release();
    await expect(current).toBeFocused();
    await expect(page.locator('.fc-expanded-row-hidden-banner-text')).toHaveCount(0);
    await expect(page.locator('[data-testid="fc-surface-status"]').filter({ hasText: 'not in the current results' })).toHaveCount(0);
  });
}
