import { expect, test } from '../fixtures/index.js';
import { expectNoBlockingAxeViolations } from '../helpers/a11y.js';
import { spawn } from 'node:child_process';
import {
  getSpecimenRoute,
  specimenManifest,
  validateSpecimenManifest,
  type SpecimenRoute,
} from '../helpers/specimen-manifest.js';

const consoleErrorsByPage = new WeakMap<import('@playwright/test').Page, string[]>();
const unexpectedRequestsByPage = new WeakMap<import('@playwright/test').Page, string[]>();
const minimumReadableTextContrast = 4.5;
const windowsVisualBaselineMaxDiffPixels = 76_000;

test.describe('FrontComposer accessibility and visual specimens', () => {
  test.use({
    locale: 'fr-FR',
    timezoneId: 'UTC',
    deviceScaleFactor: 1,
  });

  test.beforeEach(async ({ page }) => {
    attachSpecimenGuards(page);

    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });

    await page.setViewportSize({ width: 1280, height: 900 });
    await page.emulateMedia({ reducedMotion: 'reduce' });

    test.info().attachments.push({
      name: 'specimen-console-and-network-guard',
      contentType: 'text/plain',
      body: Buffer.from('Console and network guards are active for specimen routes.'),
    });

  });

  test.afterEach(async ({ page }) => {
    const consoleErrors = consoleErrorsByPage.get(page) ?? [];
    const unexpectedRequests = unexpectedRequestsByPage.get(page) ?? [];
    expect(consoleErrors, `Unhandled browser console errors:\n${consoleErrors.join('\n')}`).toEqual([]);
    expect(unexpectedRequests, `Unexpected network calls:\n${unexpectedRequests.join('\n')}`).toEqual([]);
  });

  test('manifest names owned nonblank specimen routes and exactly six visual combinations', () => {
    validateSpecimenManifest();
  });

  for (const route of specimenManifest.routes) {
    test(`${route.name} specimen is nonblank and passes blocking axe gate`, async ({ page }, testInfo) => {
      await gotoSpecimen(page, route);
      await expectNoBlockingAxeViolations(page, {
        route: route.path,
        include: [route.landmarkRoot],
        requiredSelectors: [route.readySelector, ...route.requiredSections],
        artifactPath: testInfo.outputPath(`axe-${route.name}.json`),
      });
    });
  }

  test('type specimen renders required shell, density, badge, lifecycle, detail, and nav surfaces', async ({ page }) => {
    const route = getSpecimenRoute('type');
    await gotoSpecimen(page, route);

    await expect(page.getByTestId('fc-type-slot-display')).toContainText('FrontComposer Display');
    await expect(page.getByTestId('fc-token-theme-light')).toBeVisible();
    await expect(page.getByTestId('fc-token-theme-dark')).toBeVisible();
    await expect(page.getByTestId('fc-density-state-compact')).toBeVisible();
    await expect(page.getByTestId('fc-density-state-comfortable')).toBeVisible();
    await expect(page.getByTestId('fc-density-state-roomy')).toBeVisible();
    await expect(page.getByTestId('fc-status-icon')).toHaveCount(6);
    await expect(page.getByLabel(/Status.*Neutral/u)).toHaveAttribute('data-fc-badge-slot', 'Neutral');
    await expect(page.getByLabel(/Status.*Info/u)).toHaveAttribute('data-fc-badge-slot', 'Info');
    await expect(page.getByLabel(/Status.*Success/u)).toHaveAttribute('data-fc-badge-slot', 'Success');
    await expect(page.getByLabel(/Status.*Warning/u)).toHaveAttribute('data-fc-badge-slot', 'Warning');
    await expect(page.getByLabel(/Status.*Danger/u)).toHaveAttribute('data-fc-badge-slot', 'Danger');
    await expect(page.getByLabel(/Status.*Accent/u)).toHaveAttribute('data-fc-badge-slot', 'Accent');
    await expect(page.getByTestId('fc-badge-grid').locator("[data-fc-field='Status']")).toHaveCount(6);
    await expect(page.getByTestId('fc-generated-counter-grid').locator("[data-fc-field='Count']").first()).toContainText('42');
    await expect(page.getByTestId('fc-lifecycle-idle')).toContainText('Ready to submit');
    await expect(page.getByTestId('fc-lifecycle-confirmed-rejected')).toContainText('Terminal confirmation');
    await expect(page.getByTestId('fc-expanded-detail')).toContainText('fc-correlation-0002');
    await expect(page.getByRole('navigation', { name: 'Specimen multi-level navigation' })).toBeVisible();
  });

  test('dark generated command specimens keep readable foreground contrast', async ({ page }) => {
    const route = getSpecimenRoute('type');
    await page.goto(`${route.path}?theme=dark&density=roomy`);
    await expect(page.locator(route.readySelector)).toBeVisible();

    const samples = await page.evaluate(() => {
      type Color = { r: number; g: number; b: number; a: number };

      const parseColor = (value: string): Color => {
        if (value === 'transparent') {
          return { r: 0, g: 0, b: 0, a: 0 };
        }

        const match = value.match(/^rgba?\(([\d.]+),\s*([\d.]+),\s*([\d.]+)(?:,\s*([\d.]+))?\)$/u);
        if (!match) {
          throw new Error(`Unsupported CSS color: ${value}`);
        }

        return {
          r: Number.parseFloat(match[1]),
          g: Number.parseFloat(match[2]),
          b: Number.parseFloat(match[3]),
          a: match[4] === undefined ? 1 : Number.parseFloat(match[4]),
        };
      };

      const luminance = (color: Color): number => {
        const normalize = (channel: number): number => {
          const value = channel / 255;
          return value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
        };

        return (0.2126 * normalize(color.r)) + (0.7152 * normalize(color.g)) + (0.0722 * normalize(color.b));
      };

      const contrastRatio = (foreground: string, background: string): number => {
        const foregroundLuminance = luminance(parseColor(foreground));
        const backgroundLuminance = luminance(parseColor(background));
        return (Math.max(foregroundLuminance, backgroundLuminance) + 0.05)
          / (Math.min(foregroundLuminance, backgroundLuminance) + 0.05);
      };

      const effectiveBackground = (element: Element): string => {
        for (let current: Element | null = element; current; current = current.parentElement) {
          const background = getComputedStyle(current).backgroundColor;
          if (parseColor(background).a > 0) {
            return background;
          }
        }

        return getComputedStyle(document.body).backgroundColor;
      };

      const query = (selectors: string[]): Element => {
        for (const selector of selectors) {
          const element = document.querySelector(selector);
          if (element) {
            return element;
          }
        }

        throw new Error(`Missing contrast sample target: ${selectors.join(', ')}`);
      };

      const sample = (selectors: string[]) => {
        const element = query(selectors);
        const foreground = getComputedStyle(element).color;
        const background = effectiveBackground(element);
        return {
          background,
          foreground,
          ratio: contrastRatio(foreground, background),
        };
      };

      return {
        destructiveInput: sample(["[data-testid='fc-destructive-command-specimen'] .fc-command-input"]),
        destructiveLabel: sample(["[data-testid='fc-destructive-command-specimen'] .fc-command-field-label"]),
        policyInput: sample(["[data-testid='fc-policy-command-specimen'] .fc-command-input"]),
        policyLabel: sample(["[data-testid='fc-policy-command-specimen'] .fc-command-field-label"]),
        policyWarningText: sample([
          "[data-testid='fc-policy-command-specimen'] fluent-message-bar .content",
          "[data-testid='fc-policy-command-specimen'] fluent-message-bar",
        ]),
      };
    });

    for (const [name, sample] of Object.entries(samples)) {
      expect(sample.ratio, `${name} contrast ${sample.foreground} on ${sample.background}`)
        .toBeGreaterThanOrEqual(minimumReadableTextContrast);
    }
    expect(samples.destructiveInput.foreground).toBe('rgb(20, 20, 20)');
    expect(samples.destructiveInput.background).toBe('rgb(255, 255, 255)');
    expect(samples.destructiveLabel.foreground).toBe('rgb(247, 247, 242)');
    expect(samples.policyInput.foreground).toBe('rgb(20, 20, 20)');
    expect(samples.policyInput.background).toBe('rgb(255, 255, 255)');
    expect(samples.policyLabel.foreground).toBe('rgb(247, 247, 242)');
    expect(samples.policyWarningText.foreground).toBe('rgb(59, 47, 0)');
  });

  // Touch activation (AC2) requires a touch-enabled browser context. The desktop projects
  // (chromium/firefox/webkit) run with hasTouch=false, so locator.tap() would throw
  // "The page does not support tap"; scope just this test to a touch-capable context.
  test.describe('status icon touch interactions', () => {
    test.use({ hasTouch: true });

    test('status icons expose contextual labels and reveal tooltips on focus, hover, and touch', async ({ page }) => {
      const route = getSpecimenRoute('type');
      await gotoSpecimen(page, route);

      const warningIcon = page.getByLabel(/Status.*Warning/u);
      await expect(warningIcon).toHaveAttribute('data-testid', 'fc-status-icon');
      await warningIcon.focus();
      await expect(page.locator('fluent-tooltip').filter({ hasText: 'Warning' })).toBeVisible();

      const dangerIcon = page.getByLabel(/Status.*Danger/u);
      await dangerIcon.hover();
      await expect(page.locator('fluent-tooltip').filter({ hasText: 'Danger' })).toBeVisible();

      await dangerIcon.tap();
      await expect(page.locator('fluent-tooltip').filter({ hasText: 'Danger' })).toBeVisible();
    });
  });

  test('data-formatting specimen renders deterministic text and accessible names', async ({ page }) => {
    const route = getSpecimenRoute('data-formatting');
    await gotoSpecimen(page, route);

    await expect(page.getByTestId('fc-data-formatting-specimen')).toHaveAttribute('data-culture', 'fr-FR');
    await expect(page.getByTestId('fc-format-row-locale-number')).toContainText('12 345,67');
    await expect(page.getByTestId('fc-format-row-absolute-timestamp')).toContainText('15/01/2026 13:45 UTC');
    await expect(page.getByTestId('fc-format-row-relative-timestamp')).toContainText('3 hours ago');
    await expect(page.getByTestId('fc-format-row-null-value')).toContainText('—');
    await expect(page.getByTestId('fc-format-row-currency')).toContainText('1 234,50 EUR');
    await expect(page.getByTestId('fc-format-row-boolean')).toContainText('Yes');
    await expect(page.getByLabel('Budget: 1 234,50 EUR')).toBeVisible();
    await expect(page.getByTestId('fc-generated-formatting-grid').locator("[data-fc-field='TotalOrders']").first()).toContainText(/12[\s\u202f]345,67/u);
    await expect(page.getByTestId('fc-generated-formatting-grid').locator("[data-fc-field='Budget']").first()).toContainText(/1[\s\u202f]234,50/u);
    await expect(page.getByTestId('fc-generated-formatting-grid').locator("[data-fc-field='OpaquePayload']").first()).toBeVisible();
  });

  test('keyboard flow reaches skip link, controls, grid, command form, detail, and nav without traps', async ({ page }) => {
    const route = getSpecimenRoute('type');
    await gotoSpecimen(page, route);

    await page.getByTestId('fc-specimen-skip').focus();
    await expect(page.locator(':focus')).toHaveAttribute('data-testid', 'fc-specimen-skip');

    const expected = [
      'fc-theme-control-light',
      'fc-theme-control-dark',
      'fc-density-control-compact',
      'fc-density-control-comfortable',
      'fc-density-control-roomy',
      'fc-command-input',
      'fc-command-number',
      'fc-command-submit',
      'fc-command-cancel',
      'fc-expanded-detail-summary',
      'fc-nav-foundation',
      'fc-nav-group-generated',
    ];

    for (const testId of expected) {
      await tabUntilTestId(page, testId);
      await expect(page.locator(':focus')).toBeInViewport();
    }

    await page.keyboard.press('Space');
    await expect(page.getByTestId('fc-nav-group-generated')).toHaveAttribute('aria-expanded', 'true');
    await page.keyboard.press('Escape');
    await expect(page.locator(':focus')).toBeVisible();
  });

  test('focus-visible indicator remains visible over lifecycle visuals', async ({ page }, testInfo) => {
    const route = getSpecimenRoute('type');
    await gotoSpecimen(page, route);
    await page.getByTestId('fc-command-submit').focus();

    const outline = await page.getByTestId('fc-command-submit').evaluate((element) => getComputedStyle(element).outlineStyle);
    expect(outline).not.toBe('none');
    await page.screenshot({ path: testInfo.outputPath('focus-type-command-submit.png'), fullPage: false });
  });

  test('story 11.5 scoped Fluent-root visual hooks are reachable', async ({ page }) => {
    const route = getSpecimenRoute('type');
    await page.setViewportSize({ width: 420, height: 900 });
    await gotoSpecimen(page, route);

    await page.getByTestId('fc-settings-button').click();
    await expect(page.getByTestId('fc-settings-dialog')).toBeVisible();
    await page.getByRole('button', { name: /^(Aperçu|Preview)$/u }).click();

    const preview = page.getByTestId('fc-density-preview');
    await expect(preview).toBeVisible();
    const previewStyles = await preview.evaluate((element) => {
      const styles = getComputedStyle(element);
      return {
        borderStyle: styles.borderTopStyle,
        paddingTop: Number.parseFloat(styles.paddingTop),
      };
    });
    expect(previewStyles.borderStyle).not.toBe('none');
    expect(previewStyles.paddingTop).toBeGreaterThan(0);

    const footerBox = await page.locator('.fc-settings-footer').boundingBox();
    const doneBox = await page.getByTestId('fc-settings-done').boundingBox();
    expect(footerBox).not.toBeNull();
    expect(doneBox).not.toBeNull();
    expect(doneBox!.width).toBeGreaterThan(footerBox!.width * 0.9);

    const pulseProof = await page.evaluate(() => {
      const collectRules = (rules: CSSRuleList): CSSRule[] => {
        const collected: CSSRule[] = [];
        for (const rule of Array.from(rules)) {
          collected.push(rule);
          if ('cssRules' in rule) {
            collected.push(...collectRules((rule as CSSGroupingRule).cssRules));
          }
        }

        return collected;
      };

      const styleRules = Array.from(document.styleSheets)
        .flatMap((sheet) => {
          try {
            return collectRules(sheet.cssRules);
          } catch {
            return [];
          }
        })
        .filter((rule): rule is CSSStyleRule => rule instanceof CSSStyleRule);

      const selector = styleRules
        .map((rule) => rule.selectorText)
        .find((text) => text.includes('.fc-projection-connection-status-host')
          && text.includes('.fc-projection-connection-status-pulse'));
      const scopeAttribute = selector?.match(/\.fc-projection-connection-status-host\[([^\]=]+)(?:=[^\]]+)?\]/u)?.[1];
      if (!scopeAttribute) {
        throw new Error('Projection connection status scoped selector was not present in the browser CSSOM.');
      }

      const host = document.createElement('div');
      host.className = 'fc-projection-connection-status-host';
      host.setAttribute(scopeAttribute, '');
      const pulse = document.createElement('div');
      pulse.className = 'fc-projection-connection-status fc-projection-connection-status-pulse';
      host.append(pulse);
      document.body.append(host);
      try {
        const styles = getComputedStyle(pulse);
        return {
          animationName: styles.animationName,
          animationDuration: styles.animationDuration,
          reducedMotion: matchMedia('(prefers-reduced-motion: reduce)').matches,
        };
      } finally {
        host.remove();
      }
    });

    expect(pulseProof.reducedMotion).toBe(true);
    expect(pulseProof.animationName).toBe('none');
    expect(pulseProof.animationDuration).toBe('0s');
  });

  test('forced-colors and reduced-motion states are active and perceivable', async ({ browser }) => {
    const context = await browser.newContext({
      baseURL: process.env.BASE_URL ?? 'http://127.0.0.1:5070',
      forcedColors: 'active',
      reducedMotion: 'reduce',
      ignoreHTTPSErrors: true,
    });
    const page = await context.newPage();
    const guards = attachSpecimenGuards(page);

    const route = getSpecimenRoute('type');
    await gotoSpecimen(page, route);

    await expect(page.locator(route.readySelector)).toBeVisible();
    await expect(page.getByLabel(/Status.*Warning/u)).toHaveAttribute('data-fc-badge-slot', 'Warning');
    await expect(page.getByTestId('fc-lifecycle-confirmed-rejected')).toContainText('rejection');
    expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches)).toBe(true);
    expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);

    await page.getByTestId('fc-command-submit').focus();
    const outlineColor = await page.getByTestId('fc-command-submit').evaluate((element) => getComputedStyle(element).outlineColor);
    expect(outlineColor).toBeTruthy();
    expect(guards.consoleErrors, `Unhandled browser console errors:\n${guards.consoleErrors.join('\n')}`).toEqual([]);
    expect(guards.unexpectedRequests, `Unexpected network calls:\n${guards.unexpectedRequests.join('\n')}`).toEqual([]);
    await context.close();
  });

  for (const scale of [1, 2, 4] as const) {
    test(`zoom and reflow keep critical controls reachable at ${scale * 100}%`, async ({ page }) => {
      const route = getSpecimenRoute('type');
      await page.setViewportSize({ width: Math.floor(1280 / scale), height: 900 });
      await gotoSpecimen(page, route);

      await scrollTestIdIntoView(page, 'fc-command-submit');
      await scrollTestIdIntoView(page, 'fc-multi-level-nav');
      const horizontalOverflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(horizontalOverflow).toBeLessThanOrEqual(24);
    });
  }

  for (const combination of specimenManifest.themeDensityCombinations) {
    test(`visual baseline ${combination.theme} ${combination.density}`, async ({ page }) => {
      await page.goto(`/__frontcomposer/specimens/type?theme=${combination.theme}&density=${combination.density}`);
      await expect(page.getByTestId('fc-type-specimen')).toBeVisible();
      await prepareSpecimenVisualBaseline(page);
      await expect(page).toHaveScreenshot(combination.artifact, {
        fullPage: true,
        animations: 'disabled',
        // Linux baselines are regenerated locally. Windows baselines come from CI
        // artifacts, so keep the semantic contrast guard strict and allow only a
        // narrow Windows text-rasterization delta calibrated from the CSS fix.
        ...(process.platform === 'win32' ? { maxDiffPixels: windowsVisualBaselineMaxDiffPixels } : {}),
      });
    });
  }

  test('production-style route exposure fails closed when specimen host configuration is absent', async ({ playwright }) => {
    const port = 5071;
    const server = spawn('dotnet', [
      'run',
      '--project',
      '../../samples/Counter/Counter.Web/Counter.Web.csproj',
      '--configuration',
      'Release',
      '--no-build',
      '--no-launch-profile',
      '--urls',
      `http://127.0.0.1:${port}`,
    ], {
      cwd: process.cwd(),
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: 'Production',
        Hexalith__FrontComposer__Specimens__Enabled: '',
      },
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    let serverOutput = '';
    server.stdout.on('data', (chunk) => { serverOutput += chunk.toString(); });
    server.stderr.on('data', (chunk) => { serverOutput += chunk.toString(); });

    const api = await playwright.request.newContext({ baseURL: `http://127.0.0.1:${port}` });
    try {
      await waitForHost(api);
      const response = await api.get('/__frontcomposer/specimens/type', { failOnStatusCode: false });
      expect(response.status(), `Specimen route was exposed without explicit configuration.\n${serverOutput}`).toBeGreaterThanOrEqual(400);
    } finally {
      await api.dispose();
      server.kill();
    }
  });
});

