import { expect, test } from '../fixtures/index.js';
import { applyTextSpacing, removeTextSpacing, expectFocusVisible, expectNoPageOverflow, expectNoTextClipping, expectTargetSizes, expectTextContrast, focusGeometry, inventoryTargets, type TargetException } from '../helpers/responsive-accessibility.js';
import { expectNoBlockingAxeViolations } from '../helpers/a11y.js';
import { specimenManifest } from '../helpers/specimen-manifest.js';

// Synthetic failures protect the assertions themselves. These are geometry fixtures, never AT proof.
test.describe('AE geometry regression fixtures', () => {
  test('rejects a clipped ring even when the complete target is inside the viewport', async ({ page }) => {
    await page.setContent('<button style="position:fixed;left:0;top:40px;width:40px;height:30px;outline:3px solid;outline-offset:2px">Save</button>');
    expect((await focusGeometry(page.locator('button'))).failures).toContain('viewport clips target or focus ring');
  });
  test('rejects an off-center occluder and ancestor clipping', async ({ page }) => {
    await page.setContent('<button style="position:absolute;left:40px;top:40px;width:100px;height:40px">Save</button><div style="position:absolute;left:45px;top:45px;width:3px;height:3px;background:red"></div>');
    expect((await focusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
    await page.setContent('<div style="overflow:hidden;width:35px;height:60px"><button style="width:40px;height:30px">Save</button></div>');
    expect((await focusGeometry(page.locator('button'))).failures).toContain('horizontal scrollport clips target or ring');
  });
  test('spacing exceptions reject neighboring circles and accept an isolated target', async ({ page }) => {
    const spacing: TargetException = { selector: '#a', kind: 'Spacing', rationale: 'Isolated fixture target', keyboardPath: 'Tab', evidence: 'Retained geometry inventory' };
    await page.setContent('<button id="a" style="position:absolute;left:40px;top:40px;width:10px;height:10px;padding:0">A</button><button id="b" style="position:absolute;left:60px;top:40px;width:10px;height:10px;padding:0">B</button>');
    expect((await inventoryTargets(page, [spacing])).inventory[0].failure).toContain('neighboring undersized circle');
    await page.locator('#b').evaluate(element => element.remove());
    expect((await inventoryTargets(page, [spacing])).inventory[0].failure).toBeNull();
  });
  test('rejects missing and invalid equivalent inventories', async ({ page }) => {
    await page.setContent('<button id="a" style="width:10px;height:10px;padding:0">A</button>');
    expect((await inventoryTargets(page)).inventory[0].failure).toContain('lacks an evidenced exception');
    const exception: TargetException = { selector: '#a', kind: 'Equivalent', rationale: 'Fixture', keyboardPath: 'Tab', evidence: 'Fixture', equivalentSelector: '#a' };
    expect((await inventoryTargets(page, [exception])).inventory[0].failure).toContain('Equivalent control');
  });
  test('spacing overrides reach Fluent-style shadow content and contrast composites alpha', async ({ page }) => {
    await page.setContent('<main><p id="text" style="color:rgba(0,0,0,.8);background:white">Readable text</p><div id="host"></div></main>');
    await page.locator('#host').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<p>Shadow label</p>'; });
    await applyTextSpacing(page);
    for (const locator of [page.locator('#text'), page.locator('#host p')]) {
      const values = await locator.evaluate(element => { const s = getComputedStyle(element); return { font: parseFloat(s.fontSize), line: parseFloat(s.lineHeight), letter: parseFloat(s.letterSpacing), word: parseFloat(s.wordSpacing), paragraph: parseFloat(s.marginBottom) }; });
      expect(values.line).toBeCloseTo(values.font * 1.5);
      expect(values.letter).toBeCloseTo(values.font * .12);
      expect(values.word).toBeCloseTo(values.font * .16);
      expect(values.paragraph).toBeCloseTo(values.font * 2);
    }
    await expectTextContrast(page.locator('#text'));
  });
});

test('spacing reaches a delayed shadow root and cleanup stops later injection', async ({ page }) => {
  await page.setContent('<main><div id="late"></div><div id="after"></div></main>');
  await applyTextSpacing(page);
  await page.locator('#late').evaluate(element => { element.attachShadow({ mode: 'open' }); });
  await page.locator('#late').evaluate(element => { element.shadowRoot!.innerHTML += '<p style="font-size:16px">Delayed label</p>'; });
  await expect(page.locator('#late p')).toHaveCSS('line-height', '24px');
  await expect(page.locator('#late p')).toHaveCSS('letter-spacing', '1.92px');
  await removeTextSpacing(page);
  await page.locator('#after').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<p>After cleanup</p>'; });
  expect(await page.locator('#after').evaluate(element => element.shadowRoot!.querySelector('[data-ae-text-spacing]'))).toBeNull();
  expect(await page.locator('#late').evaluate(element => element.shadowRoot!.querySelector('[data-ae-text-spacing]'))).toBeNull();
});

test('focus checks reject an absent indicator and a pointer-transparent opaque occluder', async ({ page }) => {
  await page.setContent('<style>button{outline:none!important;box-shadow:none!important}button::before,button::after{content:none!important}</style><button>Absent indicator</button>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
  await page.setContent('<button style="position:absolute;left:40px;top:40px;width:100px;height:40px;outline:2px solid">Save</button><div id="cover" style="position:absolute;left:45px;top:45px;width:5px;height:5px;background:red;pointer-events:none;z-index:2"></div>');
  const original = await page.locator('#cover').getAttribute('style');
  expect((await focusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
  await expect(page.locator('#cover')).toHaveAttribute('style', original!);
  await page.locator('#cover').evaluate(element => (element as HTMLElement).style.background = 'transparent');
  expect((await focusGeometry(page.locator('button'))).failures).not.toContain('app content obscures target or focus ring');
});

test('modal decoration cannot substitute for the focused control indicator', async ({ page }) => {
  await page.setContent('<fluent-dialog><button style="outline:none;box-shadow:none">Bare control</button></fluent-dialog>');
  await page.locator('fluent-dialog').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<div style="padding:20px;box-shadow:0 0 0 3px black"><slot></slot></div>'; });
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
});

test('a one-control keyboard trap cannot satisfy the expected cycle', async ({ page }) => {
  await page.setContent('<button id="a" style="outline:2px solid">First</button><button id="b">Second</button>');
  await page.locator('#a').evaluate(element => { element.addEventListener('keydown', event => { if ((event as KeyboardEvent).key === 'Tab') event.preventDefault(); }); });
  await page.locator('#a').focus();
  await expect(verifyKeyboardCycle(page)).rejects.toThrow(/missed expected reachable controls/);
});

test('the real missing-detail component registers recovery before a Fluent shadow action removes detail', async ({ page }) => {
  await page.goto('/__frontcomposer/specimens/type?announcementFocus=true');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  const fixture = page.getByTestId('fc-missing-detail-fixture');
  const action = fixture.getByTestId('fc-remove-focused-detail');
  await action.focus();
  await page.keyboard.press('Enter');
  const explanation = fixture.locator('.fc-expanded-row-hidden-banner-text');
  await expect(explanation).toBeVisible();
  await expect(explanation).toBeFocused();
  await expect(explanation).toContainText('not in the current results');
  await expect(fixture.getByTestId('fc-expanded-row-hidden-banner-clear')).toHaveCount(0);
});

const surfaces = [
  { name: 'Home directory', path: '/home', ready: '#fc-main-content h1' },
  { name: 'module navigation and tabs', path: '/counter/counter-projection', ready: '#fc-main-content h1' },
  ...specimenManifest.routes.map(route => ({ name: route.name, path: route.path, ready: route.readySelector })),
];

test.describe('Story 13.6 responsive operator surfaces', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => { localStorage.clear(); sessionStorage.clear(); });
    await page.emulateMedia({ reducedMotion: 'reduce' });
  });
  for (const surface of surfaces) {
    for (const spacing of [false, true]) {
      test(`${surface.name}: 320 CSS-pixel reflow${spacing ? ' with all text-spacing overrides' : ''}`, async ({ page }, testInfo) => {
        await page.setViewportSize({ width: 320, height: 900 });
        await page.goto(surface.path);
        await expect(page.locator(surface.ready).first()).toBeVisible();
        await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
        if (spacing) { await applyTextSpacing(page); await expectNoTextClipping(page); }
        await expectNoPageOverflow(page);
        const inventory = await expectTargetSizes(page);
        await testInfo.attach('AE-04-target-inventory', { body: JSON.stringify(inventory, null, 2), contentType: 'application/json' });
        await expectNoBlockingAxeViolations(page, { route: surface.path, artifactPath: testInfo.outputPath('axe-wcag22.json') });
        // Traverse actual Tab stops, checking the complete focused rectangle and ring after browser scrolling.
        await page.locator('#fc-main-content h1').first().focus();
        await verifyKeyboardCycle(page);

      });
    }
  }
  for (const theme of ['light', 'dark']) {
    test(`type states retain contrast, system colors and immediate reduced-motion cues in ${theme}`, async ({ page }, testInfo) => {
      await page.goto(`/__frontcomposer/specimens/type?theme=${theme}&density=compact`);
      await expect(page.getByTestId('fc-type-specimen')).toBeVisible();
      const samples = [];
      for (const selector of ['#fc-main-content h1', '[data-testid="fc-lifecycle-idle"]']) {
        const target = page.locator(selector).first();
        await expect(target).toBeVisible();
        samples.push({ selector, ...await expectTextContrast(target) });
      }
      await testInfo.attach(`contrast-${theme}`, { body: JSON.stringify(samples), contentType: 'application/json' });
      await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
      await expectNoPageOverflow(page);
      expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches && matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
      const state = page.getByTestId('fc-lifecycle-idle');
      await expect(state).toContainText('Ready');
      expect(await state.evaluate(element => getComputedStyle(element).forcedColorAdjust)).toBe('auto');
      const motion = await page.locator('.fc-expand-button,.fc-row-expanded,.fc-reconciliation-sweep').evaluateAll(elements => elements.map(element => ({ transition: getComputedStyle(element).transitionDuration, animation: getComputedStyle(element).animationName })));
      expect(motion.every(sample => sample.transition.split(',').every(duration => parseFloat(duration) === 0) && sample.animation === 'none')).toBe(true);
      await page.screenshot({ path: testInfo.outputPath(`forced-colors-${theme}.png`) });

      await page.emulateMedia({ forcedColors: 'none' });
      await page.setViewportSize({ width: 1280, height: 900 });
      await page.goto('/counter/counter-projection');
      await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
      const themeLabel = theme === 'dark' ? 'Dark' : 'Light';
      await page.getByTitle('Change theme').first().click();
      await page.getByRole('menuitem', { name: themeLabel, exact: true }).click();
      await expect(page.getByTitle('Change theme').first()).toContainText(themeLabel);
      const header = await page.getByRole('banner').boundingBox();
      const actions = await page.locator('.fc-shell-header-actions').boundingBox();
      expect(header!.height).toBe(48);
      expect(actions!.height).toBeLessThanOrEqual(40);
      expect(actions!.x + actions!.width).toBeLessThanOrEqual(1280);
      expect(actions!.x).toBeGreaterThan(640);

      await page.getByTestId('fc-palette-trigger').click();
      await expect(page.getByTestId('fc-palette-root')).toBeVisible();
      const paletteSamples = [];
      for (const selector of ['.fc-palette-group-heading', '.fc-palette-option-label', '.fc-palette-option-hint']) {
        const text = page.locator(selector).first();
        await expect(text).toBeVisible();
        paletteSamples.push({ selector, state: 'default', ...await expectTextContrast(text) });
      }
      const search = page.getByTestId('fc-palette-search').locator('input');
      await search.fill('counter');
      const context = page.locator('.fc-palette-option-context').first();
      await expect(context).toBeVisible();
      paletteSamples.push({ selector: '.fc-palette-option-context', state: 'query/current-context', ...await expectTextContrast(context) });
      const selected = page.locator('.fc-palette-option-selected .fc-palette-option-label');
      await expect(selected).toBeVisible();
      paletteSamples.push({ selector: '.fc-palette-option-label', state: 'selected', ...await expectTextContrast(selected) });
      await context.hover();
      paletteSamples.push({ selector: '.fc-palette-option-context', state: 'hover', ...await expectTextContrast(context) });
      await search.fill('no-match-136-contrast');
      const noResults = page.locator('.fc-palette-noresults');
      await expect(noResults).toBeVisible();
      paletteSamples.push({ selector: '.fc-palette-noresults', state: 'no-results', ...await expectTextContrast(noResults) });
      await testInfo.attach(`palette-contrast-${theme}`, { body: JSON.stringify(paletteSamples), contentType: 'application/json' });
    });
  }
  test('palette and settings retain reflow, every target, and modal focus at 320px with spacing', async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 320, height: 900 });
    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    for (const overlay of [{ trigger: 'fc-palette-trigger', root: 'fc-palette-root' }, { trigger: 'fc-settings-button', root: 'fc-settings-dialog' }]) {
      await page.getByTestId(overlay.trigger).click();
      await expect(page.getByTestId(overlay.root)).toBeVisible();
      await applyTextSpacing(page);
      await expectNoPageOverflow(page);
      const exceptions: TargetException[] = overlay.root === 'fc-settings-dialog' ? ['Compact', 'Comfortable', 'Roomy'].map(value => ({
        selector: `fluent-radio[name="fc-density-select"][value="${value}"]`, kind: 'Spacing',
        rationale: `Fluent ${value} density radio retains its Fluent visual size; neighboring distinct actions must clear its 24px circle.`,
        keyboardPath: `Settings → Density → ${value} (Tab/Arrow keys)`,
        evidence: `${overlay.root}-targets attachment: measured 24px circle versus all active modal targets and undersized circles`,
      })) : [];
      const inventory = await expectTargetSizes(page, exceptions);
      await testInfo.attach(`${overlay.root}-targets`, { body: JSON.stringify(inventory), contentType: 'application/json' });
      await verifyKeyboardCycle(page, true);
      await page.keyboard.press('Escape');
    }
  });
});

