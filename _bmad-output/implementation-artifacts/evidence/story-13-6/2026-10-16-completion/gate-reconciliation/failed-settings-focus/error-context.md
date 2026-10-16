# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> active toolbar, tabs, palette and settings states in dark
- Location: specs/accessibility-state-matrix.spec.ts:327:3

# Error details

```
Error: expect(locator).toBeFocused() failed

Locator:  getByTestId('fc-settings-button')
Expected: focused
Received: inactive
Timeout:  10000ms

Call log:
  - Expect "toBeFocused" getByTestId('fc-settings-button') with timeout 10000ms
  - waiting for getByTestId('fc-settings-button')
    24 × locator resolved to <fluent-button icon-only="" tabindex="0" role="button" appearance="subtle" title="Open settings" aria-label="Open settings" data-testid="fc-settings-button" _bl_5dc7a9dc-ff06-4cdb-9345-31d913c2b0ce="">…</fluent-button>
       - unexpected value "inactive"

```

```yaml
- button "Open settings"
```

# Test source

```ts
  281 |   await expect(expectSurfaceContrast(page.locator('main'), 'focus')).rejects.toThrow(/contrast failures/);
  282 |   await page.locator('#bad').evaluate(element => { element.style.outline = 'none'; element.style.boxShadow = '0 0 0 2px white,0 0 0 4px black'; });
  283 |   const ring = await expectSurfaceContrast(page.locator('#bad'), 'focus');
  284 |   expect(ring.some(sample => sample.ratio === 1)).toBe(true);
  285 |   expect(ring.some(sample => sample.ratio === 21)).toBe(true);
  286 | });
  287 | 
  288 | test('forced-color state checks reject a removed cue while text and selected semantics survive', async ({ page }) => {
  289 |   await page.setContent('<style>button{background:white;color:black;border:2px solid black}@media(forced-colors:active){button{border:none;outline:none!important;box-shadow:none!important}}</style><button role="tab" aria-selected="true">Current tab</button>');
  290 |   const selected = page.getByRole('tab');
  291 |   await expectSurfaceContrast(selected);
  292 |   await expectSurfaceContrast(selected, 'border');
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
  328 |     await ready(page, '/__frontcomposer/specimens/page-toolbar', theme);
  329 |     const toolbar = new PageToolbarSpecimenPage(page);
  330 |     await toolbar.searchInput.focus();
  331 |     await page.keyboard.press('Shift+Tab');
  332 |     await page.keyboard.press('Tab');
  333 |     await expectFocusVisible(toolbar.searchInput);
  334 |     await expectSurfaceContrast(page.getByTestId('fc-page-toolbar-search'), 'focus');
  335 |     await toolbar.filterTrigger.click();
  336 |     await expect(toolbar.filterContent).toBeVisible();
  337 |     await evidence(page, toolbar.filterContent, `filter-popover-${theme}`, info);
  338 |     await toolbar.filterTrigger.click();
  339 |     await toolbar.viewTrigger.click();
  340 |     await evidence(page, toolbar.viewDensityItem, `view-menu-${theme}`, info);
  341 |     await page.keyboard.press('Escape');
  342 |     await toolbar.summaryTab.focus();
  343 |     await toolbar.summaryTab.press('ArrowRight');
  344 |     await expect(toolbar.activityTab).toHaveAttribute('aria-selected', 'true');
  345 |     await expectFocusVisible(toolbar.activityTab);
  346 |     await evidence(page, toolbar.tabs, `tabs-active-${theme}`, info);
  347 |     await page.getByTestId('fc-palette-trigger').click();
  348 |     const palette = page.getByTestId('fc-palette-root');
  349 |     await evidence(page, palette, `palette-default-${theme}`, info);
  350 |     const search = page.getByTestId('fc-palette-search').locator('input');
  351 |     await search.fill('counter');
  352 |     await expect(palette.locator('[aria-selected="true"]')).toBeVisible();
  353 |     await evidence(page, palette, `palette-selected-${theme}`, info);
  354 |     await expectSurfaceContrast(palette.locator('.fc-palette-option-selected'), 'cue');
  355 |     await search.fill('no-match-accessibility-136');
  356 |     await expect(palette.locator('.fc-palette-noresults')).toBeVisible();
  357 |     await evidence(page, palette, `palette-empty-${theme}`, info);
  358 |     await page.emulateMedia({ forcedColors: 'active' });
  359 |     await expect(palette.locator('.fc-palette-noresults')).toContainText(/No/);
  360 |     await page.emulateMedia({ forcedColors: 'none' });
  361 |     await page.keyboard.press('Escape');
  362 |     await expect(palette).toHaveCount(0);
  363 |     await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  364 |     await page.keyboard.press('Tab');
  365 |     await expect(page.getByTestId('fc-settings-button')).toBeFocused();
  366 |     await page.keyboard.press('Enter');
  367 |     const settings = page.getByTestId('fc-settings-dialog');
  368 |     await expect(page.locator('#fc-settings-heading')).toBeFocused();
  369 |     await page.keyboard.press('Tab');
  370 |     await page.keyboard.press('Tab');
  371 |     const radio = settings.locator('fluent-radio:focus');
  372 |     await expectFocusVisible(radio);
  373 |     await evidence(page, settings, `settings-${theme}`, info);
  374 |     await info.attach(`settings-radio-focus-${theme}`, { body: JSON.stringify(await expectSurfaceContrast(radio, 'focus')), contentType: 'application/json' });
  375 |     const exceptions: import('../helpers/responsive-accessibility.js').TargetException[] = ['Compact', 'Comfortable', 'Roomy'].map(value => ({ selector: `fluent-radio[name="fc-density-select"][value="${value}"]`, kind: 'Spacing', rationale: 'Fluent radio with independently measured 24px spacing circle.', keyboardPath: 'Settings → Density; Tab and arrows', evidence: `settings-${theme} target inventory` }));
  376 |     const targets = await expectTargetSizes(page, exceptions);
  377 |     await info.attach(`settings-targets-${theme}`, { body: JSON.stringify(targets), contentType: 'application/json' });
  378 |     await page.emulateMedia({ forcedColors: 'active' });
  379 |     await expect(settings).toContainText('Display density');
  380 |     await page.keyboard.press('Escape');
> 381 |     await expect(page.getByTestId('fc-settings-button')).toBeFocused();
      |                                                          ^ Error: expect(locator).toBeFocused() failed
  382 |   });
  383 | 
  384 |   test(`generated validation, policy, destructive and abandonment states in ${theme}`, async ({ page }, info) => {
  385 |     await ready(page, `/__frontcomposer/specimens/type?theme=${theme}`, theme);
  386 |     const destructive = page.getByTestId('fc-destructive-command-specimen');
  387 |     await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).click();
  388 |     // Required retention deliberately stays empty: the genuine generated validation summary owns focus.
  389 |     const summary = destructive.locator('[data-testid="fc-validation-summary"]');
  390 |     await expect(summary).toBeVisible();
  391 |     await evidence(page, summary, `generated-validation-${theme}`, info);
  392 |     await evidence(page, page.getByTestId('fc-policy-command-specimen'), `policy-denied-${theme}`, info);
  393 |     const form = destructive.locator('.fc-command-form');
  394 |     await form.getByRole('combobox').click();
  395 |     await form.getByRole('combobox').press('ArrowDown');
  396 |     await form.getByRole('combobox').press('Enter');
  397 |     await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).focus();
  398 |     await page.keyboard.press('Shift+Tab');
  399 |     await page.keyboard.press('Tab');
  400 |     await page.keyboard.press('Enter');
  401 |     const dialog = page.getByRole('alertdialog');
  402 |     await expect(dialog).toBeVisible();
  403 |     await evidence(page, dialog, `destructive-dialog-${theme}`, info);
  404 |     await expectTargetSizes(page);
  405 |     await page.keyboard.press('Escape');
  406 | 
  407 |     await ready(page, '/commands/Counter/ConfigureCounterCommand', theme);
  408 |     const configure = page.locator('.fc-command-form[aria-label="Configure Counter command form"]');
  409 |     await fillFieldByLabel(configure, 'Name', 'Accessibility specimen');
  410 |     await page.waitForTimeout(6_500);
  411 |     await page.getByRole('link', { name: 'Counter', exact: true }).last().focus();
  412 |     await page.keyboard.press('Shift+Tab');
  413 |     await page.keyboard.press('Tab');
  414 |     await page.keyboard.press('Enter');
  415 |     const warning = page.getByTestId('fc-form-abandonment-warning');
  416 |     await expect(warning).toBeVisible();
  417 |     await expect(page.getByTestId('fc-form-abandonment-stay')).toBeFocused();
  418 |     await evidence(page, warning, `abandonment-${theme}`, info);
  419 |     await expectFocusVisible(page.getByTestId('fc-form-abandonment-stay'));
  420 |     await page.keyboard.press('Escape');
  421 |     await expect(warning).toHaveCount(0);
  422 |     await expect(configure).toContainText('Name');
  423 |   });
  424 | }
  425 | 
  426 | for (const theme of ['light', 'dark']) {
  427 |   test(`projection reconnect fallback stale slow cap and filter cues in ${theme}`, async ({ page }, info) => {
  428 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  429 |     const grid = page.getByTestId('fc-generated-counter-grid');
  430 |     for (const connection of ['reconnecting', 'disconnected', 'connected']) {
  431 |       await page.getByTestId(`fc-ae-connection-${connection}`).click();
  432 |       const notice = page.getByTestId('fc-projection-connection-status');
  433 |       await expect(notice).toBeVisible();
  434 |       await evidence(page, notice, `connection-${connection}-${theme}`, info, notice);
  435 |       if (connection !== 'connected') {
  436 |         await expect(grid.getByTestId('fc-projection-stale-notice')).toBeVisible();
  437 |         await evidence(page, grid.getByTestId('fc-projection-stale-notice'), `stale-${connection}-${theme}`, info);
  438 |       }
  439 |     }
  440 |     await page.getByTestId('fc-ae-page-start').click();
  441 |     const slow = grid.getByTestId('fc-slow-query-notice');
  442 |     await expect(slow).toBeVisible({ timeout: 5_000 });
  443 |     await evidence(page, slow, `slow-query-${theme}`, info);
  444 |     await page.getByTestId('fc-ae-page-cap').click();
  445 |     await expect(slow).toHaveCount(0);
  446 |     const cap = grid.getByTestId('fc-max-items-cap-notice');
  447 |     await expect(cap).toBeVisible();
  448 |     await evidence(page, cap, `result-limit-${theme}`, info);
  449 |     await page.getByTestId('fc-ae-filter-empty').click();
  450 |     await expect(page.getByTestId('fc-filter-empty-state')).toBeVisible();
  451 |     await evidence(page, page.getByTestId('fc-filter-empty-state'), `filter-empty-${theme}`, info);
  452 |     await page.getByTestId('fc-ae-filter-reset').click();
  453 |     await expect(page.getByTestId('fc-filter-empty-state')).toHaveCount(0);
  454 |   });
  455 | }
  456 | 
  457 | for (const theme of ['light', 'dark']) {
  458 |   test(`real generated filter-hidden detail stays distinct from missing-item results in ${theme}`, async ({ page }, info) => {
  459 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  460 |     const grid = page.getByTestId('fc-badge-grid');
  461 |     await grid.getByRole('gridcell', { name: 'FC-1000', exact: true }).click();
  462 |     await expect(grid.locator('.fc-expand-in-row-detail')).toContainText('FC-1000');
  463 |     await grid.locator('[data-fc-status-chip="Success"]').click();
  464 |     const clear = grid.getByTestId('fc-expanded-row-hidden-banner-clear');
  465 |     await expect(clear).toBeFocused();
  466 |     await expect(grid.getByTestId('fc-expanded-row-hidden-banner')).toContainText('hidden');
  467 |     await expect(grid.getByTestId('fc-expanded-row-hidden-banner')).not.toContainText('not in the current results');
  468 |     await evidence(page, grid.getByTestId('fc-expanded-row-hidden-banner'), `filter-hidden-detail-${theme}`, info);
  469 |     await clear.click();
  470 |     await expect(grid.locator('.fc-expand-in-row-detail')).toContainText('FC-1000');
  471 |   });
  472 | }
  473 | 
  474 | for (const theme of ['light', 'dark']) {
  475 |   test(`delayed generated detail recovery cancels after operator focus and navigation in ${theme}`, async ({ page }) => {
  476 |     await page.route('**/fc-focus.js', async route => {
  477 |       const response = await route.fetch();
  478 |       const source = (await response.text()).replace('export function focusReplacementHeading(key, headingId)', 'function originalFocusReplacementHeading(key, headingId)');
  479 |       await route.fulfill({ response, body: source + `
  480 | export async function focusReplacementHeading(key, headingId) {
  481 |   const gate = window.__aeRecoveryGate;
```