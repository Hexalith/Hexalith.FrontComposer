# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> active toolbar, tabs, palette and settings states in light
- Location: specs/accessibility-state-matrix.spec.ts:510:3

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
    24 × locator resolved to <fluent-button icon-only="" tabindex="0" role="button" appearance="subtle" title="Open settings" aria-label="Open settings" data-testid="fc-settings-button" _bl_a4579e9a-c703-45b4-b6e3-4f8739d856e8="">…</fluent-button>
       - unexpected value "inactive"

```

```yaml
- button "Open settings"
```

# Test source

```ts
  472 |   const selected = page.getByRole('tab');
  473 |   await expectSurfaceContrast(selected);
  474 |   await expectSurfaceContrast(selected, 'border');
  475 |   await page.emulateMedia({ forcedColors: 'active' });
  476 |   await expect(selected).toHaveAttribute('aria-selected', 'true');
  477 |   await expectSurfaceContrast(selected);
  478 |   await expect(expectSurfaceContrast(selected, 'border')).rejects.toThrow(/Nonempty/);
  479 | });
  480 | 
  481 | for (const theme of ['light', 'dark']) {
  482 |   test(`shell chrome, theme menu and exact desktop navigation rails in ${theme}`, async ({ page }, info) => {
  483 |     test.setTimeout(120_000);
  484 |     await ready(page, '/counter', theme);
  485 |     const shell = page.locator('.fc-shell-root');
  486 |     await evidence(page, shell, `shell-chrome-${theme}`, info);
  487 |     await page.getByTitle('Change theme').first().click();
  488 |     const themeMenu = page.getByRole('menu').first();
  489 |     await expect(themeMenu).toHaveAttribute('focusgroup', 'menu');
  490 |     await settlePaint(page);
  491 |     await themeMenu.getByRole('menuitem', { name: 'Light', exact: true }).focus();
  492 |     await page.keyboard.press('ArrowDown');
  493 |     const darkItem = themeMenu.getByRole('menuitem', { name: 'Dark', exact: true });
  494 |     await expectFocusVisible(darkItem);
  495 |     await evidence(page, themeMenu, `theme-menu-${theme}`, info);
  496 |     await expectSurfaceContrast(darkItem, 'focus');
  497 |     await expectNoBlockingAxeViolations(page, { route: '/counter#theme-menu', artifactPath: info.outputPath(`axe-shell-menu-${theme}.json`) });
  498 |     await page.keyboard.press('Escape');
  499 |     await page.setViewportSize({ width: 1600, height: 900 });
  500 |     const rail = page.getByTestId('fc-shell-navigation').getByTestId('fc-navigation-rail');
  501 |     await expect(rail).toHaveAttribute('data-rail-width', '72');
  502 |     await expect.poll(async () => (await rail.boundingBox())?.width).toBe(72);
  503 |     await evidence(page, shell, `rail-labeled-${theme}`, info, rail);
  504 |     await page.getByTestId('fc-hamburger-toggle').click();
  505 |     await expect(rail).toHaveAttribute('data-rail-width', '48');
  506 |     await expect.poll(async () => (await rail.boundingBox())?.width).toBe(48);
  507 |     await evidence(page, shell, `rail-icons-${theme}`, info, rail);
  508 |   });
  509 | 
  510 |   test(`active toolbar, tabs, palette and settings states in ${theme}`, async ({ page }, info) => {
  511 |     await ready(page, '/__frontcomposer/specimens/page-toolbar', theme);
  512 |     const toolbar = new PageToolbarSpecimenPage(page);
  513 |     await toolbar.searchInput.focus();
  514 |     await page.keyboard.press('Shift+Tab');
  515 |     await page.keyboard.press('Tab');
  516 |     await expectFocusVisible(toolbar.searchInput);
  517 |     await expectSurfaceContrast(page.getByTestId('fc-page-toolbar-search'), 'focus');
  518 |     await toolbar.filterTrigger.click();
  519 |     await expect(toolbar.filterContent).toBeVisible();
  520 |     await evidence(page, toolbar.filterContent, `filter-popover-${theme}`, info);
  521 |     await toolbar.filterTrigger.click();
  522 |     await toolbar.viewTrigger.click();
  523 |     await evidence(page, toolbar.viewDensityItem, `view-menu-${theme}`, info);
  524 |     await page.keyboard.press('Escape');
  525 |     await toolbar.summaryTab.focus();
  526 |     await toolbar.summaryTab.press('ArrowRight');
  527 |     await expect(toolbar.activityTab).toHaveAttribute('aria-selected', 'true');
  528 |     await expectFocusVisible(toolbar.activityTab);
  529 |     await evidence(page, toolbar.tabs, `tabs-active-${theme}`, info);
  530 |     await page.getByTestId('fc-palette-trigger').click();
  531 |     const palette = page.getByTestId('fc-palette-root');
  532 |     await evidence(page, palette, `palette-default-${theme}`, info);
  533 |     const search = page.getByTestId('fc-palette-search').locator('input');
  534 |     await search.fill('shortcuts');
  535 |     const informationalShortcut = palette.locator('.fc-palette-option[aria-disabled="true"]').first();
  536 |     await expect(informationalShortcut).toBeVisible();
  537 |     await page.emulateMedia({ forcedColors: 'active' });
  538 |     if (info.project.name === 'webkit') {
  539 |       await info.attach('palette-disabled-forced-palette-capability-limit', { body: 'WebKit does not replace authored colors with system palette paint in this lane.', contentType: 'text/plain' });
  540 |     } else {
  541 |       const colors = await informationalShortcut.evaluate(element => {
  542 |         const system = document.createElement('span');
  543 |         system.style.color = 'GrayText';
  544 |         element.append(system);
  545 |         const expected = getComputedStyle(system).color;
  546 |         system.remove();
  547 |         return { expected, label: getComputedStyle(element.querySelector('.fc-palette-option-label')!).color, hint: getComputedStyle(element.querySelector('.fc-palette-option-hint')!).color };
  548 |       });
  549 |       expect(colors.label).toBe(colors.expected);
  550 |       expect(colors.hint).toBe(colors.expected);
  551 |     }
  552 |     await page.emulateMedia({ forcedColors: 'none' });
  553 |     await search.fill('counter');
  554 |     await expect(palette.getByRole('option')).toHaveText(['CounterProjection', 'ConfigureCounterCommand']);
  555 |     const selectedProjection = palette.getByRole('option', { name: 'CounterProjection', exact: true });
  556 |     await expect(selectedProjection).toHaveClass(/(^|\s)fc-palette-option-selected(\s|$)/u);
  557 |     await expect(selectedProjection).toHaveAttribute('aria-selected', 'true');
  558 |     await expect(selectedProjection).toBeInViewport({ ratio: 1 });
  559 |     await settlePaint(page);
  560 |     await evidence(page, palette, `palette-selected-${theme}`, info);
  561 |     await expectSurfaceContrast(palette.locator('.fc-palette-option-selected'), 'cue');
  562 |     await search.fill('no-match-accessibility-136');
  563 |     await expect(palette.locator('.fc-palette-noresults')).toBeVisible();
  564 |     await evidence(page, palette, `palette-empty-${theme}`, info);
  565 |     await page.emulateMedia({ forcedColors: 'active' });
  566 |     await expect(palette.locator('.fc-palette-noresults')).toContainText(/No/);
  567 |     await page.emulateMedia({ forcedColors: 'none' });
  568 |     await page.keyboard.press('Escape');
  569 |     await expect(palette).toHaveCount(0);
  570 |     await expect(page.getByTestId('fc-palette-trigger')).toBeFocused();
  571 |     await page.keyboard.press('Tab');
> 572 |     await expect(page.getByTestId('fc-settings-button')).toBeFocused();
      |                                                          ^ Error: expect(locator).toBeFocused() failed
  573 |     await page.keyboard.press('Enter');
  574 |     const settings = page.getByTestId('fc-settings-dialog');
  575 |     await expect(page.locator('#fc-settings-heading')).toBeFocused();
  576 |     await page.keyboard.press('Tab');
  577 |     await page.keyboard.press('Tab');
  578 |     const radio = settings.locator('fluent-radio:focus');
  579 |     await expectFocusVisible(radio);
  580 |     await evidence(page, settings, `settings-${theme}`, info);
  581 |     await info.attach(`settings-radio-focus-${theme}`, { body: JSON.stringify(await expectSurfaceContrast(radio, 'focus')), contentType: 'application/json' });
  582 |     const exceptions: import('../helpers/responsive-accessibility.js').TargetException[] = ['Compact', 'Comfortable', 'Roomy'].map(value => ({ selector: `fluent-radio[name="fc-density-select"][value="${value}"]`, kind: 'Spacing', rationale: 'Fluent radio with independently measured 24px spacing circle.', keyboardPath: 'Settings → Density; Tab and arrows', evidence: `settings-${theme} target inventory` }));
  583 |     const targets = await expectTargetSizes(page, exceptions);
  584 |     await info.attach(`settings-targets-${theme}`, { body: JSON.stringify(targets), contentType: 'application/json' });
  585 |     await page.emulateMedia({ forcedColors: 'active' });
  586 |     await expect(settings).toContainText('Display density');
  587 |     await page.keyboard.press('Escape');
  588 |     await expect(page.getByTestId('fc-settings-button')).toBeFocused();
  589 |   });
  590 | 
  591 |   test(`generated validation, policy, destructive and abandonment states in ${theme}`, async ({ page }, info) => {
  592 |     await ready(page, `/__frontcomposer/specimens/type?theme=${theme}`, theme);
  593 |     const destructive = page.getByTestId('fc-destructive-command-specimen');
  594 |     await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).click();
  595 |     // Required retention deliberately stays empty: the genuine generated validation summary owns focus.
  596 |     const summary = destructive.locator('[data-testid="fc-validation-summary"]');
  597 |     await expect(summary).toBeVisible();
  598 |     await evidence(page, summary, `generated-validation-${theme}`, info);
  599 |     await evidence(page, page.getByTestId('fc-policy-command-specimen'), `policy-denied-${theme}`, info, undefined, { phase: 'operator-focus-before-policy-surface-scroll' });
  600 |     const form = destructive.locator('.fc-command-form');
  601 |     await form.getByRole('combobox').click();
  602 |     await form.getByRole('combobox').press('ArrowDown');
  603 |     await form.getByRole('combobox').press('Enter');
  604 |     await destructive.getByRole('button', { name: 'Purge Specimen Record', exact: true }).focus();
  605 |     await page.keyboard.press('Shift+Tab');
  606 |     await page.keyboard.press('Tab');
  607 |     await page.keyboard.press('Enter');
  608 |     const dialog = page.getByRole('alertdialog');
  609 |     await expect(dialog).toBeVisible();
  610 |     await evidence(page, dialog, `destructive-dialog-${theme}`, info);
  611 |     await expectTargetSizes(page);
  612 |     await page.keyboard.press('Escape');
  613 | 
  614 |     await ready(page, '/commands/Counter/ConfigureCounterCommand', theme);
  615 |     const configure = page.locator('.fc-command-form[aria-label="Configure Counter command form"]');
  616 |     await fillFieldByLabel(configure, 'Name', 'Accessibility specimen');
  617 |     await page.waitForTimeout(6_500);
  618 |     await page.getByRole('link', { name: 'Counter', exact: true }).last().focus();
  619 |     await page.keyboard.press('Shift+Tab');
  620 |     await page.keyboard.press('Tab');
  621 |     await page.keyboard.press('Enter');
  622 |     const warning = page.getByTestId('fc-form-abandonment-warning');
  623 |     await expect(warning).toBeVisible();
  624 |     await expect(page.getByTestId('fc-form-abandonment-stay')).toBeFocused();
  625 |     await evidence(page, warning, `abandonment-${theme}`, info);
  626 |     await expectFocusVisible(page.getByTestId('fc-form-abandonment-stay'));
  627 |     await page.keyboard.press('Escape');
  628 |     await expect(warning).toHaveCount(0);
  629 |     await expect(configure).toContainText('Name');
  630 |   });
  631 | }
  632 | 
  633 | for (const theme of ['light', 'dark']) {
  634 |   test(`projection reconnect fallback stale slow cap and filter cues in ${theme}`, async ({ page }, info) => {
  635 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  636 |     const grid = page.getByTestId('fc-generated-counter-grid');
  637 |     for (const connection of ['reconnecting', 'disconnected', 'connected']) {
  638 |       await page.getByTestId(`fc-ae-connection-${connection}`).click();
  639 |       const notice = page.getByTestId('fc-projection-connection-status');
  640 |       await expect(notice).toBeVisible();
  641 |       await evidence(page, notice, `connection-${connection}-${theme}`, info, notice, { phase: 'operator-focus-before-projection-surface-scroll' });
  642 |       if (connection !== 'connected') {
  643 |         await expect(grid.getByTestId('fc-projection-stale-notice')).toBeVisible();
  644 |         await evidence(page, grid.getByTestId('fc-projection-stale-notice'), `stale-${connection}-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
  645 |       }
  646 |     }
  647 |     await page.getByTestId('fc-ae-page-start').click();
  648 |     const slow = grid.getByTestId('fc-slow-query-notice');
  649 |     await expect(slow).toBeVisible({ timeout: 5_000 });
  650 |     await evidence(page, slow, `slow-query-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
  651 |     await page.getByTestId('fc-ae-page-cap').click();
  652 |     await expect(slow).toHaveCount(0);
  653 |     const cap = grid.getByTestId('fc-max-items-cap-notice');
  654 |     await expect(cap).toBeVisible();
  655 |     await evidence(page, cap, `result-limit-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
  656 |     await page.getByTestId('fc-ae-filter-empty').click();
  657 |     await expect(page.getByTestId('fc-filter-empty-state')).toBeVisible();
  658 |     await evidence(page, page.getByTestId('fc-filter-empty-state'), `filter-empty-${theme}`, info, undefined, { phase: 'operator-focus-before-projection-surface-scroll' });
  659 |     await page.getByTestId('fc-ae-filter-reset').click();
  660 |     await expect(page.getByTestId('fc-filter-empty-state')).toHaveCount(0);
  661 |   });
  662 | }
  663 | 
  664 | for (const theme of ['light', 'dark']) {
  665 |   test(`real generated filter-hidden detail stays distinct from missing-item results in ${theme}`, async ({ page }, info) => {
  666 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  667 |     const grid = page.getByTestId('fc-badge-grid');
  668 |     await grid.getByRole('gridcell', { name: 'FC-1000', exact: true }).click();
  669 |     await expect(grid.locator('.fc-expand-in-row-detail')).toContainText('FC-1000');
  670 |     await grid.locator('[data-fc-status-chip="Success"]').click();
  671 |     const clear = grid.getByTestId('fc-expanded-row-hidden-banner-clear');
  672 |     await expect(clear).toBeFocused();
```