import { readFile } from 'node:fs/promises';
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

test('resumed review simple contrast rejects translucent composed ancestors', async ({ page }) => {
  await page.setContent('<div id="host" style="opacity:.01;background:white"></div>');
  await page.locator('#host').evaluate(host => {
    host.attachShadow({ mode: 'open' }).innerHTML = '<p style="color:black;background:white">Essential warning</p>';
  });
  await expect(expectTextContrast(page.locator('#host p'))).rejects.toThrow(/opacity.*pixel evidence/);
  await page.locator('#host').evaluate(host => { host.style.opacity = '1'; });
  expect((await expectTextContrast(page.locator('#host p'))).ratio).toBe(21);
  await page.locator('#host p').evaluate(text => { (text as HTMLElement).style.opacity = '.01'; });
  await expect(expectTextContrast(page.locator('#host p'))).rejects.toThrow(/opacity.*pixel evidence/);
});

test('resumed review fixed pseudo focus checks the transformed target containing block', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.setContent('<style>button{position:absolute;left:1200px;top:40px;width:60px;height:40px;transform:translateX(0);outline:none}button:focus::after{content:"";position:fixed;left:50px;top:50px;width:100px;height:40px;border:2px solid black}</style><button>Save</button>');
  expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('fixed pseudo in transformed ancestry needs pixel evidence');
  await page.locator('button').evaluate(button => { button.style.transform = 'none'; });
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
});

test('resumed review transparent sibling glyphs cannot certify the warning they cover', async ({ page }) => {
  await page.setContent('<style>body{background:white}p{position:absolute;left:40px;top:40px;margin:0;font:20px monospace}#warning{color:black}#cover{color:white;z-index:1;pointer-events:none}</style><p id="warning">Essential warning</p><p id="cover">Essential warning</p>');
  await expect(measureSurfaceContrast(page.locator('#warning'))).rejects.toThrow(/Overlapping text needs pixel evidence/);
  await page.locator('#cover').evaluate(cover => { cover.style.top = '100px'; });
  expect((await measureSurfaceContrast(page.locator('#warning'))).every(sample => sample.ratio === 21)).toBe(true);
});

test('repeatable rounded native planes shield only contained focus paint', async ({ page }) => {
  await page.setContent('<style>body{background:white}#under{position:fixed;inset:0;background:black;border:3px solid black;box-shadow:0 0 20px black}button{position:absolute;left:3px;top:60px;width:100px;height:32px;background:white;color:black;border:0;outline:none}button:focus{outline:2px solid black}</style><div id="under"></div><div id="host"><button>Action</button></div>');
  await page.locator('#host').evaluate(host => {
    host.attachShadow({ mode: 'open' }).innerHTML = '<div id="plane" popover="manual" style="position:fixed;left:40px;top:40px;width:160px;height:160px;margin:0;padding:0;border:0;border-radius:12px;background:white"><slot></slot></div>';
    (host.shadowRoot!.querySelector('#plane') as HTMLElement).showPopover();
  });
  const target = page.getByRole('button');
  await target.focus();
  await expectSurfaceContrast(target, 'focus');
  await page.locator('#plane').evaluate(plane => { (plane as HTMLElement).style.background = 'transparent'; });
  await expect(measureSurfaceContrast(target, 'focus')).rejects.toThrow(/Overlapping sibling background|Competing border\/shadow/);
  await page.locator('#plane').evaluate(plane => { (plane as HTMLElement).style.background = 'white'; });
  await target.evaluate(button => { button.style.left = '-1px'; });
  await expect(measureSurfaceContrast(target, 'focus')).rejects.toThrow(/Overlapping sibling background|Competing border\/shadow/);
  await target.evaluate(button => { button.style.left = '3px'; button.style.top = '3px'; });
  await expect(measureSurfaceContrast(target, 'focus')).rejects.toThrow(/Overlapping sibling background|Competing border\/shadow/);
  await target.evaluate(button => { button.style.top = '60px'; });
  await page.evaluate(() => {
    const cover = document.createElement('div'); cover.id = 'cover'; cover.setAttribute('popover', 'manual');
    cover.style.cssText = 'position:fixed;inset:0;width:100vw;height:100vh;margin:0;border:0;padding:0;background:black';
    document.body.append(cover); cover.showPopover();
  });
  await expect(measureSurfaceContrast(target, 'focus')).rejects.toThrow(/Overlapping sibling background|Competing border\/shadow/);
  await page.locator('#cover').evaluate(cover => { (cover as HTMLElement).hidePopover(); cover.remove(); });
  await expectSurfaceContrast(target, 'focus');
});

async function roundedPlaneCornerFixture(page: import('@playwright/test').Page) {
  await page.setContent('<style>body{background:white}#under{position:fixed;inset:0;background:black}button{position:absolute;left:6px;top:6px;width:20px;height:20px;background:white;color:black;border:0;outline:none}button:focus{outline:2px solid black}</style><div id="under"></div><div id="host"><button>Action</button></div>');
  await page.locator('#host').evaluate(host => {
    host.attachShadow({ mode: 'open' }).innerHTML = '<div id="plane" popover="manual" style="position:fixed;left:40px;top:40px;width:160px;height:160px;margin:0;padding:0;border:0;border-radius:12px;background:white"><slot></slot></div>';
    (host.shadowRoot!.querySelector('#plane') as HTMLElement).showPopover();
  });
  const target = page.getByRole('button');
  await target.focus();
  await expectSurfaceContrast(target, 'focus');
  return target;
}

for (const zoomOwner of ['plane', 'host']) test(`review native-plane corner proof rejects ${zoomOwner} CSS zoom`, async ({ page }) => {
  const target = await roundedPlaneCornerFixture(page);
  await target.evaluate(button => { button.style.left = '3px'; });
  await page.locator(`#${zoomOwner}`).evaluate(owner => { (owner as HTMLElement).style.zoom = '2'; });
  await expect(measureSurfaceContrast(target, 'focus')).rejects.toThrow(/native-plane.*zoom.*pixel evidence/i);
});

