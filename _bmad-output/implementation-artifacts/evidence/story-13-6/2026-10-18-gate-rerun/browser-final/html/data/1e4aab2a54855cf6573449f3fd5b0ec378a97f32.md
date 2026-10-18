# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> shell chrome, theme menu and exact desktop navigation rails in light
- Location: specs/accessibility-state-matrix.spec.ts:462:3

# Error details

```
Test timeout of 60000ms exceeded.
```

```
Error: locator.isVisible: Test timeout of 60000ms exceeded.
Call log:
    - checking visibility of locator('.fc-shell-root').locator('fluent-text-input:not([disabled]),fluent-number-input:not([disabled]),fluent-dropdown:not([disabled]),fluent-combobox:not([disabled])').nth(7)

```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Skip to content" [ref=e3] [cursor=pointer]:
    - /url: /counter#fc-main-content
  - link "Skip to navigation" [ref=e4] [cursor=pointer]:
    - /url: /counter#fc-nav
  - generic [ref=e6]:
    - banner [ref=e7]:
      - generic [ref=e8]:
        - generic [ref=e9]:
          - button "Toggle navigation" [ref=e11] [cursor=pointer]
          - generic [ref=e15]: Hexalith FrontComposer
        - generic [ref=e16]:
          - button "Change theme" [active] [ref=e18] [cursor=pointer]:
            - generic [ref=e19]:
              - generic:
                - generic [ref=e20]: "Change theme: Light"
                - generic [aria-hidden] [ref=e21]: Light
          - button "Open command palette" [ref=e25] [cursor=pointer]
          - button "Open settings" [ref=e29] [cursor=pointer]
          - button "FU" [ref=e34] [cursor=pointer]
    - navigation "Primary navigation" [ref=e38]:
      - generic [ref=e39]:
        - button "Counter" [ref=e40] [cursor=pointer]
        - button "More Counter pages" [ref=e47] [cursor=pointer]
        - button "Default" [ref=e54] [cursor=pointer]
        - button "Specimens" [ref=e60] [cursor=pointer]
        - button "More Specimens pages" [ref=e67] [cursor=pointer]
    - generic [ref=e74]:
      - status [ref=e75]: Density set to Compact.
      - main [ref=e76]:
        - status [ref=e77]
        - status [ref=e78]
        - generic [ref=e79]:
          - heading "Counter" [level=1] [ref=e80]
          - generic "Counter pages" [ref=e81]:
            - tablist [ref=e82]:
              - generic:
                - tab "Overview" [selected] [ref=e83] [cursor=pointer]
                - tab "Projection" [ref=e85] [cursor=pointer]
            - tabpanel "Overview" [ref=e87]:
              - generic [ref=e89]:
                - generic:
                  - generic [ref=e90]:
                    - heading [level=2] [ref=e91]:
                      - button "Create with an exact target" [expanded] [ref=e92] [cursor=pointer]
                    - region "Create with an exact target" [ref=e95]:
                      - group [ref=e97]:
                        - generic "Create Counter command form" [ref=e98]:
                          - generic [ref=e99]:
                            - generic [ref=e100]:
                              - generic [ref=e102]:
                                - generic: Counter key
                                - generic "Counter key" [ref=e104]:
                                  - textbox "Counter key, Required" [ref=e106]
                              - generic [ref=e108]:
                                - generic: Initial Value
                                - generic "Initial Value" [ref=e110]:
                                  - textbox "Initial Value, Required" [ref=e112]: "0"
                              - button "Create Counter" [ref=e113] [cursor=pointer]
                            - status [ref=e115]
                  - generic [ref=e116]:
                    - heading [level=2] [ref=e117]:
                      - button "Update with an exact target" [expanded] [ref=e118] [cursor=pointer]
                    - region "Update with an exact target" [ref=e121]:
                      - group [ref=e123]:
                        - generic "Update Counter command form" [ref=e124]:
                          - generic [ref=e125]:
                            - generic [ref=e126]:
                              - generic [ref=e128]:
                                - generic: Counter key
                                - generic "Counter key" [ref=e130]:
                                  - textbox "Counter key, Required" [ref=e132]
                              - generic [ref=e134]:
                                - generic: Amount
                                - generic "Amount" [ref=e136]:
                                  - textbox "Amount, Required" [ref=e138]: "1"
                              - button "Update Counter" [ref=e139] [cursor=pointer]
                            - status [ref=e141]
                  - generic [ref=e142]:
                    - heading [level=2] [ref=e143]:
                      - button "Compact inline (3 fields)" [expanded] [ref=e144] [cursor=pointer]
                    - region "Compact inline (3 fields)" [ref=e147]:
                      - group [ref=e149]:
                        - generic "Batch Increment command form" [ref=e150]:
                          - generic [ref=e151]:
                            - generic [ref=e152]:
                              - generic [ref=e154]:
                                - generic: Amount
                                - generic "Amount" [ref=e156]:
                                  - textbox "Amount, Required" [ref=e158]: "1"
                              - generic [ref=e160]:
                                - generic: Note
                                - generic "Note" [ref=e162]:
                                  - textbox "Note, Required" [ref=e164]
                              - generic [ref=e166]:
                                - generic: Effective Date
                                - generic "Effective Date" [ref=e168]:
                                  - generic [ref=e169]:
                                    - textbox "Effective Date" [ref=e170]:
                                      - /placeholder: M/d/yyyy
                                      - text: 10/18/2026
                                    - button "Show date picker" [ref=e172]
                              - button "Batch Increment" [ref=e174] [cursor=pointer]
                            - status [ref=e176]
                  - generic [ref=e177]:
                    - heading [level=2] [ref=e178]:
                      - button "Inline + popover (1 field)" [expanded] [ref=e179] [cursor=pointer]
                    - region "Inline + popover (1 field)" [ref=e182]:
                      - button "Increment" [ref=e183] [cursor=pointer]
                  - generic [ref=e187]:
                    - heading [level=2] [ref=e188]:
                      - button "Full-page command (5 fields)" [expanded] [ref=e189] [cursor=pointer]
                    - region "Full-page command (5 fields)" [ref=e192]:
                      - generic:
                        - note [ref=e193]: Navigation state persistence (scroll, filter, sort across FullPage round-trip) lands in Story 4.3. The current demo proves routing + breadcrumb return only.
                        - link [ref=e194] [cursor=pointer]:
                          - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                          - generic [ref=e195]:
                            - generic:
                              - link [ref=e196]:
                                - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                              - text: Configure Counter
          - status [ref=e197]
    - generic [ref=e198]: Hexalith FrontComposer © 2026
