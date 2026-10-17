# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: specimen-accessibility.spec.ts >> FrontComposer accessibility and visual specimens >> visual baseline dark comfortable
- Location: specs/specimen-accessibility.spec.ts:483:5

# Error details

```
Test timeout of 60000ms exceeded.
```

```
Error: page.goto: Test timeout of 60000ms exceeded.
Call log:
  - navigating to "http://localhost:5201/__frontcomposer/specimens/type?theme=dark&density=comfortable", waiting until "load"

```

# Test source

```ts
  384 |         .getByTestId('fc-story-11-5-pulse-proof')
  385 |         .locator('.fc-projection-connection-status-pulse')
  386 |         .evaluate((element) => {
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
> 484 |       await page.goto(`/__frontcomposer/specimens/type?theme=${combination.theme}&density=${combination.density}`);
      |                  ^ Error: page.goto: Test timeout of 60000ms exceeded.
  485 |       await expect(page.getByTestId('fc-type-specimen')).toBeVisible();
  486 |       await prepareSpecimenVisualBaseline(page);
  487 |       await expect(page).toHaveScreenshot(combination.artifact, {
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
```