test('review native-plane circular corner proof rejects supported bevel paint', async ({ page }, info) => {
  const supported = await page.evaluate(() => CSS.supports('corner-shape', 'bevel'));
  await info.attach('corner-shape-capability', { body: JSON.stringify({ engine: info.project.name, supported }), contentType: 'application/json' });
  test.skip(!supported, 'This engine cannot paint corner-shape:bevel; circular positive remains independently covered.');
  const target = await roundedPlaneCornerFixture(page);
  await page.locator('#plane').evaluate(plane => { (plane as HTMLElement).style.setProperty('corner-shape', 'bevel'); });
  expect(await page.locator('#plane').evaluate(plane => getComputedStyle(plane).getPropertyValue('corner-shape'))).toMatch(/^(?:bevel|superellipse\(0\))$/u);
  await expect(measureSurfaceContrast(target, 'focus')).rejects.toThrow(/Overlapping sibling background|Competing border\/shadow/);
});

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
  test('a nonmodal dialog does not hide outside pointer targets from evidence', async ({ page }) => {
    await page.setContent('<button id="outside" style="position:absolute;left:40px;top:40px;width:5px;height:5px;padding:0">Outside</button><dialog><button id="inside" style="width:40px;height:40px">Inside</button></dialog>');
    await page.locator('dialog').evaluate(dialog => (dialog as HTMLDialogElement).show());
    const inventory = (await inventoryTargets(page)).inventory;
    expect(inventory.find(target => target.target === '#outside')?.failure).toContain('lacks an evidenced exception');
    expect(inventory.find(target => target.target === '#inside')?.failure).toBeNull();
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

test('owned overlay return waits through Escape dismissal and falls back from a hidden origin', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  const trigger = page.getByTestId('fc-palette-trigger');
  const heading = page.locator('#fc-main-content h1, main h1').first();
  const dialog = page.getByTestId('fc-focus-return-dialog');
  for (const hideOrigin of [false, true]) {
    await trigger.focus();
    await page.evaluate(async (hide) => {
      const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
      const focus = await import(path) as {
        captureOverlayOrigin: (testId: string, preserveExisting: boolean) => boolean;
        restoreOverlayOrigin: () => void;
      };
      if (!focus.captureOverlayOrigin('fc-palette-trigger', true)) throw new Error('could not capture overlay origin');
      const origin = document.querySelector<HTMLElement>('[data-testid="fc-palette-trigger"]')!;
      if (hide) origin.hidden = true;
      document.body.tabIndex = -1;
      document.body.focus();
      const modal = document.createElement('dialog');
      modal.dataset.testid = 'fc-focus-return-dialog';
      modal.innerHTML = '<button>Action</button>';
      document.body.append(modal);
      modal.showModal();
      modal.querySelector('button')!.focus();
      focus.restoreOverlayOrigin();
      // The previous twelve-frame cutoff would abandon the pending return here.
      for (let frame = 0; frame < 20; frame++) {
        await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
      }
      if (!modal.open || document.activeElement === origin) throw new Error('modal return ran before dismissal');
    }, hideOrigin);
    await page.keyboard.press('Escape');
    await expect(dialog).not.toHaveAttribute('open', '');
    if (hideOrigin) await expect(heading).toBeFocused();
    else await expect(trigger).toBeFocused();
    await dialog.evaluate(element => element.remove());
    if (hideOrigin) await trigger.evaluate(element => { (element as HTMLElement).hidden = false; });
  }
});

test('owned overlay return releases watchers when no destination appears after dismissal', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.getByTestId('fc-palette-trigger').focus();
  await page.evaluate(async () => {
    const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(path) as {
      captureOverlayOrigin: (testId: string, preserveExisting: boolean) => boolean;
      restoreOverlayOrigin: () => void;
    };
    if (!focus.captureOverlayOrigin('fc-palette-trigger', true)) throw new Error('could not capture overlay origin');
    document.querySelector<HTMLElement>('[data-testid="fc-palette-trigger"]')!.hidden = true;
    document.querySelectorAll('#fc-main-content h1, main h1').forEach(heading => heading.remove());
    const modal = document.createElement('dialog');
    modal.dataset.testid = 'fc-focus-return-dialog';
    modal.innerHTML = '<button>Action</button>';
    document.body.append(modal);
    modal.showModal();
    modal.querySelector('button')!.focus();
    const add = document.addEventListener.bind(document);
    const remove = document.removeEventListener.bind(document);
    const watched = new Map<string, EventListenerOrEventListenerObject>();
    let recording = true;
    Object.defineProperty(document, 'addEventListener', { configurable: true, value: (type: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions) => {
      if (recording && (type === 'pointerdown' || type === 'keydown')) watched.set(type, listener);
      add(type, listener, options);
    } });
    Object.defineProperty(document, 'removeEventListener', { configurable: true, value: (type: string, listener: EventListenerOrEventListenerObject, options?: boolean | EventListenerOptions) => {
      if (watched.get(type) === listener) watched.delete(type);
      remove(type, listener, options);
    } });
    focus.restoreOverlayOrigin();
    recording = false;
    (window as typeof window & { __fcFocusWatch?: { active: () => number; restore: () => void } }).__fcFocusWatch = {
      active: () => watched.size,
      restore: () => {
        Reflect.deleteProperty(document, 'addEventListener');
        Reflect.deleteProperty(document, 'removeEventListener');
      },
    };
    if (watched.size !== 2) throw new Error(`expected both return watchers, got ${watched.size}`);
  });
  await page.keyboard.press('Escape');
  await expect(page.getByTestId('fc-focus-return-dialog')).not.toHaveAttribute('open', '');
  await expect.poll(() => page.evaluate(() => (window as typeof window & { __fcFocusWatch?: { active: () => number } }).__fcFocusWatch?.active()), { timeout: 6_500 }).toBe(0);
  await page.evaluate(() => {
    const heading = document.createElement('h1');
    heading.textContent = 'Late route heading';
    document.querySelector('#fc-main-content, main')?.append(heading);
    (window as typeof window & { __fcFocusWatch?: { restore: () => void } }).__fcFocusWatch?.restore();
  });
  await expect(page.getByRole('heading', { name: 'Late route heading' })).not.toBeFocused();
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
      await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
      const themeLabel = theme === 'dark' ? 'Dark' : 'Light';
      await page.getByTitle('Change theme').first().click();
      await page.getByRole('menuitem', { name: themeLabel, exact: true }).click();
      await expect(page.getByTitle('Change theme').first()).toContainText(themeLabel);
      await settlePaint(page);
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
      await page.screenshot({ path: testInfo.outputPath(`forced-colors-${theme}.png`) });

      await page.evaluate(() => {
        const fixture = document.createElement('div');
        fixture.id = 'ae-motion-fixture';
        fixture.style.cssText = 'position:fixed;left:24px;top:80px;z-index:1';
        fixture.innerHTML = '<button class="fc-expand-button" type="button">Expand</button><div class="fc-row-expanded">Expanded row</div><div class="fc-reconciliation-sweep">Reconciled grid</div>';
        document.body.append(fixture);
      });
      const cues = ['.fc-expand-button', '.fc-row-expanded', '.fc-reconciliation-sweep'];
      const motionOf = (selector: string) => page.locator(`#ae-motion-fixture ${selector}`).evaluate(element => {
        const css = getComputedStyle(element);
        return { transition: css.transitionDuration, animation: css.animationName, outline: `${css.outlineStyle} ${css.outlineWidth}` };
      });
      // Positive control: the real fc-projection.css rules match and move before reduced motion removes them.
      await page.emulateMedia({ reducedMotion: 'no-preference' });
      for (const selector of cues) {
        await expect(page.locator(`#ae-motion-fixture ${selector}`)).toBeVisible();
        const motion = await motionOf(selector);
        expect(motion.transition.split(',').some(duration => parseFloat(duration) > 0) || motion.animation !== 'none', `${selector} normal motion`).toBe(true);
      }
      await page.emulateMedia({ reducedMotion: 'reduce' });
      for (const selector of cues) {
        const motion = await motionOf(selector);
        expect(motion.transition.split(',').every(duration => parseFloat(duration) === 0), `${selector} transition`).toBe(true);
        expect(motion.animation, `${selector} animation`).toBe('none');
      }
      // The supplementary reconciliation marker remains static and paints in both palettes. It briefly
      // marks changed rows whose refreshed values carry the content; partial reconnects show it without
      // the Reconnected notice, so it depends on no notice. This checks no contrast claim.
      expect((await motionOf('.fc-reconciliation-sweep')).outline, 'reduced-motion sweep outline').toMatch(/^solid [1-9]/u);
      await page.emulateMedia({ forcedColors: 'none', reducedMotion: 'reduce' });
      const authoredOutline = await page.locator('#ae-motion-fixture .fc-reconciliation-sweep').evaluate(element => {
        const css = getComputedStyle(element), probe = document.createElement('canvas').getContext('2d')!;
        probe.fillStyle = 'rgba(0,0,0,0)'; probe.fillStyle = css.outlineColor; probe.fillRect(0, 0, 1, 1);
        return { style: css.outlineStyle, width: parseFloat(css.outlineWidth), alpha: probe.getImageData(0, 0, 1, 1).data[3] };
      });
      expect(authoredOutline.style).toBe('solid');
      expect(authoredOutline.width).toBeGreaterThan(0);
      expect(authoredOutline.alpha, 'reduced-motion sweep outline paints a visible authored color').toBeGreaterThan(0);
      await page.locator('#ae-motion-fixture').evaluate(element => element.remove());

      await page.emulateMedia({ forcedColors: 'none' });
      await page.setViewportSize({ width: 1280, height: 900 });
      await page.goto('/counter/counter-projection');
      await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
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
    root.innerHTML = '<table class="fc-projection-grid"><tbody><tr class="fluent-data-grid-row"><td style="font-size:16px;line-height:20px">Cell</td></tr><tr class="fluent-data-grid-row"><td style="font-size:16px;line-height:20px">Later row</td></tr></tbody></table>';
    document.body.append(root);
    root.dataset.observed = '0';
    grid.observeRowMetrics('fixture', 'fixture-instance', { invokeMethodAsync: (_: string, _id: string, height: number, floor: number) => {
      root.dataset.observed = String(height); root.dataset.floor = String(floor); return Promise.resolve();
    } }, 32);
  });
  const root = page.locator('#row-metric-fixture');
  await expect.poll(() => root.evaluate(element => element.style.getPropertyValue('--fc-grid-row-height'))).toBe('32px');
  await expect(root).toHaveAttribute('data-observed', '32');
  await root.locator('td').nth(1).evaluate(element => { element.style.fontSize = '28px'; element.style.lineHeight = '42px'; });
  await expect.poll(() => root.evaluate(element => Number((element as HTMLElement).dataset.observed))).toBeGreaterThan(32);
  await expect(root).toHaveAttribute('data-floor', '32');
  await root.locator('td').nth(1).evaluate(element => { element.style.fontSize = '16px'; element.style.lineHeight = '20px'; element.innerHTML = '<div>First block</div><div>Second line<br>Third line</div>'; });
  await expect.poll(() => root.evaluate(element => Number((element as HTMLElement).dataset.observed))).toBeGreaterThanOrEqual(60);
  await root.locator('td').nth(1).evaluate(element => { element.textContent = 'Cell'; });
  await expect(root).toHaveAttribute('data-observed', '32');
  await page.evaluate(async () => {
    const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-datagrid.js';
    const grid = await import(modulePath); grid.disposeViewKey('fixture', undefined, 'fixture-instance');
    document.getElementById('row-metric-fixture')!.remove();
  });
});

