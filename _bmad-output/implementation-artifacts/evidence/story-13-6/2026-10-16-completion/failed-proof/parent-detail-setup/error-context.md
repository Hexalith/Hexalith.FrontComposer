# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> delayed generated detail recovery cancels after operator focus and navigation in dark
- Location: specs/accessibility-state-matrix.spec.ts:475:3

# Error details

```
TimeoutError: locator.click: Timeout 15000ms exceeded.
Call log:
  - waiting for getByTestId('fc-generated-counter-grid').getByRole('gridcell', { name: 'FC-AE-1', exact: true })

```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Skip to content" [ref=e3] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact&theme=dark#fc-main-content
  - link "Skip to navigation" [ref=e4] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact&theme=dark#fc-nav
  - generic [ref=e6]:
    - banner [ref=e7]:
      - generic [ref=e8]:
        - generic [ref=e9]:
          - generic [ref=e10]:
            - button "Toggle navigation" [ref=e12] [cursor=pointer]
            - generic:
              - generic:
                - generic: Counter
                - generic: Default
                - generic: Specimens
          - generic [ref=e16]: Hexalith FrontComposer
        - generic [ref=e17]:
          - button "Change theme" [ref=e19] [cursor=pointer]:
            - generic [ref=e20]:
              - generic:
                - generic [ref=e21]: "Change theme: Dark"
                - generic [aria-hidden] [ref=e22]: Dark
          - button "Open command palette" [ref=e26] [cursor=pointer]
          - button "Open settings" [ref=e30] [cursor=pointer]
          - button "FU" [ref=e35] [cursor=pointer]
    - generic [ref=e37]:
      - status [ref=e38]: Density set to Comfortable.
      - main [ref=e39]:
        - status [ref=e40]
        - status [ref=e41]
        - main [ref=e43]:
          - generic [ref=e44]:
            - paragraph [ref=e45]: FrontComposer accessibility specimen
            - heading "Type, theme, density, lifecycle, and navigation specimen" [level=1] [ref=e46]
            - paragraph [ref=e47]: Generated shell surfaces rendered with deterministic local data.
            - link "Skip to command form" [ref=e48] [cursor=pointer]:
              - /url: "#fc-specimen-command-form"
          - region [ref=e49]:
            - heading "Theme and density controls" [level=2] [ref=e50]
            - generic [ref=e51]:
              - link "light" [ref=e52] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=compact&generatedGrid=true&accessibilityScenarios=true
              - link "dark" [ref=e53] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=compact&generatedGrid=true&accessibilityScenarios=true
              - link "compact" [ref=e54] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=compact&generatedGrid=true&accessibilityScenarios=true
              - link "comfortable" [ref=e55] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=comfortable&generatedGrid=true&accessibilityScenarios=true
              - link "roomy" [ref=e56] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=roomy&generatedGrid=true&accessibilityScenarios=true
          - region [ref=e57]:
            - heading "Type ramp" [level=2] [ref=e58]
            - generic [ref=e59]:
              - article [ref=e60]:
                - paragraph [ref=e61]: Display
                - generic [ref=e62]: FrontComposer Display
              - article [ref=e63]:
                - paragraph [ref=e64]: Title
                - generic [ref=e65]: Generated command title
              - article [ref=e66]:
                - paragraph [ref=e67]: Subtitle
                - generic [ref=e68]: Projection subtitle and context
              - article [ref=e69]:
                - paragraph [ref=e70]: Body
                - generic [ref=e71]: Readable generated body text
              - article [ref=e72]:
                - paragraph [ref=e73]: Caption
                - generic [ref=e74]: Supplemental metadata caption
              - article [ref=e75]:
                - paragraph [ref=e76]: Code
                - generic [ref=e77]: FC-STATIC-0001
          - region [ref=e78]:
            - heading "Semantic color tokens" [level=2] [ref=e79]
            - generic [ref=e80]:
              - article [ref=e81]:
                - heading "light theme" [level=3] [ref=e82]
                - generic [ref=e83]:
                  - generic [ref=e85]: Neutral
                  - strong [ref=e86]: Available
                - generic [ref=e87]:
                  - generic [ref=e89]: Informational
                  - strong [ref=e90]: Queued
                - generic [ref=e91]:
                  - generic [ref=e93]: Success
                  - strong [ref=e94]: Confirmed
                - generic [ref=e95]:
                  - generic [ref=e97]: Warning
                  - strong [ref=e98]: Needs review
                - generic [ref=e99]:
                  - generic [ref=e101]: Danger
                  - strong [ref=e102]: Rejected
                - generic [ref=e103]:
                  - generic [ref=e105]: Accent
                  - strong [ref=e106]: Selected
              - article [ref=e107]:
                - heading "dark theme" [level=3] [ref=e108]
                - generic [ref=e109]:
                  - generic [ref=e111]: Neutral
                  - strong [ref=e112]: Available
                - generic [ref=e113]:
                  - generic [ref=e115]: Informational
                  - strong [ref=e116]: Queued
                - generic [ref=e117]:
                  - generic [ref=e119]: Success
                  - strong [ref=e120]: Confirmed
                - generic [ref=e121]:
                  - generic [ref=e123]: Warning
                  - strong [ref=e124]: Needs review
                - generic [ref=e125]:
                  - generic [ref=e127]: Danger
                  - strong [ref=e128]: Rejected
                - generic [ref=e129]:
                  - generic [ref=e131]: Accent
                  - strong [ref=e132]: Selected
          - region [ref=e133]:
            - heading "Density states" [level=2] [ref=e134]
            - generic [ref=e135]:
              - article [ref=e136]:
                - heading "compact density" [level=3] [ref=e137]
                - button "Primary action" [ref=e138]
                - generic [ref=e139]:
                  - text: Label
                  - textbox "compact density text input" [ref=e140]: compact sample
              - article [ref=e141]:
                - heading "comfortable density" [level=3] [ref=e142]
                - button "Primary action" [ref=e143]
                - generic [ref=e144]:
                  - text: Label
                  - textbox "comfortable density text input" [ref=e145]: comfortable sample
              - article [ref=e146]:
                - heading "roomy density" [level=3] [ref=e147]
                - button "Primary action" [ref=e148]
                - generic [ref=e149]:
                  - text: Label
                  - textbox "roomy density text input" [ref=e150]: roomy sample
          - region [ref=e151]:
            - heading "Generated projection DataGrid and badge states" [level=2] [ref=e152]
            - status [ref=e153]
            - paragraph [ref=e154]: 6 specimen status
            - group "Status filter chips" [ref=e155]:
              - button "Neutral, currently inactive" [ref=e156] [cursor=pointer]:
                - generic [ref=e157]: Neutral
              - button "Info, currently inactive" [ref=e158] [cursor=pointer]:
                - generic [ref=e159]: Info
              - button "Success, currently inactive" [ref=e160] [cursor=pointer]:
                - generic [ref=e161]: Success
              - button "Warning, currently inactive" [ref=e162] [cursor=pointer]:
                - generic [ref=e163]: Warning
              - button "Danger, currently inactive" [ref=e164] [cursor=pointer]:
                - generic [ref=e165]: Danger
              - button "Accent, currently inactive" [ref=e166] [cursor=pointer]:
                - generic [ref=e167]: Accent
            - text: 6 specimen status
            - region "specimen status" [ref=e168]:
              - table [ref=e169]:
                - rowgroup [ref=e170]:
                  - row [ref=e171]:
                    - columnheader "Id" [ref=e172]
                    - columnheader "Name" [ref=e176]
                    - columnheader "Status" [ref=e180]
                    - columnheader "Owner" [ref=e184]
                - rowgroup [ref=e188]:
                  - row [aria-hidden] [ref=e189]
              - region "Details for"
          - region "Automated accessibility scenarios" [ref=e190]:
            - heading "Fixed accessibility scenarios" [level=2] [ref=e191]
            - generic [ref=e192]:
              - generic [ref=e193] [cursor=pointer]: validation
              - generic [ref=e195] [cursor=pointer]: returned-rejection
              - generic [ref=e197] [cursor=pointer]: exception-rejection
              - generic [ref=e199] [cursor=pointer]: forbidden
              - generic [ref=e201] [cursor=pointer]: unavailable
              - generic [ref=e203] [cursor=pointer]: blank-unavailable
              - generic [ref=e205] [cursor=pointer]: rate-limited
              - generic [ref=e207] [cursor=pointer]: blank-rate-limited
              - generic [ref=e209] [cursor=pointer]: transport
              - generic [ref=e211] [cursor=pointer]: admission
              - generic [ref=e213] [cursor=pointer]: policy
              - generic [ref=e215] [cursor=pointer]: accepted
            - paragraph [ref=e217]: validation
            - generic [ref=e218]:
              - button "Run accessibility specimen" [ref=e219] [cursor=pointer]
              - status [ref=e223]
            - status "New item added outside current filters" [ref=e225]: New item. It may not match current filters yet.
            - generic [ref=e226]:
              - generic [ref=e227] [cursor=pointer]: Idle
              - generic [ref=e229] [cursor=pointer]: Submitting
              - generic [ref=e231] [cursor=pointer]: Acknowledged
              - generic [ref=e233] [cursor=pointer]: Syncing
              - generic [ref=e235] [cursor=pointer]: Confirmed
              - generic [ref=e237] [cursor=pointer]: Rejected
              - generic [ref=e239] [cursor=pointer]: IdempotentConfirmed
              - generic [ref=e241] [cursor=pointer]: NeedsReview
              - generic [ref=e243] [cursor=pointer]: Warning
              - generic [ref=e245] [cursor=pointer]: Degraded
              - generic [ref=e247] [cursor=pointer]: DegradedExhausted
            - generic [ref=e249]:
              - generic: Observable lifecycle specimen
              - status [ref=e250]
            - generic [ref=e251]:
              - generic [ref=e252] [cursor=pointer]: Connected
              - generic [ref=e254] [cursor=pointer]: Reconnecting
              - generic [ref=e256] [cursor=pointer]: Disconnected
              - generic [ref=e258] [cursor=pointer]: Start held page
              - generic [ref=e260] [cursor=pointer]: Complete held page
              - generic [ref=e262] [cursor=pointer]: Complete capped page
              - generic [ref=e264] [cursor=pointer]: Apply no-match filter
              - generic [ref=e266] [cursor=pointer]: Reset filter
            - generic [ref=e268]:
              - generic [active] [ref=e269] [cursor=pointer]: Load two rows
              - generic [ref=e271] [cursor=pointer]: Complete missing row result
              - generic [ref=e273] [cursor=pointer]: Complete empty result
              - generic [ref=e275] [cursor=pointer]: Complete failed result
              - generic [ref=e277] [cursor=pointer]: Start pending result
              - generic [ref=e279] [cursor=pointer]: Complete delayed missing result
              - generic [ref=e281] [cursor=pointer]: Repeat completed result
          - region [ref=e283]:
            - heading "Generated Counter projection grid" [level=2] [ref=e284]
            - status [ref=e285]
            - paragraph [ref=e286]: 2 counters
            - text: 2 counters
            - region "counters" [ref=e287]:
              - table [ref=e288]:
                - rowgroup [ref=e289]:
                  - row [ref=e290]:
                    - columnheader "Id" [ref=e291]
                    - columnheader "Count" [ref=e295]
                    - columnheader "Last changed" [ref=e299]
                    - columnheader "Metadata (custom renderer needed)" [ref=e303]
                - rowgroup [ref=e307]:
                  - row [aria-hidden] [ref=e308]
              - region "Details for"
          - region [ref=e309]:
            - heading "Command form and lifecycle states" [level=2] [ref=e310]
            - form "Specimen command form" [ref=e311]:
              - generic [ref=e312]:
                - text: Counter name
                - textbox "Counter name" [ref=e313]: Specimen counter
              - generic [ref=e314]:
                - text: Amount
                - spinbutton "Amount" [ref=e315]: "3"
              - button "Submit command" [ref=e316]
              - button "Cancel" [ref=e317]
            - list "Five command lifecycle states" [ref=e318]:
              - listitem [ref=e319]:
                - strong [ref=e320]: Idle
                - text: Ready to submit.
              - listitem [ref=e321]:
                - strong [ref=e322]: Submitting
                - text: Submitting command.
              - listitem [ref=e323]:
                - strong [ref=e324]: Acknowledged
                - text: Command accepted by the host.
              - listitem [ref=e325]:
                - strong [ref=e326]: Syncing
                - text: Waiting for projection synchronization.
              - listitem [ref=e327]:
                - strong [ref=e328]: Confirmed or rejected
                - text: Terminal confirmation and rejection states remain text-visible.
          - region [ref=e329]:
            - heading "Destructive command confirmation" [level=2] [ref=e330]
            - group [ref=e332]:
              - generic "Purge Specimen Record command form" [ref=e333]:
                - generic [ref=e334]:
                  - generic [ref=e335]:
                    - group "Purge details" [ref=e336]:
                      - generic [ref=e339]:
                        - generic: Record Id
                        - generic "Record Id" [ref=e341]:
                          - textbox "Record Id, Required" [ref=e343]: FC-1002
                        - generic: Record to purge.
                      - generic [ref=e346]:
                        - generic: Reason
                        - generic "Reason" [ref=e348]:
                          - textbox "Reason, Required" [ref=e350]: QA destructive confirmation coverage
                        - generic: Why this purge is required.
                    - generic [ref=e353]:
                      - generic: Retention
                      - generic "Retention" [ref=e355] [cursor=pointer]:
                        - combobox "Retention" [ref=e357]
                      - generic: How long the purged record stays recoverable.
                    - button "Purge Specimen Record" [ref=e362] [cursor=pointer]
                  - status [ref=e364]
          - region [ref=e365]:
            - heading "Policy-gated command authorization" [level=2] [ref=e366]
            - group [ref=e368]:
              - generic "Policy Allowed Specimen command form" [ref=e369]:
                - generic [ref=e370]:
                  - generic [ref=e371]:
                    - generic [ref=e373]:
                      - generic: Record Id
                      - generic "Record Id" [ref=e375]:
                        - textbox "Record Id, Required" [ref=e377]: FC-AUTH-ALLOW
                    - generic [ref=e379]:
                      - generic: Reason
                      - generic "Reason" [ref=e381]:
                        - textbox "Reason, Required" [ref=e383]: QA policy allowed coverage
                    - button "Policy Allowed Specimen" [ref=e384] [cursor=pointer]
                  - status [ref=e386]
            - group [ref=e387]:
              - group [ref=e388]:
                - heading "Permission required" [level=2] [ref=e389]
                - paragraph [ref=e390]: You do not have permission to Policy Denied Specimen.
          - region [ref=e391]:
            - heading "Expanded detail view" [level=2] [ref=e392]
            - group [ref=e393]:
              - generic "Open projection details for FC-1002" [ref=e394]
              - generic [ref=e395]:
                - term [ref=e396]: Correlation
                - definition [ref=e397]: fc-correlation-0002
                - term [ref=e398]: Last synchronized
                - definition [ref=e399]: 2026-01-15 13:45 UTC
          - navigation "Specimen multi-level navigation" [ref=e400]:
            - heading "Multi-level navigation" [level=2] [ref=e401]
            - list [ref=e402]:
              - listitem [ref=e403]:
                - link "Foundation" [ref=e404] [cursor=pointer]:
                  - /url: "#fc-type-ramp-title"
              - listitem [ref=e405]:
                - button "Generated surfaces" [expanded] [ref=e406]
                - list [ref=e407]:
                  - listitem [ref=e408]:
                    - link "Projection grid" [ref=e409] [cursor=pointer]:
                      - /url: "#fc-badge-grid-title"
                  - listitem [ref=e410]:
                    - link "Command lifecycle" [ref=e411] [cursor=pointer]:
                      - /url: "#fc-specimen-command-title"
    - generic [ref=e412]: Hexalith FrontComposer © 2026
