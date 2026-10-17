import { expect, test } from '../fixtures/index.js';
import { applyTextSpacing, removeTextSpacing, expectFocusVisible, expectNoPageOverflow, expectNoTextClipping, expectTargetSizes, expectTextContrast, expectSurfaceContrast, focusGeometry, inventoryTargets, screenshotPreservingScroll, settlePaint, measureSurfaceContrast, type TargetException } from '../helpers/responsive-accessibility.js';
import { expectNoBlockingAxeViolations } from '../helpers/a11y.js';
import { specimenManifest } from '../helpers/specimen-manifest.js';

async function fixtureFocusGeometry(target: import('@playwright/test').Locator) {
  await target.evaluate(element => {
    element.setAttribute('data-ae-focus-fixture', 'true');
    const style = document.createElement('style');
    style.textContent = '[data-ae-focus-fixture]:not(:focus){outline:none!important;box-shadow:none!important}[data-ae-focus-fixture]:not(:focus)::before,[data-ae-focus-fixture]:not(:focus)::after{content:none!important}';
    document.head.append(style);
  });
  await target.focus();
  return focusGeometry(target);
}

// Synthetic failures protect the assertions themselves. These are geometry fixtures, never AT proof.
test.describe('AE geometry regression fixtures', () => {
  test('rejects a clipped ring even when the complete target is inside the viewport', async ({ page }) => {
    await page.setContent('<button style="position:fixed;left:0;top:40px;width:40px;height:30px;outline:3px solid;outline-offset:2px">Save</button>');
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('viewport clips target or focus ring');
  });
  test('includes outer box-shadow rings and offsets in clipping geometry', async ({ page }) => {
    await page.setContent('<button style="position:fixed;left:4px;top:40px;width:40px;height:30px;appearance:none;outline:none;box-shadow:0 0 0 6px black">Save</button>');
    const first = await fixtureFocusGeometry(page.locator('button'));
    expect(first.box.left).toBe(-2);
    expect(first.failures).toContain('viewport clips target or focus ring');
    await page.locator('button').evaluate(element => { element.style.left = '20px'; element.style.boxShadow = '-18px 0 0 6px black'; });
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('viewport clips target or focus ring');
    await page.locator('button').evaluate(element => { element.style.boxShadow = 'inset 0 0 0 6px black,0 0 0 6px transparent'; });
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).not.toContain('viewport clips target or focus ring');
    await page.setContent('<style>button{position:fixed;left:4px;top:40px;width:40px;height:30px;appearance:none;outline:none}button::after{content:"";position:absolute;inset:-8px;outline:3px solid black}</style><button>Save</button>');
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('viewport clips target or focus ring');
    await page.setContent('<style>button{position:relative;width:40px;height:30px;appearance:none;outline:none}button::after{content:"";position:absolute;inset:-8px;outline:3px solid black}</style><div style="overflow:hidden;width:80px;height:80px;padding:4px"><button>Save</button></div>');
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('horizontal scrollport clips target or ring');
    await page.setContent('<div style="overflow:hidden;width:80px;height:80px;padding:4px"><button style="width:40px;height:30px;appearance:none;outline:none;box-shadow:0 0 0 6px black">Save</button></div>');
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('horizontal scrollport clips target or ring');
    await page.locator('button').evaluate(element => { element.style.boxShadow = '0 0 6px black'; });
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('blurred focus shadow needs pixel evidence');
    await page.setContent('<button style="position:fixed;left:40px;top:40px;width:80px;height:40px;appearance:none;outline:2px solid black;box-shadow:0 0 6px black">Save</button>');
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toEqual([]);
    await page.setContent('<div style="position:fixed;left:40px;top:40px;width:100px;height:60px;overflow:hidden;padding:3px"><button style="width:80px;height:40px;appearance:none;outline:2px solid black;box-shadow:0 0 6px black">Save</button></div>');
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toEqual([]);
    await page.locator('button').evaluate(element => { element.style.outlineWidth = '6px'; });
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('horizontal scrollport clips target or ring');
  });
  test('detects an opaque fixed occluder inside a zero-box open shadow host', async ({ page }) => {
    await page.setContent('<button style="position:absolute;left:40px;top:40px;width:100px;height:40px;outline:2px solid">Save</button><div id="host" style="display:contents"></div>');
    await page.locator('#host').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<div style="position:fixed;left:45px;top:45px;width:8px;height:8px;background:black;z-index:10"></div>'; });
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
    await page.locator('#host').evaluate(element => { (element.shadowRoot!.firstElementChild as HTMLElement).style.background = 'transparent'; (element.shadowRoot!.firstElementChild as HTMLElement).style.pointerEvents = 'none'; });
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).not.toContain('app content obscures target or focus ring');
  });
  test('a real Fluent dropdown owns its passive sibling indicator, while an overlapping shadow sibling still occludes', async ({ page }, info) => {
    await page.setViewportSize({ width: 320, height: 900 });
    await page.goto('/__frontcomposer/specimens/type');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const dropdown = page.locator('fluent-dropdown').first();
    const control = dropdown.locator('button[slot="control"][role="combobox"]');
    await control.focus();
    const ownership = await control.evaluate(button => {
      const host = button.parentElement!;
      const icon = host.shadowRoot!.querySelector('slot[name="indicator"] svg[aria-hidden="true"]')!;
      return { controlSlot: button.assignedSlot?.name, indicatorSlot: icon.parentElement?.getAttribute('name'), sharedShadow: button.assignedSlot?.getRootNode() === icon.getRootNode(), directDescendant: button.contains(icon) };
    });
    expect(ownership).toEqual({ controlSlot: 'control', indicatorSlot: 'indicator', sharedShadow: true, directDescendant: false });
    await info.attach('dropdown-composed-ownership', { body: JSON.stringify(ownership), contentType: 'application/json' });
    expect((await focusGeometry(control)).failures).toEqual([]);
    await dropdown.evaluate(host => {
      const overlay = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
      overlay.setAttribute('aria-hidden', 'true');
      overlay.setAttribute('style', 'position:absolute;right:8px;top:8px;width:12px;height:12px;z-index:100');
      overlay.innerHTML = '<rect width="12" height="12" fill="black"/>';
      host.shadowRoot!.querySelector('.control')!.append(overlay);
    });
    expect((await focusGeometry(control)).failures).toContain('app content obscures target or focus ring');
  });
  test('rejects an off-center occluder and ancestor clipping', async ({ page }) => {
    await page.setContent('<button style="position:absolute;left:40px;top:40px;width:100px;height:40px">Save</button><div style="position:absolute;left:45px;top:45px;width:3px;height:3px;background:red"></div>');
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
    await page.setContent('<div style="overflow:hidden;width:35px;height:60px"><button style="width:40px;height:30px">Save</button></div>');
    expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('horizontal scrollport clips target or ring');
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
  expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
  await page.setContent('<button style="position:absolute;left:40px;top:40px;width:100px;height:40px;outline:2px solid">Save</button><div id="cover" style="position:absolute;left:45px;top:45px;width:5px;height:5px;background:red;pointer-events:none;z-index:2"></div>');
  const original = await page.locator('#cover').getAttribute('style');
  expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
  await expect(page.locator('#cover')).toHaveAttribute('style', original!);
  await page.locator('#cover').evaluate(element => (element as HTMLElement).style.background = 'transparent');
  expect((await fixtureFocusGeometry(page.locator('button'))).failures).not.toContain('app content obscures target or focus ring');
});