const attachSpecimenGuards = (page: import('@playwright/test').Page) => {
  const consoleErrors: string[] = [];
  const unexpectedRequests: string[] = [];

  page.on('console', (message) => {
    if (message.type() === 'error') {
      if (message.text() === 'Failed to load resource: the server responded with a status of 404 (Not Found)') {
        return;
      }

      consoleErrors.push(message.text());
    }
  });

  page.on('request', (request) => {
    const url = new URL(request.url());
    const path = url.pathname;
    const allowed = path.startsWith('/_framework/')
      || path.startsWith('/_content/')
      || path.startsWith('/_blazor')
      || path.startsWith('/css/')
      || path.startsWith('/js/')
      || path.startsWith('/__frontcomposer/specimens/')
      || path === '/Counter.Web.styles.css'
      || path === '/'
      || path === '/favicon.ico';

    if (!allowed) {
      unexpectedRequests.push(`${request.method()} ${url.pathname}`);
    }
  });

  consoleErrorsByPage.set(page, consoleErrors);
  unexpectedRequestsByPage.set(page, unexpectedRequests);
  return { consoleErrors, unexpectedRequests };
};

const gotoSpecimen = async (page: import('@playwright/test').Page, route: SpecimenRoute): Promise<void> => {
  await page.goto(route.path);
  await expect(page.locator(route.readySelector), `${route.path} missing ready marker`).toBeVisible();
  for (const selector of route.requiredSections) {
    const count = await page.locator(selector).count();
    expect(count, `${route.path} missing ${selector}`).toBeGreaterThan(0);
  }
};