```

# Test source

```ts
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
  482 |   if (gate?.hold) {
  483 |     gate.entered = true;
  484 |     gate.capturedDetached = focusBeforeReplacement.get(key) instanceof HTMLElement && !focusBeforeReplacement.get(key).isConnected;
  485 |     await new Promise(resolve => { gate.release = () => { gate.released = true; resolve(); }; });
  486 |   }
  487 |   if (gate?.hold) gate.captureStillOwned = focusBeforeReplacement.has(key);
  488 |   const result = originalFocusReplacementHeading(key, headingId);
  489 |   if (gate?.hold) gate.returned = result;
  490 |   return result;
  491 | }` });
  492 |     });
  493 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  494 |     const grid = page.getByTestId('fc-generated-counter-grid');
  495 |     await page.getByTestId('fc-ae-rows-two').click();
> 496 |     await grid.getByRole('gridcell', { name: 'FC-AE-1', exact: true }).click();
      |                                                                        ^ TimeoutError: locator.click: Timeout 15000ms exceeded.
  497 |     const detail = grid.locator('.fc-expand-in-row-detail');
  498 |     await expect(detail).toContainText('FC-AE-1');
  499 |     await detail.locator('a,button,fluent-button,[tabindex="0"]').first().focus();
  500 |     const arm = async () => page.evaluate(() => { (window as typeof window & { __aeRecoveryGate: unknown }).__aeRecoveryGate = { hold: true, entered: false, released: false }; });
  501 |     const entered = async () => {
  502 |       await expect.poll(() => page.evaluate(() => (window as typeof window & { __aeRecoveryGate: { entered: boolean; capturedDetached?: boolean } }).__aeRecoveryGate)).toMatchObject({ entered: true, capturedDetached: true });
  503 |       await expect(detail.locator('a,button,fluent-button,[tabindex="0"]')).toHaveCount(0);
  504 |       await expect(grid.locator('.fc-expanded-row-hidden-banner-text')).toBeVisible();
  505 |     };
  506 |     const release = async () => {
  507 |       await page.evaluate(() => (window as typeof window & { __aeRecoveryGate: { release: () => void } }).__aeRecoveryGate.release());
  508 |       await expect.poll(() => page.evaluate(() => (window as typeof window & { __aeRecoveryGate: { returned?: boolean; captureStillOwned?: boolean } }).__aeRecoveryGate)).toMatchObject({ returned: false, captureStillOwned: false });
  509 |     };
  510 |     await arm();
  511 |     await page.getByTestId('fc-ae-rows-missing').dispatchEvent('click');
  512 |     await entered();
  513 |     await page.getByTestId('fc-ae-rows-two').focus();
  514 |     await release();
  515 |     await expect(page.getByTestId('fc-ae-rows-two')).toBeFocused();
  516 |     await page.getByTestId('fc-ae-rows-two').click();
  517 |     await expect(detail).toContainText('FC-AE-1');
  518 |     await detail.locator('a,button,fluent-button,[tabindex="0"]').first().focus();
  519 |     await arm();
  520 |     await page.getByTestId('fc-ae-rows-missing').dispatchEvent('click');
  521 |     await entered();
  522 |     // Keep the entered JS recovery alive across real in-app navigation, then release it.
  523 |     await page.evaluate(() => (window as typeof window & { Blazor: { navigateTo: (url: string) => void } }).Blazor.navigateTo('/home'));
  524 |     const heading = page.locator('#fc-main-content h1').first();
  525 |     await expect(page).toHaveURL(/\/home$/);
  526 |     await expect(heading).toBeFocused();
  527 |     await release();
  528 |     await expect(heading).toBeFocused();
  529 |     await expect(page.locator('.fc-expanded-row-hidden-banner-text')).toHaveCount(0);
  530 |   });
  531 | }
  532 | 
  533 | for (const theme of ['light', 'dark']) {
  534 |   test(`fresh-row presentation retains text boundary and system-color meaning in ${theme}`, async ({ page }, info) => {
  535 |     await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  536 |     const cue = page.getByTestId('fc-ae-fresh-presentation');
  537 |     await expect(cue).toContainText(/new|updated/i);
  538 |     await evidence(page, cue, `fresh-presentation-${theme}`, info);
  539 |     await expectSurfaceContrast(cue, 'border');
  540 |   });
  541 | }
  542 | 
  543 | test('paint measurement follows slots, ignores unpainted pseudo borders and sees native modal text through inert ancestors', async ({ page }) => {
  544 |   await page.setContent('<style>#edge{border:1px solid #eee;background:white}#edge::after{content:"";border-bottom:2px solid black;transform:scaleX(0);display:block}</style><div id="host" style="color:black;background:black">Slotted readable label</div><div id="edge">Boundary</div><div inert><dialog id="modal"><p>Visible modal text</p></dialog></div>');
  545 |   await page.locator('#host').evaluate(element => { element.attachShadow({ mode: 'open' }).innerHTML = '<style>slot{color:white}</style><slot></slot>'; });
  546 |   expect((await expectSurfaceContrast(page.locator('#host')))[0].ratio).toBe(21);
  547 |   expect((await expectSurfaceContrast(page.locator('#host').locator('slot')))[0].ratio).toBe(21);
  548 |   await expect(expectSurfaceContrast(page.locator('#edge'), 'border')).rejects.toThrow(/contrast failures/);
  549 |   await page.addStyleTag({ content: '#edge::after{transform:scaleX(1)}' });
  550 |   expect((await expectSurfaceContrast(page.locator('#edge'), 'border')).some(sample => sample.ratio >= 3)).toBe(true);
  551 |   await page.locator('#modal').evaluate(element => (element as HTMLDialogElement).showModal());
  552 |   await expectSurfaceContrast(page.locator('#modal'));
  553 | });
  554 | 
  555 | 
  556 | test('validation links keep native same-page href activation when focus enhancement is unavailable', async ({ page }, info) => {
  557 |   await page.route('**/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js', async route => {
  558 |     const response = await route.fetch();
  559 |     const body = (await response.text())
  560 |       .replace('export function focusValidationOutcome(', 'export function unavailableFocusValidationOutcome(')
  561 |       .replace('export function focusValidationTarget(', 'export function unavailableFocusValidationTarget(')
  562 |       + "\nexport function focusValidationOutcome(){throw new Error('Fixed focus-enhancement fixture unavailable');}\nexport function focusValidationTarget(){throw new Error('Fixed focus-enhancement fixture unavailable');}\n";
  563 |     await route.fulfill({ response, body });
  564 |   });
  565 |   await ready(page, '/__frontcomposer/specimens/type', 'light');
  566 |   await page.getByTestId('fc-destructive-command-specimen').getByRole('button', { name: 'Purge Specimen Record', exact: true }).click();
  567 |   const link = page.getByTestId('fc-validation-summary').getByRole('link', { name: 'The Retention field is required.', exact: true });
  568 |   await expect(link).toHaveCount(1);
  569 |   const href = await link.getAttribute('href');
  570 |   expect(href).toMatch(/__frontcomposer\/specimens\/type#.+-Retention$/u);
  571 |   await link.focus();
  572 |   await page.keyboard.press('Shift+Tab');
  573 |   await page.keyboard.press('Tab');
  574 |   await expect(link).toBeFocused();
  575 |   await page.keyboard.press('Enter');
  576 |   await expect(page).toHaveURL(new URL(href!, await page.locator('base').getAttribute('href').then(base => new URL(base!, page.url()).href)).href);
  577 |   await info.attach('native-validation-link-fallback', { body: JSON.stringify({ role: 'link', href, activation: 'keyboard Enter', duplicateKeyboardStops: false }), contentType: 'application/json' });
  578 | });
  579 | 
  580 | test('G09 forced-color focus geometry rejects a mode-specific clipped ring', async ({ page }) => {
  581 |   await page.setContent('<style>button{position:fixed;left:2px;top:40px;width:80px;height:40px;appearance:none;outline:none}button:focus{outline:2px solid black}@media(forced-colors:active){button:focus{outline-width:6px}}</style><button>Save</button>');
  582 |   await page.locator('button').focus();
  583 |   expect((await focusGeometry(page.locator('button'))).failures).toEqual([]);
  584 |   await page.emulateMedia({ forcedColors: 'active' });
  585 |   expect((await focusGeometry(page.locator('button'))).failures).toContain('viewport clips target or focus ring');
  586 | });
  587 | 
```