test('modal decoration cannot substitute for the focused control indicator', async ({ page }) => {
  await page.setContent('<fluent-dialog><button style="outline:none;box-shadow:none">Bare control</button></fluent-dialog>');
  await page.locator('fluent-dialog').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<div style="padding:20px;box-shadow:0 0 0 3px black"><slot></slot></div>'; });
  await page.locator('button').focus();
  expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
});

test('text clipping follows native top-layer popovers while retaining their own clipping checks', async ({ page }) => {
  await page.setContent('<div style="overflow:hidden;width:20px;height:20px"><div popover id="top"><p>Complete top-layer text outside its ancestor.</p><p id="inner">Complete inner text</p></div></div>');
  await page.locator('#top').evaluate(element => (element as HTMLElement).showPopover());
  await expectNoTextClipping(page);
  await page.locator('#inner').evaluate(element => { element.style.cssText = 'overflow:hidden;width:10px;height:10px;white-space:nowrap'; });
  await expect(expectNoTextClipping(page)).rejects.toThrow(/clipped text ranges/);
});

test('a one-control keyboard trap cannot satisfy the expected cycle', async ({ page }) => {
  await page.setContent('<style>button{margin:8px}button:not(:focus){outline:none!important}</style><button id="a" style="outline:2px solid">First</button><button id="b">Second</button>');
  await page.locator('#a').evaluate(element => { element.addEventListener('keydown', event => { if ((event as KeyboardEvent).key === 'Tab') event.preventDefault(); }); });
  await page.locator('#a').focus();
  await expect(verifyKeyboardCycle(page)).rejects.toThrow(/Application trapped Tab/);
});

test('an application-prevented Tab at the final control cannot impersonate browser chrome', async ({ page }) => {
  await page.setContent('<style>button{margin:8px}button:not(:focus){outline:none!important}</style><button id="a" style="outline:2px solid">First</button><button id="b" style="outline:2px solid">Last</button>');
  await page.locator('#b').evaluate(element => { element.addEventListener('keydown', event => { if ((event as KeyboardEvent).key === 'Tab') event.preventDefault(); }); });
  await page.locator('#a').focus();
  await expect(verifyKeyboardCycle(page)).rejects.toThrow(/Application trapped Tab/);
});

test('shell focus scrolling protects the ring and cancels stale or disposed work', async ({ page }) => {
  await page.goto('/home');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.evaluate(async () => {
    const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
    const keyboard = await import(modulePath);
    const root = document.createElement('div');
    root.id = 'ae-scroll-shell';
    root.style.cssText = 'position:fixed;inset:220px 20px auto;height:100px;overflow:auto;z-index:2000;background:Canvas;scroll-padding:8px;padding:8px;box-sizing:border-box';
    root.innerHTML = '<style>#ae-scroll-shell button{outline:none;scroll-margin:8px}#ae-scroll-shell button:focus{outline:4px solid;outline-offset:3px}</style><div style="height:180px"></div><button>Last action</button><div style="height:30px"></div>';
    document.body.append(root);
    keyboard.registerShellKeyFilter(root);
    root.querySelector('button')!.focus({ preventScroll: true });
  });
  const action = page.locator('#ae-scroll-shell button');
  await expectFocusVisible(action);
  const cancelled = await page.evaluate(async () => {
    const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
    const keyboard = await import(modulePath);
    const root = document.getElementById('ae-scroll-shell')!;
    const action = root.querySelector('button')!;
    action.blur(); root.scrollTop = 0;
    action.focus({ preventScroll: true });
    document.querySelector<HTMLElement>('[data-testid="fc-palette-trigger"]')!.focus({ preventScroll: true });
    await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
    const moved = root.scrollTop === 0;
    action.focus({ preventScroll: true });
    keyboard.unregisterShellKeyFilter(root);
    await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
    const disposed = root.scrollTop === 0;
    root.remove();
    return { moved, disposed };
  });
  expect(cancelled).toEqual({ moved: true, disposed: true });
});

test('delayed overlay disposal preserves an operator Tab after native origin return', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.getByTestId('fc-palette-trigger').focus();
  await page.evaluate(async () => {
    const focusPath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const keyboardPath = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
    const focus = await import(focusPath) as { captureOverlayOrigin: (testId: string, preserveExisting: boolean) => boolean };
    const keyboard = await import(keyboardPath) as { focusElement: (element: Element) => void };
    if (!focus.captureOverlayOrigin('fc-palette-trigger', true)) throw new Error('fixture could not reserve the modal');
    const dialog = document.createElement('dialog');
    dialog.innerHTML = '<input data-testid="fc-palette-search">';
    document.body.append(dialog); dialog.showModal();
    keyboard.focusElement(dialog.querySelector('input')!);
    await new Promise(resolve => setTimeout(resolve, 200));
    if ((window as typeof window & { __fcOverlayOpenIntent?: unknown }).__fcOverlayOpenIntent !== null) throw new Error('real palette entry did not consume its intent');
    dialog.close(); dialog.remove();
  });
  await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  await page.keyboard.press('Tab');
  const settings = page.getByTestId('fc-settings-button');
  await expect(settings).toBeFocused();
  await page.evaluate(async () => {
    const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(modulePath) as { restoreOverlayOrigin: () => void };
    focus.restoreOverlayOrigin();
    await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
    if ((window as typeof window & { __fcModalReservation?: unknown }).__fcModalReservation) throw new Error('dismissal retained its modal slot');
  });
  await expect(settings).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#fc-settings-heading')).toBeFocused();
});

test('owned palette return cancels the actual Fluent delayed return after an operator Tab', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.getByTestId('fc-palette-trigger').click();
  await expect(page.getByTestId('fc-palette-search')).toBeFocused();
  await page.evaluate(async () => {
    const path = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(path) as { containDialogFocus: (testId: string) => boolean };
    if (!focus.containDialogFocus('fc-palette-root')) throw new Error('palette containment did not attach');
    const host = document.querySelector<HTMLElement>('[data-testid="fc-palette-root"]')!.closest('fluent-dialog')!;
    const w = window as typeof window & { __aeVendorReturn?: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } };
    const callbacks: (() => void)[] = [];
    const original = window.setTimeout;
    window.setTimeout = ((handler: TimerHandler, delay?: number, ...args: unknown[]) => {
      // Hold the pinned Fluent callback itself, rather than substitute a synthetic return.
      if (delay === 25 && typeof handler === 'function' && new Error().stack?.includes('Microsoft.FluentUI.AspNetCore.Components.lib.module.js')) {
        callbacks.push(() => handler(...args));
        return 0;
      }
      return original(handler, delay, ...args);
    }) as typeof window.setTimeout;
    w.__aeVendorReturn = { host, callbacks, restore: () => { window.setTimeout = original; } };
  });
  await page.keyboard.press('Escape');
  await expect(page.getByTestId('fc-palette-root')).toHaveCount(0);
  await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  await page.keyboard.press('Tab');
  const settings = page.getByTestId('fc-settings-button');
  await expect(settings).toBeFocused();
  const proof = await page.evaluate(() => {
    const state = (window as typeof window & { __aeVendorReturn: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } }).__aeVendorReturn;
    state.restore();
    const proof = { queued: state.callbacks.length, cleared: state.host.previousActiveElement === null };
    for (const callback of state.callbacks) callback();
    return proof;
  });
  expect(proof.queued).toBeGreaterThan(0);
  expect(proof.cleared).toBe(true);
  await expect(settings).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#fc-settings-heading')).toBeFocused();
});

