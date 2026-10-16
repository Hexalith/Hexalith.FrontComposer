# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: form-abandonment-guard.spec.ts >> Story 4.2: unsaved full-page command form abandonment guard >> dirty generated full-page command form after threshold supports stay, Escape, and leave
- Location: specs/form-abandonment-guard.spec.ts:46:3

# Error details

```
Error: page.waitForFunction: Test ended.
```

# Test source

```ts
  182 |       stay.textContent = 'Sibling Stay';
  183 |       warning.append(stay);
  184 |       card.append(warning);
  185 |       root.after(card, siblingRoot);
  186 |       stay.focus();
  187 |       focus.restoreEditedOrigin(root);
  188 |       await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
  189 |     }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');
  190 | 
  191 |     await expect(fieldEditorByLabel(configureCounterForm(page), 'Description')).toBeFocused();
  192 |   });
  193 | 
  194 |   test('removed edited control restores focus to the marked full-page command heading', async ({ page, tenant }) => {
  195 |     expect(tenant.tenantId).toBeTruthy();
  196 | 
  197 |     await gotoConfigureCounter(page);
  198 |     const form = configureCounterForm(page);
  199 |     const editor = fieldEditorByLabel(form, 'Name');
  200 |     await editor.focus();
  201 | 
  202 |     await page.evaluate(async (focusModulePath) => {
  203 |       const focus = await import(focusModulePath) as {
  204 |         captureEditedOrigin: (root: HTMLElement, fieldName: string) => void;
  205 |         restoreEditedOrigin: () => void;
  206 |       };
  207 |       const root = document.querySelector<HTMLElement>('[data-fc-abandonment-root]');
  208 |       if (!root || !(document.activeElement instanceof HTMLElement)) {
  209 |         throw new Error('The generated abandonment form did not expose an editable origin.');
  210 |       }
  211 | 
  212 |       focus.captureEditedOrigin(root, 'Name');
  213 |       const active = document.activeElement;
  214 |       const fieldHost = active.closest('fluent-text-input') ?? active;
  215 |       fieldHost.remove();
  216 |       focus.restoreEditedOrigin();
  217 |       await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
  218 |     }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');
  219 | 
  220 |     const formHeading = page.locator('h1[data-fc-form-heading="true"]');
  221 |     await expect(formHeading).toHaveText('Configure Counter');
  222 |     await expect(formHeading).toBeFocused();
  223 |   });
  224 | 
  225 |   test('sequenced capture ignores a stale earlier edit after focus moves to a warning action', async ({ page, tenant }) => {
  226 |     expect(tenant.tenantId).toBeTruthy();
  227 | 
  228 |     await gotoConfigureCounter(page);
  229 |     const form = configureCounterForm(page);
  230 |     const staleCaptureAccepted = await page.evaluate(async (focusModulePath) => {
  231 |       const focus = await import(focusModulePath) as {
  232 |         captureEditedOrigin: (root: HTMLElement, fieldName: string, sequence: number) => boolean;
  233 |         restoreEditedOrigin: (root?: HTMLElement) => void;
  234 |       };
  235 |       const root = document.querySelector<HTMLElement>('[data-fc-abandonment-root]');
  236 |       if (!root) {
  237 |         throw new Error('The generated abandonment root was not rendered.');
  238 |       }
  239 | 
  240 |       focus.captureEditedOrigin(root, 'Name', 1);
  241 |       focus.captureEditedOrigin(root, 'Description', 2);
  242 |       document.querySelector<HTMLElement>('[data-testid="fc-settings-button"]')?.focus();
  243 |       const staleAccepted = focus.captureEditedOrigin(root, 'Name', 1);
  244 |       focus.restoreEditedOrigin(root);
  245 |       await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
  246 |       return staleAccepted;
  247 |     }, '/_content/Hexalith.FrontComposer.Shell/js/fc-focus.js');
  248 | 
  249 |     expect(staleCaptureAccepted).toBe(false);
  250 |     await expect(fieldEditorByLabel(form, 'Description')).toBeFocused();
  251 |   });
  252 | });
  253 | 
  254 | const gotoConfigureCounter = async (page: Page): Promise<void> => {
  255 |   await page.goto('/counter');
  256 |   await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
  257 |   await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();
  258 | 
  259 |   await page.getByRole('link', { name: 'Configure Counter' }).click();
  260 |   await expect(page).toHaveURL(FULL_PAGE_ROUTE);
  261 |   await expect(configureCounterForm(page)).toBeVisible();
  262 | };
  263 | 
  264 | const configureCounterForm = (page: Page): Locator =>
  265 |   page.locator(`${COMMAND_FORM}[aria-label="${FORM_LABEL}"]`);
  266 | 
  267 | const counterBreadcrumbLink = (page: Page): Locator =>
  268 |   page.getByRole('navigation', { name: 'breadcrumb' }).getByRole('link', { name: /counter/i });
  269 | 
  270 | const abandonmentWarning = (page: Page): Locator =>
  271 |   page.getByTestId('fc-form-abandonment-warning');
  272 | 
  273 | const fillField = async (root: Locator, label: string, value: string): Promise<void> => {
  274 |   await fillFieldByLabel(root, label, value);
  275 | };
  276 | 
  277 | const waitForConfiguredAbandonmentThreshold = async (page: Page): Promise<void> => {
  278 |   const deadline = await page.evaluate(
  279 |     (thresholdMs) => performance.now() + thresholdMs,
  280 |     FORM_ABANDONMENT_THRESHOLD_SECONDS * 1000 + SERVER_CLOCK_SLOP_MS);
  281 | 
> 282 |   await page.waitForFunction(
      |              ^ Error: page.waitForFunction: Test ended.
  283 |     (target) => performance.now() >= target,
  284 |     deadline,
  285 |     { timeout: FORM_ABANDONMENT_THRESHOLD_SECONDS * 1000 + SERVER_CLOCK_SLOP_MS + 2000 });
  286 | };
  287 | 
```