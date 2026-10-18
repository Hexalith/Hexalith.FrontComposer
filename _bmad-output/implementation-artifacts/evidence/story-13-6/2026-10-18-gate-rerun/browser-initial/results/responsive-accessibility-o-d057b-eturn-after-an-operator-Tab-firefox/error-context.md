# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: responsive-accessibility.spec.ts >> owned palette return cancels the actual Fluent delayed return after an operator Tab
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
    24 × locator resolved to <fluent-button icon-only="" tabindex="0" role="button" appearance="subtle" data-testid="fc-palette-trigger" aria-label="Open command palette" _bl_71eedfe4-4419-4363-bfd2-40d4122bf558="">…</fluent-button>
       - unexpected value "inactive"

```

```yaml
- button "Open command palette"
```

# Test source

```ts
  169 |   await page.locator('#b').evaluate(element => { element.addEventListener('keydown', event => { if ((event as KeyboardEvent).key === 'Tab') event.preventDefault(); }); });
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
  244 | test('owned palette return cancels the actual Fluent delayed return after an operator Tab', async ({ page }) => {
  245 |   await page.goto('/counter');
  246 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  247 |   await page.getByTestId('fc-palette-trigger').click();
  248 |   await expect(page.getByTestId('fc-palette-search')).toBeFocused();
  249 |   await page.evaluate(async () => {
  250 |     const path = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
  251 |     const focus = await import(path) as { containDialogFocus: (testId: string) => boolean };
  252 |     if (!focus.containDialogFocus('fc-palette-root')) throw new Error('palette containment did not attach');
  253 |     const host = document.querySelector<HTMLElement>('[data-testid="fc-palette-root"]')!.closest('fluent-dialog')!;
  254 |     const w = window as typeof window & { __aeVendorReturn?: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } };
  255 |     const callbacks: (() => void)[] = [];
  256 |     const original = window.setTimeout;
  257 |     window.setTimeout = ((handler: TimerHandler, delay?: number, ...args: unknown[]) => {
  258 |       // Hold the pinned Fluent callback itself, rather than substitute a synthetic return.
  259 |       if (delay === 25 && typeof handler === 'function' && new Error().stack?.includes('Microsoft.FluentUI.AspNetCore.Components.lib.module.js')) {
  260 |         callbacks.push(() => handler(...args));
  261 |         return 0;
  262 |       }
  263 |       return original(handler, delay, ...args);
  264 |     }) as typeof window.setTimeout;
  265 |     w.__aeVendorReturn = { host, callbacks, restore: () => { window.setTimeout = original; } };
  266 |   });
  267 |   await page.keyboard.press('Escape');
  268 |   await expect(page.getByTestId('fc-palette-root')).toHaveCount(0);
> 269 |   await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
      |                                                        ^ Error: expect(locator).toBeFocused() failed
  270 |   await page.keyboard.press('Tab');
  271 |   const settings = page.getByTestId('fc-settings-button');
  272 |   await expect(settings).toBeFocused();
  273 |   const proof = await page.evaluate(() => {
  274 |     const state = (window as typeof window & { __aeVendorReturn: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } }).__aeVendorReturn;
  275 |     state.restore();
  276 |     const proof = { queued: state.callbacks.length, cleared: state.host.previousActiveElement === null };
  277 |     for (const callback of state.callbacks) callback();
  278 |     return proof;
  279 |   });
  280 |   expect(proof.queued).toBeGreaterThan(0);
  281 |   expect(proof.cleared).toBe(true);
  282 |   await expect(settings).toBeFocused();
  283 |   await page.keyboard.press('Enter');
  284 |   await expect(page.locator('#fc-settings-heading')).toBeFocused();
  285 | });
  286 | 
  287 | for (const theme of ['light', 'dark']) {
  288 |   test(`narrow specimen navigation and grid retain unobscured focus in ${theme}`, async ({ page }, info) => {
  289 |     await page.setViewportSize({ width: 1280, height: 900 });
  290 |     await page.goto(`/__frontcomposer/specimens/type?theme=${theme}`);
  291 |     await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  292 |     const nextItem = page.getByTestId('fc-nav-group-generated').locator('..');
  293 |     expect(await nextItem.evaluate(element => getComputedStyle(element).marginBlockStart)).toBe('0px');
  294 |     await page.setViewportSize({ width: 320, height: 900 });
  295 |     expect(await nextItem.evaluate(element => getComputedStyle(element).marginBlockStart)).toBe('8px');
  296 |     const navigation = page.getByTestId('fc-nav-foundation');
  297 |     await navigation.focus();
  298 |     await expectFocusVisible(navigation);
  299 |     const grid = page.getByRole('region', { name: 'specimen status', exact: true });
  300 |     await grid.focus();
  301 |     await expectFocusVisible(grid);
  302 |     const normal = await expectSurfaceContrast(grid, 'focus');
  303 |     await page.emulateMedia({ forcedColors: 'active' });
  304 |     await expectFocusVisible(grid);
  305 |     const system = await expectSurfaceContrast(grid, 'focus');
  306 |     const paint = await grid.evaluate(element => {
  307 |       const css = getComputedStyle(element);
  308 |       return { width: css.outlineWidth, offset: css.outlineOffset, systemPaletteSupported: CSS.supports('forced-color-adjust', 'auto') };
  309 |     });
  310 |     expect(paint.width).toBe('2px');
  311 |     expect(paint.offset).toBe('-2px');
  312 |     if (!paint.systemPaletteSupported) expect(info.project.name).toBe('webkit');
  313 |     await info.attach(`narrow-focus-${theme}`, { body: JSON.stringify({ normal, system, paint }), contentType: 'application/json' });
  314 | 
  315 |     // Exercise the shared grid asset outside the type specimen, with the actual Fluent theme.
  316 |     await page.emulateMedia({ forcedColors: 'none' });
  317 |     await page.goto('/counter/counter-projection');
  318 |     await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  319 |     await page.getByTitle('Change theme').first().click();
  320 |     await page.getByRole('menuitem', { name: theme === 'dark' ? 'Dark' : 'Light', exact: true }).click();
  321 |     await expect(page.getByTitle('Change theme').first().locator('span[aria-hidden="true"]')).toHaveText(theme === 'dark' ? 'Dark' : 'Light');
  322 |     await expect.poll(async () => page.locator('.fc-shell-root').evaluate(element => {
  323 |       const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
  324 |       const context = canvas.getContext('2d')!;
  325 |       context.fillStyle = getComputedStyle(element).getPropertyValue('--colorNeutralBackground1');
  326 |       context.fillRect(0, 0, 1, 1);
  327 |       return [...context.getImageData(0, 0, 1, 1).data].slice(0, 3).reduce((sum, channel) => sum + channel, 0) < 384;
  328 |     })).toBe(theme === 'dark');
  329 |     await settlePaint(page);
  330 |     const shared = page.locator('.fc-datagrid-host[role="region"]').first();
  331 |     await shared.focus();
  332 |     await page.keyboard.press('Shift+Tab');
  333 |     await page.keyboard.press('Tab');
  334 |     await expectFocusVisible(shared);
  335 |     const sharedNormal = await expectSurfaceContrast(shared, 'focus');
  336 |     await page.emulateMedia({ forcedColors: 'active' });
  337 |     await expectFocusVisible(shared);
  338 |     const sharedSystem = await expectSurfaceContrast(shared, 'focus');
  339 |     const sharedPaint = await shared.evaluate(element => ({ width: getComputedStyle(element).outlineWidth, offset: getComputedStyle(element).outlineOffset }));
  340 |     expect(sharedPaint).toEqual({ width: '2px', offset: '-2px' });
  341 |     await info.attach(`shared-grid-focus-${theme}`, { body: JSON.stringify({ normal: sharedNormal, system: sharedSystem, paint: sharedPaint }), contentType: 'application/json' });
  342 |   });
  343 | }
  344 | 
  345 | test('the real missing-detail component registers recovery before a Fluent shadow action removes detail', async ({ page }) => {
  346 |   await page.goto('/__frontcomposer/specimens/type?announcementFocus=true');
  347 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  348 |   const fixture = page.getByTestId('fc-missing-detail-fixture');
  349 |   const action = fixture.getByTestId('fc-remove-focused-detail');
  350 |   await action.focus();
  351 |   await page.keyboard.press('Enter');
  352 |   const explanation = fixture.locator('.fc-expanded-row-hidden-banner-text');
  353 |   await expect(explanation).toBeVisible();
  354 |   await expect(explanation).toBeFocused();
  355 |   await expect(explanation).toContainText('not in the current results');
  356 |   await expect(fixture.getByTestId('fc-expanded-row-hidden-banner-clear')).toHaveCount(0);
  357 | });
  358 | 
  359 | const surfaces = [
  360 |   { name: 'Home directory', path: '/home', ready: '#fc-main-content h1' },
  361 |   { name: 'module navigation and tabs', path: '/counter/counter-projection', ready: '#fc-main-content h1' },
  362 |   ...specimenManifest.routes.map(route => ({ name: route.name, path: route.path, ready: route.readySelector })),
  363 | ];
  364 | 
  365 | test.describe('Story 13.6 responsive operator surfaces', () => {
  366 |   test.beforeEach(async ({ page }) => {
  367 |     await page.addInitScript(() => { localStorage.clear(); sessionStorage.clear(); });
  368 |     await page.emulateMedia({ reducedMotion: 'reduce' });
  369 |   });
```