# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: specimen-accessibility.spec.ts >> FrontComposer accessibility and visual specimens >> visual baseline light compact
- Location: specs/specimen-accessibility.spec.ts:483:5

# Error details

```
Error: expect(page).toHaveScreenshot(expected) failed

  Expected an image 1280px by 3778px, received 1280px by 3820px. 188932 pixels (ratio 0.04 of all image pixels) are different.

  Snapshot: frontcomposer-type-light-compact.png

Call log:
  - Expect "toHaveScreenshot(frontcomposer-type-light-compact.png)" with timeout 10000ms
    - verifying given screenshot expectation
  - taking page screenshot
    - disabled all CSS animations
  - waiting for fonts to load...
  - fonts loaded
  - Expected an image 1280px by 3778px, received 1280px by 3820px. 188932 pixels (ratio 0.04 of all image pixels) are different.
  - waiting 100ms before taking screenshot
  - taking page screenshot
    - disabled all CSS animations
  - waiting for fonts to load...
  - fonts loaded
  - captured a stable screenshot
  - Expected an image 1280px by 3778px, received 1280px by 3820px. 188932 pixels (ratio 0.04 of all image pixels) are different.

```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Passer au contenu" [ref=e3] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type?theme=light&density=compact#fc-main-content
  - link "Passer à la navigation" [ref=e4] [cursor=pointer]:
    - /url: /__frontcomposer/specimens/type?theme=light&density=compact#fc-nav
  - generic [ref=e6]:
    - banner [ref=e7]:
      - generic [ref=e8]:
        - generic [ref=e9]:
          - button "Basculer la navigation" [ref=e12] [cursor=pointer]
          - generic [ref=e16]: Hexalith FrontComposer
        - generic [ref=e17]:
          - button "Changer de thème" [ref=e19] [cursor=pointer]:
            - generic [ref=e20]:
              - generic:
                - generic [ref=e21]: "Changer de thème: Clair"
                - generic [aria-hidden] [ref=e22]: Clair
          - button "Ouvrir la palette de commandes" [ref=e26] [cursor=pointer]
          - button "Ouvrir les paramètres" [ref=e30] [cursor=pointer]
          - button "FU" [ref=e35] [cursor=pointer]
    - navigation "Navigation principale" [ref=e39]:
      - generic [ref=e40]:
        - button "Counter" [ref=e41] [cursor=pointer]
        - button "Autres pages de Counter" [ref=e47] [cursor=pointer]
        - button "Default" [ref=e54] [cursor=pointer]
        - button "Specimens" [ref=e59] [cursor=pointer]
        - button "Autres pages de Specimens" [ref=e65] [cursor=pointer]
    - generic [ref=e72]:
      - status [ref=e73]: Densité réglée sur Compact.
      - main [ref=e74]:
        - status [ref=e75]
        - status [ref=e76]
        - main [ref=e78]:
          - generic [ref=e79]:
            - paragraph [ref=e80]: FrontComposer accessibility specimen
            - heading "Type, theme, density, lifecycle, and navigation specimen" [active] [level=1] [ref=e81]
            - paragraph [ref=e82]: Generated shell surfaces rendered with deterministic local data.
            - link "Skip to command form" [ref=e83] [cursor=pointer]:
              - /url: "#fc-specimen-command-form"
          - region [ref=e84]:
            - heading "Theme and density controls" [level=2] [ref=e85]
            - generic [ref=e86]:
              - link "light" [ref=e87] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=compact
              - link "dark" [ref=e88] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=dark&density=compact
              - link "compact" [ref=e89] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=compact
              - link "comfortable" [ref=e90] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=comfortable
              - link "roomy" [ref=e91] [cursor=pointer]:
                - /url: /__frontcomposer/specimens/type?theme=light&density=roomy
          - region [ref=e92]:
            - heading "Type ramp" [level=2] [ref=e93]
            - generic [ref=e94]:
              - article [ref=e95]:
                - paragraph [ref=e96]: Display
                - generic [ref=e97]: FrontComposer Display
              - article [ref=e98]:
                - paragraph [ref=e99]: Title
                - generic [ref=e100]: Generated command title
              - article [ref=e101]:
                - paragraph [ref=e102]: Subtitle
                - generic [ref=e103]: Projection subtitle and context
              - article [ref=e104]:
                - paragraph [ref=e105]: Body
                - generic [ref=e106]: Readable generated body text
              - article [ref=e107]:
                - paragraph [ref=e108]: Caption
                - generic [ref=e109]: Supplemental metadata caption
              - article [ref=e110]:
                - paragraph [ref=e111]: Code
                - generic [ref=e112]: FC-STATIC-0001
          - region [ref=e113]:
            - heading "Semantic color tokens" [level=2] [ref=e114]
            - generic [ref=e115]:
              - article [ref=e116]:
                - heading "light theme" [level=3] [ref=e117]
                - generic [ref=e118]:
                  - generic [ref=e120]: Neutral
                  - strong [ref=e121]: Available
                - generic [ref=e122]:
                  - generic [ref=e124]: Informational
                  - strong [ref=e125]: Queued
                - generic [ref=e126]:
                  - generic [ref=e128]: Success
                  - strong [ref=e129]: Confirmed
                - generic [ref=e130]:
                  - generic [ref=e132]: Warning
                  - strong [ref=e133]: Needs review
                - generic [ref=e134]:
                  - generic [ref=e136]: Danger
                  - strong [ref=e137]: Rejected
                - generic [ref=e138]:
                  - generic [ref=e140]: Accent
                  - strong [ref=e141]: Selected
              - article [ref=e142]:
                - heading "dark theme" [level=3] [ref=e143]
                - generic [ref=e144]:
                  - generic [ref=e146]: Neutral
                  - strong [ref=e147]: Available
                - generic [ref=e148]:
                  - generic [ref=e150]: Informational
                  - strong [ref=e151]: Queued
                - generic [ref=e152]:
                  - generic [ref=e154]: Success
                  - strong [ref=e155]: Confirmed
                - generic [ref=e156]:
                  - generic [ref=e158]: Warning
                  - strong [ref=e159]: Needs review
                - generic [ref=e160]:
                  - generic [ref=e162]: Danger
                  - strong [ref=e163]: Rejected
                - generic [ref=e164]:
                  - generic [ref=e166]: Accent
                  - strong [ref=e167]: Selected
          - region [ref=e168]:
            - heading "Density states" [level=2] [ref=e169]
            - generic [ref=e170]:
              - article [ref=e171]:
                - heading "compact density" [level=3] [ref=e172]
                - button "Primary action" [ref=e173]
                - generic [ref=e174]:
                  - text: Label
                  - textbox "compact density text input" [ref=e175]: compact sample
              - article [ref=e176]:
                - heading "comfortable density" [level=3] [ref=e177]
                - button "Primary action" [ref=e178]
                - generic [ref=e179]:
                  - text: Label
                  - textbox "comfortable density text input" [ref=e180]: comfortable sample
              - article [ref=e181]:
                - heading "roomy density" [level=3] [ref=e182]
                - button "Primary action" [ref=e183]
                - generic [ref=e184]:
                  - text: Label
                  - textbox "roomy density text input" [ref=e185]: roomy sample
          - region [ref=e186]:
            - heading "Generated projection DataGrid and badge states" [level=2] [ref=e187]
            - status [ref=e188]
            - paragraph [ref=e189]: 6 specimen status
            - group "Puces de filtrage par statut" [ref=e190]:
              - button "Neutral, actuellement inactif" [ref=e191] [cursor=pointer]:
                - generic [ref=e192]: Neutral
              - button "Info, actuellement inactif" [ref=e193] [cursor=pointer]:
                - generic [ref=e194]: Info
              - button "Success, actuellement inactif" [ref=e195] [cursor=pointer]:
                - generic [ref=e196]: Success
              - button "Warning, actuellement inactif" [ref=e197] [cursor=pointer]:
                - generic [ref=e198]: Warning
              - button "Danger, actuellement inactif" [ref=e199] [cursor=pointer]:
                - generic [ref=e200]: Danger
              - button "Accent, actuellement inactif" [ref=e201] [cursor=pointer]:
                - generic [ref=e202]: Accent
            - text: 6 specimen status
            - region "specimen status" [ref=e203]:
              - table [ref=e204]:
                - rowgroup [ref=e205]:
                  - row [ref=e206]:
                    - cell [ref=e207]:
                      - generic [ref=e208]:
                        - text: FC-1000Neutral projection
                        - 'img "Status : Neutral" [ref=e210]'
                        - text: Shell
                  - row [ref=e213]:
                    - cell [ref=e214]:
                      - generic [ref=e215]:
                        - text: FC-1001Info projection
                        - 'img "Status : Info" [ref=e217]'
                        - text: SourceTools
                  - row [ref=e220]:
                    - cell [ref=e221]:
                      - generic [ref=e222]:
                        - text: FC-1002Success projection
                        - 'img "Status : Success" [ref=e224]'
                        - text: Contracts
                  - row [ref=e227]:
                    - cell [ref=e228]:
                      - generic [ref=e229]:
                        - text: FC-1003Warning projection
                        - 'img "Status : Warning" [ref=e231]'
                        - text: Testing
                  - row [ref=e234]:
                    - cell [ref=e235]:
                      - generic [ref=e236]:
                        - text: FC-1004Danger projection
                        - 'img "Status : Danger" [ref=e238]'
                        - text: CI
                  - row [ref=e241]:
                    - cell [ref=e242]:
                      - generic [ref=e243]:
                        - text: FC-1005Accent projection
                        - 'img "Status : Accent" [ref=e245]'
                        - text: Docs
              - region "Détails pour"
          - region [ref=e248]:
            - heading "Generated Counter projection grid" [level=2] [ref=e249]
            - status [ref=e250]
            - paragraph [ref=e251]: 1 counters
            - text: 1 counters
            - region "counters" [ref=e252]:
              - table [ref=e253]:
                - rowgroup [ref=e254]:
                  - row [ref=e255]:
                    - cell [ref=e256]:
                      - generic [ref=e257]:
                        - text: fc-correlation-00024215/01/2026
                        - generic "Metadata avec le type non pris en charge System.Collections.Generic.Dictionary nécessite un rendu personnalisé" [ref=e259]:
                          - strong [ref=e260]: Metadata
                          - text: (System.Collections.Generic.Dictionary) Ce champ nécessite un rendu personnalisé.
                          - link "Apprendre à personnaliser ce champ" [ref=e261] [cursor=pointer]:
                            - /url: https://hexalith.github.io/FrontComposer/diagnostics/HFC1002
              - region "Détails pour"
          - region [ref=e262]:
            - heading "Command form and lifecycle states" [level=2] [ref=e263]
            - form "Specimen command form" [ref=e264]:
              - generic [ref=e265]:
                - text: Counter name
                - textbox "Counter name" [ref=e266]: Specimen counter
              - generic [ref=e267]:
                - text: Amount
                - spinbutton "Amount" [ref=e268]: "3"
              - button "Submit command" [ref=e269]
              - button "Cancel" [ref=e270]
            - list "Five command lifecycle states" [ref=e271]:
              - listitem [ref=e272]:
                - strong [ref=e273]: Idle
                - text: Ready to submit.
              - listitem [ref=e274]:
                - strong [ref=e275]: Submitting
                - text: Submitting command.
              - listitem [ref=e276]:
                - strong [ref=e277]: Acknowledged
                - text: Command accepted by the host.
              - listitem [ref=e278]:
                - strong [ref=e279]: Syncing
                - text: Waiting for projection synchronization.
              - listitem [ref=e280]:
                - strong [ref=e281]: Confirmed or rejected
                - text: Terminal confirmation and rejection states remain text-visible.
          - region [ref=e282]:
            - heading "Destructive command confirmation" [level=2] [ref=e283]
            - group [ref=e285]:
              - generic "Purge Specimen Record command form" [ref=e286]:
                - generic [ref=e287]:
                  - generic [ref=e288]:
                    - group "Purge details" [ref=e289]:
                      - generic [ref=e291]:
                        - generic [ref=e292]:
                          - generic: Record Id
                          - generic "Record Id" [ref=e294]:
                            - textbox "Record Id, Required" [ref=e296]: FC-1002
                        - generic [ref=e297]: Record to purge.
                      - generic [ref=e298]:
                        - generic [ref=e299]:
                          - generic: Reason
                          - generic "Reason" [ref=e301]:
                            - textbox "Reason, Required" [ref=e303]: QA destructive confirmation coverage
                        - generic [ref=e304]: Why this purge is required.
                    - generic [ref=e305]:
                      - generic [ref=e306]:
                        - generic: Retention
                        - generic "Retention" [ref=e308] [cursor=pointer]:
                          - combobox "Retention" [ref=e310]
                      - generic [ref=e314]: How long the purged record stays recoverable.
                    - button "Purge Specimen Record" [ref=e315] [cursor=pointer]
                  - status [ref=e317]
          - region [ref=e318]:
            - heading "Policy-gated command authorization" [level=2] [ref=e319]
            - group [ref=e321]:
              - generic "Policy Allowed Specimen command form" [ref=e322]:
                - generic [ref=e323]:
                  - generic [ref=e324]:
                    - generic [ref=e326]:
                      - generic: Record Id
                      - generic "Record Id" [ref=e328]:
                        - textbox "Record Id, Required" [ref=e330]: FC-AUTH-ALLOW
                    - generic [ref=e332]:
                      - generic: Reason
                      - generic "Reason" [ref=e334]:
                        - textbox "Reason, Required" [ref=e336]: QA policy allowed coverage
                    - button "Policy Allowed Specimen" [ref=e337] [cursor=pointer]
                  - status [ref=e339]
            - group [ref=e340]:
              - group [ref=e341]:
                - heading "Autorisation requise" [level=2] [ref=e342]
                - paragraph [ref=e343]: Vous n'avez pas l'autorisation d'exécuter Policy Denied Specimen.
          - region [ref=e344]:
            - heading "Expanded detail view" [level=2] [ref=e345]
            - group [ref=e346]:
              - generic "Open projection details for FC-1002" [ref=e347]
              - generic [ref=e348]:
                - term [ref=e349]: Correlation
                - definition [ref=e350]: fc-correlation-0002
                - term [ref=e351]: Last synchronized
                - definition [ref=e352]: 2026-01-15 13:45 UTC
          - navigation "Specimen multi-level navigation" [ref=e353]:
            - heading "Multi-level navigation" [level=2] [ref=e354]
            - list [ref=e355]:
              - listitem [ref=e356]:
                - link "Foundation" [ref=e357] [cursor=pointer]:
                  - /url: "#fc-type-ramp-title"
              - listitem [ref=e358]:
                - button "Generated surfaces" [expanded] [ref=e359]
                - list [ref=e360]:
                  - listitem [ref=e361]:
                    - link "Projection grid" [ref=e362] [cursor=pointer]:
                      - /url: "#fc-badge-grid-title"
                  - listitem [ref=e363]:
                    - link "Command lifecycle" [ref=e364] [cursor=pointer]:
                      - /url: "#fc-specimen-command-title"
    - generic [ref=e365]: Hexalith FrontComposer © 2026