const prepareSpecimenVisualBaseline = async (page: import('@playwright/test').Page): Promise<void> => {
  await page.addStyleTag({
    content: `
      .pa-3.fluent-layout-item {
        overflow: visible !important;
        height: auto !important;
        max-height: none !important;
      }

      .fc-shell-root,
      fluent-layout,
      .fluent-layout {
        height: auto !important;
        min-height: auto !important;
        overflow: visible !important;
      }
    `,
  });

  await page.evaluate(() => {
    window.scrollTo(0, 0);
    for (const element of document.querySelectorAll<HTMLElement>('.fluent-layout-item')) {
      element.scrollTop = 0;
    }
  });
  await expect(page.getByTestId('fc-type-specimen')).toBeInViewport();
};

const scrollTestIdIntoView = async (page: import('@playwright/test').Page, testId: string): Promise<void> => {
  await expect(async () => {
    const locator = page.getByTestId(testId);
    await expect(locator).toHaveCount(1);
    await locator.scrollIntoViewIfNeeded();
    await expect(locator).toBeInViewport();
  }).toPass({ timeout: 5_000 });
};

const tabUntilTestId = async (page: import('@playwright/test').Page, testId: string): Promise<void> => {
  for (let attempt = 0; attempt < 20; attempt += 1) {
    await page.keyboard.press('Tab');
    const focusedTestId = await page.locator(':focus').getAttribute('data-testid');
    if (focusedTestId === testId) {
      return;
    }
  }

  throw new Error(`Expected focus to reach ${testId}`);
};

const waitForHost = async (request: import('@playwright/test').APIRequestContext): Promise<void> => {
  const deadline = Date.now() + 60_000;
  let lastError: unknown;
  while (Date.now() < deadline) {
    try {
      const response = await request.get('/', { failOnStatusCode: false, timeout: 2_000 });
      if (response.status() < 500) {
        return;
      }
    } catch (error) {
      lastError = error;
    }

    await new Promise((resolve) => setTimeout(resolve, 500));
  }

  throw new Error(`Counter production smoke host did not start: ${String(lastError)}`);
};