test('missing-detail recovery consumes only lost detail focus and cancels stale captures', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  const result = await page.evaluate(async () => {
    const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(path);
    const host = document.createElement('div');
    host.setAttribute('data-fc-datagrid', 'geometry-fixture');
    host.style.cssText = 'position:fixed;inset:120px 12px auto;z-index:2000;padding:8px;background:var(--colorNeutralBackground1)';
    host.innerHTML = '<div class="fc-expand-in-row-detail"><button>Detail action</button></div><span id="missing-fixture" tabindex="-1">The expanded item is not in the current results.</span><button id="other-fixture">Other action</button>';
    document.body.append(host);
    focus.trackDetailFocus('fixture-recovery', 'geometry-fixture');
    const detail = host.querySelector('button')!;
    detail.focus();
    detail.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, composed: true }));
    detail.remove();
    const recovered = focus.focusReplacementHeading('fixture-recovery', 'missing-fixture');
    const focusedExplanation = document.activeElement?.id === 'missing-fixture';
    const shadowHost = document.createElement('div');
    const shadowButton = document.createElement('button'); shadowButton.textContent = 'Shadow action';
    shadowHost.attachShadow({ mode: 'open' }).append(shadowButton);
    host.querySelector('.fc-expand-in-row-detail')!.append(shadowHost);
    shadowButton.focus();
    shadowButton.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, composed: true }));
    shadowHost.remove();
    const shadowRecovered = focus.focusReplacementHeading('fixture-recovery', 'missing-fixture');
    const explanation = host.querySelector<HTMLElement>('#missing-fixture')!;
    const bounds = explanation.getBoundingClientRect();
    const visibleExplanation = explanation.checkVisibility() && bounds.left >= 0 && bounds.top >= 0
      && bounds.right <= innerWidth && bounds.bottom <= innerHeight;
    host.querySelector('.fc-expand-in-row-detail')!.append(detail);
    detail.focus();
    host.querySelector<HTMLButtonElement>('#other-fixture')!.focus();
    detail.remove();
    const preserved = !focus.focusReplacementHeading('fixture-recovery', 'missing-fixture') && document.activeElement?.id === 'other-fixture';
    host.querySelector('.fc-expand-in-row-detail')!.append(detail);
    detail.focus();
    focus.cancelReplacementFocus('fixture-recovery');
    detail.remove();
    const cancelled = !focus.focusReplacementHeading('fixture-recovery', 'missing-fixture');
    host.remove();
    return { recovered, focusedExplanation, visibleExplanation, shadowRecovered, preserved, cancelled };
  });
  expect(result).toEqual({ recovered: true, focusedExplanation: true, visibleExplanation: true, shadowRecovered: true, preserved: true, cancelled: true });
});

