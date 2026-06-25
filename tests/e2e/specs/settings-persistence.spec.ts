// QA E2E — Story 1.6: Theme, density, and settings persistence (FR15, NFR6, NFR9).
//
// These specs fill the E2E gaps left by density-transition.spec.ts, which already covers the
// settings *button* open path and in-session viewport forcing. They add the missing surfaces:
//   AC1 — the Ctrl+, / Meta+, keyboard entry point (the second open path through the single
//         FcSettingsDialogLauncher.ShowAsync) and the dialog exposing all three controls
//         (density radio group + embedded FcThemeToggle + FcDensityPreviewPanel live preview).
//   AC2 — density AND theme preferences *persisting across a full page reload* (the headline
//         "remembers my preferences across sessions" claim — persist -> flush -> hydrate), plus
//         density changes being announced through the aria-live FcDensityAnnouncer (NFR6).
//
// AC3 (single-writer discipline) is enforced by the .NET SliceSingleWriterGovernanceTests +
// NFR17ComplianceTripwireTests and is not an E2E concern.
//
// The tenant fixture is pulled in every test so IUserContextAccessor resolves a tenant/user;
// without it density/theme persist fail-closes (scope guard, HFC2105) and the persistence
// assertions would pass for the wrong reason.

import { expect, test } from '../fixtures/index.js';
import { SettingsPage } from '../page-objects/settings.page.js';
import { ShellPage, ViewportBreakpoints } from '../page-objects/shell.page.js';

const DESKTOP_WIDTH = 1920; // >= desktopMin (1366): user density preference applies, no viewport forcing.

test.describe('Story 1.6: theme, density, and settings persistence @p1 @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      const marker = '__frontcomposer_e2e_storage_cleared';
      if (window.sessionStorage.getItem(marker) !== 'true') {
        window.localStorage.clear();
        window.sessionStorage.clear();
        window.sessionStorage.setItem(marker, 'true');
      }
    });
  });

  test('Ctrl+, opens the settings dialog exposing density radios, theme toggle, and live preview @p1', async ({
    page,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(DESKTOP_WIDTH);

    const settings = new SettingsPage(page);
    await settings.openViaShortcut();

    // AC1 — the keyboard path lands on the same dialog the button uses.
    await expect(settings.dialog).toBeVisible();
    await expect(settings.dialogBody).toBeVisible();

    // AC1 — density FluentRadioGroup (Compact / Comfortable / Roomy).
    await expect(settings.densityRadio('Compact')).toBeVisible();
    await expect(settings.densityRadio('Comfortable')).toBeVisible();
    await expect(settings.densityRadio('Roomy')).toBeVisible();

    // AC1 — embedded FcThemeToggle + FcDensityPreviewPanel live preview.
    await expect(settings.themeSection).toBeVisible();
    await expect(settings.themeToggleButton).toBeVisible();
    await expect(settings.densityPreview).toBeVisible();
  });

  test('density preference persists across a page reload @p1', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(DESKTOP_WIDTH);

    const settings = new SettingsPage(page);
    await settings.openViaButton();

    // Pick a non-default density (default is Compact) so restoration is observable.
    await settings.selectDensity('Roomy');

    // Live (no-Apply): the body cascade reflects the choice immediately (FcDensityApplier).
    await expect.poll(() => settings.appliedDensity()).toBe('roomy');

    // The write must drain into localStorage before we reload (fire-and-forget channel).
    await expect.poll(() => settings.storedValue(tenant, 'density')).not.toBeNull();

    // Reload — the viewport (context-level) is preserved, so no viewport forcing on rehydrate.
    await page.reload();
    await shell.shellRoot.waitFor();

    // AC2 — density is restored from IStorageService on app-init hydration.
    await expect.poll(() => settings.appliedDensity()).toBe('roomy');
  });

  test('desktop sessions without a stored density preference settle on Compact @p1', async ({
    page,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(DESKTOP_WIDTH);

    const settings = new SettingsPage(page);

    await expect.poll(() => settings.storedValue(tenant, 'density')).toBeNull();
    await expect.poll(() => settings.appliedDensity()).toBe('compact');
  });

  test('Restore defaults clears the density preference back to Compact at desktop @p1', async ({
    page,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(DESKTOP_WIDTH);

    const settings = new SettingsPage(page);
    await settings.openViaButton();
    await settings.selectDensity('Roomy');

    await expect.poll(() => settings.appliedDensity()).toBe('roomy');
    await expect.poll(() => settings.storedValue(tenant, 'density')).not.toBeNull();

    await settings.restoreDefaults();

    await expect.poll(() => settings.storedValue(tenant, 'density')).toBeNull();
    await expect.poll(() => settings.appliedDensity()).toBe('compact');
    await expect(settings.densityRadio('Compact')).toBeChecked();
  });

  test('tablet viewport still forces Comfortable even when Compact is selected @p1', async ({
    page,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(DESKTOP_WIDTH);

    const settings = new SettingsPage(page);
    await settings.openViaButton();
    await settings.selectDensity('Roomy');
    await expect.poll(() => settings.appliedDensity()).toBe('roomy');

    await settings.selectDensity('Compact');

    await expect.poll(() => settings.appliedDensity()).toBe('compact');

    await shell.resizeTo(ViewportBreakpoints.tabletMax);

    await expect.poll(() => settings.appliedDensity()).toBe('comfortable');
    await expect(page.getByTestId('fc-settings-forced-note')).toBeVisible();
  });

  test('theme preference is persisted to scoped storage and survives a reload @p1', async ({
    page,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(DESKTOP_WIDTH);

    const settings = new SettingsPage(page);
    await settings.openViaButton();

    await settings.selectTheme('Dark');

    // AC2 — theme persists to the scoped key {tenantId}:{userId}:theme (ThemeEffects single writer).
    await expect.poll(() => settings.storedValue(tenant, 'theme')).not.toBeNull();
    const persisted = await settings.storedValue(tenant, 'theme');

    await page.reload();
    await shell.shellRoot.waitFor();

    // AC2 — the persisted theme survives the reload (read back on hydration).
    await expect.poll(() => settings.storedValue(tenant, 'theme')).toBe(persisted);
  });

  test('density change is announced through the aria-live announcer @p1', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(DESKTOP_WIDTH);

    const settings = new SettingsPage(page);
    await settings.openViaButton();

    const announcer = settings.densityAnnouncer.first();
    await expect(announcer).toBeAttached();
    // NFR6 / WCAG — the first render is suppressed, so the region starts empty.
    await expect(announcer).toBeEmpty();

    // Change density away from the default (Compact) — this drives an EffectiveDensity change.
    await settings.selectDensity('Roomy');

    // AC2 / NFR6 — the change is announced (region gains text) for assistive technology.
    await expect(announcer).not.toBeEmpty();
  });
});
