# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: review-focus-order.temp.spec.ts >> active toolbar, tabs, palette and settings states in dark
- Location: specs/review-focus-order.temp.spec.ts:327:3

# Error details

```
Error: expect(locator).toBeFocused() failed

Locator: locator('#fc-settings-heading')
Expected: focused
Timeout: 10000ms
Error: element(s) not found

Call log:
  - Expect "toBeFocused" locator('#fc-settings-heading') with timeout 10000ms
  - waiting for locator('#fc-settings-heading')

```

```yaml
- link "Skip to content":
  - /url: /__frontcomposer/specimens/page-toolbar#fc-main-content
- link "Skip to navigation":
  - /url: /__frontcomposer/specimens/page-toolbar#fc-nav
- banner:
  - button "Toggle navigation"
  - text: Hexalith FrontComposer
  - button "Change theme": "Change theme: Dark"
  - button "Open command palette"
  - button "Open settings"
  - button "FU"
- status: Density set to Comfortable.
- main:
  - status
  - status
  - main "Page toolbar specimen":
    - paragraph: FrontComposer accessibility specimen
    - heading "Page toolbar specimen" [level=1]
    - paragraph: Reusable search, filter, view, action, and tab toolbar rendered with Fluent UI.
    - region "Toolbar":
      - heading "Toolbar" [level=2]
      - toolbar "Orders page tools":
        - searchbox "Search orders": open
        - button "Order filters"
        - button "View options"
        - text: Refresh
    - region "Page tabs":
      - heading "Page tabs" [level=2]
      - tablist:
        - tab "Summary"
        - tab "Activity" [selected]
        - tab "Archived" [disabled]
        - tab "History"
      - tabpanel "Activity":
        - paragraph: Activity panel content
    - region "Toolbar state":
      - heading "Toolbar state" [level=2]
      - text: "Current search: open Active tab: activity Refresh count: 0"
- text: Hexalith FrontComposer © 2026
- alertdialog:
  - text: Command palette
  - dialog "Command palette":
    - combobox [expanded]:
      - searchbox
    - listbox:
      - heading "Projections" [level=4]
      - group "Projections":
        - option "CounterProjection"
        - option "SpecimenFormattingProjection"
        - option "SpecimenStatusProjection"
      - heading "Commands" [level=4]
      - group "Commands":
        - option "Keyboard Shortcuts View all keyboard shortcuts" [selected]
    - status
```

# Test source