async function verifyKeyboardCycle(page: import('@playwright/test').Page, modal = false) {
  const expectedCount = await page.evaluate((requireModal) => {
    const all: Element[] = [];
    const visit = (root: Document | ShadowRoot) => root.querySelectorAll('*').forEach(element => { all.push(element); if (element.shadowRoot) visit(element.shadowRoot); });
    visit(document);
    const parent = (element: Element): Element | null => element.assignedSlot ?? element.parentElement ?? (element.getRootNode() instanceof ShadowRoot ? (element.getRootNode() as ShadowRoot).host : null);
    const contains = (ancestor: Element, child: Element) => { for (let current: Element | null = child; current; current = parent(current)) if (current === ancestor) return true; return false; };
    const visible = (element: Element) => { const box = element.getBoundingClientRect(); const css = getComputedStyle(element); return box.width > 0 && box.height > 0 && css.visibility !== 'hidden' && css.display !== 'none'; };
    const dialog = all.filter(element => element.matches('dialog[open]') && visible(element)).at(-1);
    const root = dialog?.getRootNode();
    const activeModal = root instanceof ShadowRoot ? root.host : dialog;
    if (requireModal && !activeModal) throw new Error('Expected an active modal');
    const expected = [...new Set(all.filter(element => element instanceof HTMLElement && element.tabIndex >= 0 && visible(element)
      && !element.matches(':disabled,[disabled],[aria-disabled="true"]')
      && !Array.from((function* () { for (let current: Element | null = element; current; current = parent(current)) yield current; })()).some(ancestor => ancestor.hasAttribute('inert') || ancestor.getAttribute('aria-hidden') === 'true'))
      .map(element => { let owner = element; while (owner.getRootNode() instanceof ShadowRoot) owner = (owner.getRootNode() as ShadowRoot).host; return owner; }))]
      .filter(element => !activeModal || contains(activeModal, element));
    const radios = expected.filter(element => element.matches('input[type="radio"],[role="radio"],fluent-radio'));
    const radioGroup = (element: Element) => element.getAttribute('name') ?? element.closest('fluent-radio-group,[role="radiogroup"]')?.id ?? '';
    const tabs = expected.filter(element => element.matches('[role="tab"],fluent-tab'));
    const tabGroup = (element: Element) => element.closest('[role="tablist"],fluent-tablist');
    const selectedTab = (element: Element) => element.getAttribute('aria-selected') === 'true' || Boolean(element.shadowRoot?.querySelector('[aria-selected="true"]'));
    const stops = expected.filter(element => (!radios.includes(element) || element === (radios.filter(other => radioGroup(other) === radioGroup(element)).find(other => (other as HTMLInputElement).checked || other.getAttribute('aria-checked') === 'true') ?? radios.find(other => radioGroup(other) === radioGroup(element))))
      && (!tabs.includes(element) || element === (tabs.filter(other => tabGroup(other) === tabGroup(element)).find(selectedTab) ?? tabs.find(other => tabGroup(other) === tabGroup(element)))));
    const describe = (element: Element) => `${element.tagName.toLowerCase()}${element.id ? `#${element.id}` : ''}${element.getAttribute('data-testid') ? `[${element.getAttribute('data-testid')}]` : ''}${element.getAttribute('role') ? ` role=${element.getAttribute('role')}` : ''} ${(element.getAttribute('aria-label') ?? element.textContent ?? '').trim().replace(/\s+/gu, ' ').slice(0, 70)}`;
    // Fluent's native focusgroup exposes one Tab stop; its entry member can differ from the checked radio.
    const canonical = (element: Element) => radios.includes(element) ? stops.find(stop => radios.includes(stop) && radioGroup(stop) === radioGroup(element)) ?? element : element;
    (window as unknown as { aeKeyboard: unknown }).aeKeyboard = { stops, activeModal, contains, describe, canonical };
    return stops.length;
  }, modal);
  expect(expectedCount, 'Expected reachable keyboard controls').toBeGreaterThan(0);
  let previousIndex = -1;
  const step = async (reverse = false) => {
    for (let outside = 0; outside < 3; outside++) {
      await page.keyboard.press(reverse ? 'Shift+Tab' : 'Tab');
      const result = await page.evaluate(() => {
        const state = (window as unknown as { aeKeyboard: { stops: Element[]; activeModal?: Element; contains: (ancestor: Element, child: Element) => boolean; describe: (element: Element) => string; canonical: (element: Element) => Element } }).aeKeyboard;
        const active = document.activeElement;
        if (!document.hasFocus() || !active || active === document.body) return { outside: true, modal: Boolean(state.activeModal), index: -1 };
        return { outside: false, modal: Boolean(state.activeModal), member: !state.activeModal || state.contains(state.activeModal, active), index: state.stops.indexOf(state.canonical(active)), control: state.describe(active) };
      });
      if (result.outside) {
        expect(result.modal, 'Modal keyboard focus left its document').toBe(false);
        expect(previousIndex, 'Document focus left before its complete boundary').toBe(reverse ? 0 : expectedCount - 1);
        // Firefox retains the last activeElement while focus is in browser chrome, and
        // automation cannot Tab through browser UI. Resume at the opposite document
        // boundary only after proving the final app stop; modal escapes always fail.
        await page.bringToFront();
        const boundary = await page.evaluate(backward => {
          const state = (window as unknown as { aeKeyboard: { stops: HTMLElement[] } }).aeKeyboard;
          const index = backward ? state.stops.length - 1 : 0;
          state.stops[index].focus(); return index;
        }, reverse);
        previousIndex = boundary;
        await expectFocusVisible(page.locator(':focus').first());
        return boundary;
      }
      expect(result.member, 'Focus left the active modal').toBe(true);
      expect(result.index, `Focused control was absent from the reachable inventory: ${result.control}`).toBeGreaterThanOrEqual(0);
      await expectFocusVisible(page.locator(':focus').first());
      previousIndex = result.index;
      return result.index;
    }
    throw new Error('Keyboard focus did not return from browser chrome');
  };
  const sequence: number[] = [];
  for (let stop = 0; stop <= expectedCount; stop++) {
    const index = await step();
    if (sequence.includes(index)) {
      expect(index, 'Forward cycle returned to a different control').toBe(sequence[0]);
      const missing = await page.evaluate(seen => { const state = (window as unknown as { aeKeyboard: { stops: Element[]; describe: (element: Element) => string; canonical: (element: Element) => Element } }).aeKeyboard; return state.stops.filter((_, index) => !seen.includes(index)).map(state.describe); }, sequence);
      expect(missing, 'Cycle missed expected reachable controls').toEqual([]);
      for (const expected of [...sequence].reverse()) expect(await step(true), 'Reverse boundary/order missed a control').toBe(expected);
      return;
    }
    sequence.push(index);
  }
  throw new Error('Keyboard inventory did not complete its expected cycle');
}

