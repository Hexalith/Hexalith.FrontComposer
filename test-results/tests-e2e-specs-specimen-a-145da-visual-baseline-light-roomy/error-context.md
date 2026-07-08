# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: tests/e2e/specs/specimen-accessibility.spec.ts >> FrontComposer accessibility and visual specimens >> visual baseline light roomy
- Location: tests/e2e/specs/specimen-accessibility.spec.ts:295:5

# Error details

```
Error: page.goto: Protocol error (Page.navigate): Cannot navigate to invalid URL
Call log:
  - navigating to "/__frontcomposer/specimens/type?theme=light&density=roomy", waiting until "load"

```

# Test source

```ts
  196 |     expect(doneBox!.width).toBeGreaterThan(footerBox!.width * 0.9);
  197 | 
  198 |     const pulseProof = await page.evaluate(() => {
  199 |       const collectRules = (rules: CSSRuleList): CSSRule[] => {
  200 |         const collected: CSSRule[] = [];
  201 |         for (const rule of Array.from(rules)) {
  202 |           collected.push(rule);
  203 |           if ('cssRules' in rule) {
  204 |             collected.push(...collectRules((rule as CSSGroupingRule).cssRules));
  205 |           }
  206 |         }
  207 | 
  208 |         return collected;
  209 |       };
  210 | 
  211 |       const styleRules = Array.from(document.styleSheets)
  212 |         .flatMap((sheet) => {
  213 |           try {
  214 |             return collectRules(sheet.cssRules);
  215 |           } catch {
  216 |             return [];
  217 |           }
  218 |         })
  219 |         .filter((rule): rule is CSSStyleRule => rule instanceof CSSStyleRule);
  220 | 
  221 |       const selector = styleRules
  222 |         .map((rule) => rule.selectorText)
  223 |         .find((text) => text.includes('.fc-projection-connection-status-host')
  224 |           && text.includes('.fc-projection-connection-status-pulse'));
  225 |       const scopeAttribute = selector?.match(/\.fc-projection-connection-status-host\[([^\]=]+)(?:=[^\]]+)?\]/u)?.[1];
  226 |       if (!scopeAttribute) {
  227 |         throw new Error('Projection connection status scoped selector was not present in the browser CSSOM.');
  228 |       }
  229 | 
  230 |       const host = document.createElement('div');
  231 |       host.className = 'fc-projection-connection-status-host';
  232 |       host.setAttribute(scopeAttribute, '');
  233 |       const pulse = document.createElement('div');
  234 |       pulse.className = 'fc-projection-connection-status fc-projection-connection-status-pulse';
  235 |       host.append(pulse);
  236 |       document.body.append(host);
  237 |       try {
  238 |         const styles = getComputedStyle(pulse);
  239 |         return {
  240 |           animationName: styles.animationName,
  241 |           animationDuration: styles.animationDuration,
  242 |           reducedMotion: matchMedia('(prefers-reduced-motion: reduce)').matches,
  243 |         };
  244 |       } finally {
  245 |         host.remove();
  246 |       }
  247 |     });
  248 | 
  249 |     expect(pulseProof.reducedMotion).toBe(true);
  250 |     expect(pulseProof.animationName).toBe('none');
  251 |     expect(pulseProof.animationDuration).toBe('0s');
  252 |   });
  253 | 
  254 |   test('forced-colors and reduced-motion states are active and perceivable', async ({ browser }) => {
  255 |     const context = await browser.newContext({
  256 |       baseURL: process.env.BASE_URL ?? 'http://127.0.0.1:5070',
  257 |       forcedColors: 'active',
  258 |       reducedMotion: 'reduce',
  259 |       ignoreHTTPSErrors: true,
  260 |     });
  261 |     const page = await context.newPage();
  262 |     const guards = attachSpecimenGuards(page);
  263 | 
  264 |     const route = getSpecimenRoute('type');
  265 |     await gotoSpecimen(page, route);
  266 | 
  267 |     await expect(page.locator(route.readySelector)).toBeVisible();
  268 |     await expect(page.getByLabel(/Status.*Warning/u)).toHaveAttribute('data-fc-badge-slot', 'Warning');
  269 |     await expect(page.getByTestId('fc-lifecycle-confirmed-rejected')).toContainText('rejection');
  270 |     expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches)).toBe(true);
  271 |     expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
  272 | 
  273 |     await page.getByTestId('fc-command-submit').focus();
  274 |     const outlineColor = await page.getByTestId('fc-command-submit').evaluate((element) => getComputedStyle(element).outlineColor);
  275 |     expect(outlineColor).toBeTruthy();
  276 |     expect(guards.consoleErrors, `Unhandled browser console errors:\n${guards.consoleErrors.join('\n')}`).toEqual([]);
  277 |     expect(guards.unexpectedRequests, `Unexpected network calls:\n${guards.unexpectedRequests.join('\n')}`).toEqual([]);
  278 |     await context.close();
  279 |   });
  280 | 
  281 |   for (const scale of [1, 2, 4] as const) {
  282 |     test(`zoom and reflow keep critical controls reachable at ${scale * 100}%`, async ({ page }) => {
  283 |       const route = getSpecimenRoute('type');
  284 |       await page.setViewportSize({ width: Math.floor(1280 / scale), height: 900 });
  285 |       await gotoSpecimen(page, route);
  286 | 
  287 |       await scrollTestIdIntoView(page, 'fc-command-submit');
  288 |       await scrollTestIdIntoView(page, 'fc-multi-level-nav');
  289 |       const horizontalOverflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  290 |       expect(horizontalOverflow).toBeLessThanOrEqual(24);
  291 |     });
  292 |   }
  293 | 
  294 |   for (const combination of specimenManifest.themeDensityCombinations) {
  295 |     test(`visual baseline ${combination.theme} ${combination.density}`, async ({ page }) => {
> 296 |       await page.goto(`/__frontcomposer/specimens/type?theme=${combination.theme}&density=${combination.density}`);
      |                  ^ Error: page.goto: Protocol error (Page.navigate): Cannot navigate to invalid URL
  297 |       await expect(page.getByTestId('fc-type-specimen')).toBeVisible();
  298 |       await prepareSpecimenVisualBaseline(page);
  299 |       await expect(page).toHaveScreenshot(combination.artifact, {
  300 |         fullPage: true,
  301 |         animations: 'disabled',
  302 |       });
  303 |     });
  304 |   }
  305 | 
  306 |   test('production-style route exposure fails closed when specimen host configuration is absent', async ({ playwright }) => {
  307 |     const port = 5071;
  308 |     const server = spawn('dotnet', [
  309 |       'run',
  310 |       '--project',
  311 |       '../../samples/Counter/Counter.Web/Counter.Web.csproj',
  312 |       '--configuration',
  313 |       'Release',
  314 |       '--no-build',
  315 |       '--no-launch-profile',
  316 |       '--urls',
  317 |       `http://127.0.0.1:${port}`,
  318 |     ], {
  319 |       cwd: process.cwd(),
  320 |       env: {
  321 |         ...process.env,
  322 |         ASPNETCORE_ENVIRONMENT: 'Production',
  323 |         Hexalith__FrontComposer__Specimens__Enabled: '',
  324 |       },
  325 |       stdio: ['ignore', 'pipe', 'pipe'],
  326 |     });
  327 |     let serverOutput = '';
  328 |     server.stdout.on('data', (chunk) => { serverOutput += chunk.toString(); });
  329 |     server.stderr.on('data', (chunk) => { serverOutput += chunk.toString(); });
  330 | 
  331 |     const api = await playwright.request.newContext({ baseURL: `http://127.0.0.1:${port}` });
  332 |     try {
  333 |       await waitForHost(api);
  334 |       const response = await api.get('/__frontcomposer/specimens/type', { failOnStatusCode: false });
  335 |       expect(response.status(), `Specimen route was exposed without explicit configuration.\n${serverOutput}`).toBeGreaterThanOrEqual(400);
  336 |     } finally {
  337 |       await api.dispose();
  338 |       server.kill();
  339 |     }
  340 |   });
  341 | });
  342 | 
  343 | const attachSpecimenGuards = (page: import('@playwright/test').Page) => {
  344 |   const consoleErrors: string[] = [];
  345 |   const unexpectedRequests: string[] = [];
  346 | 
  347 |   page.on('console', (message) => {
  348 |     if (message.type() === 'error') {
  349 |       if (message.text() === 'Failed to load resource: the server responded with a status of 404 (Not Found)') {
  350 |         return;
  351 |       }
  352 | 
  353 |       consoleErrors.push(message.text());
  354 |     }
  355 |   });
  356 | 
  357 |   page.on('request', (request) => {
  358 |     const url = new URL(request.url());
  359 |     const path = url.pathname;
  360 |     const allowed = path.startsWith('/_framework/')
  361 |       || path.startsWith('/_content/')
  362 |       || path.startsWith('/_blazor')
  363 |       || path.startsWith('/css/')
  364 |       || path.startsWith('/js/')
  365 |       || path.startsWith('/__frontcomposer/specimens/')
  366 |       || path === '/Counter.Web.styles.css'
  367 |       || path === '/'
  368 |       || path === '/favicon.ico';
  369 | 
  370 |     if (!allowed) {
  371 |       unexpectedRequests.push(`${request.method()} ${url.pathname}`);
  372 |     }
  373 |   });
  374 | 
  375 |   consoleErrorsByPage.set(page, consoleErrors);
  376 |   unexpectedRequestsByPage.set(page, unexpectedRequests);
  377 |   return { consoleErrors, unexpectedRequests };
  378 | };
  379 | 
  380 | const gotoSpecimen = async (page: import('@playwright/test').Page, route: SpecimenRoute): Promise<void> => {
  381 |   await page.goto(route.path);
  382 |   await expect(page.locator(route.readySelector), `${route.path} missing ready marker`).toBeVisible();
  383 |   for (const selector of route.requiredSections) {
  384 |     const count = await page.locator(selector).count();
  385 |     expect(count, `${route.path} missing ${selector}`).toBeGreaterThan(0);
  386 |   }
  387 | };
  388 | 
  389 | const prepareSpecimenVisualBaseline = async (page: import('@playwright/test').Page): Promise<void> => {
  390 |   await page.addStyleTag({
  391 |     content: `
  392 |       .pa-3.fluent-layout-item {
  393 |         overflow: visible !important;
  394 |         height: auto !important;
  395 |         max-height: none !important;
  396 |       }
```