for (const theme of ['light', 'dark']) {
  test(`narrow specimen navigation and grid retain unobscured focus in ${theme}`, async ({ page }, info) => {
    await page.setViewportSize({ width: 1280, height: 900 });
    await page.goto(`/__frontcomposer/specimens/type?theme=${theme}`);
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    const nextItem = page.getByTestId('fc-nav-group-generated').locator('..');
    expect(await nextItem.evaluate(element => getComputedStyle(element).marginBlockStart)).toBe('0px');
    await page.setViewportSize({ width: 320, height: 900 });
    expect(await nextItem.evaluate(element => getComputedStyle(element).marginBlockStart)).toBe('8px');
    const navigation = page.getByTestId('fc-nav-foundation');
    await navigation.focus();
    await expectFocusVisible(navigation);
    const grid = page.getByRole('region', { name: 'specimen status', exact: true });
    await grid.focus();
    await expectFocusVisible(grid);
    const normal = await expectSurfaceContrast(grid, 'focus');
    await page.emulateMedia({ forcedColors: 'active' });
    await expectFocusVisible(grid);
    const system = await expectSurfaceContrast(grid, 'focus');
    const paint = await grid.evaluate(element => {
      const css = getComputedStyle(element);
      return { width: css.outlineWidth, offset: css.outlineOffset, systemPaletteSupported: CSS.supports('forced-color-adjust', 'auto') };
    });
    expect(paint.width).toBe('2px');
    expect(paint.offset).toBe('-2px');
    if (!paint.systemPaletteSupported) expect(info.project.name).toBe('webkit');
    await info.attach(`narrow-focus-${theme}`, { body: JSON.stringify({ normal, system, paint }), contentType: 'application/json' });

    // Exercise the shared grid asset outside the type specimen, with the actual Fluent theme.
    await page.emulateMedia({ forcedColors: 'none' });
    await page.goto('/counter/counter-projection');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await page.getByTitle('Change theme').first().click();
    await page.getByRole('menuitem', { name: theme === 'dark' ? 'Dark' : 'Light', exact: true }).click();
    await expect(page.getByTitle('Change theme').first().locator('span[aria-hidden="true"]')).toHaveText(theme === 'dark' ? 'Dark' : 'Light');
    await expect.poll(async () => page.locator('.fc-shell-root').evaluate(element => {
      const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
      const context = canvas.getContext('2d')!;
      context.fillStyle = getComputedStyle(element).getPropertyValue('--colorNeutralBackground1');
      context.fillRect(0, 0, 1, 1);
      return [...context.getImageData(0, 0, 1, 1).data].slice(0, 3).reduce((sum, channel) => sum + channel, 0) < 384;
    })).toBe(theme === 'dark');
    await settlePaint(page);
    const shared = page.locator('.fc-datagrid-host[role="region"]').first();
    await shared.focus();
    await page.keyboard.press('Shift+Tab');
    await page.keyboard.press('Tab');
    await expectFocusVisible(shared);
    const sharedNormal = await expectSurfaceContrast(shared, 'focus');
    await page.emulateMedia({ forcedColors: 'active' });
    await expectFocusVisible(shared);
    const sharedSystem = await expectSurfaceContrast(shared, 'focus');
    const sharedPaint = await shared.evaluate(element => ({ width: getComputedStyle(element).outlineWidth, offset: getComputedStyle(element).outlineOffset }));
    expect(sharedPaint).toEqual({ width: '2px', offset: '-2px' });
    await info.attach(`shared-grid-focus-${theme}`, { body: JSON.stringify({ normal: sharedNormal, system: sharedSystem, paint: sharedPaint }), contentType: 'application/json' });
  });
}

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
        if (spacing) await applyTextSpacing(page);
        await expectNoTextClipping(page);
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
      const adjustment = await state.evaluate(element => getComputedStyle(element).getPropertyValue('forced-color-adjust'));
      if (adjustment) expect(adjustment).toBe('auto');
      else {
        expect(testInfo.project.name).toBe('webkit');
        await testInfo.attach('forced-palette-capability-limit', { body: 'WebKit exposes the forced-colors media query but does not implement forced-color-adjust/authored-color replacement. System-palette painting requires the supported platform lane.', contentType: 'text/plain' });
      }
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
  const engine = page.context().browser()!.browserType().name();
  const expectedCount = await page.evaluate(({ requireModal, engine }) => {
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
    // Firefox includes overflowing scroll containers in sequential navigation even
    // when their authored tabIndex is -1 (including an overflowing Fluent tablist).
    const nativeScrollStop = (element: Element) => {
      // Modal containment deliberately wraps its authored controls before the
      // browser's implicit document scroll-container stops can be reached.
      if (engine !== 'firefox' || activeModal) return false;
      const css = getComputedStyle(element);
      return /(auto|scroll)/u.test(css.overflowX) && element.scrollWidth > element.clientWidth
        || /(auto|scroll)/u.test(css.overflowY) && element.scrollHeight > element.clientHeight;
    };
    const expected = [...new Set(all.filter(element => element instanceof HTMLElement && (element.tabIndex >= 0 || nativeScrollStop(element)) && visible(element)
      && !element.matches(':disabled,[disabled],[aria-disabled="true"]')
      && element !== dialog
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
    const state = { stops, activeModal, contains, describe, canonical, tabEvent: null as KeyboardEvent | null, focusEvents: 0, focusCalls: 0, restore: () => {} };
    document.addEventListener('keydown', event => {
      if (event.key === 'Tab') { state.tabEvent = event; state.focusEvents = 0; state.focusCalls = 0; }
    }, true);
    document.addEventListener('focusin', () => { state.focusEvents++; }, true);
    const nativeFocus = HTMLElement.prototype.focus;
    HTMLElement.prototype.focus = function (options?: FocusOptions) { state.focusCalls++; nativeFocus.call(this, options); };
    state.restore = () => { HTMLElement.prototype.focus = nativeFocus; };
    (window as unknown as { aeKeyboard: unknown }).aeKeyboard = state;
    return stops.length;
  }, { requireModal: modal, engine });
  expect(expectedCount, 'Expected reachable keyboard controls').toBeGreaterThan(0);
  try {
  let previousIndex = -1;
  const step = async (reverse = false) => {
    for (let outside = 0; outside < 3; outside++) {
      await page.keyboard.press(reverse ? 'Shift+Tab' : 'Tab');
      // Allow application timers triggered by Tab to reveal a boundary refocus trap.
      await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 25)));

      const result = await page.evaluate(() => {
        const state = (window as unknown as { aeKeyboard: { stops: Element[]; activeModal?: Element; contains: (ancestor: Element, child: Element) => boolean; describe: (element: Element) => string; canonical: (element: Element) => Element; tabEvent: KeyboardEvent | null; focusEvents: number; focusCalls: number } }).aeKeyboard;
        const active = document.activeElement;
        if (!document.hasFocus() || !active || active === document.body) return { outside: true, modal: Boolean(state.activeModal), index: -1 };
        return { outside: false, modal: Boolean(state.activeModal), member: !state.activeModal || state.contains(state.activeModal, active), index: state.stops.indexOf(state.canonical(active)), control: state.describe(active), tabPrevented: state.tabEvent?.defaultPrevented, focusEvents: state.focusEvents, focusCalls: state.focusCalls };
      });
      if (result.index === previousIndex && (result.tabPrevented || (result.focusEvents ?? 0) > 0 || (result.focusCalls ?? 0) > 0) && !(modal && expectedCount === 1)) throw new Error(`Application trapped Tab on a reachable control: ${result.control}`);
      const stationaryBrowserBoundary = !result.modal && result.index === previousIndex && previousIndex === (reverse ? 0 : expectedCount - 1) && result.tabPrevented === false;
      if (result.outside || stationaryBrowserBoundary) {
        expect(result.modal, 'Modal keyboard focus left its document').toBe(false);
        expect(previousIndex, 'Document focus left before its complete boundary').toBe(reverse ? 0 : expectedCount - 1);
        // Firefox headless retains its last activeElement and document.hasFocus() at
        // browser chrome. Resume only at a proven app boundary after an unprevented
        // Tab. An app trap, missed stop, or modal escape still fails.
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
  } finally {
    await page.evaluate(() => (window as unknown as { aeKeyboard: { restore: () => void } }).aeKeyboard.restore());
  }
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

test.describe('review measurement regressions', () => {
  test('G01 permanent shadows and inset decoration cannot establish or mask focus', async ({ page }) => {
    for (const shadow of ['0 0 0 4px black', 'inset 0 0 0 4px black', '0 0 6px black']) {
      await page.setContent(`<button style="margin:40px;appearance:none;outline:none;box-shadow:${shadow}">Save</button>`);
      await page.locator('button').focus();
      expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
      await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Nonempty/);
    }
    for (const shadow of ['0 0 0 4px black', 'inset 0 0 0 4px black']) {
      await page.setContent(`<style>button{margin:40px;appearance:none;background:white;outline:none;box-shadow:${shadow}}button:focus{outline:2px solid #eee}</style><button>Save</button>`);
      await page.locator('button').focus();
      await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/contrast failures/);
    }
    await page.setContent('<style>button{margin:40px;appearance:none;background:white;outline:none}button:focus{outline:2px solid #eee;box-shadow:inset 0 0 0 4px black}</style><button>Save</button>');
    await page.locator('button').focus();
    await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/contrast failures/);
    await page.setContent('<style>button{margin:40px;appearance:none;background:white;outline:none}button:focus{box-shadow:0 0 0 2px white,0 0 0 4px black}</style><button>Save</button>');
    await page.locator('button').focus();
    expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
    await expectSurfaceContrast(page.locator('button'), 'focus');
  });

  test('G02 same-level later paint and nested stacking contexts can occlude an outer ring', async ({ page }) => {
    const target = '<style>button{position:absolute;left:40px;top:40px;width:80px;height:40px;appearance:none;outline:none}button:focus{outline:6px solid black}</style><button>Save</button>';
    await page.setContent(target + '<div style="position:absolute;left:121px;top:50px;width:3px;height:10px;background:black"></div>');
    await page.locator('button').focus();
    expect((await focusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
    await page.setContent('<div style="position:relative;z-index:0">' + target.replace('position:absolute;', 'position:absolute;z-index:100;') + '</div><div style="position:absolute;z-index:1;left:0;top:0"><div style="position:absolute;z-index:1;left:129px;top:58px;width:3px;height:10px;background:black"></div></div>');
    await page.locator('button').focus();
    expect((await focusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
  });

  test('G03 transparent supported CSS colors are absent focus paint', async ({ page }) => {
    for (const paint of ['outline:3px solid color(srgb 0 0 0 / 0)', 'box-shadow:0 0 0 6px color(srgb 0 0 0 / 0)']) {
      await page.setContent(`<style>button{margin:40px;appearance:none;outline:none;box-shadow:none}button:focus{${paint}}</style><button>Save</button>`);
      await page.locator('button').focus();
      expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
    }
  });

  test('G04 focus clipping and masks fail closed on the target and its ancestry', async ({ page }) => {
    for (const clip of ['clip-path:inset(0)', 'clip:rect(0px,100px,50px,0px)', 'mask-image:linear-gradient(black,black)']) for (const owner of ['target','ancestor']) {
      await page.setContent(`<style>button{position:absolute;left:10px;top:10px;width:80px;height:30px;outline:none;appearance:none}button:focus{outline:6px solid black}</style><div style="position:absolute;left:40px;top:40px;width:100px;height:50px;${owner === 'ancestor' ? clip : ''}"><button style="${owner === 'target' ? clip : ''}">Save</button></div>`);
      await page.locator('button').focus();
      expect((await focusGeometry(page.locator('button'))).failures).toContain('focus clip-path/clip/mask needs pixel evidence');
    }
  });

  test('G05 SVG fill and stroke opacity contribute to effective contrast', async ({ page }) => {
    for (const paint of ['fill="black" fill-opacity=".1"', 'fill="none" stroke="black" stroke-width="4" stroke-opacity=".1"']) {
      await page.setContent(`<main style="background:white"><svg width="40" height="40"><rect x="4" y="4" width="30" height="30" ${paint}/></svg></main>`);
      await expect(expectSurfaceContrast(page.locator('main'), 'icon')).rejects.toThrow(/icon contrast failures/);
    }
  });

  test('G06 visible clipped low-contrast text cannot disappear from the inventory', async ({ page }) => {
    for (const clip of ['clip-path:inset(1px)', 'position:absolute;clip:rect(0px,100px,20px,0px)']) {
      await page.setContent(`<main style="background:white"><p style="color:black">Readable</p><p style="color:#eee;${clip}">Still visible low contrast</p></main>`);
      await expect(expectSurfaceContrast(page.locator('main'))).rejects.toThrow(/Clipped contrast paint|contrast failures/);
    }
  });

  test('G12 inward outlines contrast with the control paint', async ({ page }) => {
    await page.setContent('<style>main{background:white;padding:40px}button{background:black;color:white;outline:none;appearance:none}button:focus{outline:2px solid black;outline-offset:-2px}</style><main><button>Save</button></main>');
    await page.locator('button').focus();
    await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/focus contrast failures/);
  });

  test('G13 zero and negative spread shadows without offset or blur draw no outer indicator', async ({ page }) => {
    for (const spread of [0, -4]) {
      await page.setContent(`<style>button{margin:40px;appearance:none;outline:none}button:focus{box-shadow:0 0 0 ${spread}px black}</style><button>Save</button>`);
      await page.locator('button').focus();
      expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
      await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Nonempty/);
    }
  });
});

test('supported clipped paint keeps a focus underline and excludes only fully hidden text', async ({ page }) => {
  await page.setContent('<style>button{position:relative;margin:40px;width:120px;height:40px;outline:none;appearance:none}button::after{content:"";position:absolute;inset:0;border-bottom:2px solid black;clip-path:inset(calc(100% - 2px) 1px 0px);transform:scaleX(0)}button:focus::after{transform:scaleX(1)}</style><main style="background:white;color:black"><button>Save</button><p>Readable</p><span style="position:absolute;width:1px;height:1px;clip-path:inset(50%);color:#eee">Nonvisual heading</span></main>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  const samples = await expectSurfaceContrast(page.locator('main'));
  expect(samples.some(sample => sample.paint === 'Nonvisual heading')).toBe(false);
  await expectSurfaceContrast(page.locator('button'), 'focus');
  await page.locator('span').evaluate(element => { element.style.width = '120px'; element.style.height = '30px'; element.style.clipPath = 'inset(1px)'; });
  await expect(expectSurfaceContrast(page.locator('main'))).rejects.toThrow(/contrast failures/);
});

test('diagnostic screenshots restore scroll without repairing an actually clipped focus ring', async ({ page }) => {
  await page.setContent('<style>button{outline:none}button:focus{outline:3px solid black;outline-offset:2px}</style><div id="scroller" style="position:absolute;left:40px;top:40px;width:240px;height:100px;overflow:auto;padding:10px"><button>Keep focus</button><div id="capture" style="margin-top:400px;height:30px">Diagnostic target</div></div>');
  const button = page.locator('button'), scroller = page.locator('#scroller');
  await button.focus();
  expect((await focusGeometry(button)).failures).toEqual([]);
  await screenshotPreservingScroll(page.locator('#capture'));
  expect(await scroller.evaluate(element => element.scrollTop)).toBe(0);
  await expect(button).toBeFocused();
  expect((await focusGeometry(button)).failures).toEqual([]);
  await scroller.evaluate(element => { element.scrollTop = 12; });
  const failures = (await focusGeometry(button)).failures;
  expect(failures).toContain('vertical scrollport clips target or ring');
  await screenshotPreservingScroll(page.locator('#capture'));
  expect(await scroller.evaluate(element => element.scrollTop)).toBe(12);
  expect((await focusGeometry(button)).failures).toEqual(failures);
});

test('real Fluent search and generated editors retain their focus contour with text spacing', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 900 });
  for (const route of ['/__frontcomposer/specimens/page-toolbar', '/__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true']) {
    await page.goto(route);
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await applyTextSpacing(page);
    const control = route.includes('page-toolbar')
      ? page.getByTestId('fc-page-toolbar-search')
      : page.getByTestId('fc-destructive-command-specimen').locator('fluent-text-input').first();
    await expect(control).toBeVisible();
    await control.focus();
    await page.keyboard.press('Shift+Tab');
    await page.keyboard.press('Tab');
    await expectFocusVisible(control);
    expect((await focusGeometry(control.locator('input'))).failures).toEqual([]);
    await expectSurfaceContrast(control, 'focus');
  }
});