test('row metrics preserve the compact minimum and report growth with the matching density floor', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.evaluate(async () => {
    const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-datagrid.js';
    const grid = await import(modulePath);
    const root = document.createElement('div');
    root.id = 'row-metric-fixture'; root.dataset.fcDatagrid = 'fixture'; root.dataset.fcDatagridInstance = 'fixture-instance';
    root.innerHTML = '<table class="fc-projection-grid"><tbody><tr class="fluent-data-grid-row"><td style="font-size:16px;line-height:20px">Cell</td></tr></tbody></table>';
    document.body.append(root);
    root.dataset.observed = '0';
    grid.observeRowMetrics('fixture', 'fixture-instance', { invokeMethodAsync: (_: string, _id: string, height: number, floor: number) => {
      root.dataset.observed = String(height); root.dataset.floor = String(floor); return Promise.resolve();
    } }, 32);
  });
  const root = page.locator('#row-metric-fixture');
  await expect.poll(() => root.evaluate(element => element.style.getPropertyValue('--fc-grid-row-height'))).toBe('32px');
  await expect(root).toHaveAttribute('data-observed', '32');
  await root.locator('td').evaluate(element => { element.style.fontSize = '28px'; element.style.lineHeight = '42px'; });
  await expect.poll(() => root.evaluate(element => Number((element as HTMLElement).dataset.observed))).toBeGreaterThan(32);
  await expect(root).toHaveAttribute('data-floor', '32');
  await root.locator('td').evaluate(element => { element.style.fontSize = '16px'; element.style.lineHeight = '20px'; element.innerHTML = '<div>First block</div><div>Second line<br>Third line</div>'; });
  await expect.poll(() => root.evaluate(element => Number((element as HTMLElement).dataset.observed))).toBeGreaterThanOrEqual(60);
  await root.locator('td').evaluate(element => { element.textContent = 'Cell'; });
  await expect(root).toHaveAttribute('data-observed', '32');
  await page.evaluate(async () => {
    const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-datagrid.js';
    const grid = await import(modulePath); grid.disposeViewKey('fixture', undefined, 'fixture-instance');
    document.getElementById('row-metric-fixture')!.remove();
  });
});

