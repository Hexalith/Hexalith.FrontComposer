import { expect, test } from '../fixtures/index.js';
import { applyTextSpacing, removeTextSpacing, expectFocusVisible, expectNoPageOverflow, expectNoTextClipping, expectTargetSizes, expectTextContrast, expectSurfaceContrast, focusGeometry, inventoryTargets, screenshotPreservingScroll, settlePaint, type TargetException } from '../helpers/responsive-accessibility.js';
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
    const state = { stops, activeModal, contains, describe, canonical, tabEvent: null as KeyboardEvent | null };
    document.addEventListener('keydown', event => {
      if (event.key === 'Tab') state.tabEvent = event;
    }, true);
    (window as unknown as { aeKeyboard: unknown }).aeKeyboard = state;
    return stops.length;
  }, { requireModal: modal, engine });
  expect(expectedCount, 'Expected reachable keyboard controls').toBeGreaterThan(0);
  let previousIndex = -1;
  const step = async (reverse = false) => {
    for (let outside = 0; outside < 3; outside++) {
      await page.keyboard.press(reverse ? 'Shift+Tab' : 'Tab');
      const result = await page.evaluate(() => {
        const state = (window as unknown as { aeKeyboard: { stops: Element[]; activeModal?: Element; contains: (ancestor: Element, child: Element) => boolean; describe: (element: Element) => string; canonical: (element: Element) => Element; tabEvent: KeyboardEvent | null } }).aeKeyboard;
        const active = document.activeElement;
        if (!document.hasFocus() || !active || active === document.body) return { outside: true, modal: Boolean(state.activeModal), index: -1 };
        return { outside: false, modal: Boolean(state.activeModal), member: !state.activeModal || state.contains(state.activeModal, active), index: state.stops.indexOf(state.canonical(active)), control: state.describe(active), tabPrevented: state.tabEvent?.defaultPrevented };
      });
      if (result.index === previousIndex && result.tabPrevented && !(modal && expectedCount === 1)) throw new Error(`Application trapped Tab on a reachable control: ${result.control}`);
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