test('R01 rejects an unprevented boundary Tab that application code refocuses', async ({ page }) => {
  await page.setContent('<style>button{margin:8px;outline:none}button:focus{outline:2px solid}</style><button id="first">First</button><button id="last">Last</button>');
  await page.locator('#last').evaluate(button => button.addEventListener('keydown', event => {
    const key = event as KeyboardEvent;
    if (key.key === 'Tab' && !key.shiftKey) setTimeout(() => (button as HTMLElement).focus(), 0);
  }));
  await expect(verifyKeyboardCycle(page)).rejects.toThrow(/Application trapped Tab/);
});

for (const opacity of [.01, .1, 1]) {
  test(`R02 pseudo focus paint composites opacity ${opacity}`, async ({ page }) => {
    await page.setContent(`<style>button{position:absolute;left:40px;top:40px;width:100px;height:40px;background:white;border:0;outline:none}button:focus::after{content:"";position:absolute;inset:0;border:2px solid black;opacity:${opacity}}</style><button>Action</button>`);
    await page.locator('button').focus();
    if (opacity < 1) await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/contrast failures/);
    else await expectSurfaceContrast(page.locator('button'), 'focus');
  });
}

for (const shape of ['ellipse', 'line', 'polyline', 'use']) {
  test(`R03 measures SVG ${shape} without a passing rectangle masking it`, async ({ page }) => {
    const svg = { ellipse: '<ellipse cx="50" cy="20" rx="10" ry="10" fill="#eee"/>', line: '<line x1="40" y1="10" x2="60" y2="30" stroke="#eee" stroke-width="3"/>', polyline: '<polyline points="40,10 50,30 60,10" fill="none" stroke="#eee" stroke-width="3"/>', use: '<defs><path id="glyph" d="M40 10h20v20h-20z"/></defs><use href="#glyph" fill="#eee"/>' }[shape];
    await page.setContent(`<svg style="background:white" width="80" height="40"><rect width="20" height="20" fill="black"/>${svg}</svg>`);
    await expect(expectSurfaceContrast(page.locator('svg'), 'icon')).rejects.toThrow(shape === 'use' ? /pixel evidence/ : /contrast failures/);
    await page.locator('svg').evaluate(svg => svg.querySelectorAll('ellipse,line,polyline,use').forEach(node => { node.setAttribute('fill', node.matches('line,polyline') ? 'none' : 'black'); node.setAttribute('stroke', 'black'); }));
    if (shape !== 'use') await expectSurfaceContrast(page.locator('svg'), 'icon');
  });
}