test('real generated compact rows preserve 32px, match ItemSize through spacing, and restore after density changes', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/__frontcomposer/specimens/type?generatedGrid=true&density=compact');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  const table = page.getByTestId('fc-generated-counter-grid').locator('table.fluent-data-grid');
  await expect(table).toBeVisible();
  const density = async (name: string) => {
    await page.getByTestId(`fc-density-control-${name.toLowerCase()}`).click();
    await expect(page.getByTestId('fc-type-specimen')).toHaveAttribute('data-density', name.toLowerCase());
  };
  const metrics = () => table.evaluate(element => ({ itemSize: Number(element.getAttribute('data-fc-grid-item-size')), rows: [...element.querySelectorAll('tbody tr.fluent-data-grid-row')].map(row => row.getBoundingClientRect().height) }));
  await expect.poll(metrics).toEqual({ itemSize: 32, rows: [32] });
  const grid = page.getByTestId('fc-generated-counter-grid').locator('[data-fc-datagrid]').first();
  await grid.focus();
  // Use the operator's Tab path so the initial route-heading restoration guard is released.
  for (let step = 0; step < 10 && !await table.locator('fluent-button').first().evaluate(element => element === document.activeElement); step++) await page.keyboard.press('Tab');
  await expect(table.locator('fluent-button').first()).toBeFocused();
  const focusedControl = await table.locator('fluent-button').first().elementHandle();
  await applyTextSpacing(page);
  await expect.poll(async () => { const measured = await metrics(); return measured.itemSize >= 32 && measured.rows.length > 0 && measured.rows.every(height => Math.abs(height - measured.itemSize) < .5); }).toBe(true);
  const spaced = await metrics();
  // Intrinsic widths can keep spaced text on one line. Growth follows painted content,
  // while the density floor stays exact; the larger text below guarantees a remount.
  if (spaced.itemSize > 32) {
    await expect.poll(() => focusedControl!.evaluate(element => element.isConnected)).toBe(false);
    await expect(grid).toBeFocused();
  } else {
    expect(await focusedControl!.evaluate(element => element.isConnected)).toBe(true);
    await expect(table.locator('fluent-button').first()).toBeFocused();
  }
  await testInfo.attach('generated-compact-spacing-row-metrics', { body: JSON.stringify(spaced), contentType: 'application/json' });
  // A separately labelled user-text-size stress exercises real generated content
  // beyond canonical spacing, while the virtualizer must follow the rendered height.
  const largerText = await page.addStyleTag({ content: '[data-testid="fc-generated-counter-grid"] td { font-size:24px !important; }' });
  await largerText.evaluate(element => (element as Element).setAttribute('data-ae-text-spacing', 'additional-user-text-size-stress'));
  await expect.poll(async () => (await metrics()).itemSize).toBeGreaterThan(32);
  await expect.poll(() => focusedControl!.evaluate(element => element.isConnected)).toBe(false);
  await expect(grid).toBeFocused();
  const firstGrowth = (await metrics()).itemSize;
  await page.getByTestId('fc-command-input').focus();
  await largerText.evaluate(element => { element.textContent = element.textContent!.replace('24px', '32px'); });
  await expect.poll(async () => (await metrics()).itemSize).toBeGreaterThan(firstGrowth);
  await expect.poll(async () => { const measured = await metrics(); return measured.rows.length > 0 && measured.rows.every(height => Math.abs(height - measured.itemSize) < .5); }).toBe(true);
  await testInfo.attach('generated-compact-larger-user-text-row-metrics', { body: JSON.stringify(await metrics()), contentType: 'application/json' });
  await expect(page.getByTestId('fc-command-input')).toBeFocused();
  await density('Roomy');
  await expect.poll(async () => (await metrics()).itemSize).toBeGreaterThanOrEqual(56);
  await removeTextSpacing(page);
  await expect.poll(metrics).toEqual({ itemSize: 56, rows: [56] });
  await density('Compact');
  await expect.poll(metrics).toEqual({ itemSize: 32, rows: [32] });
});
