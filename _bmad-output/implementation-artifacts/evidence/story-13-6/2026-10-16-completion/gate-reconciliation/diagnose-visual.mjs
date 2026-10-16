// Diagnostic only: reproduces canonical visual preparation without altering source or baselines.
// Run from repository root: node <this-file> <output-directory>.
import { createRequire } from 'node:module';
import { mkdir, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
const require = createRequire(resolve('tests/e2e/package.json'));
const { chromium, firefox, webkit, devices, expect } = require('@playwright/test');
const output = resolve(process.argv[2]);
const theme = process.argv.find(arg => arg.startsWith('--theme='))?.slice(8) ?? 'light';
const density = process.argv.find(arg => arg.startsWith('--density='))?.slice(10) ?? 'compact';
await mkdir(output, { recursive: true });
const results = [];
for (const [name, browserType, device] of [
  ['chromium', chromium, 'Desktop Chrome'],
  ['firefox', firefox, 'Desktop Firefox'],
  ['webkit', webkit, 'Desktop Safari'],
]) {
  const browser = await browserType.launch();
  try {
    const context = await browser.newContext({ ...devices[device], locale: 'fr-FR', timezoneId: 'UTC', deviceScaleFactor: 1, viewport: { width: 1280, height: 900 }, reducedMotion: 'reduce' });
    const page = await context.newPage();
    const fonts = [], failed = [], errors = [];
    page.on('request', request => { if (request.resourceType() === 'font') fonts.push(new URL(request.url()).pathname); });
    page.on('requestfailed', request => failed.push({ path: new URL(request.url()).pathname, error: request.failure()?.errorText }));
    page.on('pageerror', error => errors.push(error.message));
    await page.goto(`http://127.0.0.1:5070/__frontcomposer/specimens/type?theme=${theme}&density=${density}`);
    await expect(page.getByTestId('fc-type-specimen')).toBeVisible();
    await expect(page.locator('.fc-shell-root')).toHaveAttribute('data-fc-interactive', 'true');
    await expect(page.locator('body')).toHaveAttribute('data-fc-density', 'compact');
    await page.waitForFunction(() => [...document.querySelectorAll('*')].filter(e => e.localName.includes('-')).every(e => customElements.get(e.localName) !== undefined));
    await page.evaluate(() => document.fonts.ready);
    await page.addStyleTag({ content: '.pa-3.fluent-layout-item{overflow:visible!important;height:auto!important;max-height:none!important}.fc-shell-root,fluent-layout,.fluent-layout{height:auto!important;min-height:auto!important;overflow:visible!important}' });
    await page.evaluate(() => { window.scrollTo(0, 0); document.querySelectorAll('.fluent-layout-item').forEach(e => { e.scrollTop = 0; }); });
    await expect(page.getByTestId('fc-type-specimen')).toBeInViewport();
    let previous = -1, stable = 0;
    await expect.poll(async () => { const height = await page.evaluate(() => document.documentElement.scrollHeight); stable = height === previous ? stable + 1 : 0; previous = height; return stable; }, { intervals: [100, 100, 250, 250], timeout: 10000 }).toBeGreaterThanOrEqual(2);
    const measure = () => page.evaluate(() => {
      const canvas = document.createElement('canvas').getContext('2d');
      const text = 'Type, theme, density, lifecycle, and navigation specimen';
      const metrics = {};
      metrics.inheritedFluentStack = (() => { canvas.font = `16px ${getComputedStyle(document.body).fontFamily}`; return canvas.measureText(text).width; })();
      for (const family of ['sans-serif', 'serif', 'system-ui', 'Noto Sans', 'DejaVu Sans', 'Arial', 'Liberation Sans', 'Segoe UI', 'Roboto', '-apple-system']) {
        canvas.font = ['sans-serif', 'serif', 'system-ui'].includes(family) ? `16px ${family}` : `16px "${family}"`;
        metrics[family] = canvas.measureText(text).width;
      }
      const samples = ['body', '.fc-shell-root', '#fc-type-specimen-title', '[data-testid="fc-type-slot-display"]', '[data-testid="fc-type-slot-display"] .fc-specimen__display', '[data-testid="fc-theme-control-light"]', '[data-testid="fc-density-state-compact"] button', '[data-testid="fc-generated-counter-grid"]', '.fc-projection-grid tr', '[data-testid="fc-generated-counter-grid"] .fc-projection-grid tbody tr', '.fc-field-placeholder', '#fc-specimen-command-form', '[data-testid="fc-destructive-command-specimen"]', '[data-testid="fc-policy-command-specimen"]', '[data-testid="fc-expanded-detail"]'].map(selector => {
        const element = document.querySelector(selector);
        if (!element) return { selector, missing: true };
        const s = getComputedStyle(element), r = element.getBoundingClientRect();
        return { selector, fontFamily: s.fontFamily, fontSize: s.fontSize, fontWeight: s.fontWeight, lineHeight: s.lineHeight, padding: s.padding, minHeight: s.minHeight, x: r.x, y: r.y, width: r.width, height: r.height };
      });
      return { userAgent: navigator.userAgent, locale: navigator.language, dpr: devicePixelRatio, viewport: [innerWidth, innerHeight], documentHeight: document.documentElement.scrollHeight, fontSetStatus: document.fonts.status, fontFaces: [...document.fonts].map(f => ({ family: f.family, status: f.status })), fontFamilyBase: getComputedStyle(document.body).getPropertyValue('--fontFamilyBase'), metrics, samples };
    });
    const initial = await measure();
    await page.screenshot({ path: resolve(output, `${name}-initial.png`), fullPage: true, animations: 'disabled' });
    // A second font-ready/layout-stable capture tests loading races without fixed sleeps.
    await page.evaluate(() => document.fonts.ready);
    await expect(page.getByTestId('fc-type-specimen')).toBeVisible();
    const settled = await measure();
    await page.screenshot({ path: resolve(output, `${name}-settled.png`), fullPage: true, animations: 'disabled' });
    let counterfactual;
    if (process.argv.includes('--historical-layout')) {
      // Deliberately undo only two identified historical layout deltas in this page.
      // This is diagnostic evidence, never a replacement for canonical assertions.
      await page.addStyleTag({ content: '.fc-shell-root{overflow-wrap:normal!important}.fc-specimen a,.fc-specimen summary{display:revert!important;min-block-size:auto!important}.fc-specimen .fc-specimen__control{display:inline-flex!important}' });
      counterfactual = await measure();
      await page.screenshot({ path: resolve(output, `${name}-historical-layout.png`), fullPage: true, animations: 'disabled' });
    }
    results.push({ engine: name, theme, density, version: browser.version(), initial, settled, counterfactual, fontRequests: fonts, failedRequests: failed, pageErrors: errors });
    await context.close();
  } finally { await browser.close(); }
}
await writeFile(resolve(output, 'measurements.json'), JSON.stringify(results, null, 2) + '\n');
console.log(JSON.stringify(results.map(r => ({ engine: r.engine, height: r.initial.documentHeight, fontFamily: r.initial.samples[0].fontFamily, fontRequests: r.fontRequests, pageErrors: r.pageErrors }))));