test('R04 zero-width SVG stroke does not count as meaningful icon paint', async ({ page }) => {
  await page.setContent('<svg style="background:white"><rect width="40" height="40" fill="none" stroke="black" stroke-width="0"/></svg>');
  await expect(expectSurfaceContrast(page.locator('svg'), 'icon')).rejects.toThrow(/Nonempty/);
  await page.locator('rect').evaluate(node => node.setAttribute('stroke-width', '2'));
  await expectSurfaceContrast(page.locator('svg'), 'icon');
});

for (const effect of ['filter:brightness(0)', 'mix-blend-mode:multiply', 'backdrop-filter:brightness(0)']) {
  test(`R05 rejects unsupported composition ${effect}`, async ({ page }) => {
    await page.setContent(`<div style="background:black;${effect}"><p style="color:white">Meaningful label</p></div>`);
    await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/pixel evidence/);
    await page.locator('div').evaluate(node => { node.style.filter = 'none'; node.style.mixBlendMode = 'normal'; node.style.backdropFilter = 'none'; });
    await expectSurfaceContrast(page.locator('p'));
  });
}

test('R06 effective glyph fill determines text contrast', async ({ page }) => {
  await page.setContent('<p style="color:black;-webkit-text-fill-color:white;background:white">Invisible glyphs</p>');
  await expect(expectSurfaceContrast(page.locator('p'))).rejects.toThrow(/contrast failures/);
  await page.locator('p').evaluate(node => node.style.setProperty('-webkit-text-fill-color', 'black'));
  await expectSurfaceContrast(page.locator('p'));
});

test('R07 sibling background paint cannot be replaced with ancestor white', async ({ page }) => {
  await page.setContent('<div style="position:relative;background:white"><div id="under" style="position:absolute;inset:0;background:black"></div><p style="position:relative;color:black">Hidden black glyphs</p></div>');
  await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/sibling background needs pixel evidence/);
  await page.locator('#under').evaluate(node => node.remove());
  await expectSurfaceContrast(page.locator('p'));
});

test('R08 invisible ancestor cannot certify focus geometry', async ({ page }) => {
  await page.setContent('<style>button{outline:none}button:focus{outline:2px solid black}</style><div style="opacity:0"><button>Action</button></div>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
  await page.locator('div').evaluate(node => node.style.opacity = '1');
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
});

test('R09 descendant extending over the target ring is an obstruction', async ({ page }) => {
  await page.setContent('<style>button{position:absolute;left:40px;top:40px;width:100px;height:40px;outline:none}button:focus{outline:2px solid black}</style><button>Action<span style="position:absolute;inset:-5px;background:white"></span></button>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
  await page.locator('span').evaluate(node => node.remove());
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
});

test('R10 generated meaningful text cannot disappear beside passing text', async ({ page }) => {
  await page.setContent('<style>div{background:white;color:black}div::before{content:"Generated label";color:#eee}</style><div>Passing label</div>');
  await expect(expectSurfaceContrast(page.locator('div'))).rejects.toThrow(/contrast failures/);
  await page.addStyleTag({ content: 'div::before{color:black}' });
  await expectSurfaceContrast(page.locator('div'));
});

test('R11 collapsed offset shadow paints no focus pixels', async ({ page }) => {
  await page.setContent('<style>button{appearance:none;margin:40px;outline:none}button:focus{box-shadow:10px 0 0 -100px black}</style><button>Action</button>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
  await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Nonempty/);
  await page.addStyleTag({ content: 'button:focus{box-shadow:10px 0 0 2px black}' });
  await expectSurfaceContrast(page.locator('button'), 'focus');
});

test('R12 permanent shadow cannot certify an invisible focus-only layer', async ({ page }) => {
  await page.setContent('<style>button{margin:40px;background:white;outline:none;box-shadow:0 0 0 3px black}button:focus{box-shadow:0 0 0 3px black,0 0 0 1px white}</style><button>Action</button>');
  await page.locator('button').focus();
  await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Nonempty|contrast failures/);
  await page.addStyleTag({ content: 'button:focus{box-shadow:0 0 0 3px black,0 0 0 5px black}' });
  await expectSurfaceContrast(page.locator('button'), 'focus');
});

test('R13 inset pseudo focus border compares against control paint', async ({ page }) => {
  await page.setContent('<style>button{position:relative;margin:40px;background:black;color:white;outline:none}button:focus::after{content:"";position:absolute;inset:0;border:2px solid black}</style><button>Action</button>');
  await page.locator('button').focus();
  await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/contrast failures/);
  await page.addStyleTag({ content: 'button:focus::after{border-color:white}' });
  await expectSurfaceContrast(page.locator('button'), 'focus');
});

