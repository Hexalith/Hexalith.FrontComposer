# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: responsive-accessibility.spec.ts >> owned overlay return releases watchers when no destination appears after dismissal
- Location: specs/responsive-accessibility.spec.ts:285:1

# Error details

```
Error: page.evaluate: Error: expected both return watchers, got 1
```

# Page snapshot

```yaml
- generic [ref=e1]:
  - generic [ref=e2]:
    - link "Skip to content" [ref=e3]:
      - /url: /counter#fc-main-content
    - link "Skip to navigation" [ref=e4]:
      - /url: /counter#fc-nav
    - generic [ref=e6]:
      - banner [ref=e7]:
        - generic [ref=e8]:
          - generic [ref=e9]:
            - generic [ref=e10]:
              - button "Toggle navigation" [ref=e12] [cursor=pointer]
              - generic:
                - generic:
                  - generic:
                    - generic:
                      - button "Close"
                  - generic:
                    - generic:
                      - generic:
                        - generic:
                          - navigation "Primary navigation":
                            - generic:
                              - button "Counter"
                              - generic:
                                - generic:
                                  - button "More Counter pages"
                              - button "Default"
                              - button "Specimens"
                              - generic:
                                - generic:
                                  - button "More Specimens pages"
            - generic [ref=e16]: Hexalith FrontComposer
          - generic [ref=e17]:
            - button "Change theme" [ref=e19] [cursor=pointer]:
              - generic [ref=e20]:
                - generic:
                  - generic [ref=e21]: "Change theme: Light"
                  - generic [aria-hidden] [ref=e22]: Light
            - button "Open settings" [ref=e26] [cursor=pointer]
            - button "FU" [ref=e31] [cursor=pointer]
      - navigation "Primary navigation" [ref=e35]:
        - generic [ref=e36]:
          - button "Counter" [ref=e37] [cursor=pointer]
          - button "More Counter pages" [ref=e43] [cursor=pointer]
          - button "Default" [ref=e50] [cursor=pointer]
          - button "Specimens" [ref=e55] [cursor=pointer]
          - button "More Specimens pages" [ref=e61] [cursor=pointer]
      - generic [ref=e68]:
        - status [ref=e69]: Density set to Compact.
        - main [ref=e70]:
          - status [ref=e71]
          - status [ref=e72]
          - generic [ref=e73]:
            - generic "Counter pages" [ref=e74]:
              - tablist [ref=e75]:
                - generic:
                  - tab "Overview" [selected] [ref=e76] [cursor=pointer]
                  - tab "Projection" [ref=e78] [cursor=pointer]
              - tabpanel "Overview" [ref=e80]:
                - generic [ref=e82]:
                  - generic:
                    - generic [ref=e83]:
                      - heading [level=2] [ref=e84]:
                        - button "Create with an exact target" [expanded] [ref=e85] [cursor=pointer]
                      - region "Create with an exact target" [ref=e88]:
                        - group [ref=e90]:
                          - generic "Create Counter command form" [ref=e91]:
                            - generic [ref=e92]:
                              - generic [ref=e93]:
                                - generic [ref=e95]:
                                  - generic: Counter key
                                  - generic "Counter key" [ref=e97]:
                                    - textbox "Counter key, Required" [ref=e99]
                                - generic [ref=e101]:
                                  - generic: Initial Value
                                  - generic "Initial Value" [ref=e103]:
                                    - textbox "Initial Value, Required" [ref=e105]: "0"
                                - button "Create Counter" [ref=e106] [cursor=pointer]
                              - status [ref=e108]
                    - generic [ref=e109]:
                      - heading [level=2] [ref=e110]:
                        - button "Update with an exact target" [expanded] [ref=e111] [cursor=pointer]
                      - region "Update with an exact target" [ref=e114]:
                        - group [ref=e116]:
                          - generic "Update Counter command form" [ref=e117]:
                            - generic [ref=e118]:
                              - generic [ref=e119]:
                                - generic [ref=e121]:
                                  - generic: Counter key
                                  - generic "Counter key" [ref=e123]:
                                    - textbox "Counter key, Required" [ref=e125]
                                - generic [ref=e127]:
                                  - generic: Amount
                                  - generic "Amount" [ref=e129]:
                                    - textbox "Amount, Required" [ref=e131]: "1"
                                - button "Update Counter" [ref=e132] [cursor=pointer]
                              - status [ref=e134]
                    - generic [ref=e135]:
                      - heading [level=2] [ref=e136]:
                        - button "Compact inline (3 fields)" [expanded] [ref=e137] [cursor=pointer]
                      - region "Compact inline (3 fields)" [ref=e140]:
                        - group [ref=e142]:
                          - generic "Batch Increment command form" [ref=e143]:
                            - generic [ref=e144]:
                              - generic [ref=e145]:
                                - generic [ref=e147]:
                                  - generic: Amount
                                  - generic "Amount" [ref=e149]:
                                    - textbox "Amount, Required" [ref=e151]: "1"
                                - generic [ref=e153]:
                                  - generic: Note
                                  - generic "Note" [ref=e155]:
                                    - textbox "Note, Required" [ref=e157]
                                - generic [ref=e159]:
                                  - generic: Effective Date
                                  - generic "Effective Date" [ref=e161]:
                                    - generic [ref=e162]:
                                      - textbox "Effective Date" [ref=e163]:
                                        - /placeholder: M/d/yyyy
                                        - text: 10/18/2026
                                      - button "Show date picker" [ref=e165]
                                - button "Batch Increment" [ref=e167] [cursor=pointer]
                              - status [ref=e169]
                    - generic [ref=e170]:
                      - heading [level=2] [ref=e171]:
                        - button "Inline + popover (1 field)" [expanded] [ref=e172] [cursor=pointer]
                      - region "Inline + popover (1 field)" [ref=e175]:
                        - button "Increment" [ref=e176] [cursor=pointer]
                    - generic [ref=e180]:
                      - heading [level=2] [ref=e181]:
                        - button "Full-page command (5 fields)" [expanded] [ref=e182] [cursor=pointer]
                      - region "Full-page command (5 fields)" [ref=e185]:
                        - generic:
                          - note [ref=e186]: Navigation state persistence (scroll, filter, sort across FullPage round-trip) lands in Story 4.3. The current demo proves routing + breadcrumb return only.
                          - link [ref=e187] [cursor=pointer]:
                            - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                            - generic [ref=e188]:
                              - generic:
                                - link [ref=e189]:
                                  - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                                - text: Configure Counter
            - status [ref=e190]
      - generic [ref=e191]: Hexalith FrontComposer © 2026
  - dialog [ref=e194]:
    - button "Action" [active] [ref=e195]
```