```

# Test source

```ts
  387 |           const styles = getComputedStyle(element);
  388 |           return {
  389 |             animationDuration: styles.animationDuration,
  390 |             animationName: styles.animationName,
  391 |             reducedMotion: matchMedia('(prefers-reduced-motion: reduce)').matches,
  392 |           };
  393 |         });
  394 | 
  395 |       expect(reducedPulseProof.reducedMotion).toBe(true);
  396 |       expect(reducedPulseProof.animationName).toBe('none');
  397 |       expect(reducedPulseProof.animationDuration).toBe('0s');
  398 |     } finally {
  399 |       await page.evaluate(() => document.querySelector('[data-testid="fc-story-11-5-pulse-proof"]')?.remove());
  400 |     }
  401 |   });
  402 | 
  403 |   test('forced-colors and reduced-motion states are active and perceivable', async ({ browser }) => {
  404 |     const context = await browser.newContext({
  405 |       baseURL: process.env.BASE_URL ?? 'http://127.0.0.1:5070',
  406 |       forcedColors: 'active',
  407 |       reducedMotion: 'reduce',
  408 |       ignoreHTTPSErrors: true,
  409 |     });
  410 |     const page = await context.newPage();
  411 |     const guards = attachSpecimenGuards(page);
  412 | 
  413 |     const route = getSpecimenRoute('type');
  414 |     await gotoSpecimen(page, route);
  415 | 
  416 |     await expect(page.locator(route.readySelector)).toBeVisible();
  417 |     await expect(page.getByLabel(/Status.*Warning/u)).toHaveAttribute('data-fc-badge-slot', 'Warning');
  418 |     await expect(page.getByTestId('fc-lifecycle-confirmed-rejected')).toContainText('rejection');
  419 |     expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches)).toBe(true);
  420 |     expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
  421 | 
  422 |     const freshRowProof = await page.evaluate(() => {
  423 |       const collect = (rules: CSSRuleList): CSSRule[] => Array.from(rules).flatMap((rule) =>
  424 |         'cssRules' in rule ? [rule, ...collect((rule as CSSGroupingRule).cssRules)] : [rule]);
  425 |       const rules = Array.from(document.styleSheets).flatMap((sheet) => {
  426 |         try { return collect(sheet.cssRules); } catch { return []; }
  427 |       });
  428 |       const selector = rules.filter((rule): rule is CSSStyleRule => rule instanceof CSSStyleRule)
  429 |         .map((rule) => rule.selectorText).find((text) => text.includes('.fc-new-item-indicator['));
  430 |       const scope = selector?.match(/\.fc-new-item-indicator\[([^\]=]+)(?:=[^\]]+)?\]/u)?.[1];
  431 |       if (!scope) throw new Error('Fresh-row indicator scoped CSS is absent.');
  432 |       const cue = document.createElement('div');
  433 |       cue.className = 'fc-new-item-indicator';
  434 |       cue.setAttribute(scope, '');
  435 |       cue.style.width = '240px';
  436 |       cue.textContent = `Updated row: ${'longidentifier'.repeat(32)}.`;
  437 |       document.body.append(cue);
  438 |       try {
  439 |         const styles = getComputedStyle(cue);
  440 |         const system = document.createElement('div');
  441 |         system.style.borderInlineStart = '3px solid CanvasText';
  442 |         document.body.append(system);
  443 |         const systemColor = getComputedStyle(system).borderInlineStartColor;
  444 |         system.remove();
  445 |         return { borderColor: styles.borderInlineStartColor, systemColor,
  446 |           borderStyle: styles.borderInlineStartStyle, borderWidth: styles.borderInlineStartWidth,
  447 |           backgroundImage: styles.backgroundImage, animationName: styles.animationName,
  448 |           transitionDuration: styles.transitionDuration, forcedColorAdjust: styles.forcedColorAdjust,
  449 |           longIdentifierWraps: cue.scrollWidth <= cue.clientWidth && cue.getBoundingClientRect().height > 40 };
  450 |       } finally { cue.remove(); }
  451 |     });
  452 |     expect(freshRowProof.borderColor).toBe(freshRowProof.systemColor);
  453 |     expect(freshRowProof.borderStyle).toBe('solid');
  454 |     expect(freshRowProof.borderWidth).toBe('3px');
  455 |     expect(freshRowProof.backgroundImage).toBe('none');
  456 |     expect(freshRowProof.animationName).toBe('none');
  457 |     expect(freshRowProof.transitionDuration).toBe('0s');
  458 |     expect(freshRowProof.forcedColorAdjust).not.toBe('none');
  459 |     expect(freshRowProof.longIdentifierWraps).toBe(true);
  460 | 
  461 |     await page.getByTestId('fc-command-submit').focus();
  462 |     const outlineColor = await page.getByTestId('fc-command-submit').evaluate((element) => getComputedStyle(element).outlineColor);
  463 |     expect(outlineColor).toBeTruthy();
  464 |     expect(guards.consoleErrors, `Unhandled browser console errors:\n${guards.consoleErrors.join('\n')}`).toEqual([]);
  465 |     expect(guards.unexpectedRequests, `Unexpected network calls:\n${guards.unexpectedRequests.join('\n')}`).toEqual([]);
  466 |     await context.close();
  467 |   });
  468 | 
  469 |   for (const scale of [1, 2, 4] as const) {
  470 |     test(`viewport reflow keeps critical controls reachable at ${Math.floor(1280 / scale)} CSS pixels (not browser zoom)`, async ({ page }) => {
  471 |       const route = getSpecimenRoute('type');
  472 |       await page.setViewportSize({ width: Math.floor(1280 / scale), height: 900 });
  473 |       await gotoSpecimen(page, route);
  474 | 
  475 |       await scrollTestIdIntoView(page, 'fc-command-submit');
  476 |       await scrollTestIdIntoView(page, 'fc-multi-level-nav');
  477 |       const horizontalOverflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  478 |       expect(horizontalOverflow).toBeLessThanOrEqual(1);
  479 |     });
  480 |   }
  481 | 
  482 |   for (const combination of specimenManifest.themeDensityCombinations) {
  483 |     test(`visual baseline ${combination.theme} ${combination.density}`, async ({ page }) => {
  484 |       await page.goto(`/__frontcomposer/specimens/type?theme=${combination.theme}&density=${combination.density}`);
  485 |       await expect(page.getByTestId('fc-type-specimen')).toBeVisible();
  486 |       await prepareSpecimenVisualBaseline(page);
> 487 |       await expect(page).toHaveScreenshot(combination.artifact, {
      |                          ^ Error: expect(page).toHaveScreenshot(expected) failed
  488 |         fullPage: true,
  489 |         animations: 'disabled',
  490 |         // Linux baselines are regenerated locally. Windows baselines come from CI
  491 |         // artifacts, so keep the semantic contrast guard strict and allow only a
  492 |         // narrow Windows text-rasterization delta calibrated from the CSS fix.
  493 |         ...(process.platform === 'win32' ? { maxDiffPixels: windowsVisualBaselineMaxDiffPixels } : {}),
  494 |       });
  495 |     });
  496 |   }
  497 | 
  498 |   test('production-style route exposure fails closed when specimen host configuration is absent', async ({ playwright }) => {
  499 |     const port = 5071;
  500 |     const server = spawn('dotnet', [
  501 |       'run',
  502 |       '--project',
  503 |       '../../samples/Counter/Counter.Web/Counter.Web.csproj',
  504 |       '--configuration',
  505 |       process.env.FC_E2E_BUILD_CONFIGURATION ?? 'Release',
  506 |       '--no-build',
  507 |       '--no-launch-profile',
  508 |       '--urls',
  509 |       `http://127.0.0.1:${port}`,
  510 |     ], {
  511 |       cwd: process.cwd(),
  512 |       env: {
  513 |         ...process.env,
  514 |         ASPNETCORE_ENVIRONMENT: 'Production',
  515 |         Hexalith__FrontComposer__Specimens__Enabled: '',
  516 |       },
  517 |       stdio: ['ignore', 'pipe', 'pipe'],
  518 |     });
  519 |     let serverOutput = '';
  520 |     server.stdout.on('data', (chunk) => { serverOutput += chunk.toString(); });
  521 |     server.stderr.on('data', (chunk) => { serverOutput += chunk.toString(); });
  522 | 
  523 |     const api = await playwright.request.newContext({ baseURL: `http://127.0.0.1:${port}` });
  524 |     try {
  525 |       await waitForHost(api);
  526 |       const response = await api.get('/__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true', { failOnStatusCode: false });
  527 |       expect(response.status(), `Specimen route was exposed without explicit configuration.\n${serverOutput}`).toBeGreaterThanOrEqual(400);
  528 |     } finally {
  529 |       await api.dispose();
  530 |       server.kill();
  531 |     }
  532 |   });
  533 | });
  534 | 
  535 | const attachSpecimenGuards = (page: import('@playwright/test').Page) => {
  536 |   const consoleErrors: string[] = [];
  537 |   const unexpectedRequests: string[] = [];
  538 | 
  539 |   page.on('console', (message) => {
  540 |     if (message.type() === 'error') {
  541 |       if (message.text() === 'Failed to load resource: the server responded with a status of 404 (Not Found)') {
  542 |         return;
  543 |       }
  544 | 
  545 |       consoleErrors.push(message.text());
  546 |     }
  547 |   });
  548 | 
  549 |   page.on('request', (request) => {
  550 |     const url = new URL(request.url());
  551 |     const path = url.pathname;
  552 |     const allowed = path.startsWith('/_framework/')
  553 |       || path.startsWith('/_content/')
  554 |       || path.startsWith('/_blazor')
  555 |       || path.startsWith('/css/')
  556 |       || path.startsWith('/js/')
  557 |       || path.startsWith('/__frontcomposer/specimens/')
  558 |       || path === '/Counter.Web.styles.css'
  559 |       || path === '/'
  560 |       || path === '/favicon.ico';
  561 | 
  562 |     if (!allowed) {
  563 |       unexpectedRequests.push(`${request.method()} ${url.pathname}`);
  564 |     }
  565 |   });
  566 | 
  567 |   consoleErrorsByPage.set(page, consoleErrors);
  568 |   unexpectedRequestsByPage.set(page, unexpectedRequests);
  569 |   return { consoleErrors, unexpectedRequests };
  570 | };
  571 | 
  572 | const gotoSpecimen = async (page: import('@playwright/test').Page, route: SpecimenRoute): Promise<void> => {
  573 |   await page.goto(route.path);
  574 |   await expect(page.locator(route.readySelector), `${route.path} missing ready marker`).toBeVisible();
  575 |   await page.waitForFunction(() => [...document.querySelectorAll('*')]
  576 |     .filter((element) => element.localName.includes('-'))
  577 |     .every((element) => customElements.get(element.localName) !== undefined));
  578 |   await expect(page.locator('.fc-shell-root')).toHaveAttribute('data-fc-interactive', 'true');
  579 |   for (const form of await page.locator('.fc-command-form').all()) {
  580 |     await waitForGeneratedFormReady(form);
  581 |   }
  582 |   if (route.name === 'type') {
  583 |     for (const section of ['fc-destructive-command-specimen', 'fc-policy-command-specimen']) {
  584 |       const form = page.getByTestId(section).locator('.fc-command-form');
  585 |       await expect(fieldEditorByLabel(form, 'Record Id')).toHaveAccessibleName(/^Record Id(?:,.*)?$/u);
  586 |       await expect(fieldEditorByLabel(form, 'Reason')).toHaveAccessibleName(/^Reason(?:,.*)?$/u);
  587 |     }
```