```ts
  293 |   await page.emulateMedia({ forcedColors: 'active' });
  294 |   await expect(selected).toHaveAttribute('aria-selected', 'true');
  295 |   await expectSurfaceContrast(selected);
  296 |   await expect(expectSurfaceContrast(selected, 'border')).rejects.toThrow(/Nonempty/);
  297 | });
  298 | 
  299 | for (const theme of ['light', 'dark']) {
  300 |   test(`shell chrome, theme menu and exact desktop navigation rails in ${theme}`, async ({ page }, info) => {
  301 |     await ready(page, '/counter', theme);
  302 |     const shell = page.locator('.fc-shell-root');
  303 |     await evidence(page, shell, `shell-chrome-${theme}`, info);
  304 |     await page.getByTitle('Change theme').first().click();
  305 |     const themeMenu = page.getByRole('menu').first();
  306 |     await expect(themeMenu).toHaveAttribute('focusgroup', 'menu');
  307 |     await settlePaint(page);
  308 |     await themeMenu.getByRole('menuitem', { name: 'Light', exact: true }).focus();
  309 |     await page.keyboard.press('ArrowDown');
  310 |     const darkItem = themeMenu.getByRole('menuitem', { name: 'Dark', exact: true });
  311 |     await expectFocusVisible(darkItem);
  312 |     await evidence(page, themeMenu, `theme-menu-${theme}`, info);
  313 |     await expectSurfaceContrast(darkItem, 'focus');
  314 |     await expectNoBlockingAxeViolations(page, { route: '/counter#theme-menu', artifactPath: info.outputPath(`axe-shell-menu-${theme}.json`) });
  315 |     await page.keyboard.press('Escape');
  316 |     await page.setViewportSize({ width: 1600, height: 900 });
  317 |     const rail = page.getByTestId('fc-shell-navigation').getByTestId('fc-navigation-rail');
  318 |     await expect(rail).toHaveAttribute('data-rail-width', '72');
  319 |     await expect.poll(async () => (await rail.boundingBox())?.width).toBe(72);
  320 |     await evidence(page, shell, `rail-labeled-${theme}`, info, rail);
  321 |     await page.getByTestId('fc-hamburger-toggle').click();
  322 |     await expect(rail).toHaveAttribute('data-rail-width', '48');
  323 |     await expect.poll(async () => (await rail.boundingBox())?.width).toBe(48);
  324 |     await evidence(page, shell, `rail-icons-${theme}`, info, rail);
  325 |   });
  326 | 
  327 |   test(`active toolbar, tabs, palette and settings states in ${theme}`, async ({ page }, info) => {
  328 |     await page.addInitScript(() => {
  329 |       const w = window as any; w.__aeOrder = [];
  330 |       const node = (value: any) => value instanceof Element ? { tag: value.tagName, id: value.id, test: value.getAttribute('data-testid') } : null;
  331 |       w.__aeRecord = (kind: string, extra: any = null) => {
  332 |         const r = w.__fcModalReservation;
  333 |         w.__aeOrder.push({ time: performance.now(), kind, extra, active: node(document.activeElement), origin: node(w.__fcOverlayOrigin), reservation: r ? { createdAt: r.createdAt, durable: r.durable, owner: r.owner, origin: node(r.origin), moved: r.moved, watchFocus: r.watchFocus } : null, intent: w.__fcOverlayOpenIntent ? { moved: w.__fcOverlayOpenIntent.moved, origin: node(w.__fcOverlayOpenIntent.origin) } : null });
  334 |       };
  335 |       const focus = HTMLElement.prototype.focus;
  336 |       HTMLElement.prototype.focus = function(options) { w.__aeRecord('HTMLElement.focus', { target: node(this), stack: new Error().stack }); return focus.call(this, options); };
  337 |       const close = HTMLDialogElement.prototype.close;
  338 |       HTMLDialogElement.prototype.close = function(value) { w.__aeRecord('dialog.close before', node(this)); const result = close.call(this, value); w.__aeRecord('dialog.close after', node(this)); return result; };
  339 |       for (const kind of ['focusin','focusout','keydown','keyup','pointerdown','click']) document.addEventListener(kind, (event: any) => w.__aeRecord(kind, { target: node(event.target), key: event.key }), true);
  340 |     });
  341 |     await page.route('**/fc-focus.js', async route => {
  342 |       const response = await route.fetch();
  343 |       let source = await response.text();
  344 |       source = source.replace('export function restoreOverlayOrigin(', 'function originalRestoreOverlayOrigin(').replace('export function captureOverlayOrigin(', 'function originalCaptureOverlayOrigin(');
  345 |       source = source.replace('        if (userMovedFocus) {', `        window.__aeRecord('restoreOverlayOrigin callback', { userMovedFocus, origin: origin instanceof Element ? {tag:origin.tagName,id:origin.id,test:origin.getAttribute('data-testid'),connected:origin.isConnected}:null });\n        if (userMovedFocus) {`);
  346 |       source += `
  347 | export function restoreOverlayOrigin(...args) { window.__aeRecord('restoreOverlayOrigin before', args); const result = originalRestoreOverlayOrigin(...args); window.__aeRecord('restoreOverlayOrigin after', args); return result; }
  348 | export function captureOverlayOrigin(...args) { window.__aeRecord('captureOverlayOrigin before', args); const result = originalCaptureOverlayOrigin(...args); window.__aeRecord('captureOverlayOrigin after', {args,result}); return result; }
  349 | `;
  350 |       await route.fulfill({ response, body: source });
  351 |     });
  352 |     try {
  353 |     await ready(page, '/__frontcomposer/specimens/page-toolbar', theme);
  354 |     const toolbar = new PageToolbarSpecimenPage(page);
  355 |     await toolbar.searchInput.focus();
  356 |     await page.keyboard.press('Shift+Tab');
  357 |     await page.keyboard.press('Tab');
  358 |     await expectFocusVisible(toolbar.searchInput);
  359 |     await expectSurfaceContrast(page.getByTestId('fc-page-toolbar-search'), 'focus');
  360 |     await toolbar.filterTrigger.click();
  361 |     await expect(toolbar.filterContent).toBeVisible();
  362 |     await evidence(page, toolbar.filterContent, `filter-popover-${theme}`, info);
  363 |     await toolbar.filterTrigger.click();
  364 |     await toolbar.viewTrigger.click();
  365 |     await evidence(page, toolbar.viewDensityItem, `view-menu-${theme}`, info);
  366 |     await page.keyboard.press('Escape');
  367 |     await toolbar.summaryTab.focus();
  368 |     await toolbar.summaryTab.press('ArrowRight');
  369 |     await expect(toolbar.activityTab).toHaveAttribute('aria-selected', 'true');
  370 |     await expectFocusVisible(toolbar.activityTab);
  371 |     await evidence(page, toolbar.tabs, `tabs-active-${theme}`, info);
  372 |     await page.getByTestId('fc-palette-trigger').click();
  373 |     const palette = page.getByTestId('fc-palette-root');
  374 |     await evidence(page, palette, `palette-default-${theme}`, info);
  375 |     const search = page.getByTestId('fc-palette-search').locator('input');
  376 |     await search.fill('counter');
  377 |     await expect(palette.locator('[aria-selected="true"]')).toBeVisible();
  378 |     await evidence(page, palette, `palette-selected-${theme}`, info);
  379 |     await expectSurfaceContrast(palette.locator('.fc-palette-option-selected'), 'cue');
  380 |     await search.fill('no-match-accessibility-136');
  381 |     await expect(palette.locator('.fc-palette-noresults')).toBeVisible();
  382 |     await evidence(page, palette, `palette-empty-${theme}`, info);
  383 |     await page.emulateMedia({ forcedColors: 'active' });
  384 |     await expect(palette.locator('.fc-palette-noresults')).toContainText(/No/);
  385 |     await page.emulateMedia({ forcedColors: 'none' });
  386 |     await page.keyboard.press('Escape');
  387 |     await expect(palette).toHaveCount(0);
  388 |     await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  389 |     await page.keyboard.press('Tab');
  390 |     await expect(page.getByTestId('fc-settings-button')).toBeFocused();
  391 |     await page.keyboard.press('Enter');
  392 |     const settings = page.getByTestId('fc-settings-dialog');
> 393 |     await expect(page.locator('#fc-settings-heading')).toBeFocused();
      |                                                        ^ Error: expect(locator).toBeFocused() failed
  394 |     await page.keyboard.press('Tab');
  395 |     await page.keyboard.press('Tab');
  396 |     const radio = settings.locator('fluent-radio:focus');
  397 |     await expectFocusVisible(radio);
  398 |     await evidence(page, settings, `settings-${theme}`, info);
  399 |     await info.attach(`settings-radio-focus-${theme}`, { body: JSON.stringify(await expectSurfaceContrast(radio, 'focus')), contentType: 'application/json' });
  400 |     const exceptions: import('../helpers/responsive-accessibility.js').TargetException[] = ['Compact', 'Comfortable', 'Roomy'].map(value => ({ selector: `fluent-radio[name="fc-density-select"][value="${value}"]`, kind: 'Spacing', rationale: 'Fluent radio with independently measured 24px spacing circle.', keyboardPath: 'Settings → Density; Tab and arrows', evidence: `settings-${theme} target inventory` }));
  401 |     const targets = await expectTargetSizes(page, exceptions);
  402 |     await info.attach(`settings-targets-${theme}`, { body: JSON.stringify(targets), contentType: 'application/json' });
  403 |     await page.emulateMedia({ forcedColors: 'active' });
  404 |     await expect(settings).toContainText('Display density');
  405 |     await page.keyboard.press('Escape');
  406 |     await expect(page.getByTestId('fc-settings-button')).toBeFocused();
  407 |     } finally {
  408 |       await page.evaluate(() => { const w = window as any; w.__aeRecord('final selected paint', [...document.querySelectorAll('.fc-palette-option-selected')].map(e=>{const c=getComputedStyle(e);return{outline:c.outline,offset:c.outlineOffset,rect:e.getBoundingClientRect().toJSON(),connected:e.isConnected,clip:c.clip,clipPath:c.clipPath}})); });
  409 |       await info.attach('focus-close-order', { body: JSON.stringify(await page.evaluate(() => (window as any).__aeOrder), null, 2), contentType: 'application/json' });
  410 |     }
  411 |   });
  412 | 
  413 |   test(`generated validation, policy, destructive and abandonment states in ${theme}`, async ({ page }, info) => {
  414 |     await ready(page, `/__frontcomposer/specimens/type?theme=${theme}`, theme);
  415 |     const destructive = page.getByTestId('fc-destructive-command-specimen');
  416 |     await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).click();
  417 |     // Required retention deliberately stays empty: the genuine generated validation summary owns focus.
  418 |     const summary = destructive.locator('[data-testid="fc-validation-summary"]');
  419 |     await expect(summary).toBeVisible();
  420 |     await evidence(page, summary, `generated-validation-${theme}`, info);
  421 |     await evidence(page, page.getByTestId('fc-policy-command-specimen'), `policy-denied-${theme}`, info);
  422 |     const form = destructive.locator('.fc-command-form');
  423 |     await form.getByRole('combobox').click();
  424 |     await form.getByRole('combobox').press('ArrowDown');
  425 |     await form.getByRole('combobox').press('Enter');
  426 |     await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).focus();
  427 |     await page.keyboard.press('Shift+Tab');
  428 |     await page.keyboard.press('Tab');
  429 |     await page.keyboard.press('Enter');
  430 |     const dialog = page.getByRole('alertdialog');
  431 |     await expect(dialog).toBeVisible();
  432 |     await evidence(page, dialog, `destructive-dialog-${theme}`, info);
  433 |     await expectTargetSizes(page);
  434 |     await page.keyboard.press('Escape');
  435 | 
  436 |     await ready(page, '/commands/Counter/ConfigureCounterCommand', theme);
  437 |     const configure = page.locator('.fc-command-form[aria-label="Configure Counter command form"]');
  438 |     await fillFieldByLabel(configure, 'Name', 'Accessibility specimen');
  439 |     await page.waitForTimeout(6_500);
  440 |     await page.getByRole('link', { name: 'Counter', exact: true }).last().focus();
  441 |     await page.keyboard.press('Shift+Tab');
  442 |     await page.keyboard.press('Tab');
  443 |     await page.keyboard.press('Enter');
  444 |     const warning = page.getByTestId('fc-form-abandonment-warning');
  445 |     await expect(warning).toBeVisible();
  446 |     await expect(page.getByTestId('fc-form-abandonment-stay')).toBeFocused();
  447 |     await evidence(page, warning, `abandonment-${theme}`, info);
  448 |     await expectFocusVisible(page.getByTestId('fc-form-abandonment-stay'));
  449 |     await page.keyboard.press('Escape');
  450 |     await expect(warning).toHaveCount(0);
  451 |     await expect(configure).toContainText('Name');
  452 |   });
  453 | }
  454 | 
  455 | for (const theme of ['light', 'dark']) {
  456 |   test(`projection reconnect fallback stale slow cap and filter cues in ${theme}`, async ({ page }, info) => {
  457 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  458 |     const grid = page.getByTestId('fc-generated-counter-grid');
  459 |     for (const connection of ['reconnecting', 'disconnected', 'connected']) {
  460 |       await page.getByTestId(`fc-ae-connection-${connection}`).click();
  461 |       const notice = page.getByTestId('fc-projection-connection-status');
  462 |       await expect(notice).toBeVisible();
  463 |       await evidence(page, notice, `connection-${connection}-${theme}`, info, notice);
  464 |       if (connection !== 'connected') {
  465 |         await expect(grid.getByTestId('fc-projection-stale-notice')).toBeVisible();
  466 |         await evidence(page, grid.getByTestId('fc-projection-stale-notice'), `stale-${connection}-${theme}`, info);
  467 |       }
  468 |     }
  469 |     await page.getByTestId('fc-ae-page-start').click();
  470 |     const slow = grid.getByTestId('fc-slow-query-notice');
  471 |     await expect(slow).toBeVisible({ timeout: 5_000 });
  472 |     await evidence(page, slow, `slow-query-${theme}`, info);
  473 |     await page.getByTestId('fc-ae-page-cap').click();
  474 |     await expect(slow).toHaveCount(0);
  475 |     const cap = grid.getByTestId('fc-max-items-cap-notice');
  476 |     await expect(cap).toBeVisible();
  477 |     await evidence(page, cap, `result-limit-${theme}`, info);
  478 |     await page.getByTestId('fc-ae-filter-empty').click();
  479 |     await expect(page.getByTestId('fc-filter-empty-state')).toBeVisible();
  480 |     await evidence(page, page.getByTestId('fc-filter-empty-state'), `filter-empty-${theme}`, info);
  481 |     await page.getByTestId('fc-ae-filter-reset').click();
  482 |     await expect(page.getByTestId('fc-filter-empty-state')).toHaveCount(0);
  483 |   });
  484 | }
  485 | 
  486 | for (const theme of ['light', 'dark']) {
  487 |   test(`real generated filter-hidden detail stays distinct from missing-item results in ${theme}`, async ({ page }, info) => {
  488 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  489 |     const grid = page.getByTestId('fc-badge-grid');
  490 |     await grid.getByRole('gridcell', { name: 'FC-1000', exact: true }).click();
  491 |     await expect(grid.locator('.fc-expand-in-row-detail')).toContainText('FC-1000');
  492 |     await grid.locator('[data-fc-status-chip="Success"]').click();
  493 |     const clear = grid.getByTestId('fc-expanded-row-hidden-banner-clear');
```