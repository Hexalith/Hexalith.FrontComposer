// ATDD RED PHASE — Story 3-2 Task 10.11 (AC4, AC5, AC7)
// These specs fail with selector timeouts until the sidebar components render their data-testid
// markers (Task 6-8) and Counter.Web boots with the framework sidebar (Task 9).

import { expect, test } from '../fixtures/index.js';
import { ShellPage, ViewportBreakpoints } from '../page-objects/shell.page.js';

test.describe('Story 3-2: sidebar navigation responsive behavior @p0 @smoke', () => {
  test('resizes across tiers: full nav → rail → drawer-only @p0 @smoke', async ({ page, tenant }) => {
    // F17 — pulls the tenant fixture so IUserContextAccessor resolves; without it, any
    // NavigationEffects path fail-closes (AC2 scope guard) and tests would pass for the wrong reason.
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();

    // --- Desktop (≥1366) — AC3 ---
    await shell.resizeTo(1920);
    await expect(shell.fullNav).toBeVisible();
    await expect(shell.collapsedRail).toBeHidden();
    await expect(shell.hamburgerToggle).toBeHidden();

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
    { width: ViewportBreakpoints.desktopMin, tier: 'Desktop', expectFull: true, expectRail: false, expectHamburger: false },
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

  test('sidebar collapse persists across refresh (AC2 round-trip at Desktop) @p0', async ({ page, tenant }) => {
    // F17 — tenant fixture is mandatory here: AC2 hydrate path skips when IUserContextAccessor.TenantId
    // is null/empty. Without the fixture, reload would no-op and the test would pass by coincidence.
    expect(tenant.tenantId).toBeTruthy();

    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(1920);
    await expect(shell.fullNav).toBeVisible();

    // Manual collapse at Desktop → hamburger appears, full nav swaps to rail (D9).
    await shell.clickHamburger();
    await expect(shell.collapsedRail).toBeVisible({ timeout: 2_000 });

    // Refresh — the NavigationEffects hydrate path must restore SidebarCollapsed=true.
    await page.reload();
    await shell.shellRoot.waitFor();
    await expect(shell.collapsedRail).toBeVisible({ timeout: 5_000 });
    await expect(shell.fullNav).toBeHidden();
  });

  test('Counter bounded context renders exactly one nav category with one projection item (AC1 + AC7) @p0 @smoke', async ({ page, tenant }) => {
    expect(tenant.tenantId).toBeTruthy();
    const shell = new ShellPage(page);
    await shell.goto();
    await shell.resizeTo(1920);

    await expect(shell.counterCategory).toBeVisible();
    const counterItems = shell.counterCategory.getByRole('link');
    await expect(counterItems).toHaveCount(1);
  });
});
