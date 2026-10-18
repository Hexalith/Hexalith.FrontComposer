# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> active toolbar, tabs, palette and settings states in dark
- Location: specs/accessibility-state-matrix.spec.ts:499:3

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
    24 × locator resolved to <fluent-button icon-only="" tabindex="0" role="button" appearance="subtle" title="Open settings" aria-label="Open settings" data-testid="fc-settings-button" _bl_87ea29e5-21c0-4187-916e-5938d946b8a9="">…</fluent-button>
       - unexpected value "inactive"

```

```yaml
- button "Open settings"
```

# Test source

```ts
  461 |   const selected = page.getByRole('tab');
  462 |   await expectSurfaceContrast(selected);
  463 |   await expectSurfaceContrast(selected, 'border');
  464 |   await page.emulateMedia({ forcedColors: 'active' });
  465 |   await expect(selected).toHaveAttribute('aria-selected', 'true');
  466 |   await expectSurfaceContrast(selected);
  467 |   await expect(expectSurfaceContrast(selected, 'border')).rejects.toThrow(/Nonempty/);
  468 | });
  469 | 
  470 | for (const theme of ['light', 'dark']) {
  471 |   test(`shell chrome, theme menu and exact desktop navigation rails in ${theme}`, async ({ page }, info) => {
  472 |     test.setTimeout(120_000);
  473 |     await ready(page, '/counter', theme);
  474 |     const shell = page.locator('.fc-shell-root');
  475 |     await evidence(page, shell, `shell-chrome-${theme}`, info);
  476 |     await page.getByTitle('Change theme').first().click();
  477 |     const themeMenu = page.getByRole('menu').first();
  478 |     await expect(themeMenu).toHaveAttribute('focusgroup', 'menu');
  479 |     await settlePaint(page);
  480 |     await themeMenu.getByRole('menuitem', { name: 'Light', exact: true }).focus();
  481 |     await page.keyboard.press('ArrowDown');
  482 |     const darkItem = themeMenu.getByRole('menuitem', { name: 'Dark', exact: true });
  483 |     await expectFocusVisible(darkItem);
  484 |     await evidence(page, themeMenu, `theme-menu-${theme}`, info);
  485 |     await expectSurfaceContrast(darkItem, 'focus');
  486 |     await expectNoBlockingAxeViolations(page, { route: '/counter#theme-menu', artifactPath: info.outputPath(`axe-shell-menu-${theme}.json`) });
  487 |     await page.keyboard.press('Escape');
  488 |     await page.setViewportSize({ width: 1600, height: 900 });
  489 |     const rail = page.getByTestId('fc-shell-navigation').getByTestId('fc-navigation-rail');
  490 |     await expect(rail).toHaveAttribute('data-rail-width', '72');
  491 |     await expect.poll(async () => (await rail.boundingBox())?.width).toBe(72);
  492 |     await evidence(page, shell, `rail-labeled-${theme}`, info, rail);
  493 |     await page.getByTestId('fc-hamburger-toggle').click();
  494 |     await expect(rail).toHaveAttribute('data-rail-width', '48');
  495 |     await expect.poll(async () => (await rail.boundingBox())?.width).toBe(48);
  496 |     await evidence(page, shell, `rail-icons-${theme}`, info, rail);
  497 |   });
  498 | 
  499 |   test(`active toolbar, tabs, palette and settings states in ${theme}`, async ({ page }, info) => {
  500 |     await ready(page, '/__frontcomposer/specimens/page-toolbar', theme);
  501 |     const toolbar = new PageToolbarSpecimenPage(page);
  502 |     await toolbar.searchInput.focus();
  503 |     await page.keyboard.press('Shift+Tab');
  504 |     await page.keyboard.press('Tab');
  505 |     await expectFocusVisible(toolbar.searchInput);
  506 |     await expectSurfaceContrast(page.getByTestId('fc-page-toolbar-search'), 'focus');
  507 |     await toolbar.filterTrigger.click();
  508 |     await expect(toolbar.filterContent).toBeVisible();
  509 |     await evidence(page, toolbar.filterContent, `filter-popover-${theme}`, info);
  510 |     await toolbar.filterTrigger.click();
  511 |     await toolbar.viewTrigger.click();
  512 |     await evidence(page, toolbar.viewDensityItem, `view-menu-${theme}`, info);
  513 |     await page.keyboard.press('Escape');
  514 |     await toolbar.summaryTab.focus();
  515 |     await toolbar.summaryTab.press('ArrowRight');
  516 |     await expect(toolbar.activityTab).toHaveAttribute('aria-selected', 'true');
  517 |     await expectFocusVisible(toolbar.activityTab);
  518 |     await evidence(page, toolbar.tabs, `tabs-active-${theme}`, info);
  519 |     await page.getByTestId('fc-palette-trigger').click();
  520 |     const palette = page.getByTestId('fc-palette-root');
  521 |     await evidence(page, palette, `palette-default-${theme}`, info);
  522 |     const search = page.getByTestId('fc-palette-search').locator('input');
  523 |     await search.fill('shortcuts');
  524 |     const informationalShortcut = palette.locator('.fc-palette-option[aria-disabled="true"]').first();
  525 |     await expect(informationalShortcut).toBeVisible();
  526 |     await page.emulateMedia({ forcedColors: 'active' });
  527 |     if (info.project.name === 'webkit') {
  528 |       await info.attach('palette-disabled-forced-palette-capability-limit', { body: 'WebKit does not replace authored colors with system palette paint in this lane.', contentType: 'text/plain' });
  529 |     } else {
  530 |       const colors = await informationalShortcut.evaluate(element => {
  531 |         const system = document.createElement('span');
  532 |         system.style.color = 'GrayText';
  533 |         element.append(system);
  534 |         const expected = getComputedStyle(system).color;
  535 |         system.remove();
  536 |         return { expected, label: getComputedStyle(element.querySelector('.fc-palette-option-label')!).color, hint: getComputedStyle(element.querySelector('.fc-palette-option-hint')!).color };
  537 |       });
  538 |       expect(colors.label).toBe(colors.expected);
  539 |       expect(colors.hint).toBe(colors.expected);
  540 |     }
  541 |     await page.emulateMedia({ forcedColors: 'none' });
  542 |     await search.fill('counter');
  543 |     await expect(palette.getByRole('option')).toHaveText(['CounterProjection', 'ConfigureCounterCommand']);
  544 |     const selectedProjection = palette.getByRole('option', { name: 'CounterProjection', exact: true });
  545 |     await expect(selectedProjection).toHaveClass(/(^|\s)fc-palette-option-selected(\s|$)/u);
  546 |     await expect(selectedProjection).toHaveAttribute('aria-selected', 'true');
  547 |     await expect(selectedProjection).toBeInViewport({ ratio: 1 });
  548 |     await settlePaint(page);
  549 |     await evidence(page, palette, `palette-selected-${theme}`, info);
  550 |     await expectSurfaceContrast(palette.locator('.fc-palette-option-selected'), 'cue');
  551 |     await search.fill('no-match-accessibility-136');
  552 |     await expect(palette.locator('.fc-palette-noresults')).toBeVisible();
  553 |     await evidence(page, palette, `palette-empty-${theme}`, info);
  554 |     await page.emulateMedia({ forcedColors: 'active' });
  555 |     await expect(palette.locator('.fc-palette-noresults')).toContainText(/No/);
  556 |     await page.emulateMedia({ forcedColors: 'none' });
  557 |     await page.keyboard.press('Escape');
  558 |     await expect(palette).toHaveCount(0);
  559 |     await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  560 |     await page.keyboard.press('Tab');
> 561 |     await expect(page.getByTestId('fc-settings-button')).toBeFocused();
      |                                                          ^ Error: expect(locator).toBeFocused() failed
  562 |     await page.keyboard.press('Enter');
  563 |     const settings = page.getByTestId('fc-settings-dialog');
  564 |     await expect(page.locator('#fc-settings-heading')).toBeFocused();
  565 |     await page.keyboard.press('Tab');
  566 |     await page.keyboard.press('Tab');
  567 |     const radio = settings.locator('fluent-radio:focus');
  568 |     await expectFocusVisible(radio);
  569 |     await evidence(page, settings, `settings-${theme}`, info);
  570 |     await info.attach(`settings-radio-focus-${theme}`, { body: JSON.stringify(await expectSurfaceContrast(radio, 'focus')), contentType: 'application/json' });
  571 |     const exceptions: import('../helpers/responsive-accessibility.js').TargetException[] = ['Compact', 'Comfortable', 'Roomy'].map(value => ({ selector: `fluent-radio[name="fc-density-select"][value="${value}"]`, kind: 'Spacing', rationale: 'Fluent radio with independently measured 24px spacing circle.', keyboardPath: 'Settings → Density; Tab and arrows', evidence: `settings-${theme} target inventory` }));
  572 |     const targets = await expectTargetSizes(page, exceptions);
  573 |     await info.attach(`settings-targets-${theme}`, { body: JSON.stringify(targets), contentType: 'application/json' });
  574 |     await page.emulateMedia({ forcedColors: 'active' });
  575 |     await expect(settings).toContainText('Display density');
  576 |     await page.keyboard.press('Escape');
  577 |     await expect(page.getByTestId('fc-settings-button')).toBeFocused();
  578 |   });
  579 | 
  580 |   test(`generated validation, policy, destructive and abandonment states in ${theme}`, async ({ page }, info) => {
  581 |     await ready(page, `/__frontcomposer/specimens/type?theme=${theme}`, theme);
  582 |     const destructive = page.getByTestId('fc-destructive-command-specimen');
  583 |     await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).click();
  584 |     // Required retention deliberately stays empty: the genuine generated validation summary owns focus.
  585 |     const summary = destructive.locator('[data-testid="fc-validation-summary"]');
  586 |     await expect(summary).toBeVisible();
  587 |     await evidence(page, summary, `generated-validation-${theme}`, info);
  588 |     await evidence(page, page.getByTestId('fc-policy-command-specimen'), `policy-denied-${theme}`, info, undefined, { phase: 'operator-focus-before-policy-surface-scroll' });
  589 |     const form = destructive.locator('.fc-command-form');
  590 |     await form.getByRole('combobox').click();
  591 |     await form.getByRole('combobox').press('ArrowDown');
  592 |     await form.getByRole('combobox').press('Enter');
  593 |     await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).focus();
  594 |     await page.keyboard.press('Shift+Tab');
  595 |     await page.keyboard.press('Tab');
  596 |     await page.keyboard.press('Enter');
  597 |     const dialog = page.getByRole('alertdialog');
  598 |     await expect(dialog).toBeVisible();
  599 |     await evidence(page, dialog, `destructive-dialog-${theme}`, info);
  600 |     await expectTargetSizes(page);
  601 |     await page.keyboard.press('Escape');
  602 | 
  603 |     await ready(page, '/commands/Counter/ConfigureCounterCommand', theme);
  604 |     const configure = page.locator('.fc-command-form[aria-label="Configure Counter command form"]');
  605 |     await fillFieldByLabel(configure, 'Name', 'Accessibility specimen');
  606 |     await page.waitForTimeout(6_500);
  607 |     await page.getByRole('link', { name: 'Counter', exact: true }).last().focus();
  608 |     await page.keyboard.press('Shift+Tab');
  609 |     await page.keyboard.press('Tab');
  610 |     await page.keyboard.press('Enter');
  611 |     const warning = page.getByTestId('fc-form-abandonment-warning');
  612 |     await expect(warning).toBeVisible();
  613 |     await expect(page.getByTestId('fc-form-abandonment-stay')).toBeFocused();
  614 |     await evidence(page, warning, `abandonment-${theme}`, info);
  615 |     await expectFocusVisible(page.getByTestId('fc-form-abandonment-stay'));
  616 |     await page.keyboard.press('Escape');
  617 |     await expect(warning).toHaveCount(0);
  618 |     await expect(configure).toContainText('Name');
  619 |   });
  620 | }
  621 | 
  622 | for (const theme of ['light', 'dark']) {
  623 |   test(`projection reconnect fallback stale slow cap and filter cues in ${theme}`, async ({ page }, info) => {
  624 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  625 |     const grid = page.getByTestId('fc-generated-counter-grid');
  626 |     for (const connection of ['reconnecting', 'disconnected', 'connected']) {
  627 |       await page.getByTestId(`fc-ae-connection-${connection}`).click();
  628 |       const notice = page.getByTestId('fc-projection-connection-status');
  629 |       await expect(notice).toBeVisible();
  630 |       await evidence(page, notice, `connection-${connection}-${theme}`, info, notice, { phase: 'operator-focus-before-projection-surface-scroll' });
  631 |       if (connection !== 'connected') {
  632 |         await expect(grid.getByTestId('fc-projection-stale-notice')).toBeVisible();
  633 |         await evidence(page, grid.getByTestId('fc-projection-stale-notice'), `stale-${connection}-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
  634 |       }
  635 |     }
  636 |     await page.getByTestId('fc-ae-page-start').click();
  637 |     const slow = grid.getByTestId('fc-slow-query-notice');
  638 |     await expect(slow).toBeVisible({ timeout: 5_000 });
  639 |     await evidence(page, slow, `slow-query-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
  640 |     await page.getByTestId('fc-ae-page-cap').click();
  641 |     await expect(slow).toHaveCount(0);
  642 |     const cap = grid.getByTestId('fc-max-items-cap-notice');
  643 |     await expect(cap).toBeVisible();
  644 |     await evidence(page, cap, `result-limit-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
  645 |     await page.getByTestId('fc-ae-filter-empty').click();
  646 |     await expect(page.getByTestId('fc-filter-empty-state')).toBeVisible();
  647 |     await evidence(page, page.getByTestId('fc-filter-empty-state'), `filter-empty-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
  648 |     await page.getByTestId('fc-ae-filter-reset').click();
  649 |     await expect(page.getByTestId('fc-filter-empty-state')).toHaveCount(0);
  650 |   });
  651 | }
  652 | 
  653 | for (const theme of ['light', 'dark']) {
  654 |   test(`real generated filter-hidden detail stays distinct from missing-item results in ${theme}`, async ({ page }, info) => {
  655 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  656 |     const grid = page.getByTestId('fc-badge-grid');
  657 |     await grid.getByRole('gridcell', { name: 'FC-1000', exact: true }).click();
  658 |     await expect(grid.locator('.fc-expand-in-row-detail')).toContainText('FC-1000');
  659 |     await grid.locator('[data-fc-status-chip="Success"]').click();
  660 |     const clear = grid.getByTestId('fc-expanded-row-hidden-banner-clear');
  661 |     await expect(clear).toBeFocused();
```