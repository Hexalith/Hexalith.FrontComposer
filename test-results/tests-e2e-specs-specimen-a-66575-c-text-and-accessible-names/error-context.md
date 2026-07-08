# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: tests/e2e/specs/specimen-accessibility.spec.ts >> FrontComposer accessibility and visual specimens >> data-formatting specimen renders deterministic text and accessible names
- Location: tests/e2e/specs/specimen-accessibility.spec.ts:112:3

# Error details

```
Error: page.goto: Protocol error (Page.navigate): Cannot navigate to invalid URL
Call log:
  - navigating to "/__frontcomposer/specimens/data-formatting?culture=fr&ui-culture=en", waiting until "load"

```

# Test source

```ts
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
  296 |       await page.goto(`/__frontcomposer/specimens/type?theme=${combination.theme}&density=${combination.density}`);
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
> 381 |   await page.goto(route.path);
      |              ^ Error: page.goto: Protocol error (Page.navigate): Cannot navigate to invalid URL
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
  397 | 
  398 |       .fc-shell-root,
  399 |       fluent-layout,
  400 |       .fluent-layout {
  401 |         height: auto !important;
  402 |         min-height: auto !important;
  403 |         overflow: visible !important;
  404 |       }
  405 |     `,
  406 |   });
  407 | 
  408 |   await page.evaluate(() => {
  409 |     window.scrollTo(0, 0);
  410 |     for (const element of document.querySelectorAll<HTMLElement>('.fluent-layout-item')) {
  411 |       element.scrollTop = 0;
  412 |     }
  413 |   });
  414 |   await expect(page.getByTestId('fc-type-specimen')).toBeInViewport();
  415 | };
  416 | 
  417 | const scrollTestIdIntoView = async (page: import('@playwright/test').Page, testId: string): Promise<void> => {
  418 |   await expect(async () => {
  419 |     const locator = page.getByTestId(testId);
  420 |     await expect(locator).toHaveCount(1);
  421 |     await locator.scrollIntoViewIfNeeded();
  422 |     await expect(locator).toBeInViewport();
  423 |   }).toPass({ timeout: 5_000 });
  424 | };
  425 | 
  426 | const tabUntilTestId = async (page: import('@playwright/test').Page, testId: string): Promise<void> => {
  427 |   for (let attempt = 0; attempt < 20; attempt += 1) {
  428 |     await page.keyboard.press('Tab');
  429 |     const focusedTestId = await page.locator(':focus').getAttribute('data-testid');
  430 |     if (focusedTestId === testId) {
  431 |       return;
  432 |     }
  433 |   }
  434 | 
  435 |   throw new Error(`Expected focus to reach ${testId}`);
  436 | };
  437 | 
  438 | const waitForHost = async (request: import('@playwright/test').APIRequestContext): Promise<void> => {
  439 |   const deadline = Date.now() + 60_000;
  440 |   let lastError: unknown;
  441 |   while (Date.now() < deadline) {
  442 |     try {
  443 |       const response = await request.get('/', { failOnStatusCode: false, timeout: 2_000 });
  444 |       if (response.status() < 500) {
  445 |         return;
  446 |       }
  447 |     } catch (error) {
  448 |       lastError = error;
  449 |     }
  450 | 
  451 |     await new Promise((resolve) => setTimeout(resolve, 500));
  452 |   }
  453 | 
  454 |   throw new Error(`Counter production smoke host did not start: ${String(lastError)}`);
  455 | };
  456 | 
```