test('real generated compact rows preserve 32px, match ItemSize through spacing, and restore after density changes', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.getByTestId('fc-ae-rows-two').click();
  const table = page.getByTestId('fc-generated-counter-grid').locator('table.fluent-data-grid');
  await expect(table).toBeVisible();
  const density = async (name: string) => {
    await page.getByTestId(`fc-density-control-${name.toLowerCase()}`).click();
    await expect(page.getByTestId('fc-type-specimen')).toHaveAttribute('data-density', name.toLowerCase());
  };
  const metrics = () => table.evaluate(element => ({ itemSize: Number(element.getAttribute('data-fc-grid-item-size')), rows: [...element.querySelectorAll('tbody tr.fluent-data-grid-row')].map(row => row.getBoundingClientRect().height) }));
  await expect.poll(metrics).toEqual({ itemSize: 32, rows: [32, 32] });
  const grid = page.getByTestId('fc-generated-counter-grid').locator('[data-fc-datagrid]').first();
  await grid.focus();
  // Use the operator's Tab path so the initial route-heading restoration guard is released.
  for (let step = 0; step < 10 && !await table.locator('fluent-button').first().evaluate(element => element === document.activeElement); step++) await page.keyboard.press('Tab');
  await expect(table.locator('fluent-button').first()).toBeFocused();
  const focusedControl = await table.locator('fluent-button').first().elementHandle();
  await applyTextSpacing(page);
  await expect.poll(async () => { const measured = await metrics(); return measured.itemSize >= 32 && measured.rows.length === 2 && measured.rows.every(height => Math.abs(height - measured.itemSize) < .5); }).toBe(true);
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
  const largerText = await page.addStyleTag({ content: '[data-testid="fc-generated-counter-grid"] tbody tr.fluent-data-grid-row:nth-child(2 of .fluent-data-grid-row) td { font-size:64px !important; }' });
  await largerText.evaluate(element => (element as Element).setAttribute('data-ae-text-spacing', 'additional-user-text-size-stress'));
  await expect.poll(async () => (await metrics()).itemSize).toBeGreaterThan(32);
  await expect.poll(() => focusedControl!.evaluate(element => element.isConnected)).toBe(false);
  await expect(grid).toBeFocused();
  await expect.poll(async () => {
    const fonts = await table.evaluate(element => [...element.querySelectorAll('tbody tr.fluent-data-grid-row')].map(row => parseFloat(getComputedStyle(row.querySelector('td')!).fontSize)));
    return fonts.length === 2 && fonts.every(Number.isFinite) && fonts[1] > fonts[0];
  }, {message:'Only the settled second row receives the larger user-text override'}).toBe(true);
  await expect.poll(async () => (await metrics()).itemSize).toBeGreaterThan(spaced.itemSize);
  await expect.poll(async () => { const measured = await metrics(); return measured.rows.length === 2 && measured.rows.every(height => Math.abs(height-measured.itemSize) < .5) && measured.itemSize === Math.ceil(Math.max(...measured.rows)); }).toBe(true);
  const firstGrowth = (await metrics()).itemSize;
  await page.getByTestId('fc-command-input').focus();
  await largerText.evaluate(element => { element.textContent = element.textContent!.replace('64px', '80px'); });
  await expect.poll(async () => (await metrics()).itemSize).toBeGreaterThan(firstGrowth);
  await expect.poll(async () => { const measured = await metrics(); return measured.rows.length === 2 && measured.rows.every(height => Math.abs(height - measured.itemSize) < .5); }).toBe(true);
  await testInfo.attach('generated-compact-larger-user-text-row-metrics', { body: JSON.stringify(await metrics()), contentType: 'application/json' });
  await expect(page.getByTestId('fc-command-input')).toBeFocused();
  await density('Roomy');
  await expect.poll(async () => (await metrics()).itemSize).toBeGreaterThanOrEqual(56);
  await removeTextSpacing(page);
  await expect.poll(metrics).toEqual({ itemSize: 56, rows: [56, 56] });
  await density('Compact');
  await expect.poll(metrics).toEqual({ itemSize: 32, rows: [32, 32] });
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

test('repeatable palette dismissal preserves Tab through delayed entry and native return callbacks', async ({ page }, info) => {
  // Delay only the completion reply of the real entry interop. Its focus behavior,
  // component registration/disposal and the keyboard dismissal all remain genuine.
  await page.route('**/fc-keyboard.js', async route => {
    const response = await route.fetch();
    const source = await response.text();
    expect(source).toContain('export function focusElement(element)');
    await route.fulfill({ response, body: source.replace('export function focusElement(element)', 'function originalPaletteEntry(element)') + `
export function focusElement(element) {
  const result = originalPaletteEntry(element);
  if (element?.getAttribute?.('data-testid') !== 'fc-palette-search') return result;
  return new Promise(resolve => { window.__aeReleasePaletteEntry = resolve; });
}` });
  });
  await page.goto('/__frontcomposer/specimens/page-toolbar');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  await page.getByTestId('fc-palette-trigger').click();
  await expect(page.getByTestId('fc-palette-search')).toBeFocused();
  await page.waitForFunction(() => typeof (window as typeof window & { __aeReleasePaletteEntry?: unknown }).__aeReleasePaletteEntry === 'function');
  await page.evaluate(() => {
    const host = document.querySelector('[data-testid="fc-palette-root"]')!.closest('fluent-dialog')!;
    const callbacks: (() => void)[] = [];
    const original = window.setTimeout;
    window.setTimeout = ((handler: TimerHandler, delay?: number, ...args: unknown[]) => {
      if (delay === 25 && typeof handler === 'function' && new Error().stack?.includes('Microsoft.FluentUI.AspNetCore.Components.lib.module.js')) {
        callbacks.push(() => handler(...args)); return 0;
      }
      return original(handler, delay, ...args);
    }) as typeof window.setTimeout;
    (window as typeof window & { __aePaletteReturn?: unknown }).__aePaletteReturn = { host, callbacks, restore: () => { window.setTimeout = original; } };
  });
  await page.keyboard.press('Escape');
  await expect(page.getByTestId('fc-palette-root')).toHaveCount(0);
  await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  await page.keyboard.press('Tab');
  const next = page.getByTestId('fc-settings-button');
  await expect(next).toBeFocused();
  const proof = await page.evaluate(() => {
    const w = window as typeof window & {
      __aeReleasePaletteEntry: () => void;
      __aePaletteReturn: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void };
    };
    w.__aeReleasePaletteEntry();
    w.__aePaletteReturn.restore();
    const proof = { queued: w.__aePaletteReturn.callbacks.length, cleared: w.__aePaletteReturn.host.previousActiveElement === null };
    for (const callback of w.__aePaletteReturn.callbacks) callback();
    return proof;
  });
  await info.attach('palette-delayed-return', { body: JSON.stringify(proof), contentType: 'application/json' });
  expect(proof.queued).toBeGreaterThan(0);
  await expect(next).toBeFocused();
  expect(proof.cleared).toBe(true);
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
      host.dispatchEvent(new CustomEvent('beforetoggle', { detail: { newState: 'closed' } }));
      host.shadowRoot?.querySelector('dialog')?.dispatchEvent(new Event('close'));
      if (!reservation.watchFocus || !reservation.moved || !intent.watchFocus || !intent.moved) throw new Error('duplicate native close discarded later external input');
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

test('resume review offset opaque shadow cannot hide changed focus paint', async ({ page }) => {
  await page.setContent('<style>body{background:white}button{margin:40px;width:100px;height:40px;background:white;outline:none;border:0;border-radius:0;box-shadow:1px 0 0 10px white}button:focus{box-shadow:1px 0 0 10px white,0 0 0 2px black}</style><button>Action</button>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('focused target has no visible focus indicator');
  await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Nonempty/);
  await page.addStyleTag({ content: 'button:focus{box-shadow:1px 0 0 10px white,0 0 0 14px black}' });
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  await expectSurfaceContrast(page.locator('button'), 'focus');
});

test('resume review ordinary flow descendant can obscure the target ring', async ({ page }) => {
  await page.setContent('<style>a{display:block;margin:40px;width:100px;height:40px;outline:none}a:focus{outline:2px solid black}span{display:block;width:140px;height:80px;margin:-50px 0 0 -20px;background:white}</style><a href="#">Action<span></span></a>');
  await page.locator('a').focus();
  expect((await focusGeometry(page.locator('a'))).failures).toContain('app content obscures target or focus ring');
  await page.locator('span').evaluate(node => { node.style.cssText = 'width:10px;height:10px;margin:0'; });
  expect((await focusGeometry(page.locator('a'))).failures).toEqual([]);
});

test('resume review fully clipped pseudo borders supply no contrast evidence', async ({ page }) => {
  await page.setContent('<style>button{position:relative;margin:40px;width:100px;height:40px;background:white;outline:none;border:0}button::after{content:"";position:absolute;inset:0;border:2px solid black;clip-path:inset(50%)}</style><button>Action</button>');
  for (const kind of ['border', 'cue'] as const) await expect(expectSurfaceContrast(page.locator('button'), kind)).rejects.toThrow(/Nonempty|Clipped pseudo/);
  await page.addStyleTag({ content: 'button::after{clip-path:none}' });
  for (const kind of ['border', 'cue'] as const) await expectSurfaceContrast(page.locator('button'), kind);
});

test('resume review overflowing generated text cannot borrow its host background', async ({ page }) => {
  await page.setContent('<style>body{background:black}div{background:white;color:black;width:50px;white-space:nowrap}div::after{content:"Long generated label that overflows"}</style><div></div>');
  await expect(measureSurfaceContrast(page.locator('div'))).rejects.toThrow(/Generated text geometry/);
  await page.addStyleTag({ content: 'div{width:500px}' });
  await expectSurfaceContrast(page.locator('div'));
});

test('generated pseudo text clipped by its own overflow needs pixel evidence', async ({ page }) => {
  await page.setContent('<style>p{width:500px;height:100px}p::before{content:"Long generated label";display:block;width:10px;height:10px;overflow:hidden}</style><p></p>');
  await expect(expectNoTextClipping(page)).rejects.toThrow(/generated text clipping needs pixel evidence/);
  await page.addStyleTag({ content: 'p::before{width:auto;height:auto;overflow:visible}' });
  await expectNoTextClipping(page);
});

test('resume review descendant background paint cannot cover sampled text', async ({ page }) => {
  await page.setContent('<style>p{position:relative;background:white;color:black}span{position:absolute;inset:0;background:black;z-index:1}</style><p>Visible label<span></span></p>');
  await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
  await page.locator('span').evaluate(node => node.remove());
  await expectSurfaceContrast(page.locator('p'));
});

for (const slotted of [false, true]) test(`resume review cross-root background paint is checked for ${slotted ? 'slotted' : 'shadow'} text`, async ({ page }) => {
  await page.setContent('<div id="host"></div>');
  await page.locator('#host').evaluate((host, slotted) => {
    const root = host.attachShadow({ mode: 'open' });
    if (slotted) {
      host.innerHTML = '<p slot="label" style="background:white;color:black;position:relative">Visible label</p>';
      root.innerHTML = '<slot name="label"></slot><div id="cover" style="position:fixed;inset:0;background:black;z-index:10"></div>';
    } else {
      root.innerHTML = '<p style="background:white;color:black">Visible label</p>';
      const cover = document.createElement('div'); cover.id = 'cover'; cover.style.cssText = 'position:fixed;inset:0;background:black;z-index:10'; document.body.append(cover);
    }
  }, slotted);
  await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Overlapping sibling background/);
  await page.locator('#cover').evaluate(node => node.remove());
  await expectSurfaceContrast(page.locator('p'));
});

test('resume review text stroke requires actual glyph paint evidence', async ({ page }) => {
  await page.setContent('<p style="background:white;color:black;-webkit-text-stroke:10px white">Label</p>');
  await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Text stroke/);
  await page.locator('p').evaluate(node => node.style.setProperty('-webkit-text-stroke-width', '0px'));
  await expectSurfaceContrast(page.locator('p'));
});

test('resume review degenerate SVG fill supplies no painted icon', async ({ page }) => {
  await page.setContent('<svg width="100" height="100" style="background:white"><path d="M10 10L80 80" fill="black"/></svg>');
  await expect(expectSurfaceContrast(page.locator('svg'), 'icon')).rejects.toThrow(/Nonempty/);
  await page.locator('path').evaluate(node => node.setAttribute('d', 'M10 10L80 80L10 80Z'));
  await expectSurfaceContrast(page.locator('svg'), 'icon');
});

test('resume review unpainted SVG dash patterns cannot certify an icon', async ({ page }) => {
  await page.setContent('<svg width="100" height="40" style="background:white"><line x1="10" y1="20" x2="80" y2="20" stroke="black" stroke-width="3" stroke-dasharray="0 1000"/></svg>');
  await expect(measureSurfaceContrast(page.locator('svg'), 'icon')).rejects.toThrow(/SVG dashed stroke/);
  await page.locator('line').evaluate(node => node.setAttribute('stroke-dasharray', 'none'));
  await expectSurfaceContrast(page.locator('svg'), 'icon');
});

test('resume review transformed static containing block owns pseudo border adjacency', async ({ page }) => {
  await page.setContent('<style>body{background:white}button{margin:40px;width:100px;height:40px;position:static;transform:translate(0px);background:black;outline:none;border:0}button:focus::after{content:"";position:absolute;inset:0;border:2px solid black}</style><button>Action</button>');
  await page.locator('button').focus();
  await expect(expectSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/contrast failures/);
  await page.addStyleTag({ content: 'button:focus::after{border-color:white}' });
  await expectSurfaceContrast(page.locator('button'), 'focus');
});


test('resume review scrollport clips its own ordinary flow content before obstruction checks', async ({ page }) => {
  await page.setContent('<style>div{margin:40px;width:100px;height:40px;overflow:auto;outline:none}div:focus{outline:2px solid black}span{display:block;width:200px;height:100px;background:white}</style><div tabindex="0"><span>Scrollable content</span></div>');
  await page.locator('div').focus();
  expect((await focusGeometry(page.locator('div'))).failures).toEqual([]);
  await page.locator('div').evaluate(node => node.style.overflow = 'visible');
  expect((await focusGeometry(page.locator('div'))).failures).toContain('app content obscures target or focus ring');
});


test('resume review native slots own plain labels but never hide opaque projected obstructions', async ({ page }) => {
  await page.setContent('<style>fluent-button{display:block;width:100px;height:40px;margin:40px;outline:none}fluent-button:focus{outline:2px solid black}span{display:block;width:140px;height:80px;margin:-20px 0 0 -20px;background:white}</style><fluent-button tabindex="0">Plain label</fluent-button>');
  await page.locator('fluent-button').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<slot></slot>'; });
  await page.locator('fluent-button').focus();
  expect((await focusGeometry(page.locator('fluent-button'))).failures).toEqual([]);
  await page.locator('fluent-button').evaluate(element => { element.innerHTML = '<span>Opaque obstruction</span>'; });
  expect((await focusGeometry(page.locator('fluent-button'))).failures).toContain('app content obscures target or focus ring');
});

// Resumed B1–B4 counterexamples must fail the evidence helpers on every engine.
test('B1 native value glyphs cannot be vertically clipped inside a short control', async ({ page }) => {
  await page.setContent('<input aria-label="Value" value="Clipped Ag" style="box-sizing:border-box;width:220px;height:32px;padding:0;font:64px Arial">');
  await expect(expectNoTextClipping(page)).rejects.toThrow(/native value is vertically clipped/);
  await page.locator('input').evaluate(input => { input.style.height = '96px'; });
  await expectNoTextClipping(page);
});

