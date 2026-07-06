// ATDD RED PHASE — Story 3-2 Task 10.11 (AC4, AC5, AC7)
// These specs fail with selector timeouts until the sidebar components render their data-testid
// markers (Task 6-8) and Counter.Web boots with the framework sidebar (Task 9).

import { expect, test } from '../fixtures/index.js';
import { expectNoBlockingAxeViolations } from '../helpers/a11y.js';
import { SettingsPage, type ThemeLabel } from '../page-objects/settings.page.js';
import { ShellPage, ViewportBreakpoints } from '../page-objects/shell.page.js';

test.describe('Story 8.5: navigation rail responsive behavior @p0 @smoke', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      const marker = '__frontcomposer_e2e_sidebar_storage_cleared';
      if (window.sessionStorage.getItem(marker) !== 'true') {
        window.localStorage.clear();
        window.sessionStorage.clear();
        window.sessionStorage.setItem(marker, 'true');
      }
    });
  });

  test('resizes across tiers: labeled rail → icon-only rail → drawer-only @p0 @smoke', async ({ page, tenant }) => {
    // F17 — pulls the tenant fixture so IUserContextAccessor resolves; without it, any
    // NavigationEffects path fail-closes (AC2 scope guard) and tests would pass for the wrong reason.
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();

    // --- Desktop (≥1366) — AC3 ---
    // Story 8.5: the hamburger is visible at Desktop and toggles the rail between
    // 72px labeled and 48px icon-only modes.
    await shell.resizeTo(1920);
    await expect(shell.fullNav).toBeVisible();
    await expect(shell.collapsedRail).toBeHidden();
    await expect(shell.hamburgerToggle).toBeVisible();

    // --- CompactDesktop (1024–1365) — AC4 ---
    await shell.resizeTo(1200);
    await expect(shell.collapsedRail).toBeVisible();
    await expect(shell.fullNav).toBeHidden();
    await expect(shell.hamburgerToggle).toBeVisible();

    // --- Tablet (768–1023) — AC5 ---
    await shell.resizeTo(900);
    await expect(shell.navigationPane).toBeHidden();
    await expect(shell.hamburgerToggle).toBeVisible();

    // --- Phone (<768) — AC5 ---
    await shell.resizeTo(600);
    await expect(shell.navigationPane).toBeHidden();
    await expect(shell.hamburgerToggle).toBeVisible();
  });

  // F11 — boundary coverage. Catches off-by-one in `matchMedia` queries (e.g., `(min-width: 1366px)`
  // vs `(min-width: 1367px)`). The interior-width test above can't see this class of bug.
  const boundaryCases = [
    { width: ViewportBreakpoints.desktopMin, tier: 'Desktop', expectFull: true, expectRail: false, expectHamburger: true },
    { width: ViewportBreakpoints.compactDesktopMax, tier: 'CompactDesktop-upper', expectFull: false, expectRail: true, expectHamburger: true },
    { width: ViewportBreakpoints.compactDesktopMin, tier: 'CompactDesktop-lower', expectFull: false, expectRail: true, expectHamburger: true },
    { width: ViewportBreakpoints.tabletMax, tier: 'Tablet-upper', expectFull: false, expectRail: false, expectHamburger: true },
    { width: ViewportBreakpoints.tabletMin, tier: 'Tablet-lower', expectFull: false, expectRail: false, expectHamburger: true },
    { width: ViewportBreakpoints.phoneMax, tier: 'Phone', expectFull: false, expectRail: false, expectHamburger: true },
  ] as const;

  for (const { width, tier, expectFull, expectRail, expectHamburger } of boundaryCases) {
    test(`tier boundary at ${width}px resolves to ${tier} @p1`, async ({ page, tenant }) => {
      expect(tenant.tenantId).toBeTruthy();
      const shell = new ShellPage(page);
      await shell.goto();
      await shell.resizeTo(width);

      if (expectFull) {
        await expect(shell.fullNav).toBeVisible();
      } else {
        await expect(shell.fullNav).toBeHidden();
      }
      if (expectRail) {
        await expect(shell.collapsedRail).toBeVisible();
      } else {
        await expect(shell.collapsedRail).toBeHidden();
      }
      if (expectHamburger) {
        await expect(shell.hamburgerToggle).toBeVisible();
      } else {
        await expect(shell.hamburgerToggle).toBeHidden();
      }
    });
  }

  test('sidebar collapse persists across refresh and resize back to Desktop (AC2 round-trip) @p0', async ({ page, tenant }) => {
    // F17 — tenant fixture is mandatory here: AC2 hydrate path skips when IUserContextAccessor.TenantId
    // is null/empty. Without the fixture, reload would no-op and the test would pass by coincidence.
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();

    // Round-trip steps:
    //   1. Start at Desktop: 72px labeled rail.
    //   2. Click hamburger: 48px icon-only rail through SidebarToggledAction.
    //   3. Reload: collapsed state persists at Desktop.
    await shell.resizeTo(1920);
    await expect(shell.fullNav).toBeVisible();
    await shell.clickHamburger();
    await expect(shell.collapsedRail).toBeVisible();

    await page.reload();
    await shell.shellRoot.waitFor();
    await expect(shell.collapsedRail).toBeVisible({ timeout: 5_000 });
    await expect(shell.fullNav).toBeHidden();
    await expect(shell.hamburgerToggle).toBeVisible();
  });

  test('Counter bounded context renders one rail tile with one projection menu item (AC1 + AC7) @p0 @smoke', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(1920);

    await expect(shell.counterCategory).toBeVisible();
    await shell.counterCategory.click();
    await expect(shell.counterFlyout).toHaveAttribute('role', 'menu');
    await expect(shell.counterProjectionItem).toBeVisible();
    await expect(shell.counterProjectionItem).toHaveCount(1);
  });

  test('Counter flyout opens from keyboard and Escape restores focus to the tile (AC4) @p0', async ({
    page,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();
    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(1920);

    await shell.openCounterFlyoutWithKeyboard('Space');

    await expect(shell.counterFlyout).toHaveAttribute('role', 'menu');
    await expect(shell.counterProjectionItem).toBeVisible();

    await page.keyboard.press('Escape');

    await expect(shell.counterProjectionItem).toBeHidden();
    await expect(page.locator(':focus')).toHaveAttribute('data-testid', 'fc-nav-context-Counter');
  });

  test('Counter projection menu item activates from keyboard and marks one active route (AC3 + AC4) @p0', async ({
    page,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();
    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(1920);

    await shell.openCounterFlyoutWithKeyboard('Enter');
    await shell.counterProjectionItem.focus();
    await page.keyboard.press('Enter');

    await expect(page).toHaveURL(/\/counter\/counter-projection$/);
    await expect(shell.counterCategory).toHaveAttribute('aria-current', 'page');
    await expect(shell.counterProjectionItem).toHaveAttribute('aria-current', 'page');
    await expect(page.locator('[data-href][aria-current="page"]')).toHaveCount(1);
  });

  for (const theme of ['Light', 'Dark'] as const satisfies readonly ThemeLabel[]) {
    test(`Counter rail and flyout pass light/dark visual and a11y checks in ${theme.toLowerCase()} theme (AC2 + AC5) @p1`, async ({
      page,
      tenant,
    }, testInfo) => {
      expect(tenant.tenantId).toBeTruthy();
      const shell = new ShellPage(page);
      await shell.goto();
      await shell.resizeTo(1920);

      const settings = new SettingsPage(page);
      await settings.openViaButton();
      await settings.selectTheme(theme);
      await page.getByTestId('fc-settings-done').click();

      await page.goto('/counter/counter-projection');
      await shell.shellRoot.waitFor();
      await shell.counterCategory.click();
      await expect(shell.counterProjectionItem).toBeVisible();

      await assertActiveRailUsesAccentThreadOnly(shell);
      await expectNoBlockingAxeViolations(page, {
        route: `Story 8.5 navigation rail ${theme}`,
        include: [
          '[data-testid="fc-shell-navigation"] [data-testid="fc-navigation-rail"]',
          '[data-testid="fc-shell-navigation"] [data-testid="fc-nav-flyout-Counter"]',
        ],
        requiredSelectors: [
          '[data-testid="fc-shell-navigation"] [data-testid="fc-navigation-rail"]',
          '[data-testid="fc-shell-navigation"] [data-testid="fc-nav-flyout-Counter"]',
          '[data-testid="fc-shell-navigation"] [data-testid="fc-nav-flyout-projection-Counter-CounterProjection"]',
        ],
        artifactPath: testInfo.outputPath(`axe-story-8-5-nav-${theme.toLowerCase()}.json`),
      });
    });
  }
});

const assertActiveRailUsesAccentThreadOnly = async (shell: ShellPage): Promise<void> => {
  await expect(shell.counterCategory).toHaveAttribute('data-active', 'true');

  const visual = await shell.counterCategory.evaluate((element) => {
    const styles = getComputedStyle(element);
    const probe = document.createElement('span');
    probe.style.backgroundColor = 'var(--fc-color-accent, var(--fc-accent-base-color))';
    probe.style.display = 'none';
    element.appendChild(probe);
    const accentBackground = getComputedStyle(probe).backgroundColor;
    probe.remove();

    return {
      accentBackground,
      backgroundColor: styles.backgroundColor,
      borderInlineStartColor: styles.borderInlineStartColor,
      borderInlineStartStyle: styles.borderInlineStartStyle,
      borderInlineStartWidth: styles.borderInlineStartWidth,
    };
  });

  expect(visual.borderInlineStartWidth).toBe('3px');
  expect(visual.borderInlineStartStyle).toBe('solid');
  expect(visual.borderInlineStartColor).toBe(visual.accentBackground);
  expect(visual.backgroundColor).not.toBe(visual.accentBackground);
};