test('R14 placeholder alpha determines effective contrast', async ({ page }) => {
  await page.setContent('<style>input{background:white;color:black}input::placeholder{color:black;opacity:.1}</style><input placeholder="Meaningful placeholder">');
  await expect(expectSurfaceContrast(page.locator('input'))).rejects.toThrow(/contrast failures/);
  await page.addStyleTag({ content: 'input::placeholder{opacity:1}' });
  await expectSurfaceContrast(page.locator('input'));
});

test('reflow rejects unlabelled nested horizontal scrolling and stale or unjustified target exceptions', async ({ page }) => {
  await page.setContent('<div style="width:100px;overflow:auto"><div style="width:200px">Long content</div></div><button id="small" style="width:10px;height:10px;padding:0">A</button>');
  await expect(expectNoPageOverflow(page)).rejects.toThrow(/scroll/);
  const record: TargetException = { selector: '#small', kind: 'Essential', rationale: 'Small fixture', keyboardPath: 'Tab', evidence: 'fixture' };
  expect((await inventoryTargets(page, [record])).inventory[0].failure).toContain('Essential');
  await page.locator('#small').evaluate(node => { node.style.width = node.style.height = '30px'; });
  expect((await inventoryTargets(page, [record])).inventory[0].failure).toContain('stale');
});

test('R15 owned Settings return cancels the actual Fluent delayed return after an operator Tab', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.goto('/__frontcomposer/specimens/page-toolbar');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.getByTitle('Change theme').first().click();
  await page.getByRole('menuitem', { name: 'Dark', exact: true }).click();
  const toolbar = new (await import('../page-objects/page-toolbar-specimen.page.js')).PageToolbarSpecimenPage(page);
  await toolbar.summaryTab.focus();
  await toolbar.summaryTab.press('ArrowRight');
  await page.getByTestId('fc-palette-trigger').click();
  await expect(page.getByTestId('fc-palette-search')).toBeFocused();
  await page.getByTestId('fc-palette-search').locator('input').fill('no-match-accessibility-136');
  await page.keyboard.press('Escape');
  await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByTestId('fc-settings-button')).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#fc-settings-heading')).toBeFocused();
  await page.keyboard.press('Tab');
  await page.keyboard.press('Tab');
  await page.evaluate(async () => {
    const path = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(path) as { containDialogFocus: (testId: string) => boolean };
    if (!focus.containDialogFocus('fc-settings-dialog')) throw new Error('palette containment did not attach');
    const host = document.querySelector<HTMLElement>('[data-testid="fc-settings-dialog"]')!.closest('fluent-dialog')!;
    const w = window as typeof window & { __aeVendorReturn?: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } };
    const callbacks: (() => void)[] = [];
    const original = window.setTimeout;
    window.setTimeout = ((handler: TimerHandler, delay?: number, ...args: unknown[]) => {
      // Hold the pinned Fluent callback itself, rather than substitute a synthetic return.
      if (delay === 25 && typeof handler === 'function' && new Error().stack?.includes('Microsoft.FluentUI.AspNetCore.Components.lib.module.js')) {
        callbacks.push(() => handler(...args));
        return 0;
      }
      return original(handler, delay, ...args);
    }) as typeof window.setTimeout;
    w.__aeVendorReturn = { host, callbacks, restore: () => { window.setTimeout = original; } };
  });
  await page.keyboard.press('Escape');
  await expect(page.getByTestId('fc-settings-dialog')).toHaveCount(0);
  await expect(page.getByTestId('fc-settings-button')).toBeFocused();
  await page.keyboard.press('Shift+Tab');
  const settings = page.getByTestId('fc-palette-trigger');
  await expect(settings).toBeFocused();
  const proof = await page.evaluate(() => {
    const state = (window as typeof window & { __aeVendorReturn: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } }).__aeVendorReturn;
    state.restore();
    const proof = { queued: state.callbacks.length, cleared: state.host.previousActiveElement === null };
    for (const callback of state.callbacks) callback();
    return proof;
  });
  expect(proof.queued).toBeGreaterThan(0);
  expect(proof.cleared).toBe(true);
  await expect(settings).toBeFocused();
});


test('reused owned dialog dismissal resets only its current return and preserves later external input', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.evaluate(async () => {
    const path = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(path) as { containDialogFocus: (testId: string) => boolean };
    const host = document.createElement('fluent-dialog');
    host.hidden = true;
    host.innerHTML = '<div data-testid="fc-settings-dialog"></div>';
    document.body.append(host);
    const w = window as typeof window & { __fcModalReservation?: { watchFocus: boolean; moved: boolean }; __fcOverlayOpenIntent?: { watchFocus: boolean; moved: boolean } };
    for (let opening = 0; opening < 2; opening++) {
      document.querySelector<HTMLElement>('[data-testid="fc-palette-trigger"]')!.focus();
      const reservation = { watchFocus: true, moved: true };
      const intent = { watchFocus: true, moved: true };
      w.__fcModalReservation = reservation;
      w.__fcOverlayOpenIntent = intent;
      if (!focus.containDialogFocus('fc-settings-dialog')) throw new Error('reused host containment failed');
      host.dispatchEvent(new CustomEvent('beforetoggle', { detail: { newState: 'closed' } }));
      if (reservation.watchFocus || reservation.moved || intent.watchFocus || intent.moved) throw new Error('native return retained modal input tracking');
      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
      document.querySelector<HTMLElement>('[data-testid="fc-settings-button"]')!.focus();
      if (!reservation.watchFocus || !reservation.moved || !intent.watchFocus || !intent.moved) throw new Error('later external input was discarded');
    }
    delete w.__fcModalReservation;
    delete w.__fcOverlayOpenIntent;
    host.remove();
  });
});

test('forced-color replacement capability is recorded for its engine and run', async ({ page }, info) => {
  await page.setContent('<span style="color:rgb(255,0,0);background:rgb(0,255,0);forced-color-adjust:auto">Capability</span>');
  await page.emulateMedia({ forcedColors: 'active' });
  const forced = await page.locator('span').evaluate(element => {
    const css = getComputedStyle(element);
    return { forcedColors: matchMedia('(forced-colors: active)').matches, color: css.color, background: css.backgroundColor };
  });
  const authoredColorReplacement = forced.color !== 'rgb(255, 0, 0)' && forced.background !== 'rgb(0, 255, 0)';
  if (!authoredColorReplacement) expect(info.project.name).toBe('webkit');
  const path = info.outputPath(`forced-colors-capability-${info.project.name}.json`);
  await (await import('node:fs/promises')).writeFile(path, JSON.stringify({ engine: info.project.name, runId: process.env.FC_A11Y_EVIDENCE_RUN_ID, testId: info.testId, attempt: info.retry, authoredColorReplacement, forced }));
  await info.attach('forced-color-capability', { path, contentType: 'application/json' });
});


