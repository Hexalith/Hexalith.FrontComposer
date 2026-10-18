# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: responsive-accessibility.spec.ts >> owned overlay return waits until a native dialog has dismissed
- Location: specs/responsive-accessibility.spec.ts:244:1

# Error details

```
Error: expect(locator).toBeFocused() failed

Locator:  getByTestId('fc-palette-trigger')
Expected: focused
Received: inactive
Timeout:  10000ms

Call log:
  - Expect "toBeFocused" getByTestId('fc-palette-trigger') with timeout 10000ms
  - waiting for getByTestId('fc-palette-trigger')
    24 × locator resolved to <fluent-button icon-only="" tabindex="0" role="button" appearance="subtle" data-testid="fc-palette-trigger" aria-label="Open command palette" _bl_a442ff45-28dc-4a4e-9062-e079165c910b="">…</fluent-button>
       - unexpected value "inactive"

```

```yaml
- button "Open command palette"
```

# Test source

```ts
  170 |   await page.locator('#a').focus();
  171 |   await expect(verifyKeyboardCycle(page)).rejects.toThrow(/Application trapped Tab/);
  172 | });
  173 | 
  174 | test('shell focus scrolling protects the ring and cancels stale or disposed work', async ({ page }) => {
  175 |   await page.goto('/home');
  176 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  177 |   await page.evaluate(async () => {
  178 |     const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
  179 |     const keyboard = await import(modulePath);
  180 |     const root = document.createElement('div');
  181 |     root.id = 'ae-scroll-shell';
  182 |     root.style.cssText = 'position:fixed;inset:220px 20px auto;height:100px;overflow:auto;z-index:2000;background:Canvas;scroll-padding:8px;padding:8px;box-sizing:border-box';
  183 |     root.innerHTML = '<style>#ae-scroll-shell button{outline:none;scroll-margin:8px}#ae-scroll-shell button:focus{outline:4px solid;outline-offset:3px}</style><div style="height:180px"></div><button>Last action</button><div style="height:30px"></div>';
  184 |     document.body.append(root);
  185 |     keyboard.registerShellKeyFilter(root);
  186 |     root.querySelector('button')!.focus({ preventScroll: true });
  187 |   });
  188 |   const action = page.locator('#ae-scroll-shell button');
  189 |   await expectFocusVisible(action);
  190 |   const cancelled = await page.evaluate(async () => {
  191 |     const modulePath: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
  192 |     const keyboard = await import(modulePath);
  193 |     const root = document.getElementById('ae-scroll-shell')!;
  194 |     const action = root.querySelector('button')!;
  195 |     action.blur(); root.scrollTop = 0;
  196 |     action.focus({ preventScroll: true });
  197 |     document.querySelector<HTMLElement>('[data-testid="fc-palette-trigger"]')!.focus({ preventScroll: true });
  198 |     await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
  199 |     const moved = root.scrollTop === 0;
  200 |     action.focus({ preventScroll: true });
  201 |     keyboard.unregisterShellKeyFilter(root);
  202 |     await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
  203 |     const disposed = root.scrollTop === 0;
  204 |     root.remove();
  205 |     return { moved, disposed };
  206 |   });
  207 |   expect(cancelled).toEqual({ moved: true, disposed: true });
  208 | });
  209 | 
  210 | test('delayed overlay disposal preserves an operator Tab after native origin return', async ({ page }) => {
  211 |   await page.goto('/counter');
  212 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  213 |   await page.getByTestId('fc-palette-trigger').focus();
  214 |   await page.evaluate(async () => {
  215 |     const focusPath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
  216 |     const keyboardPath = '/_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js';
  217 |     const focus = await import(focusPath) as { captureOverlayOrigin: (testId: string, preserveExisting: boolean) => boolean };
  218 |     const keyboard = await import(keyboardPath) as { focusElement: (element: Element) => void };
  219 |     if (!focus.captureOverlayOrigin('fc-palette-trigger', true)) throw new Error('fixture could not reserve the modal');
  220 |     const dialog = document.createElement('dialog');
  221 |     dialog.innerHTML = '<input data-testid="fc-palette-search">';
  222 |     document.body.append(dialog); dialog.showModal();
  223 |     keyboard.focusElement(dialog.querySelector('input')!);
  224 |     await new Promise(resolve => setTimeout(resolve, 200));
  225 |     if ((window as typeof window & { __fcOverlayOpenIntent?: unknown }).__fcOverlayOpenIntent !== null) throw new Error('real palette entry did not consume its intent');
  226 |     dialog.close(); dialog.remove();
  227 |   });
  228 |   await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  229 |   await page.keyboard.press('Tab');
  230 |   const settings = page.getByTestId('fc-settings-button');
  231 |   await expect(settings).toBeFocused();
  232 |   await page.evaluate(async () => {
  233 |     const modulePath = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
  234 |     const focus = await import(modulePath) as { restoreOverlayOrigin: () => void };
  235 |     focus.restoreOverlayOrigin();
  236 |     await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
  237 |     if ((window as typeof window & { __fcModalReservation?: unknown }).__fcModalReservation) throw new Error('dismissal retained its modal slot');
  238 |   });
  239 |   await expect(settings).toBeFocused();
  240 |   await page.keyboard.press('Enter');
  241 |   await expect(page.locator('#fc-settings-heading')).toBeFocused();
  242 | });
  243 | 
  244 | test('owned overlay return waits until a native dialog has dismissed', async ({ page }) => {
  245 |   await page.goto('/counter');
  246 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  247 |   const trigger = page.getByTestId('fc-palette-trigger');
  248 |   await trigger.focus();
  249 |   await page.evaluate(async () => {
  250 |     const focus = await import('/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js') as {
  251 |       captureOverlayOrigin: (testId: string, preserveExisting: boolean) => boolean;
  252 |       restoreOverlayOrigin: () => void;
  253 |     };
  254 |     if (!focus.captureOverlayOrigin('fc-palette-trigger', true)) throw new Error('could not capture overlay origin');
  255 |     document.body.tabIndex = -1;
  256 |     document.body.focus();
  257 |     const dialog = document.createElement('dialog');
  258 |     dialog.innerHTML = '<button>Action</button>';
  259 |     document.body.append(dialog);
  260 |     dialog.showModal();
  261 |     dialog.querySelector('button')!.focus();
  262 |     focus.restoreOverlayOrigin();
  263 |     await new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));
  264 |     if (document.activeElement === document.querySelector('[data-testid="fc-palette-trigger"]')) {
  265 |       throw new Error('origin was focused while the dialog was still modal');
  266 |     }
  267 |     dialog.close();
  268 |     dialog.remove();
  269 |   });
> 270 |   await expect(trigger).toBeFocused();
      |                         ^ Error: expect(locator).toBeFocused() failed
  271 | });
  272 | 
  273 | test('owned palette return cancels the actual Fluent delayed return after an operator Tab', async ({ page }) => {
  274 |   await page.goto('/counter');
  275 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  276 |   await page.getByTestId('fc-palette-trigger').click();
  277 |   await expect(page.getByTestId('fc-palette-search')).toBeFocused();
  278 |   await page.evaluate(async () => {
  279 |     const path = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
  280 |     const focus = await import(path) as { containDialogFocus: (testId: string) => boolean };
  281 |     if (!focus.containDialogFocus('fc-palette-root')) throw new Error('palette containment did not attach');
  282 |     const host = document.querySelector<HTMLElement>('[data-testid="fc-palette-root"]')!.closest('fluent-dialog')!;
  283 |     const w = window as typeof window & { __aeVendorReturn?: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } };
  284 |     const callbacks: (() => void)[] = [];
  285 |     const original = window.setTimeout;
  286 |     window.setTimeout = ((handler: TimerHandler, delay?: number, ...args: unknown[]) => {
  287 |       // Hold the pinned Fluent callback itself, rather than substitute a synthetic return.
  288 |       if (delay === 25 && typeof handler === 'function' && new Error().stack?.includes('Microsoft.FluentUI.AspNetCore.Components.lib.module.js')) {
  289 |         callbacks.push(() => handler(...args));
  290 |         return 0;
  291 |       }
  292 |       return original(handler, delay, ...args);
  293 |     }) as typeof window.setTimeout;
  294 |     w.__aeVendorReturn = { host, callbacks, restore: () => { window.setTimeout = original; } };
  295 |   });
  296 |   await page.keyboard.press('Escape');
  297 |   await expect(page.getByTestId('fc-palette-root')).toHaveCount(0);
  298 |   await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  299 |   await page.keyboard.press('Tab');
  300 |   const settings = page.getByTestId('fc-settings-button');
  301 |   await expect(settings).toBeFocused();
  302 |   const proof = await page.evaluate(() => {
  303 |     const state = (window as typeof window & { __aeVendorReturn: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } }).__aeVendorReturn;
  304 |     state.restore();
  305 |     const proof = { queued: state.callbacks.length, cleared: state.host.previousActiveElement === null };
  306 |     for (const callback of state.callbacks) callback();
  307 |     return proof;
  308 |   });
  309 |   expect(proof.queued).toBeGreaterThan(0);
  310 |   expect(proof.cleared).toBe(true);
  311 |   await expect(settings).toBeFocused();
  312 |   await page.keyboard.press('Enter');
  313 |   await expect(page.locator('#fc-settings-heading')).toBeFocused();
  314 | });
  315 | 
  316 | for (const theme of ['light', 'dark']) {
  317 |   test(`narrow specimen navigation and grid retain unobscured focus in ${theme}`, async ({ page }, info) => {
  318 |     await page.setViewportSize({ width: 1280, height: 900 });
  319 |     await page.goto(`/__frontcomposer/specimens/type?theme=${theme}`);
  320 |     await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  321 |     const nextItem = page.getByTestId('fc-nav-group-generated').locator('..');
  322 |     expect(await nextItem.evaluate(element => getComputedStyle(element).marginBlockStart)).toBe('0px');
  323 |     await page.setViewportSize({ width: 320, height: 900 });
  324 |     expect(await nextItem.evaluate(element => getComputedStyle(element).marginBlockStart)).toBe('8px');
  325 |     const navigation = page.getByTestId('fc-nav-foundation');
  326 |     await navigation.focus();
  327 |     await expectFocusVisible(navigation);
  328 |     const grid = page.getByRole('region', { name: 'specimen status', exact: true });
  329 |     await grid.focus();
  330 |     await expectFocusVisible(grid);
  331 |     const normal = await expectSurfaceContrast(grid, 'focus');
  332 |     await page.emulateMedia({ forcedColors: 'active' });
  333 |     await expectFocusVisible(grid);
  334 |     const system = await expectSurfaceContrast(grid, 'focus');
  335 |     const paint = await grid.evaluate(element => {
  336 |       const css = getComputedStyle(element);
  337 |       return { width: css.outlineWidth, offset: css.outlineOffset, systemPaletteSupported: CSS.supports('forced-color-adjust', 'auto') };
  338 |     });
  339 |     expect(paint.width).toBe('2px');
  340 |     expect(paint.offset).toBe('-2px');
  341 |     if (!paint.systemPaletteSupported) expect(info.project.name).toBe('webkit');
  342 |     await info.attach(`narrow-focus-${theme}`, { body: JSON.stringify({ normal, system, paint }), contentType: 'application/json' });
  343 | 
  344 |     // Exercise the shared grid asset outside the type specimen, with the actual Fluent theme.
  345 |     await page.emulateMedia({ forcedColors: 'none' });
  346 |     await page.goto('/counter/counter-projection');
  347 |     await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  348 |     await page.getByTitle('Change theme').first().click();
  349 |     await page.getByRole('menuitem', { name: theme === 'dark' ? 'Dark' : 'Light', exact: true }).click();
  350 |     await expect(page.getByTitle('Change theme').first().locator('span[aria-hidden="true"]')).toHaveText(theme === 'dark' ? 'Dark' : 'Light');
  351 |     await expect.poll(async () => page.locator('.fc-shell-root').evaluate(element => {
  352 |       const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
  353 |       const context = canvas.getContext('2d')!;
  354 |       context.fillStyle = getComputedStyle(element).getPropertyValue('--colorNeutralBackground1');
  355 |       context.fillRect(0, 0, 1, 1);
  356 |       return [...context.getImageData(0, 0, 1, 1).data].slice(0, 3).reduce((sum, channel) => sum + channel, 0) < 384;
  357 |     })).toBe(theme === 'dark');
  358 |     await settlePaint(page);
  359 |     const shared = page.locator('.fc-datagrid-host[role="region"]').first();
  360 |     await shared.focus();
  361 |     await page.keyboard.press('Shift+Tab');
  362 |     await page.keyboard.press('Tab');
  363 |     await expectFocusVisible(shared);
  364 |     const sharedNormal = await expectSurfaceContrast(shared, 'focus');
  365 |     await page.emulateMedia({ forcedColors: 'active' });
  366 |     await expectFocusVisible(shared);
  367 |     const sharedSystem = await expectSurfaceContrast(shared, 'focus');
  368 |     const sharedPaint = await shared.evaluate(element => ({ width: getComputedStyle(element).outlineWidth, offset: getComputedStyle(element).outlineOffset }));
  369 |     expect(sharedPaint).toEqual({ width: '2px', offset: '-2px' });
  370 |     await info.attach(`shared-grid-focus-${theme}`, { body: JSON.stringify({ normal: sharedNormal, system: sharedSystem, paint: sharedPaint }), contentType: 'application/json' });
```