for (const slotted of [false, true]) test(`B1 text clips follow ${slotted ? 'slotted' : 'shadow'} composed ancestors`, async ({ page }) => {
  await page.setContent('<div id="clip" style="width:20px;height:50px;overflow:hidden"><div id="host"></div></div>');
  await page.locator('#host').evaluate((host, slotted) => {
    host.attachShadow({ mode: 'open' }).innerHTML = slotted ? '<slot></slot>' : '<span style="white-space:nowrap">Composed label must stay readable</span>';
    if (slotted) host.innerHTML = '<span style="white-space:nowrap">Composed label must stay readable</span>';
  }, slotted);
  await expect(expectNoTextClipping(page)).rejects.toThrow(/#clip/);
  await page.locator('#clip').evaluate(clip => { clip.style.width = '400px'; });
  await expectNoTextClipping(page);
});

test('B1 explicit inset text clips are measured and nested scrollports retain their visible portion', async ({ page }) => {
  await page.setContent('<p id="clip" style="width:200px;clip-path:inset(0 95% 0 0)">Explicitly clipped label</p>');
  await expect(expectNoTextClipping(page)).rejects.toThrow(/#clip/);
  await page.locator('#clip').evaluate(clip => { clip.style.clipPath = 'inset(0)'; });
  await expectNoTextClipping(page);
  await page.setContent('<div style="width:60px;height:40px;overflow:hidden"><div style="width:60px;height:40px;overflow:auto"><span style="white-space:nowrap">Scrollable unbroken native label</span></div></div>');
  await expectNoTextClipping(page);
});

test('B2 decoded replaced paint covering text requires pixel evidence', async ({ page }) => {
  await page.setContent('<div style="position:relative;width:220px;background:white;color:black"><span id="label">Visible label</span><img alt="" width="220" height="30" style="position:absolute;inset:0" src="data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22220%22 height=%2230%22%3E%3Crect width=%22220%22 height=%2230%22 fill=%22black%22/%3E%3C/svg%3E"></div>');
  await page.locator('img').evaluate(async image => { await (image as HTMLImageElement).decode(); });
  await expect(measureSurfaceContrast(page.locator('#label'))).rejects.toThrow(/Overlapping replaced paint needs pixel evidence/);
  await page.locator('img').evaluate(image => { image.style.clipPath = 'inset(100%)'; });
  await expectSurfaceContrast(page.locator('#label'));
});

test('B2 positioned opaque pseudo paint cannot certify the text it covers', async ({ page }) => {
  await page.setContent('<style>#label{position:relative;display:block;width:220px;background:white;color:black}#label::after{content:"";position:absolute;inset:0;background:black}</style><span id="label">Visible label</span>');
  await expect(measureSurfaceContrast(page.locator('#label'))).rejects.toThrow(/Overlapping pseudo background needs pixel evidence/);
  await page.addStyleTag({ content: '#label::after{clip-path:inset(100%)}' });
  await expectSurfaceContrast(page.locator('#label'));
});

test('B2 exterior focus contrast cannot borrow white behind unrelated ring-area paint', async ({ page }) => {
  await page.setContent('<style>body{background:white}button{position:fixed;left:40px;top:40px;width:100px;height:40px;outline:none;background:white;border:0;border-radius:0}button:focus{outline:2px solid black}#under{position:fixed;left:38px;top:38px;width:104px;height:44px;background:black}</style><div id="under"></div><button>Action</button>');
  await page.locator('button').focus();
  await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping sibling background needs pixel evidence/);
  await page.locator('#under').evaluate(under => { under.style.left = '240px'; });
  await expectSurfaceContrast(page.locator('button'), 'focus');
});

test('B3 inventories pointer-active menu checkbox and radio roles beside passing targets', async ({ page }) => {
  await page.setContent('<button style="width:40px;height:40px">Passing</button><div role="menuitemcheckbox" onclick="void 0" style="position:fixed;left:100px;top:40px;width:5px;height:5px"></div><div role="menuitemradio" onclick="void 0" style="position:fixed;left:140px;top:40px;width:5px;height:5px"></div>');
  const inventory = (await inventoryTargets(page)).inventory;
  expect(inventory).toHaveLength(3);
  expect(inventory.filter(target => target.failure)).toHaveLength(2);
  await page.locator('[role^="menuitem"]').evaluateAll(controls => controls.forEach(control => { (control as HTMLElement).style.width = '24px'; (control as HTMLElement).style.height = '24px'; }));
  await expectTargetSizes(page);
});

test('B3 aria-hidden does not hide a visible pointer-active control from target evidence', async ({ page }) => {
  await page.setContent('<button style="width:40px;height:40px">Passing</button><div aria-hidden="true"><button id="hidden" style="position:fixed;left:100px;top:40px;width:5px;height:5px;padding:0"></button></div>');
  expect((await inventoryTargets(page)).inventory.find(target => target.target === '#hidden')?.failure).toContain('lacks an evidenced exception');
  await page.locator('#hidden').evaluate(control => { control.style.width = control.style.height = '24px'; });
  await expectTargetSizes(page);
});

test('B3 minimums and equivalent exceptions use reachable clipped target rectangles', async ({ page }) => {
  await page.setContent('<div id="clip" style="position:fixed;left:40px;top:40px;width:10px;height:10px;overflow:hidden"><button id="clipped" style="width:100px;height:40px">Action</button></div><button id="small" style="position:fixed;left:200px;top:40px;width:10px;height:10px;padding:0"></button>');
  const equivalent: TargetException = { selector: '#small', kind: 'Equivalent', rationale: 'Same action', keyboardPath: 'Tab', evidence: 'Retained target fixture', equivalentSelector: '#clipped' };
  const inventory = (await inventoryTargets(page, [equivalent])).inventory;
  expect(inventory.find(target => target.target === '#clipped')).toMatchObject({ width: 10, height: 10, conforming: false });
  expect(inventory.find(target => target.target === '#clipped')?.failure).toContain('Clipping');
  expect(inventory.find(target => target.target === '#small')?.failure).toContain('Equivalent control');
  await page.locator('#clip').evaluate(clip => { clip.style.width = '100px'; clip.style.height = '40px'; });
  await expectTargetSizes(page, [equivalent]);
});

test('B3 a fully clipped essential target remains an explicit failure', async ({ page }) => {
  await page.setContent('<button style="width:40px;height:40px">Passing</button><div style="width:0;height:0;overflow:hidden"><button id="essential" data-ae-essential-target style="width:40px;height:40px">Essential action</button></div>');
  const exception: TargetException = { selector: '#essential', kind: 'Essential', rationale: 'Fixture only', keyboardPath: 'Tab', evidence: 'Retained clip fixture', essentialReason: 'Demonstrates a required control cannot silently disappear' };
  expect((await inventoryTargets(page, [exception])).inventory.find(target => target.target === '#essential')?.failure).toContain('Clipping');
});

test('B4 nontranslation transforms require actual indicator-envelope evidence while translation passes', async ({ page }) => {
  await page.setContent('<style>button{position:fixed;left:3px;top:40px;width:40px;height:30px;transform-origin:0 0;transform:scale(3);outline:none}button:focus{outline:2px solid black}</style><button>Action</button>');
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('transformed focus indicator envelope needs pixel evidence');
  await page.locator('button').evaluate(button => { button.style.transform = 'translate(40px, 0)'; });
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  await page.locator('button').evaluate(button => { button.style.scale = '3'; });
  expect((await focusGeometry(page.locator('button'))).failures).toContain('transformed focus indicator envelope needs pixel evidence');
});

test('B5 query navigation cancels old detail captures before replacement recovery', async ({ page }) => {
  await page.goto('/counter');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  const result = await page.evaluate(async () => {
    const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
    const focus = await import(path);
    const original = location.href;
    const host = document.createElement('div');
    host.setAttribute('data-fc-datagrid', 'query-fixture');
    host.innerHTML = '<div class="fc-expand-in-row-detail"><button>Old query action</button></div><span id="query-explanation" tabindex="-1">New query explanation</span>';
    document.body.append(host);
    focus.trackDetailFocus('query-recovery', 'query-fixture');
    const detail = host.querySelector('button')!;
    detail.focus();
    const pathname = location.pathname;
    history.pushState({}, '', `${pathname}?query=new`);
    const samePath = location.pathname === pathname;
    detail.remove();
    const cancelled = !focus.focusReplacementHeading('query-recovery', 'query-explanation') && document.activeElement?.id !== 'query-explanation';
    host.querySelector('.fc-expand-in-row-detail')!.append(detail);
    focus.trackDetailFocus('query-recovery', 'query-fixture');
    detail.focus();
    detail.remove();
    const currentRecovered = focus.focusReplacementHeading('query-recovery', 'query-explanation');
    focus.cancelReplacementFocus('query-recovery');
    host.remove();
    history.replaceState({}, '', original);
    return { samePath, cancelled, currentRecovered };
  });
  expect(result).toEqual({ samePath: true, cancelled: true, currentRecovered: true });
});

test('B5 real co-mounted logical owners recover the affected physical projection once', async ({ page }, info) => {
  await page.goto('/__frontcomposer/specimens/type?announcementFocus=true');
  await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  const fixture = page.getByTestId('fc-duplicate-missing-detail-fixture');
  await expect(fixture.locator('[data-fc-datagrid]')).toHaveCount(2);
  await fixture.evaluate(root => {
    const state = { focus: [] as string[], speech: [] as string[] };
    (window as typeof window & { aeDuplicateRecovery?: typeof state }).aeDuplicateRecovery = state;
    root.addEventListener('focusin', event => {
      const target = event.target;
      if (target instanceof Element && target.matches('.fc-expanded-row-hidden-banner-text')) state.focus.push(target.closest('[data-testid^="fc-duplicate-missing-detail-"]')!.getAttribute('data-testid')!);
    });
    new MutationObserver(() => {
      for (const status of root.querySelectorAll('[data-testid="fc-surface-status"]')) if (status.textContent?.trim()) state.speech.push(status.textContent.trim());
    }).observe(root, { childList: true, characterData: true, subtree: true });
  });
  await page.getByTestId('fc-duplicate-missing-detail-1').getByTestId('fc-remove-duplicate-focused-detail').focus();
  await page.keyboard.press('Enter');
  await expect(page.getByTestId('fc-duplicate-missing-detail-1').locator('.fc-expanded-row-hidden-banner-text')).toBeFocused();
  await expect(fixture.locator('.fc-expanded-row-hidden-banner-text')).toHaveCount(2);
  const result = await page.evaluate(() => (window as typeof window & { aeDuplicateRecovery?: { focus: string[]; speech: string[] } }).aeDuplicateRecovery);
  expect(result).toEqual({ focus: ['fc-duplicate-missing-detail-1'], speech: [] });
  await info.attach('duplicate-projection-recovery', { body: JSON.stringify(result), contentType: 'application/json' });
});

for (const indicator of ['box-shadow', 'pseudo border']) test(`B2 exterior ${indicator} contrast checks the actual painted ring adjacency`, async ({ page }) => {
  const focus = indicator === 'box-shadow' ? 'button:focus{box-shadow:0 0 0 2px black}' : 'button:focus::after{content:"";position:absolute;inset:-2px;border:2px solid black}';
  await page.setContent(`<style>body{background:white}button{position:fixed;left:40px;top:40px;width:100px;height:40px;outline:none;background:white;border:0;border-radius:0}#under{position:fixed;left:38px;top:38px;width:104px;height:44px;background:black}${focus}</style><div id="under"></div><button>Action</button>`);
  await page.locator('button').focus();
  await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping sibling background needs pixel evidence/);
  await page.locator('#under').evaluate(under => { under.style.left = '240px'; });
  await expectSurfaceContrast(page.locator('button'), 'focus');
});