test('desktop rail flyout controls fit both 72px and 48px rails without horizontal scrolling', async ({ page }) => {
  await page.setViewportSize({ width: 1600, height: 900 });
  await page.goto('/__frontcomposer/specimens/type?density=compact');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  const rail = page.getByTestId('fc-shell-navigation').getByTestId('fc-navigation-rail');
  for (const width of [72, 48]) {
    if (width === 48) await page.getByTestId('fc-hamburger-toggle').click();
    await expect(rail).toHaveAttribute('data-rail-width', String(width));
    await expect.poll(async () => (await rail.boundingBox())?.width).toBe(width);
    for (const forcedColors of ['none', 'active'] as const) {
      await page.emulateMedia({ forcedColors });
      await expectNoPageOverflow(page);
      const regions = await page.getByTestId('fc-shell-navigation').evaluate(element => ({ client: element.clientWidth, scroll: element.scrollWidth }));
      expect(regions.scroll).toBeLessThanOrEqual(regions.client + 1);
      await expect(rail.locator('[data-testid="fc-nav-flyout-trigger-Counter"]')).toHaveAttribute('icon-only', '');
    }
  }
});


for (const shape of ['line', 'path']) for (const axis of ['horizontal', 'vertical']) {
  test(`review axis-aligned SVG ${shape} ${axis} retains its painted stroke envelope`, async ({ page }) => {
    const points = axis === 'horizontal' ? 'x1="40" y1="20" x2="80" y2="20"' : 'x1="60" y1="5" x2="60" y2="35"';
    const line = shape === 'line' ? `<line ${points}/>` : `<path d="${axis === 'horizontal' ? 'M40 20H80' : 'M60 5V35'}"/>`;
    await page.setContent(`<svg width="100" height="40" style="background:white"><rect width="20" height="20" fill="black"/><g fill="none" stroke="#eee" stroke-width="3">${line}</g></svg>`);
    await expect(expectSurfaceContrast(page.locator('svg'), 'icon')).rejects.toThrow(/contrast failures/);
    await page.locator('g').evaluate(node => node.setAttribute('stroke', 'black'));
    const samples = await expectSurfaceContrast(page.locator('svg'), 'icon');
    expect(samples.some(sample => sample.node === shape && sample.paint === 'icon stroke')).toBe(true);
  });
}

for (const kind of ['generated', 'placeholder', 'focus']) {
  test(`review actual ${kind} pseudo composition inspects filters`, async ({ page }, info) => {
    const markup = kind === 'generated' ? '<style>div::before{content:"Label";color:black;filter:brightness(0)}</style><div style="background:white"></div>'
      : kind === 'placeholder' ? '<style>input::placeholder{color:black;opacity:1;filter:brightness(0)}</style><input placeholder="Label" style="background:white">'
      : '<style>button{position:relative;margin:40px;background:white;outline:none}button:focus::after{content:"";position:absolute;inset:0;border:2px solid black;filter:brightness(0)}</style><button>Action</button>';
    await page.setContent(markup);
    const target = page.locator(kind === 'generated' ? 'div' : kind === 'placeholder' ? 'input' : 'button');
    if (kind === 'focus') await target.focus();
    const computedFilter = await target.evaluate((node, kind) => getComputedStyle(node, kind === 'placeholder' ? '::placeholder' : kind === 'generated' ? '::before' : '::after').filter, kind);
    if (kind === 'placeholder' && computedFilter === 'none') {
      expect(info.project.name).toBe('firefox'); // Firefox ignores filter on ::placeholder.
      await expectSurfaceContrast(target);
    } else await expect(measureSurfaceContrast(target, kind === 'focus' ? 'focus' : 'text')).rejects.toThrow(/Filtered or blended/);
    if (kind === 'focus') await expect(focusGeometry(target)).rejects.toThrow(/Filtered or blended/);
    await page.addStyleTag({ content: '*::before,*::after,*::placeholder{filter:none!important}' });
    await expectSurfaceContrast(target, kind === 'focus' ? 'focus' : 'text');
  });
}

test('review generated glyphs outside their host require pixel evidence', async ({ page }) => {
  await page.setContent('<style>body{background:black}div{position:relative;background:white;color:black;width:100px;height:40px}div::before{content:"Label";position:absolute;left:120px}</style><div></div>');
  await expect(measureSurfaceContrast(page.locator('div'))).rejects.toThrow(/Generated text geometry/);
  await page.addStyleTag({ content: 'div::before{position:static}' });
  await expectSurfaceContrast(page.locator('div'));
});

for (const style of ['position:relative;top:40px', 'position:sticky;top:0', 'transform:translateY(40px)', '']) {
  test(`review overlapping sibling backgrounds require evidence ${style || 'static negative margin'}`, async ({ page }) => {
    await page.setContent(`<div style="background:white"><div id="under" style="background:black;height:40px;${style}"></div><p style="position:relative;margin-top:${style.includes('40px') ? '0' : '-32px'};color:black">Label</p></div>`);
    await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
    await page.locator('#under').evaluate(node => node.remove());
    await expectSurfaceContrast(page.locator('p'));
  });
}

test('review a sharp shadow concealed by its opaque control is not focus paint', async ({ page }) => {
  await page.setContent('<style>button{width:100px;height:40px;margin:40px;border:0;border-radius:0;background:white;outline:none}button:focus{box-shadow:1px 0 0 -2px black}</style><button>Action</button>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
  await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Nonempty/);
  await page.addStyleTag({ content: 'button:focus{box-shadow:4px 0 0 -2px black}' });
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  await expectSurfaceContrast(page.locator('button'), 'focus');
});

test('review permanent pseudo background cannot certify a same-color focus border', async ({ page }) => {
  await page.setContent('<style>button{position:relative;margin:40px;background:white;outline:none}button::after{content:"";position:absolute;inset:0;background:black;border:2px solid transparent}button:focus::after{border-color:black}</style><button>Action</button>');
  await page.locator('button').focus();
  await expect(focusGeometry(page.locator('button'))).rejects.toThrow(/Pseudo focus background/);
  await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Pseudo focus background/);
  await page.addStyleTag({ content: 'button::after{background:transparent}' });
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  await expectSurfaceContrast(page.locator('button'), 'focus');
});

test('review margin-shifted pseudo borders use the backdrop outside their host', async ({ page }) => {
  await page.setContent('<style>body{background:black}button{position:relative;margin:40px;width:100px;height:40px;background:white;outline:none}button:focus::after{content:"";position:absolute;left:0;top:0;width:100px;height:40px;box-sizing:border-box;margin-left:120px;border:2px solid black}</style><button>Action</button>');
  await page.locator('button').focus();
  await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/contrast failures/);
  await page.addStyleTag({ content: 'button:focus::after{border-color:white}' });
  await expectSurfaceContrast(page.locator('button'), 'focus');
});

