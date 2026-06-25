// ATDD RED PHASE — Story 3-2 Task 10.11 (AC4, AC5, AC7)
// Selectors target data-testid attributes the sidebar components will emit after Task 6-8 land.
// Until then, Playwright times out on `waitFor` — the intended red-phase signal.

import type { Locator, Page } from '@playwright/test';

/**
 * Viewport-tier breakpoints — single source of truth for the E2E layer. Mirrors D4:
 * Desktop ≥ 1366, CompactDesktop 1024-1365, Tablet 768-1023, Phone < 768.
 * Any change here must land with a matching change in src/.../js/fc-layout-breakpoints.js
 * and ViewportTier enum (D4).
 */
export const ViewportBreakpoints = {
  desktopMin: 1366,
  compactDesktopMax: 1365,
  compactDesktopMin: 1024,
  tabletMax: 1023,
  tabletMin: 768,
  phoneMax: 767,
} as const;

/**
 * Page object for the framework-owned FrontComposerShell — covers the Story 8.5 unified
 * navigation rail and hamburger toggle. The legacy fullNav/collapsedRail property names are
 * retained for older specs: fullNav = 72px labeled rail, collapsedRail = 48px icon-only rail.
 */
export class ShellPage {
  readonly page: Page;
  readonly shellRoot: Locator;
  readonly navigationPane: Locator;
  readonly fullNav: Locator;
  readonly collapsedRail: Locator;
  readonly hamburgerToggle: Locator;
  readonly counterCategory: Locator;
  readonly counterFlyout: Locator;
  readonly counterProjectionItem: Locator;

  constructor(page: Page) {
    this.page = page;
    this.shellRoot = page.locator('.fc-shell-root');
    this.navigationPane = page.getByTestId('fc-shell-navigation');
    this.fullNav = page.locator('[data-testid="fc-navigation-rail"][data-rail-width="72"]');
    this.collapsedRail = page.locator('[data-testid="fc-navigation-rail"][data-rail-width="48"]');
    this.hamburgerToggle = page.getByTestId('fc-hamburger-toggle');
    this.counterCategory = page.getByTestId('fc-nav-context-Counter');
    this.counterFlyout = page.getByTestId('fc-nav-flyout-Counter');
    this.counterProjectionItem = page.getByTestId('fc-nav-flyout-projection-Counter-CounterProjection');
  }

  /**
   * Navigates to the Counter sample's root (MainLayout is the shell-under-test after Story 3-2 Task 9).
   */
  async goto(): Promise<void> {
    await this.page.goto('/');
    await this.shellRoot.waitFor();
  }

  async resizeTo(width: number, height = 900): Promise<void> {
    await this.page.setViewportSize({ width, height });
    // No explicit wait — callers assert tier-specific visibility via expect().toBeVisible(),
    // which polls until the matchMedia → JS module → C# dispatch chain lands. Deterministic,
    // no compounding timeouts. See test-review F1 (2026-04-19).
  }

  async isFullNavVisible(): Promise<boolean> {
    return this.fullNav.isVisible();
  }

  async isCollapsedRailVisible(): Promise<boolean> {
    return this.collapsedRail.isVisible();
  }

  async isHamburgerVisible(): Promise<boolean> {
    return this.hamburgerToggle.isVisible();
  }

  async clickHamburger(): Promise<void> {
    await this.hamburgerToggle.click();
  }

  async openCounterFlyoutWithKeyboard(key: 'Enter' | 'Space'): Promise<void> {
    await this.counterCategory.focus();
    await this.page.keyboard.press(key);
    await this.counterFlyout.waitFor({ state: 'visible' });
  }
}