test('B3 offscreen skip links stay excluded while ordinary page and scrollport controls remain reachable', async ({ page }) => {
  await page.setContent('<style>.fc-skip-link:not(:focus){position:absolute;left:-10000px}</style><a class="fc-skip-link" href="#content">Skip to content</a><div style="position:fixed;left:10px;top:10px;width:160px;height:40px;overflow:hidden"><div style="width:160px;height:40px;overflow:auto"><div style="height:200px"></div><button id="scrollable" style="width:100px;height:32px">Scroll action</button></div></div><main id="content" style="margin-top:1200px"><button id="below" style="width:100px;height:32px">Page action</button></main>');
  const inventory = await expectTargetSizes(page);
  expect(inventory.map(target => target.target)).toEqual(['#scrollable', '#below']);
  expect(inventory.every(target => target.width === 100 && target.height === 32)).toBe(true);
});

test('B2 transparent SVG viewport gutters leave adjacent focus paint measurable but real vector paint does not', async ({ page }) => {
  await page.setContent('<style>body{background:white}button{position:fixed;left:40px;top:40px;width:100px;height:40px;background:white;border:0;outline:none}button:focus{outline:2px solid black}svg{position:fixed;left:20px;top:50px}</style><svg width="20" height="20"><rect x="2" y="2" width="10" height="16" fill="black"/></svg><button>Action</button>');
  await page.locator('button').focus();
  await expectSurfaceContrast(page.locator('button'), 'focus');
  await page.locator('rect').evaluate(rect => { rect.setAttribute('width', '20'); });
  await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping replaced paint needs pixel evidence/);
  await page.locator('rect').evaluate(rect => { rect.setAttribute('width', '10'); rect.setAttribute('fill', 'none'); rect.setAttribute('stroke', 'black'); rect.setAttribute('stroke-width', '16'); });
  await expect(measureSurfaceContrast(page.locator('button'), 'focus')).rejects.toThrow(/Overlapping replaced paint needs pixel evidence/);
});


test('B1 lifecycle focus-only headings are checked when revealed, while visible text clips remain failures', async ({ page }) => {
  await page.setContent('<style>.fc-lifecycle-heading:not(:focus-within){position:absolute;width:1px;height:1px;overflow:hidden;clip-path:inset(50%);white-space:nowrap}</style><div class="fc-lifecycle-heading"><h2 tabindex="-1" data-fc-lifecycle-heading>Active command heading</h2></div>');
  await expectNoTextClipping(page);
  await page.locator('h2').focus();
  await expectNoTextClipping(page);
  await page.locator('h2').evaluate(heading => { heading.style.clipPath = 'inset(0 95% 0 0)'; });
  await expect(expectNoTextClipping(page)).rejects.toThrow(/h2/);
});


async function rowObserverFixture(page: import('@playwright/test').Page, html: string) {
  await page.setContent(html);
  const source = await readFile(new URL('../../../src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-datagrid.js', import.meta.url), 'utf8');
  await page.evaluate(async source => { (window as any).aeGrid = await import('data:text/javascript;charset=utf-8,'+encodeURIComponent(source)); }, source);
}

test('C2 row metrics observe shadow content and acknowledge a retried same-height delivery', async ({ page }) => {
  await rowObserverFixture(page, '<div data-fc-datagrid="view" data-fc-datagrid-instance="one" style="height:200px;overflow:auto"><table><tbody><tr class="fluent-data-grid-row"><td><custom-cell></custom-cell></td></tr></tbody></table></div>');
  await page.evaluate(() => {
    document.querySelector('custom-cell')!.attachShadow({mode:'open'}).innerHTML='<span style="display:block;font-size:12px">Row</span>';
    const owner = window as any; owner.aeCalls=[]; owner.aeRejected=false;
    owner.aeGrid.observeRowMetrics('view','one',{invokeMethodAsync: (_: string,_id: string,height: number) => {
      owner.aeCalls.push(height);
      if (height > 32 && !owner.aeRejected) { owner.aeRejected=true; return Promise.reject(new Error('transient')); }
      return Promise.resolve();
    }},32);
  });
  await expect.poll(() => page.evaluate(() => (window as any).aeCalls)).toEqual([32]);
  await page.locator('custom-cell span').evaluate(span => { (span as HTMLElement).style.fontSize='80px'; });
  await expect.poll(() => page.evaluate(() => (window as any).aeCalls.filter((height: number)=>height>32).length)).toBe(2);
  const result = await page.evaluate(() => ({calls:(window as any).aeCalls, actual:Math.ceil(document.querySelector('tr')!.getBoundingClientRect().height), css:document.querySelector<HTMLElement>('[data-fc-datagrid]')!.style.getPropertyValue('--fc-grid-row-height')}));
  expect(result.calls.at(-1)).toBe(result.actual);
  expect(result.css).toBe(`${result.actual}px`);
});

test('C2 row metrics settle multiple grids and retain external changes and bounded disposal', async ({ page }) => {
  await rowObserverFixture(page, '<div data-fc-datagrid="a" data-fc-datagrid-instance="a"><table><tbody><tr class="fluent-data-grid-row"><td>A</td></tr></tbody></table></div><div data-fc-datagrid="b" data-fc-datagrid-instance="b"><table><tbody><tr class="fluent-data-grid-row"><td>B</td></tr></tbody></table></div>');
  await page.evaluate(() => {
    const owner=window as any; owner.aeMeasures=0; owner.aeCalls=[];
    const native=Element.prototype.getBoundingClientRect;
    Element.prototype.getBoundingClientRect=function(){if(this.matches('tr.fluent-data-grid-row'))owner.aeMeasures++;return native.call(this);};
    for (const key of ['a','b']) owner.aeGrid.observeRowMetrics(key,key,{invokeMethodAsync:(_:string,id:string,height:number)=>{owner.aeCalls.push([id,height]);return Promise.resolve();}},32);
  });
  await expect.poll(() => page.evaluate(() => (window as any).aeCalls.length)).toBe(2);
  await page.waitForTimeout(150);
  const settled=await page.evaluate(() => (window as any).aeMeasures);
  await page.waitForTimeout(150);
  expect(await page.evaluate(() => (window as any).aeMeasures)).toBe(settled);
  await page.addStyleTag({content:'td{font-size:60px}'});
  await expect.poll(() => page.evaluate(() => (window as any).aeCalls.filter(([,height]:[string,number])=>height>32).length)).toBe(2);
  await page.evaluate(() => {
    const owner=window as any; owner.aeFailures=0;
    owner.aeGrid.disposeViewKey('a',undefined,'a');
    owner.aeGrid.observeRowMetrics('a','a',{invokeMethodAsync:()=>{owner.aeFailures++;return Promise.reject(new Error('offline'));}},32);
  });
  await expect.poll(() => page.evaluate(() => (window as any).aeFailures)).toBe(3);
  await page.waitForTimeout(200);
  expect(await page.evaluate(() => (window as any).aeFailures)).toBe(3);
  await page.evaluate(() => { const owner=window as any; owner.aeGrid.observeRowMetrics('a','a',{invokeMethodAsync:()=>{owner.aeFailures++;return Promise.reject(new Error('offline'));}},32);owner.aeGrid.disposeViewKey('a',undefined,'a'); });
  const disposed=await page.evaluate(() => (window as any).aeFailures);
  await page.waitForTimeout(200);
  expect(await page.evaluate(() => (window as any).aeFailures)).toBe(disposed);
});

for (const native of ['textarea','select']) test(`C2 clipping measures visible native ${native} values while allowing native scrolling`, async ({ page }) => {
  await page.setContent(native === 'textarea' ? '<textarea style="font-size:32px;height:10px;padding:0;border:0;resize:none">Important label</textarea>' : '<select style="font-size:32px;height:10px;padding:0;border:0"><option>Important label</option></select>');
  await expect(expectNoTextClipping(page)).rejects.toThrow(/native value is vertically clipped/);
  await page.locator(native).evaluate(element => { (element as HTMLElement).style.height='80px'; if (element instanceof HTMLTextAreaElement) element.value='One line\nTwo lines\nThree lines\nFour lines\nFive lines'; });
  await expectNoTextClipping(page);
});

test('C2 visible aria-hidden labels and generated labels cannot disappear from clipping proof', async ({ page }) => {
  await page.setContent('<p aria-hidden="true" style="width:10px;height:8px;overflow:hidden;white-space:nowrap">Important long visible label</p>');
  await expect(expectNoTextClipping(page)).rejects.toThrow(/clipped text/);
  await page.locator('p').evaluate(element => { element.style.width='400px';element.style.height='40px'; });
  await expectNoTextClipping(page);
  await page.setContent('<style>p::before{content:"Important long visible label"}p{width:10px;height:8px;overflow:hidden;white-space:nowrap}</style><p></p>');
  await expect(expectNoTextClipping(page)).rejects.toThrow(/generated text clipping needs pixel evidence/);
  await page.locator('p').evaluate(element => { element.style.overflow='visible'; });
  await expectNoTextClipping(page);
  await page.setContent('<style>p::before{content:"" / "Accessible alternate"}div{width:100px;height:8px;overflow:hidden}p{margin:0}</style><div><p></p></div>');
  await expectNoTextClipping(page);
  await page.addStyleTag({content:'p::before{content:"Required / *" / ""}'});
  await expect(expectNoTextClipping(page)).rejects.toThrow(/clipped text/);
  await page.locator('div').evaluate(element=>{element.style.height='40px';});
  await expectNoTextClipping(page);
});

test('C2 transparent dark pages use actual Canvas rather than an invented white backdrop', async ({ page }) => {
  await page.setContent('<style>:root{color-scheme:dark}p{color:black}</style><p>Essential warning</p>');
  const samples=await measureSurfaceContrast(page.locator('p'));
  expect(samples[0].background.slice(0,3).every(channel=>channel<64)).toBe(true);
  expect(samples[0].ratio).toBeLessThan(4.5);
  await page.locator('p').evaluate(element=>{element.style.color='white';});
  await expectSurfaceContrast(page.locator('p'));
});

for (const paint of ['pseudo','border','shadow']) test(`C2 ${paint}-only pointer-transparent paint cannot obscure focused controls undetected`, async ({ page }) => {
  const cover=paint==='pseudo' ? '#cover{position:absolute;left:400px;top:200px;width:1px;height:1px}#cover::before{content:"";position:fixed;left:40px;top:40px;width:100px;height:40px;background:white;z-index:100;pointer-events:none}' : paint==='border' ? '#cover{position:absolute;left:40px;top:40px;width:100px;height:40px;border:20px solid white;box-sizing:border-box;z-index:10;pointer-events:none}' : '#cover{position:absolute;left:190px;top:40px;width:100px;height:40px;box-shadow:-150px 0 0 10px white;z-index:10;pointer-events:none}';
  await page.setContent(`<style>button{position:absolute;left:40px;top:40px;width:100px;height:40px;outline:none}button:focus{outline:2px solid black}${cover}</style><button>Save</button><div id="cover"></div>`);
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toContain('competing pseudo/border/shadow paint needs pixel evidence');
  await page.locator('#cover').evaluate(element=>element.remove());
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
});