test('review transformed static descendants remain focus obstructions', async ({ page }) => {
  await page.setContent('<style>button{margin:40px;width:100px;height:40px;outline:none}button:focus{outline:2px solid black}span{display:block;width:120px;height:60px;background:white;transform:translate(-12px,-24px)}</style><button>Action<span></span></button>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('app content obscures target or focus ring');
  await page.locator('span').evaluate(node => { node.style.transform = 'none'; node.style.width = node.style.height = '10px'; });
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
});

test('review generated large text uses the existing 3 to 1 threshold', async ({ page }) => {
  await page.setContent('<style>div{background:white}div::before{content:"Large label";font-size:32px;color:#888}</style><div></div>');
  const samples = await expectSurfaceContrast(page.locator('div'));
  expect(samples[0].minimum).toBe(3);
  expect(samples[0].ratio).toBeGreaterThan(3.5);
  expect(samples[0].ratio).toBeLessThan(3.6);
  await page.addStyleTag({ content: 'div::before{color:#aaa}' });
  await expect(expectSurfaceContrast(page.locator('div'))).rejects.toThrow(/contrast failures/);
});

test('review recorded operator focus on SVG survives deferred overlay restoration', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.setContent('<button id="origin">Origin</button>');
  await page.evaluate(async () => {
    const path = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(path) as { restoreOverlayOrigin: () => void };
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('tabindex', '0'); svg.id = 'operator-svg'; document.body.append(svg); svg.focus();
    const w = window as typeof window & { __fcOverlayOrigin?: Element; __fcModalReservation?: { moved: boolean } };
    w.__fcOverlayOrigin = document.querySelector('#origin')!;
    w.__fcModalReservation = { moved: true };
    if (document.activeElement !== svg) throw new Error('SVG fixture did not receive focus');
    focus.restoreOverlayOrigin();
    await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
  });
  await expect(page.locator('#operator-svg')).toBeFocused();
});


for (const effect of ['mix-blend-mode:multiply', 'backdrop-filter:brightness(0)']) {
  test(`review actual generated pseudo composition rejects ${effect}`, async ({ page }) => {
    await page.setContent(`<style>div{background:white}div::before{content:"Label";color:black;${effect}}</style><div></div>`);
    await expect(measureSurfaceContrast(page.locator('div'))).rejects.toThrow(/Filtered or blended/);
    await page.addStyleTag({ content: 'div::before{mix-blend-mode:normal;backdrop-filter:none}' });
    await expectSurfaceContrast(page.locator('div'));
  });
}


test('review translucent permanent shadow cannot certify covered changed focus paint', async ({ page }) => {
  await page.setContent('<style>button{margin:40px;width:100px;height:40px;background:white;outline:none;border:0;border-radius:0;box-shadow:0 0 0 3px rgb(255 255 255 / .95)}button:focus{box-shadow:0 0 0 3px rgb(255 255 255 / .95),0 0 0 2px black}</style><button>Action</button>');
  await page.locator('button').focus();
  await expect(focusGeometry(page.locator('button'))).rejects.toThrow(/Overlapping translucent shadow layers/);
  await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping translucent shadow layers/);
  await page.addStyleTag({ content: 'button{box-shadow:0 0 0 3px white}button:focus{box-shadow:0 0 0 3px white,0 0 0 5px black}' });
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  await expectSurfaceContrast(page.locator('button'), 'focus');
  await page.addStyleTag({ content: 'button{box-shadow:none}button:focus{box-shadow:0 0 2px rgb(0 0 0 / .12),0 2px 4px rgb(0 0 0 / .14),0 0 0 2px black}' });
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  const light = await expectSurfaceContrast(page.locator('button'), 'focus');
  expect(light.some(sample => sample.measurement === 'conservative-bound' && sample.ratio >= 3 && sample.ratio < 21)).toBe(true);
  await page.addStyleTag({ content: 'body,button{background:rgb(41,41,41)}button:focus{box-shadow:0 0 2px rgb(0 0 0 / .24),0 2px 4px rgb(0 0 0 / .28),0 0 0 2px white}' });
  const geometry = await focusGeometry(page.locator('button'));
  expect(geometry.failures).toEqual([]);
  expect(geometry.shadowBounds).toHaveLength(1);
  const dark = await expectSurfaceContrast(page.locator('button'), 'focus');
  const bound = dark.find(sample => sample.measurement === 'conservative-bound')!.shadowBound!;
  expect(bound.ratioMinimum).toBeGreaterThanOrEqual(3);
  expect(bound.layers).toHaveLength(2);
  expect(bound.backgroundLayers).toHaveLength(2);
  expect(bound.layers.every(layer => layer.maximumAlpha >= layer.declaredAlpha)).toBe(true);
  expect([...bound.foreground.minimum, ...bound.foreground.maximum, ...bound.background.minimum, ...bound.background.maximum].every(Number.isInteger)).toBe(true);
  expect(bound.foreground.minimum.every((value, i) => value < bound.foreground.maximum[i])).toBe(true);
  expect(bound.background.minimum.every((value, i) => value < bound.background.maximum[i])).toBe(true);
  // A later black blur can erase contrast against a light adjacent background;
  // the foreground-only overlays cannot bound that composition on their own.
  await page.addStyleTag({ content: 'body,button{background:white}button:focus{box-shadow:0 0 2px rgb(0 0 0 / .12),0 0 0 2px black,0 0 12px rgb(0 0 0 / .9)}' });
  await expect(focusGeometry(page.locator('button'))).rejects.toThrow(/Overlapping translucent shadow layers/);
  await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping translucent shadow layers/);
  // Canvas can quantize .999 to opaque; a blur still has spatial alpha below one.
  await page.addStyleTag({ content: 'body,button{background:black}button:focus{box-shadow:0 0 2px 20px rgb(0 0 0 / .999),0 0 0 2px white}' });
  await expect(focusGeometry(page.locator('button'))).rejects.toThrow(/Overlapping translucent shadow layers/);
  await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping translucent shadow layers/);
  await page.addStyleTag({ content: 'body,button{background:color(srgb .16 .16 .16)}button:focus{box-shadow:0 0 2px rgb(0 0 0 / .24),0 0 0 2px white}' });
  await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Shadow background color/);
});


test('review normal-flow generated label spacing remains measured', async ({ page }) => {
  await page.setContent('<style>label{background:white;color:black}label::after{content:"*" / "";margin-left:4px;color:black}</style><label>Required field</label>');
  expect((await expectSurfaceContrast(page.locator('label'))).some(sample => sample.paint === '::after generated text')).toBe(true);
  await page.addStyleTag({ content: 'label::after{margin-left:-20px}' });
  await expect(measureSurfaceContrast(page.locator('label'))).rejects.toThrow(/Generated text geometry/);
});

test('review clipped text cannot overlap an adjacent static footer but visible overlap is rejected', async ({ page }) => {
  await page.setContent('<style>body{background:white}#root{position:fixed;left:40px;top:40px;width:200px}#port{height:30px;overflow:hidden;background:white}p{margin:0;font-size:12px;line-height:20px;color:black}footer{height:30px;background:black}</style><div id="root"><div id="port"><p>Visible line<br>Partly clipped line</p></div><footer></footer></div>');
  await expectSurfaceContrast(page.locator('p'));
  await page.addStyleTag({ content: '#port{height:60px}footer{margin-top:-30px}' });
  await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
});


test('review opaque native top-layer surfaces shield text from page backgrounds only', async ({ page }) => {
  await page.setContent('<style>body{background:white}#under{position:fixed;inset:0;background:black}dialog{background:white;color:black}p{position:relative}</style><div id="under"></div><dialog><p>Modal label</p></dialog>');
  await page.locator('dialog').evaluate(dialog => (dialog as HTMLDialogElement).showModal());
  await expectSurfaceContrast(page.locator('p'));
  await page.evaluate(() => {
    const overlay = document.createElement('div'); overlay.id = 'over'; overlay.setAttribute('popover', 'manual');
    overlay.style.cssText = 'position:fixed;inset:0;width:100vw;height:100vh;margin:0;background:black';
    document.body.append(overlay); overlay.showPopover();
  });
  await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
  await page.locator('#over').evaluate(overlay => { (overlay as HTMLElement).hidePopover(); overlay.remove(); });
  await page.locator('dialog').evaluate(dialog => { const layer = document.createElement('div'); layer.style.cssText = 'position:absolute;inset:0;background:black'; dialog.prepend(layer); });
  await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
});
