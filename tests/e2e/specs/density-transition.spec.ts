// ATDD RED PHASE — Story 3-3 Task 10.12 (D7, D9, D11, D12, D16; AC1, AC2, AC4, AC6, AC7)
// These specs fail with selector timeouts and DOM-attribute miss until:
//   Task 4 — fc-density.js + body[data-fc-density] applier mounts
//   Task 5 — FcSettingsButton auto-populates HeaderEnd
//   Task 6 — FcSettingsDialog opens via IDialogService
//   Task 8 — Ctrl+, opens the dialog
//   Task 9 — Counter.Web boots with the framework defaults

import { expect, test } from '../fixtures/index.js';
import { ShellPage, ViewportBreakpoints } from '../page-objects/shell.page.js';

const SETTINGS_BUTTON = '[data-testid="fc-settings-button"]';
const SETTINGS_DIALOG = '[role="dialog"]';
const DENSITY_RADIO_COMPACT = 'input[type="radio"][value="Compact"]';

test.describe('Story 3-3: display density and user settings @p0 @smoke', () => {
  test('settings button opens dialog at desktop @p1', async ({ page, tenant }) => {
    // F17 — pulls the tenant fixture so IUserContextAccessor resolves; without it, density persist
    // fail-closes (AC3 scope guard) and tests would pass for the wrong reason.
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(1920);

    // Auto-populated settings button visible in HeaderEnd (D12).
    await expect(page.locator(SETTINGS_BUTTON)).toBeVisible();
    await page.locator(SETTINGS_BUTTON).click();
    await expect(page.locator(SETTINGS_DIALOG)).toBeVisible();
  });

  test('density auto-switches at Tablet boundary preserving user preference @p1', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(1920);

    // Open settings + select Compact.
    await page.locator(SETTINGS_BUTTON).click();
    await expect(page.locator(SETTINGS_DIALOG)).toBeVisible();
    await page.locator(DENSITY_RADIO_COMPACT).check();

    // Body cascade reflects the user choice at Desktop (AC6).
    await expect.poll(() => page.evaluate(() => document.body.dataset.fcDensity)).toBe('compact');

    // Resize down to Tablet — viewport-forced Comfortable (ADR-040).
    await shell.resizeTo(ViewportBreakpoints.tabletMax);
    await expect.poll(() => page.evaluate(() => document.body.dataset.fcDensity)).toBe('comfortable');

    // Resize back to Desktop — user preference re-applies without action (AC4).
    await shell.resizeTo(1920);
    await expect.poll(() => page.evaluate(() => document.body.dataset.fcDensity)).toBe('compact');
  });
});