for (const pseudo of [false,true]) test(`C2 competing ${pseudo?'pseudo':'element'} shadows cannot certify nominal text contrast`, async ({ page }) => {
  await page.setContent(`<style>#text{position:absolute;left:50px;top:50px;width:100px;height:30px;color:black;background:white;margin:0}#cover{position:absolute;left:300px;top:50px;width:10px;height:30px;z-index:10;pointer-events:none}${pseudo?'#cover::after{content:"";position:absolute;inset:0;box-shadow:-250px 0 0 10px black}':'#cover{box-shadow:-250px 0 0 10px black}'}</style><p id="text">Essential warning</p><div id="cover"></div>`);
  await expect(measureSurfaceContrast(page.locator('#text'))).rejects.toThrow(/Competing border\/shadow paint needs pixel evidence/);
  if (!pseudo) {
    await page.locator('#cover').evaluate(element=>{element.style.boxShadow='-250px 0 2px rgba(0,0,0,.12)';});
    const supported=await expectSurfaceContrast(page.locator('#text'));
    expect(supported[0].measurement).toBe('conservative-bound');
    expect(supported[0].ratio).toBeGreaterThanOrEqual(4.5);
    expect(supported[0].ratio).toBeLessThan(21);
    await page.locator('#text').evaluate(element=>{element.style.color='#757575';});
    expect((await expectTextContrast(page.locator('#text'))).ratio).toBeGreaterThanOrEqual(4.5);
    await expect(measureSurfaceContrast(page.locator('#text'))).rejects.toThrow(/conservative ratio .* below 4.5/);
    await page.locator('#text').evaluate(element=>{element.style.color='#555';});
    await expectSurfaceContrast(page.locator('#text'));
    await page.evaluate(()=>{const clone=document.querySelector('#cover')!.cloneNode() as HTMLElement;clone.id='second-cover';clone.style.cssText='position:absolute;left:300px;top:50px;width:10px;height:30px;z-index:10;pointer-events:none;box-shadow:-250px 0 2px rgba(0,0,0,.12)';document.body.append(clone);});
    await expect(measureSurfaceContrast(page.locator('#text'))).rejects.toThrow(/conservative ratio .* below 4.5/);
    await page.locator('#second-cover').evaluate(element=>element.remove());
    await page.locator('#cover').evaluate(element=>{element.style.boxShadow='-250px 0 2px rgba(0,0,0,.12), -250px 0 2px rgba(0,0,0,.12)';});
    await expect(measureSurfaceContrast(page.locator('#text'))).rejects.toThrow(/conservative ratio .* below 4.5/);
    await page.locator('#text').evaluate(element=>{element.style.color='black';});
    for (const shadow of ['-250px 0 2px rgba(0,0,0,.99)','-250px 0 2px rgba(0,0,255,.12)','-250px 0 0 rgba(0,0,0,.12)']) {
      await page.locator('#cover').evaluate((element,shadow)=>{element.style.boxShadow=shadow;},shadow);
      await expect(measureSurfaceContrast(page.locator('#text'))).rejects.toThrow(/Competing border\/shadow paint needs pixel evidence/);
    }
  }
  await page.locator('#cover').evaluate(element=>element.remove());
  await expectSurfaceContrast(page.locator('#text'));
});

test('C3 disjoint wrapped text fragments keep independent shadow bounds', async ({ page }) => {
  await page.setContent('<style>body{background:white}#text{position:absolute;left:50px;top:50px;width:6ch;margin:0;font:20px/40px monospace;color:#555;background:white}.cover{position:absolute;left:300px;width:10px;height:32px;z-index:10;pointer-events:none;box-shadow:-250px 0 2px rgba(0,0,0,.12)}</style><p id="text">AAAAA BBBBB</p><div class="cover" style="top:50px"></div><div class="cover" style="top:90px"></div>');
  expect(await page.locator('#text').evaluate(element=>{const range=document.createRange();range.selectNodeContents(element);return range.getClientRects().length;})).toBe(2);
  const samples=await expectSurfaceContrast(page.locator('#text'));
  expect(samples[0].measurement).toBe('conservative-bound');
  expect(samples[0].shadowBound?.layers).toHaveLength(1);
  expect(samples[0].shadowBound?.layers[0].exteriorBlur).toBeUndefined();
  await page.evaluate(()=>{const cover=document.createElement('div');cover.className='cover';cover.style.cssText='top:50px;height:72px';document.body.append(cover);});
  await expect(measureSurfaceContrast(page.locator('#text'))).rejects.toThrow(/conservative ratio .* below 4.5/);
});

test('C2 text shadow composition requires actual glyph evidence', async ({ page }) => {
  await page.setContent('<p style="color:black;text-shadow:0 0 0 white,0 0 0 white;background:white">Visible label</p>');
  await expect(measureSurfaceContrast(page.locator('p'))).rejects.toThrow(/Text shadow composition needs pixel evidence/);
  await page.locator('p').evaluate(element=>{element.style.textShadow='none';});
  await expectSurfaceContrast(page.locator('p'));
});

test('C2 target size uses remaining pointer area while valid coverage and owned shadows stay supported', async ({ page }) => {
  await page.setContent('<button id="target" style="position:absolute;left:40px;top:40px;width:30px;height:30px;padding:0"></button><div id="cover" style="position:absolute;left:40px;top:40px;width:29px;height:30px;background:black"></div>');
  expect((await inventoryTargets(page)).inventory[0]).toMatchObject({width:1,height:30,conforming:false});
  await page.locator('#cover').evaluate(cover=>{cover.style.width='6px';});
  await expectTargetSizes(page);
  await page.locator('#cover').evaluate(cover=>{cover.style.width='29px';cover.style.pointerEvents='none';});
  await expectTargetSizes(page);
  await page.locator('#cover').evaluate(cover=>cover.remove());
  await page.setContent('<fluent-button id="target" tabindex="0" style="display:block;width:30px;height:30px"></fluent-button>');
  await page.locator('#target').evaluate(target=>{target.attachShadow({mode:'open'}).innerHTML='<span style="display:block;width:30px;height:30px">Owned</span>';});
  await expectTargetSizes(page);
  await page.setContent('<dialog style="position:fixed;left:40px;top:40px;width:200px;height:180px;margin:0;padding:0;border:0;overflow:auto"><div style="position:relative;height:400px"><button id="held" style="position:absolute;left:20px;top:10px;width:80px;height:30px">Keep focus</button><button id="scroll-target" style="position:absolute;left:20px;top:130px;width:30px;height:30px;padding:0">Go</button></div><div id="footer" style="position:sticky;bottom:0;height:60px;background:white">Owned footer</div></dialog>');
  await page.locator('dialog').evaluate(element=>(element as HTMLDialogElement).showModal());
  await page.locator('#held').focus();
  const before=await page.evaluate(()=>({dialog:document.querySelector('dialog')!.scrollTop,page:document.scrollingElement!.scrollTop,focus:document.activeElement?.id}));
  expect(await page.locator('#scroll-target').evaluate(element=>{const box=element.getBoundingClientRect();return document.elementFromPoint(box.left+box.width/2,box.top+box.height/2)?.id;})).toBe('footer');
  expect((await inventoryTargets(page)).inventory.find(target=>target.target==='#scroll-target')).toMatchObject({width:30,height:30,conforming:true,failure:null});
  expect(await page.evaluate(()=>({dialog:document.querySelector('dialog')!.scrollTop,page:document.scrollingElement!.scrollTop,focus:document.activeElement?.id}))).toEqual(before);
  await page.locator('dialog').evaluate(element=>{const cover=document.createElement('div');cover.style.cssText='position:fixed;inset:0;background:black;z-index:100';element.append(cover);});
  expect((await inventoryTargets(page)).inventory.find(target=>target.target==='#scroll-target')).toMatchObject({width:0,height:0,conforming:false,failure:expect.stringContaining('Clipping leaves the target without the required reachable area')});
  expect(await page.evaluate(()=>({dialog:document.querySelector('dialog')!.scrollTop,page:document.scrollingElement!.scrollTop,focus:document.activeElement?.id}))).toEqual(before);
});

test('C2 a Fluent-owned contained tab indicator is supported but outward or unrelated pseudo paint still requires proof', async ({ page }) => {
  await page.setContent('<style>fluent-tablist{position:fixed;left:40px;top:40px;width:100px;height:40px}fluent-tab{display:block;width:100px;height:40px;outline:none}fluent-tab:focus{outline:2px solid black}fluent-tablist::after{content:"";position:absolute;left:10px;right:10px;bottom:-.0004px;height:3px;background:blue}</style><fluent-tablist><fluent-tab aria-selected="true" tabindex="0">Activity</fluent-tab></fluent-tablist>');
  await page.locator('fluent-tab').focus();
  await expectSurfaceContrast(page.locator('fluent-tab'),'focus');
  await page.addStyleTag({content:'fluent-tablist::after{bottom:-1.7px}'});
  await expect(measureSurfaceContrast(page.locator('fluent-tab'),'focus')).rejects.toThrow(/Overlapping pseudo background needs pixel evidence/);
  await page.addStyleTag({content:'fluent-tablist::after{display:none}#other::after{content:"";position:fixed;left:38px;top:40px;width:2px;height:40px;background:black}'});
  await page.evaluate(()=>document.body.append(Object.assign(document.createElement('div'),{id:'other'})));
  await expect(measureSurfaceContrast(page.locator('fluent-tab'),'focus')).rejects.toThrow(/Overlapping pseudo background needs pixel evidence/);
});

test('C2 native popover elevation belongs only to its explicit anchor and never excuses opaque covers', async ({ page }) => {
  await page.setContent('<style>button{position:fixed;left:40px;top:40px;width:100px;height:40px;outline:none}button:focus{outline:2px solid black}</style><button id="anchor">Filter</button><fluent-popover-b anchor-id="anchor" opened="true" style="--shadow16:0 0 2px rgba(0,0,0,.12),0 8px 16px rgba(0,0,0,.14)"></fluent-popover-b>');
  await page.locator('fluent-popover-b').evaluate(host=>{
    host.attachShadow({mode:'open'}).innerHTML='<div fuib part="dialog" popover="manual" style="position:fixed;left:40px;top:90px;width:100px;height:30px;margin:0;padding:0;border:0"></div>';
    const sheet=new CSSStyleSheet();sheet.replaceSync(':host div[fuib][popover]{box-shadow:var(--shadow16)}');host.shadowRoot!.adoptedStyleSheets=[sheet];
    (host.shadowRoot!.firstElementChild as HTMLElement).showPopover();
  });
  await page.locator('button').focus();
  expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  await page.locator('fluent-popover-b').evaluate(host=>host.removeAttribute('anchor-id'));
  expect((await focusGeometry(page.locator('button'))).failures).toContain('competing pseudo/border/shadow paint needs pixel evidence');
  await page.locator('fluent-popover-b').evaluate(host=>{
    host.setAttribute('anchor-id','anchor');
    (host.shadowRoot!.firstElementChild as HTMLElement).style.boxShadow='0 0 300px 100px rgba(0,0,0,.99)';
  });
  expect((await focusGeometry(page.locator('button'))).failures).toContain('competing pseudo/border/shadow paint needs pixel evidence');
  await page.locator('fluent-popover-b').evaluate(host=>{
    const cover=host.shadowRoot!.firstElementChild as HTMLElement;
    cover.style.left='190px';cover.style.top='40px';cover.style.boxShadow='-150px 0 0 10px white';
  });
  expect((await focusGeometry(page.locator('button'))).failures).toContain('competing pseudo/border/shadow paint needs pixel evidence');
});