# Test source

```ts
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
  244 | test('owned overlay return waits through Escape dismissal and falls back from a hidden origin', async ({ page }) => {
  245 |   await page.goto('/counter');
  246 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  247 |   const trigger = page.getByTestId('fc-palette-trigger');
  248 |   const heading = page.locator('#fc-main-content h1, main h1').first();
  249 |   const dialog = page.getByTestId('fc-focus-return-dialog');
  250 |   for (const hideOrigin of [false, true]) {
  251 |     await trigger.focus();
  252 |     await page.evaluate(async (hide) => {
  253 |       const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
  254 |       const focus = await import(path) as {
  255 |         captureOverlayOrigin: (testId: string, preserveExisting: boolean) => boolean;
  256 |         restoreOverlayOrigin: () => void;
  257 |       };
  258 |       if (!focus.captureOverlayOrigin('fc-palette-trigger', true)) throw new Error('could not capture overlay origin');
  259 |       const origin = document.querySelector<HTMLElement>('[data-testid="fc-palette-trigger"]')!;
  260 |       if (hide) origin.hidden = true;
  261 |       document.body.tabIndex = -1;
  262 |       document.body.focus();
  263 |       const modal = document.createElement('dialog');
  264 |       modal.dataset.testid = 'fc-focus-return-dialog';
  265 |       modal.innerHTML = '<button>Action</button>';
  266 |       document.body.append(modal);
  267 |       modal.showModal();
  268 |       modal.querySelector('button')!.focus();
  269 |       focus.restoreOverlayOrigin();
  270 |       // The previous twelve-frame cutoff would abandon the pending return here.
  271 |       for (let frame = 0; frame < 20; frame++) {
  272 |         await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
  273 |       }
  274 |       if (!modal.open || document.activeElement === origin) throw new Error('modal return ran before dismissal');
  275 |     }, hideOrigin);
  276 |     await page.keyboard.press('Escape');
  277 |     await expect(dialog).not.toHaveAttribute('open', '');
  278 |     if (hideOrigin) await expect(heading).toBeFocused();
  279 |     else await expect(trigger).toBeFocused();
  280 |     await dialog.evaluate(element => element.remove());
  281 |     if (hideOrigin) await trigger.evaluate(element => { (element as HTMLElement).hidden = false; });
  282 |   }
  283 | });
  284 | 
  285 | test('owned overlay return releases watchers when no destination appears after dismissal', async ({ page }) => {
  286 |   await page.goto('/counter');
  287 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  288 |   await page.getByTestId('fc-palette-trigger').focus();
> 289 |   await page.evaluate(async () => {
      |              ^ Error: page.evaluate: Error: expected both return watchers, got 1
  290 |     const path: string = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
  291 |     const focus = await import(path) as {
  292 |       captureOverlayOrigin: (testId: string, preserveExisting: boolean) => boolean;
  293 |       restoreOverlayOrigin: () => void;
  294 |     };
  295 |     if (!focus.captureOverlayOrigin('fc-palette-trigger', true)) throw new Error('could not capture overlay origin');
  296 |     document.querySelector<HTMLElement>('[data-testid="fc-palette-trigger"]')!.hidden = true;
  297 |     document.querySelectorAll('#fc-main-content h1, main h1').forEach(heading => heading.remove());
  298 |     const modal = document.createElement('dialog');
  299 |     modal.dataset.testid = 'fc-focus-return-dialog';
  300 |     modal.innerHTML = '<button>Action</button>';
  301 |     document.body.append(modal);
  302 |     modal.showModal();
  303 |     modal.querySelector('button')!.focus();
  304 |     const add = document.addEventListener.bind(document);
  305 |     const remove = document.removeEventListener.bind(document);
  306 |     const watched = new Set<EventListenerOrEventListenerObject>();
  307 |     let recording = true;
  308 |     Object.defineProperty(document, 'addEventListener', { configurable: true, value: (type: string, listener: EventListenerOrEventListenerObject, options?: boolean | AddEventListenerOptions) => {
  309 |       if (recording && (type === 'pointerdown' || type === 'keydown')) watched.add(listener);
  310 |       add(type, listener, options);
  311 |     } });
  312 |     Object.defineProperty(document, 'removeEventListener', { configurable: true, value: (type: string, listener: EventListenerOrEventListenerObject, options?: boolean | EventListenerOptions) => {
  313 |       if (type === 'pointerdown' || type === 'keydown') watched.delete(listener);
  314 |       remove(type, listener, options);
  315 |     } });
  316 |     focus.restoreOverlayOrigin();
  317 |     recording = false;
  318 |     (window as typeof window & { __fcFocusWatch?: { active: () => number; restore: () => void } }).__fcFocusWatch = {
  319 |       active: () => watched.size,
  320 |       restore: () => {
  321 |         Reflect.deleteProperty(document, 'addEventListener');
  322 |         Reflect.deleteProperty(document, 'removeEventListener');
  323 |       },
  324 |     };
  325 |     if (watched.size !== 2) throw new Error(`expected both return watchers, got ${watched.size}`);
  326 |   });
  327 |   await page.keyboard.press('Escape');
  328 |   await expect(page.getByTestId('fc-focus-return-dialog')).not.toHaveAttribute('open', '');
  329 |   await expect.poll(() => page.evaluate(() => (window as typeof window & { __fcFocusWatch?: { active: () => number } }).__fcFocusWatch?.active()), { timeout: 6_500 }).toBe(0);
  330 |   await page.evaluate(() => {
  331 |     const heading = document.createElement('h1');
  332 |     heading.textContent = 'Late route heading';
  333 |     document.querySelector('#fc-main-content, main')?.append(heading);
  334 |     (window as typeof window & { __fcFocusWatch?: { restore: () => void } }).__fcFocusWatch?.restore();
  335 |   });
  336 |   await expect(page.getByRole('heading', { name: 'Late route heading' })).not.toBeFocused();
  337 | });
  338 | 
  339 | test('owned palette return cancels the actual Fluent delayed return after an operator Tab', async ({ page }) => {
  340 |   await page.goto('/counter');
  341 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  342 |   await page.getByTestId('fc-palette-trigger').click();
  343 |   await expect(page.getByTestId('fc-palette-search')).toBeFocused();
  344 |   await page.evaluate(async () => {
  345 |     const path = '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js';
  346 |     const focus = await import(path) as { containDialogFocus: (testId: string) => boolean };
  347 |     if (!focus.containDialogFocus('fc-palette-root')) throw new Error('palette containment did not attach');
  348 |     const host = document.querySelector<HTMLElement>('[data-testid="fc-palette-root"]')!.closest('fluent-dialog')!;
  349 |     const w = window as typeof window & { __aeVendorReturn?: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } };
  350 |     const callbacks: (() => void)[] = [];
  351 |     const original = window.setTimeout;
  352 |     window.setTimeout = ((handler: TimerHandler, delay?: number, ...args: unknown[]) => {
  353 |       // Hold the pinned Fluent callback itself, rather than substitute a synthetic return.
  354 |       if (delay === 25 && typeof handler === 'function' && new Error().stack?.includes('Microsoft.FluentUI.AspNetCore.Components.lib.module.js')) {
  355 |         callbacks.push(() => handler(...args));
  356 |         return 0;
  357 |       }
  358 |       return original(handler, delay, ...args);
  359 |     }) as typeof window.setTimeout;
  360 |     w.__aeVendorReturn = { host, callbacks, restore: () => { window.setTimeout = original; } };
  361 |   });
  362 |   await page.keyboard.press('Escape');
  363 |   await expect(page.getByTestId('fc-palette-root')).toHaveCount(0);
  364 |   await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  365 |   await page.keyboard.press('Tab');
  366 |   const settings = page.getByTestId('fc-settings-button');
  367 |   await expect(settings).toBeFocused();
  368 |   const proof = await page.evaluate(() => {
  369 |     const state = (window as typeof window & { __aeVendorReturn: { host: Element & { previousActiveElement?: Element | null }; callbacks: (() => void)[]; restore: () => void } }).__aeVendorReturn;
  370 |     state.restore();
  371 |     const proof = { queued: state.callbacks.length, cleared: state.host.previousActiveElement === null };
  372 |     for (const callback of state.callbacks) callback();
  373 |     return proof;
  374 |   });
  375 |   expect(proof.queued).toBeGreaterThan(0);
  376 |   expect(proof.cleared).toBe(true);
  377 |   await expect(settings).toBeFocused();
  378 |   await page.keyboard.press('Enter');
  379 |   await expect(page.locator('#fc-settings-heading')).toBeFocused();
  380 | });
  381 | 
  382 | for (const theme of ['light', 'dark']) {
  383 |   test(`narrow specimen navigation and grid retain unobscured focus in ${theme}`, async ({ page }, info) => {
  384 |     await page.setViewportSize({ width: 1280, height: 900 });
  385 |     await page.goto(`/__frontcomposer/specimens/type?theme=${theme}`);
  386 |     await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  387 |     const nextItem = page.getByTestId('fc-nav-group-generated').locator('..');
  388 |     expect(await nextItem.evaluate(element => getComputedStyle(element).marginBlockStart)).toBe('0px');
  389 |     await page.setViewportSize({ width: 320, height: 900 });
```