```

# Test source

```ts
  1   | import { PageToolbarSpecimenPage } from '../page-objects/page-toolbar-specimen.page.js';
  2   | import { fillFieldByLabel } from '../helpers/fluent-fields.js';
  3   | import { writeFile } from 'node:fs/promises';
  4   | import { expect, test } from '../fixtures/index.js';
  5   | import type { Locator, Page, TestInfo } from '@playwright/test';
  6   | import { applyTextSpacing, focusGeometry, expectFocusVisible, expectNoPageOverflow, expectNoTextClipping, expectSurfaceContrast, expectTargetSizes, measureSurfaceContrast, screenshotPreservingScroll, settlePaint } from '../helpers/responsive-accessibility.js';
  7   | import { expectNoBlockingAxeViolations } from '../helpers/a11y.js';
  8   | import { LIFECYCLE_STATES } from '../fixtures/lifecycle.fixture.js';
  9   | 
  10  | const scenarioPath = '/__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact';
  11  | const outcomes = ['validation', 'returned-rejection', 'exception-rejection', 'forbidden', 'unavailable', 'blank-unavailable', 'rate-limited', 'blank-rate-limited', 'transport', 'admission', 'policy'] as const;
  12  | 
  13  | async function ready(page: Page, path: string, theme: string) {
  14  |   await page.goto(path);
  15  |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  16  |   await page.getByTitle('Change theme').first().click();
  17  |   await page.getByRole('menuitem', { name: theme === 'dark' ? 'Dark' : 'Light', exact: true }).click();
  18  |   await page.setViewportSize({ width: 320, height: 900 });
  19  |   await page.emulateMedia({ reducedMotion: 'reduce' });
  20  |   await applyTextSpacing(page);
  21  |   await settlePaint(page);
  22  | }
  23  | 
  24  | type VisibleSurfaceInspection = { phase: string; retainedFocus?: Locator };
  25  | 
  26  | async function evidence(page: Page, root: Locator, state: string, info: TestInfo, icons?: Locator, inspection?: VisibleSurfaceInspection) {
  27  |   // These explicitly opted-in journeys inspect distant paint without moving operator
  28  |   // focus. Each mode measures that focus while visible, before its own root scroll.
  29  |   const inspectBeforeRootScroll = async (mode: 'normal' | 'forced-colors') => {
  30  |     expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches)).toBe(mode === 'forced-colors');
  31  |     const activeElement = await page.evaluateHandle(() => document.activeElement);
  32  |     try {
  33  |       const focused = inspection!.retainedFocus ?? page.locator(':focus').first();
  34  |       const active = await page.evaluate(() => document.activeElement?.tagName ?? 'BODY');
  35  |       const operatorFocused = !['BODY','HTML'].includes(active) && await focused.count() > 0;
  36  |       if (inspection!.retainedFocus) await expect(focused).toBeFocused();
  37  |       let result;
  38  |       if (operatorFocused) {
  39  |         await focused.scrollIntoViewIfNeeded();
  40  |         await expect(focused).toBeFocused();
  41  |         await settlePaint(page);
  42  |         const requireIndicator = await focused.evaluate(element => element.matches(':focus-visible') || Boolean(element.shadowRoot?.querySelector(':focus-visible')));
  43  |         const geometry = await focusGeometry(focused, { requireIndicator });
  44  |         expect(geometry.failures, `Visible ${mode} focus before root scroll: ${state} ${JSON.stringify(geometry)}`).toEqual([]);
  45  |         const contrast = requireIndicator ? await expectSurfaceContrast(focused, 'focus') : null;
  46  |         result = { mode, status: 'measured', phase: inspection!.phase, ...geometry, contrast };
  47  |       } else result = { mode, status: 'none', phase: inspection!.phase, target: active };
  48  |       await root.scrollIntoViewIfNeeded();
  49  |       expect(await activeElement.evaluate(element => document.activeElement === element), `Root inspection retains activeElement: ${state} ${mode}`).toBe(true);
  50  |       if (inspection!.retainedFocus) await expect(focused).toBeFocused();
  51  |       return { ...result, activeElementRetainedAfterRootScroll: true };
  52  |     } finally { await activeElement.dispose(); }
  53  |   };
  54  | 
  55  |   await expect(root).toBeVisible();
  56  |   await expectNoPageOverflow(page);
  57  |   await expectNoTextClipping(page);
  58  |   await settlePaint(page);
  59  |   const theme = state.endsWith('-dark') ? 'Dark' : 'Light';
  60  |   await expect(page.getByTitle('Change theme').first().locator('span[aria-hidden="true"]')).toHaveText(theme);
  61  |   const themePaint = await page.locator('.fc-shell-root').evaluate(element => {
  62  |     const css = getComputedStyle(element), background = css.getPropertyValue('--colorNeutralBackground1').trim();
  63  |     const canvas = document.createElement('canvas'); canvas.width = canvas.height = 1;
  64  |     const context = canvas.getContext('2d')!; context.fillStyle = background; context.fillRect(0, 0, 1, 1);
  65  |     return { background, rgb: [...context.getImageData(0, 0, 1, 1).data].slice(0, 3) };
  66  |   });
  67  |   expect(themePaint.rgb.reduce((sum, channel) => sum + channel, 0) < 384, 'Actual Fluent theme paint').toBe(theme === 'Dark');
  68  |   const reducedMotion = await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches);
  69  |   expect(reducedMotion).toBe(true);
  70  |   const normalFocus = inspection ? await inspectBeforeRootScroll('normal') : undefined;
  71  |   const collectCues = async () => {
  72  |     const cues = [];
  73  |     if (state.startsWith('generated-validation-')) cues.push(await expectSurfaceContrast(root, 'focus'));
  74  |     if (state.startsWith('fresh-presentation-')) cues.push(await expectSurfaceContrast(root, 'border'));
  75  |     if (state.startsWith('generated-missing-lost-focus-')) cues.push(await expectSurfaceContrast(root, 'focus'));
  76  |     if (state.startsWith('tabs-active-')) cues.push(await expectSurfaceContrast(root.locator('[aria-selected="true"]'), 'focus'));
  77  |     if (state.startsWith('palette-selected-')) cues.push(await expectSurfaceContrast(root.locator('.fc-palette-option-selected'), 'cue'));
  78  |     if (state.startsWith('destructive-dialog-')) cues.push(await expectSurfaceContrast(page.getByTestId('fc-destructive-cancel'), 'focus'));
  79  |     if (state.startsWith('abandonment-')) cues.push(await expectSurfaceContrast(page.getByTestId('fc-form-abandonment-stay'), 'focus'));
  80  |     if (state.startsWith('rail-')) cues.push(await expectSurfaceContrast(page.getByTestId('fc-shell-navigation').getByTestId('fc-navigation-rail').locator('[aria-current="page"]'), 'border'));
  81  |     if (state.startsWith('settings-')) cues.push(await expectSurfaceContrast(root.locator('fluent-radio:focus'), 'focus'));
  82  |     if (state.startsWith('theme-menu-')) cues.push(await expectSurfaceContrast(root.locator('[role="menuitem"]:focus'), 'focus'));
  83  |     return cues;
  84  |   };
  85  |   const cues = await collectCues();
  86  |   const exceptions: import('../helpers/responsive-accessibility.js').TargetException[] = state.startsWith('settings-')
  87  |     ? ['Compact', 'Comfortable', 'Roomy'].map(value => ({ selector: `fluent-radio[name="fc-density-select"][value="${value}"]`, kind: 'Spacing' as const, rationale: 'Fluent density radio keeps its native visual size with a clear 24px spacing circle.', keyboardPath: 'Settings → Density; Tab and arrows', evidence: `ae-state-${state}.json target inventory` }))
  88  |     : state.startsWith('abandonment-') ? [{ selector: 'nav[aria-label="breadcrumb"] > a', kind: 'Spacing', rationale: 'The full-page breadcrumb link is isolated from all neighboring targets; its 24px circle is measured against the complete inventory.', keyboardPath: 'Full-page form → Counter breadcrumb (Shift+Tab)', evidence: `ae-state-${state}.json target inventory` }] : [];
  89  |   const targets = await expectTargetSizes(page, exceptions);
  90  |   const text = await expectSurfaceContrast(root);
  91  |   const paintedIcons = await measureSurfaceContrast(icons ?? root, 'icon');
  92  |   const icon = paintedIcons.length ? await expectSurfaceContrast(icons ?? root, 'icon') : [];
  93  |   const collectBoundaries = async () => {
  94  |     const boundaries = [];
  95  |     for (const control of await root.locator('fluent-text-input:not([disabled]),fluent-number-input:not([disabled]),fluent-dropdown:not([disabled]),fluent-combobox:not([disabled])').all()) {
> 96  |       if (!await control.isVisible()) continue;
      |                          ^ Error: locator.isVisible: Test timeout of 60000ms exceeded.
  97  |       // isVisible includes controls fully outside a scrollport. Native intersection
  98  |       // geometry excludes only those whose boundaries currently paint no pixels.
  99  |       const painted = await control.evaluate(node => new Promise<boolean>(resolve => {
  100 |         const observer = new IntersectionObserver(([entry]) => {
  101 |           observer.disconnect();
  102 |           resolve(entry.intersectionRect.width > 0 && entry.intersectionRect.height > 0);
  103 |         });
  104 |         observer.observe(node);
  105 |       }));
  106 |       if (painted) boundaries.push(await expectSurfaceContrast(control, 'border'));
  107 |     }
  108 |     return boundaries;
  109 |   };
  110 |   const boundaries = await collectBoundaries();
  111 |   if (state.startsWith('lifecycle-rejected-')) expect(icon.length, 'Required normal rejected-state icon samples').toBeGreaterThan(0);
  112 |   if (state.startsWith('policy-denied-')) expect(boundaries.length, 'Required normal policy control-boundary samples').toBeGreaterThan(0);
  113 |   await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
  114 |   await settlePaint(page);
  115 |   const inspectedSystemFocus = inspection ? await inspectBeforeRootScroll('forced-colors') : undefined;
  116 |   const forcedColors = await page.evaluate(() => matchMedia('(forced-colors: active)').matches);
  117 |   const forcedColorsPaint = await page.evaluate(() => {
  118 |     const probe = document.createElement('div');
  119 |     probe.style.cssText = 'position:fixed;left:-10000px;color:rgb(255,0,0);background:rgb(0,255,0);forced-color-adjust:auto';
  120 |     document.body.append(probe);
  121 |     const css = getComputedStyle(probe), changed = css.color !== 'rgb(255, 0, 0)' && css.backgroundColor !== 'rgb(0, 255, 0)';
  122 |     probe.remove(); return changed;
  123 |   });
  124 |   const systemText = forcedColorsPaint ? await expectSurfaceContrast(root) : [];
  125 |   const systemIcon = forcedColorsPaint && icon.length ? await expectSurfaceContrast(icons ?? root, 'icon') : [];
  126 |   const systemBoundaries = forcedColorsPaint ? await collectBoundaries() : [];
  127 |   const systemCues = forcedColorsPaint ? await collectCues() : [];
  128 |   if (forcedColorsPaint) {
  129 |     if (state.startsWith('lifecycle-rejected-')) expect(systemIcon.length, 'Required forced-color rejected-state icon samples').toBeGreaterThan(0);
  130 |     if (state.startsWith('policy-denied-')) expect(systemBoundaries.length, 'Required forced-color policy control-boundary samples').toBeGreaterThan(0);
  131 |   }
  132 |   const forcedColorsStatus = forcedColorsPaint ? 'painted system palette' : 'unsupported authored-color replacement; media rules only';
  133 |   if (!forcedColorsPaint) expect(info.project.name, 'Only the probed WebKit engine lacks forced palette emulation').toBe('webkit');
  134 |   await expect(root).toBeVisible();
  135 |   await expectNoPageOverflow(page);
  136 |   await expectNoTextClipping(page);
  137 |   const focused = page.locator(':focus').first();
  138 |   const active = await page.evaluate(() => document.activeElement?.tagName ?? 'BODY');
  139 |   const operatorFocused = !['BODY','HTML'].includes(active) && await focused.count() > 0;
  140 |   const systemFocus = inspectedSystemFocus ?? (operatorFocused
  141 |     ? { mode: 'forced-colors', status: 'measured', ...await focusGeometry(focused, { requireIndicator: await focused.evaluate(element => element.matches(':focus-visible') || Boolean(element.shadowRoot?.querySelector(':focus-visible'))) }) }
  142 |     : { mode: 'forced-colors', status: 'none', target: active });
  143 |   if ('failures' in systemFocus) expect(systemFocus.failures, `Forced-color focused geometry: ${state} ${JSON.stringify(systemFocus)}`).toEqual([]);
  144 |   const systemSnapshot = forcedColorsPaint ? `system-state-${state}.png` : null;
  145 |   if (systemSnapshot) await screenshotPreservingScroll(root, { path: info.outputPath(systemSnapshot) });
  146 |   await page.emulateMedia({ forcedColors: 'none' });
  147 |   await settlePaint(page);
  148 |   const path = info.outputPath(`ae-state-${state}.json`);
  149 |   await writeFile(path, JSON.stringify({ testId: info.testId, attempt: info.retry, state, engine: info.project.name, runId: process.env.FC_A11Y_EVIDENCE_RUN_ID, viewport: page.viewportSize(), theme, themePaint, targets, ...(inspection ? { normalFocus, rootPaintPhase: 'after-root-scroll-with-retained-active-element', fixtureOptions: info.config.metadata.testHostOptions } : {}), text, icon, boundaries, cues, decorativeIconExemptions: ['.fc-empty-state-body__icon: redundant illustration accompanying complete visible empty-state text', '.fluent-skeleton: decorative loading skeleton accompanying aria-busy and its accessible name'], forcedColors, forcedColorsPaint, forcedColorsStatus, systemText, systemIcon, systemBoundaries, systemCues, systemFocus, systemSnapshot, reducedMotion }, null, 2));
  150 |   await info.attach(`state-${state}`, { path, contentType: 'application/json' });
  151 | }
  152 | 
  153 | for (const theme of ['light', 'dark']) {
  154 |   test.describe(`AE complete state matrix ${theme}`, () => {
  155 |     test.beforeEach(async ({ page }) => {
  156 |       await page.addInitScript(() => { localStorage.clear(); sessionStorage.clear(); });
  157 |     });
  158 | 
  159 |     for (const outcome of outcomes) {
  160 |       test(`generated zero-field ${outcome}: safe feedback, focus, owner, next attempt`, async ({ page }, info) => {
  161 |         await ready(page, `${scenarioPath}&theme=${theme}`, theme);
  162 |         const root = page.getByTestId('fc-ae-scenarios');
  163 |         await root.getByTestId(`fc-ae-select-${outcome}`).click();
  164 |         const trigger = root.getByRole('button', { name: 'Run accessibility specimen', exact: true });
  165 |         await trigger.focus();
  166 |         await page.keyboard.press('Shift+Tab');
  167 |         await page.keyboard.press('Tab');
  168 |         await page.keyboard.press('Enter');
  169 |         const feedback = root.getByTestId('fc-zero-field-outcome');
  170 |         await expect(feedback).toBeVisible();
  171 |         await expect(feedback).toHaveAttribute('aria-live', 'off');
  172 |         await expect(trigger).toBeFocused();
  173 |         await expect(feedback).not.toHaveText(/^\s*$/u);
  174 |         await expect(feedback.locator('a,button,fluent-button')).toHaveCount(0);
  175 |         if (outcome.includes('rate-limited')) await expect(feedback).toContainText('Retry after 17 seconds.');
  176 |         const copy = (await feedback.innerText()).trim();
  177 |         await expect(root.getByTestId('fc-surface-status').filter({ hasText: outcome.includes('rejection') ? /rejected/i : copy })).toHaveCount(1);
  178 |         await expect(root.locator('[role="alert"],[aria-live="assertive"]')).toHaveCount(0);
  179 |         await feedback.scrollIntoViewIfNeeded();
  180 |         await evidence(page, feedback, `zero-${outcome}-${theme}`, info);
  181 |         await expectFocusVisible(trigger);
  182 |         const focusContrast = await expectSurfaceContrast(trigger, 'focus');
  183 |         // No automatic pre-accept retry: the independent dispatch counter stays at one (zero for denials).
  184 |         await page.waitForTimeout(350);
  185 |         await root.getByTestId('fc-ae-select-accepted').click();
  186 |         await expect(root.getByTestId('fc-ae-command-probe')).toHaveAttribute('data-dispatch-count', ['admission', 'policy'].includes(outcome) ? '0' : '1');
  187 |         await expect(feedback).toHaveText(copy);
  188 |         await trigger.click();
  189 |         await expect(feedback).toHaveCount(0);
  190 |         await expect(page.locator('.fc-pending-command-summary')).toBeVisible();
  191 |         if (outcome === 'validation') {
  192 |           await expect(trigger).toBeFocused();
  193 |           // Inspect the global accepted summary separately after proving trigger retention.
  194 |           await page.locator('#fc-main-content h1').first().focus();
  195 |           await page.locator('.fc-pending-command-summary').scrollIntoViewIfNeeded();
  196 |           await evidence(page, page.locator('.fc-pending-command-summary'), `zero-accepted-${theme}`, info);
```