test('shadow-root scroll labels resolve within their own root', async ({ page }) => {
  await page.setContent('<div id="host"></div>');
  await page.locator('#host').evaluate(host => {
    host.attachShadow({ mode: 'open' }).innerHTML = '<span id="grid-label">Records</span><div role="region" aria-labelledby="grid-label" style="width:80px;overflow-x:auto"><div role="grid" style="width:160px;height:30px">Data</div></div>';
  });
  await expectNoPageOverflow(page);
});

test('offscreen controls must have a reachable viewport position', async ({ page }) => {
  await page.setContent('<style>html,body{height:100%;overflow:clip;margin:0}</style><button id="unreachable" style="position:absolute;top:2000px;left:20px;width:30px;height:30px">Save</button>');
  const target = (await inventoryTargets(page)).inventory.find(item => item.target === '#unreachable');
  expect(target?.conforming).toBe(false);
  // The clipping reason is added to, never substituted for, the missing-exception reason.
  expect(target?.failure).toContain('Undersized target lacks an evidenced exception');
  expect(target?.failure).toContain('Clipping leaves the target without the required reachable area');
});

test('clipped pointer covers use their painted edge for target size', async ({ page }) => {
  await page.setContent('<button id="target" style="position:absolute;left:40px;top:40px;width:30px;height:30px;padding:0">Save</button><div style="position:absolute;left:40px;top:40px;width:60px;height:30px;clip-path:inset(0 88.333333% 0 0);background:black"></div>');
  const target = (await inventoryTargets(page)).inventory.find(item => item.target === '#target');
  expect(target?.conforming).toBe(false);
  expect(target?.width).toBeGreaterThan(22.9);
  expect(target?.width).toBeLessThan(23.1);
});

test('fixed pseudo paint inside a transformed containing block needs pixel evidence', async ({ page }) => {
  await page.setContent('<style>#text{position:absolute;left:50px;top:50px;width:100px;height:30px;color:black;background:white;margin:0}#host{position:absolute;left:100px;top:50px;width:10px;height:10px;transform:translateX(0)}#host::after{content:"";position:fixed;left:-50px;top:0;width:100px;height:30px;background:black;z-index:10}</style><p id="text">Essential warning</p><div id="host"></div>');
  await expect(measureSurfaceContrast(page.locator('#text'))).rejects.toThrow(/Fixed pseudo transformed containing block needs pixel evidence/);
});

test('overflowing table text preserves only the native reachable pointer strip', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 900 });
  await page.setContent('<style>table{table-layout:fixed;width:248px;border-spacing:0;font:16px/44px monospace}td{padding:0;height:44px;white-space:nowrap;overflow:visible}#owner{position:fixed;left:8px;top:80px;width:100px;height:44px}#help{display:block;width:100px;height:44px}</style><table id="fixture"><tr><td>A</td><td>B</td><td id="date">01/15/2026</td><td id="metadata" tabindex="0" onclick="this.dataset.clicked = \'true\'">X</td></tr></table><div id="owner" tabindex="0"><a id="help" href="#docs">Help</a></div>');
  const cell = page.locator('#metadata');
  const sizeDateText = async (width: number) => page.locator('#date').evaluate((date, targetWidth) => {
    const range = document.createRange();
    range.selectNodeContents(date);
    // Calibrate the fixture to its installed font on each runner.
    const fontSize = parseFloat(getComputedStyle(date).fontSize);
    (date as HTMLElement).style.fontSize = `${fontSize * targetWidth / range.getBoundingClientRect().width}px`;
  }, width);
  for (const forcedColors of ['none', 'active'] as const) {
    await page.emulateMedia({ forcedColors });
    for (const offscreen of [false, true]) {
      await page.locator('#fixture').evaluate((table, belowViewport) => {
        (table as HTMLElement).style.marginTop = belowViewport ? '1200px' : '0';
        window.scrollTo({ top: 0, behavior: 'instant' });
      }, offscreen);
      await sizeDateText(96);
      const clearWidth = await cell.evaluate(element => {
        const range = document.createRange();
        range.selectNodeContents(document.querySelector('#date')!);
        return element.getBoundingClientRect().right - range.getBoundingClientRect().right;
      });
      expect(clearWidth).toBeGreaterThanOrEqual(24);
      expect(clearWidth).toBeLessThan(32);
      const inventory = await expectTargetSizes(page);
      const target = inventory.find(item => item.target === '#metadata');
      expect(target?.width).toBeCloseTo(clearWidth, 1);
      expect(target?.height).toBe(44);
      expect(inventory.find(item => item.target === '#owner')).toMatchObject({ width: 100, height: 44, conforming: true });
      expect(inventory.find(item => item.target === '#help')).toMatchObject({ width: 100, height: 44, conforming: true });
      expect(await page.evaluate(() => scrollY)).toBe(0);

      await sizeDateText(108);
      const narrowClearWidth = await cell.evaluate(element => {
        const range = document.createRange();
        range.selectNodeContents(document.querySelector('#date')!);
        return element.getBoundingClientRect().right - range.getBoundingClientRect().right;
      });
      expect(narrowClearWidth).toBeGreaterThan(0);
      expect(narrowClearWidth).toBeLessThan(24);
      const undersized = (await inventoryTargets(page)).inventory.find(item => item.target === '#metadata');
      expect(undersized?.width).toBeGreaterThan(0);
      expect(undersized?.height).toBeGreaterThan(0);
      expect(Math.min(undersized!.width, undersized!.height)).toBeLessThan(24);
      expect(undersized).toMatchObject({ conforming: false, failure: expect.stringContaining('Clipping leaves the target without the required reachable area') });
    }
  }
  await sizeDateText(96);
  await cell.click({ position: { x: 60, y: 22 } });
  await expect(cell).toHaveAttribute('data-clicked', 'true');
  await page.getByRole('link', { name: 'Help', exact: true }).click();
  await expect(page).toHaveURL(/#docs$/);
});

test('R4 painted descendants of a visibility:hidden ancestor stay in target and contrast evidence', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}</style><div style="visibility:hidden"><button id="painted" style="visibility:visible;position:absolute;left:20px;top:20px;width:10px;height:10px;padding:0;border:0">x</button><button id="gone" style="position:absolute;left:200px;top:20px;width:40px;height:40px">y</button></div><div id="root" style="padding-top:80px"><div style="visibility:hidden"><p style="visibility:visible;color:#bbb;background:white;margin:0 20px">Faint visible label</p></div></div>');
  const inventory = (await inventoryTargets(page)).inventory;
  expect(inventory.find(target => target.target === '#painted')?.failure).toContain('lacks an evidenced exception');
  expect(inventory.some(target => target.target === '#gone')).toBe(false);
  const samples = await measureSurfaceContrast(page.locator('#root'));
  expect(samples.some(sample => sample.paint === 'Faint visible label' && sample.ratio < 4.5)).toBe(true);
});

test('R4 out-of-flow text beyond an intermediate overflow clip keeps contrast evidence', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}span{color:#777;background:#888}</style><div id="root" style="position:relative;width:400px;height:200px">'
    + '<div style="overflow:hidden;width:50px;height:20px"><span style="position:absolute;left:100px;top:0">Escaped absolute</span></div>'
    + '<div style="overflow:hidden;width:50px;height:20px"><span style="position:fixed;left:100px;top:300px">Escaped fixed</span></div>'
    + '<div style="position:relative;overflow:hidden;width:50px;height:20px;margin-top:60px"><span style="position:absolute;left:100px;top:0">Clipped by its block</span></div></div>');
  const samples = await measureSurfaceContrast(page.locator('#root'));
  expect(samples.filter(sample => ['Escaped absolute', 'Escaped fixed'].includes(sample.paint) && sample.ratio < 4.5)).toHaveLength(2);
  // Overflow on the positioned containing block still clips its absolute content.
  expect(samples.some(sample => sample.paint === 'Clipped by its block')).toBe(false);
});

test('R4 unrelated shadowed text cannot block a distant contrast measurement', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}p{margin:0;font:16px sans-serif}</style><div id="root" style="padding:20px"><p style="color:black">Measured label</p></div><p id="shadowed" style="position:absolute;left:20px;top:400px;color:black;text-shadow:1px 1px red">Unrelated shadow</p>');
  await expectSurfaceContrast(page.locator('#root'));
  await page.locator('#shadowed').evaluate(element => { element.style.top = '20px'; });
  await expect(measureSurfaceContrast(page.locator('#root'))).rejects.toThrow(/needs pixel evidence/);
});

test('R5 hidden ancestors contribute no backdrop and no sample to contrast evidence', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}p{margin:0;padding:4px 20px;font:16px sans-serif}</style><div id="root">'
    + '<div style="visibility:hidden;background:black"><p style="visibility:visible;color:#ccc">Ghost label</p></div>'
    + '<div style="visibility:hidden"><p style="color:#ccc">Inherited hidden label</p></div></div>');
  const samples = await measureSurfaceContrast(page.locator('#root'));
  // The black fill never paints: the light label is measured against the white canvas and fails.
  expect(samples.find(sample => sample.paint === 'Ghost label')?.ratio).toBeLessThan(4.5);
  expect(samples.some(sample => sample.paint === 'Inherited hidden label')).toBe(false);
});

test('R5 escaped out-of-flow text keeps its real containing block and backdrop', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}span{font:16px sans-serif}</style><div id="root" style="position:relative;width:500px;height:240px">'
    + '<div style="position:relative;overflow:hidden;width:50px;height:20px"><div style="position:absolute"><span style="position:fixed;left:100px;top:300px;color:#777;background:#888">Nested fixed</span></div></div>'
    + '<div style="overflow:hidden;width:50px;height:20px;background:black;margin-top:40px"><span style="position:absolute;left:150px;top:60px;color:#ddd">Escaped over white</span></div></div>');
  const samples = await measureSurfaceContrast(page.locator('#root'));
  expect(samples.find(sample => sample.paint === 'Nested fixed')?.ratio).toBeLessThan(4.5);
  // The black overflow box is not behind the escaped label, so it cannot certify its contrast.
  expect(samples.find(sample => sample.paint === 'Escaped over white')?.ratio).toBeLessThan(4.5);
  await page.locator('span').last().evaluate(element => { element.style.left = '30px'; });
  await expect(measureSurfaceContrast(page.locator('#root'))).rejects.toThrow(/partial ancestor background needs pixel evidence/);
});

test('R5 visible descendants of hidden ancestors stay in clipping, focus and competing-paint evidence', async ({ page }) => {
  await page.setContent('<style>body{margin:0}</style><div style="visibility:hidden"><div style="visibility:visible;width:40px;overflow:hidden;white-space:nowrap">Clipped visible label text</div></div>');
  await expect(expectNoTextClipping(page)).rejects.toThrow(/clipped text ranges/);
  await page.setContent('<style>body{margin:0}</style><div style="visibility:hidden"><div style="width:40px;overflow:hidden;white-space:nowrap">Clipped hidden label text</div></div>');
  await expectNoTextClipping(page);
  await page.setContent('<style>body{margin:0;background:white}button{position:absolute;left:100px;top:100px;width:80px;height:32px;outline:2px solid black;outline-offset:2px}</style><button>Save</button>'
    + '<div style="visibility:hidden"><div id="cover" style="visibility:visible;position:absolute;left:94px;top:94px;width:92px;height:44px;border:3px solid red;box-sizing:border-box;pointer-events:none"></div></div>');
  await page.locator('button').focus();
  expect((await fixtureFocusGeometry(page.locator('button'))).failures).toContain('competing pseudo/border/shadow paint needs pixel evidence');
  await page.locator('#cover').evaluate(cover => { cover.style.visibility = ''; });
  expect((await fixtureFocusGeometry(page.locator('button'))).failures).toEqual([]);
  await page.setContent('<style>body{margin:0;background:white}p{position:absolute;left:20px;top:20px;margin:0;font:16px sans-serif;color:black}</style><p id="text">Covered label</p>'
    + '<div style="visibility:hidden"><div id="cover" style="visibility:visible;position:absolute;left:10px;top:15px;width:200px;height:30px;background:black;pointer-events:none"></div></div>');
  await expect(measureSurfaceContrast(page.locator('#text'))).rejects.toThrow(/needs pixel evidence/);
  await page.locator('#cover').evaluate(cover => { cover.style.visibility = ''; });
  await expectSurfaceContrast(page.locator('#text'));
});

test('R5 unrelated text whose shadow reaches the sample still needs pixel evidence', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}p{margin:0;font:16px sans-serif;color:black}</style><div id="root" style="padding:20px"><p>Measured label</p></div><p id="shadowed" style="position:absolute;left:20px;top:-12px;text-shadow:0 30px 0 red">Distant letters</p>');
  await expect(measureSurfaceContrast(page.locator('#root'))).rejects.toThrow(/needs pixel evidence/);
  await page.locator('#shadowed').evaluate(element => { element.style.textShadow = 'none'; });
  await expectSurfaceContrast(page.locator('#root'));
});


for (const property of [
  'will-change:translate', 'will-change:scale', 'will-change:rotate', 'will-change:contain',
  'will-change:backdrop-filter', 'will-change:transform-style', 'will-change:offset-path', 'will-change:-webkit-backdrop-filter',
  'content-visibility:auto', 'content-visibility:hidden', 'transform-style:preserve-3d',
]) test(`R6 containing block ${property} retains its overflow clip`, async ({ page }) => {
  await page.setContent(`<style>body{margin:0;background:white}span{color:#bbb;font:16px sans-serif}</style><div id="root"><p>Visible control</p><div id="block" style="${property};overflow:hidden;width:50px;height:30px"><span style="position:fixed;left:120px;top:100px">Clipped fixed label</span></div></div>`);
  // Layout is the oracle: a contained label moves with the block. Only WebKit honors the prefixed hint.
  const contained = await page.locator('span').evaluate(element => Math.abs(element.getBoundingClientRect().top - 100) > .5);
  if (!property.includes('-webkit-')) expect(contained, `${property} creates a containing block`).toBe(true);
  const samples = await measureSurfaceContrast(page.locator('#root'));
  expect(samples.some(sample => sample.paint === 'Visible control')).toBe(true);
  expect(samples.some(sample => sample.paint === 'Clipped fixed label')).toBe(!contained);
  await page.locator('#block').evaluate(element => { element.style.cssText = 'overflow:hidden;width:50px;height:30px'; });
  const escaped = await measureSurfaceContrast(page.locator('#root'));
  expect(escaped.find(sample => sample.paint === 'Clipped fixed label')?.ratio).toBeLessThan(4.5);
});

// A backdrop-filter block contains the label, and its backdrop paint fails closed in every engine.
for (const property of ['backdrop-filter:blur(1px)', '-webkit-backdrop-filter:blur(1px)']) test(`R7 containing block ${property} needs pixel evidence`, async ({ page }) => {
  await page.setContent(`<style>body{margin:0;background:white}span{color:#bbb;font:16px sans-serif}</style><div id="root"><p>Visible control</p><div style="${property};overflow:hidden;width:50px;height:30px"><span style="position:fixed;left:120px;top:100px">Clipped fixed label</span></div></div>`);
  const contained = await page.locator('span').evaluate(element => Math.abs(element.getBoundingClientRect().top - 100) > .5);
  if (!property.startsWith('-webkit-')) expect(contained, `${property} creates a containing block`).toBe(true);
  if (contained) await expect(measureSurfaceContrast(page.locator('#root'))).rejects.toThrow(/Filtered or blended paint needs pixel evidence/);
  else expect((await measureSurfaceContrast(page.locator('#root'))).find(sample => sample.paint === 'Clipped fixed label')?.ratio).toBeLessThan(4.5);
});

test('R6 display contents and contain-intrinsic-size do not create containing blocks', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}</style><div id="root" style="position:relative;width:400px;height:200px"><div style="overflow:hidden;width:50px;height:30px"><div style="position:relative;display:contents"><span style="position:absolute;left:120px;top:100px;color:#bbb">Contents escape</span></div></div>'
    + '<div style="will-change:contain-intrinsic-size;overflow:hidden;width:50px;height:30px"><span style="position:fixed;left:120px;top:300px;color:#bbb">Unrelated will-change</span></div></div>');
  const samples = await measureSurfaceContrast(page.locator('#root'));
  for (const label of ['Contents escape', 'Unrelated will-change']) expect(samples.find(sample => sample.paint === label)?.ratio).toBeLessThan(4.5);
});

test('R6 escaped backgrounds distinguish contained, disjoint, partial and empty fills', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}span{position:fixed;left:40px;top:40px;color:#ddd;font:16px sans-serif}</style><div id="root"><div id="fill" style="width:300px;height:200px;background:black"><span>Escaped label</span></div></div>');
  expect((await measureSurfaceContrast(page.locator('#root'))).find(sample => sample.paint === 'Escaped label')?.background).toEqual([0, 0, 0, 1]);
  await page.locator('#fill').evaluate(element => { element.style.width = '20px'; });
  expect((await measureSurfaceContrast(page.locator('#root'))).find(sample => sample.paint === 'Escaped label')?.ratio).toBeLessThan(4.5);
  await page.locator('#fill').evaluate(element => { element.style.width = '70px'; });
  await expect(measureSurfaceContrast(page.locator('#root'))).rejects.toThrow(/partial ancestor background needs pixel evidence/);
  // A zero-width line through the glyphs has no fill area and must not be called a partial backdrop.
  await page.locator('#fill').evaluate(element => { element.style.cssText = 'position:absolute;left:70px;top:0;width:0;height:200px;background:black'; });
  expect((await measureSurfaceContrast(page.locator('#root'))).find(sample => sample.paint === 'Escaped label')?.ratio).toBeLessThan(4.5);
});

test('R6 a nested absolute ancestor cannot replace a fixed labels backdrop escape', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}</style><div id="root"><div style="position:relative;overflow:hidden;width:50px;height:30px;background:black"><div style="position:absolute"><span style="position:fixed;left:120px;top:100px;color:#ddd">Nested fixed backdrop</span></div></div></div>');
  const sample = (await measureSurfaceContrast(page.locator('#root'))).find(sample => sample.paint === 'Nested fixed backdrop');
  expect(sample?.ratio).toBeLessThan(4.5);
  expect(sample?.background).toEqual([255,255,255,1]);
});

for (const position of ['absolute', 'fixed']) for (const kind of ['focus', 'border'] as const)
  test(`R6 exterior ${kind} retains its ${position} targets containing block`, async ({ page }) => {
    await page.setContent(`<style>body{margin:0;background:white}button{position:${position};left:120px;top:100px;width:100px;height:40px;background:white;color:black;border:2px solid #ccc;outline:none}button:focus{outline:2px solid #ccc;outline-offset:2px}</style><div id="root" style="position:relative;width:400px;height:250px"><div style="width:30px;height:30px;background:black"><button>Save</button></div></div>`);
    const target = page.getByRole('button');
    if (kind === 'focus') await target.focus();
    const samples = await measureSurfaceContrast(target, kind);
    expect(samples.length).toBeGreaterThan(0);
    expect(samples.every(sample => sample.ratio < 3 && sample.background.slice(0,3).every(channel => channel === 255))).toBe(true);
  });

test('R6 hidden ancestors cannot shield a native plane from competing page paint', async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}#under{position:fixed;inset:0;background:black}#plane{position:fixed;left:40px;top:40px;width:300px;height:150px;margin:0;padding:0;border:0;background:transparent}#backdrop{visibility:hidden;background:white;width:300px;height:150px}p{visibility:visible;padding:20px;margin:0;color:black}</style><div id="under"></div><div id="plane" popover="manual"><div id="backdrop"><p>Unshielded label</p></div></div>');
  await page.locator('#plane').evaluate(element => { (element as HTMLElement).showPopover(); });
  await expect(measureSurfaceContrast(page.locator('#plane'))).rejects.toThrow(/Overlapping sibling background needs pixel evidence/);
  await page.locator('#backdrop').evaluate(element => { element.style.visibility = 'visible'; });
  await expectSurfaceContrast(page.locator('#plane'));
});

test('R6 simple contrast ignores hidden ancestor fills', async ({ page }) => {
  await page.setContent('<style>body{background:white}</style><div style="visibility:hidden;background:black"><p style="visibility:visible;color:#ccc">Ghost simple label</p></div>');
  await expect(expectTextContrast(page.locator('p'))).rejects.toThrow(/toBeGreaterThanOrEqual/);
  await page.locator('p').evaluate(element => { element.style.color = 'black'; });
  expect((await expectTextContrast(page.locator('p'))).ratio).toBe(21);
});

for (const paint of ['blur', 'stroke']) test(`R6 competing glyph ${paint} reach requires pixel evidence only at the sample`, async ({ page }) => {
  await page.setContent('<style>body{margin:0;background:white}p{position:absolute;left:100px;margin:0;font:16px sans-serif;color:black}#label{top:100px}#cover{top:135px}</style><p id="label">Measured label</p><p id="cover">Distant letters</p>');
  const target = page.locator('#label');
  await expectSurfaceContrast(target);
  await page.locator('#cover').evaluate((element, paint) => {
    if (paint === 'blur') element.style.textShadow = '0 0 20px red';
    else element.style.setProperty('-webkit-text-stroke', '30px red');
  }, paint);
  await expect(measureSurfaceContrast(target)).rejects.toThrow(/Text (shadow|stroke) composition needs pixel evidence/);
  await page.locator('#cover').evaluate((element, paint) => {
    if (paint === 'blur') element.style.textShadow = '0 0 2px red';
    else element.style.setProperty('-webkit-text-stroke', '2px red');
  }, paint);
  await expectSurfaceContrast(target);
});
