import { expect, type Locator, type Page } from '@playwright/test';

import type { TenantContext } from '../fixtures/tenant.fixture.js';
import { ViewportBreakpoints } from './shell.page.js';

/**
 * Density radio values exactly as emitted by FcSettingsDialog's FluentRadioGroup
 * (src/.../Components/Layout/FcSettingsDialog.razor:22-30). The body data-fc-density
 * attribute is the lower-cased form.
 */
export type DensityValue = 'Compact' | 'Comfortable' | 'Roomy';

/**
 * English theme labels rendered by FcThemeToggle's FluentMenuItems
 * (src/.../Components/Layout/FcThemeToggle.razor; FcShellResources.resx Theme*Label).
 */
export type ThemeLabel = 'Light' | 'Dark' | 'System';

/**
 * Page object for Story 1.6 — theme, density, and settings persistence.
 *
 * Encapsulates the framework-owned FcSettingsDialog and its two open paths (the header
 * FcSettingsButton and the Ctrl+, / Meta+, shortcut), both of which route through the single
 * FcSettingsDialogLauncher.ShowAsync (AC1). Selectors are grounded against the real source:
 *   - dialog body  data-testid="fc-settings-dialog"   (FcSettingsDialog.razor:18)
 *   - settings btn data-testid="fc-settings-button"   (FcSettingsButton — shared launcher)
 *   - density preview data-testid="fc-density-preview" (FcDensityPreviewPanel.razor)
 *   - density announcer div[role="status"][aria-live="polite"].fc-sr-only (FcDensityAnnouncer.razor)
 *
 * Persistence is asserted through observable surfaces only: body[data-fc-density] for the full
 * persist -> flush -> hydrate -> DOM loop, and the scoped localStorage keys
 * {tenant}:{user}:{theme|density} for the write side (StorageKeys.BuildKey + LocalStorageService),
 * where tenant/user segments are trimmed, NFC-normalized, URL-encoded, and email users are lower-cased.
 */
export class SettingsPage {
  readonly page: Page;
  readonly settingsButton: Locator;
  readonly dialog: Locator;
  readonly dialogBody: Locator;
  readonly themeSection: Locator;
  readonly themeToggleButton: Locator;
  readonly densityPreview: Locator;
  readonly densityAnnouncer: Locator;
  readonly restoreDefaultsButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.settingsButton = page.getByTestId('fc-settings-button');
    this.dialogBody = page.getByTestId('fc-settings-dialog');
    this.dialog = page.getByRole('alertdialog');
    this.themeSection = page.locator('#fc-theme-section');
    // FcThemeToggle renders a FluentMenuButton carrying Title="Change theme" (ThemeToggleAriaLabel).
    this.themeToggleButton = page.getByTitle('Change theme').nth(1);
    this.densityPreview = page.getByTestId('fc-density-preview');
    this.restoreDefaultsButton = page.getByTestId('fc-settings-reset');
    // FcDensityAnnouncer: visually-hidden role="status" region (aria-atomic distinguishes it).
    this.densityAnnouncer = page.locator(
      'div.fc-sr-only[role="status"][aria-live="polite"][aria-atomic="true"]',
    );
  }

  densityRadio(value: DensityValue): Locator {
    return this.page.locator(`fluent-radio[value="${value}"]`);
  }

  /** Opens the dialog via the header settings button (one of the two AC1 entry points). */
  async openViaButton(): Promise<void> {
    await expect(this.page.locator('.fc-shell-root[data-fc-interactive="true"]')).toBeVisible();
    await expect(this.settingsButton).toBeVisible();
    await expect(this.settingsButton).toBeEnabled();
    await this.settingsButton.click();
    await this.dialogBody.waitFor();
  }

  /**
   * Opens the dialog via the Ctrl+, shortcut (Meta+, on macOS) — the keyboard AC1 entry point.
   * Both paths land on the same FcSettingsDialogLauncher.ShowAsync.
   */
  async openViaShortcut(): Promise<void> {
    const accelerator = process.platform === 'darwin' ? 'Meta+Comma' : 'Control+Comma';
    await this.page.locator('.fc-shell-root[data-fc-interactive="true"]').focus();
    await this.page.keyboard.press(accelerator);
    await this.dialogBody.waitFor();
  }

  /** Selects a density level via the live (no-Apply) radio group. */
  async selectDensity(value: DensityValue): Promise<void> {
    await this.densityRadio(value).focus();
    await this.page.keyboard.press('Space');
  }

  async expectDensitySelected(value: DensityValue): Promise<void> {
    await expect.poll(() => this.densityRadio(value).evaluate((radio) => (radio as HTMLInputElement).checked)).toBe(true);
  }

  async expandThemeSection(): Promise<void> {
    await this.dialogBody.getByRole('button', { name: 'Theme', exact: true }).click();
  }

  async expandPreviewSection(): Promise<void> {
    await this.dialogBody.getByRole('button', { name: 'Preview', exact: true }).click();
  }

  /** Clears persisted shell preferences via the dialog's Restore defaults action. */
  async restoreDefaults(): Promise<void> {
    await this.restoreDefaultsButton.click();
  }

  /** Selects a theme via the embedded FcThemeToggle menu. */
  async selectTheme(label: ThemeLabel): Promise<void> {
    await this.expandThemeSection();
    await this.themeToggleButton.click();
    await this.page.getByRole('menuitem', { name: label, exact: true }).click();
  }

  /** Reads the body's applied density (the single DOM writer is FcDensityApplier, ADR-041). */
  async appliedDensity(): Promise<string | undefined> {
    return this.page.evaluate(() => document.body.dataset.fcDensity);
  }

  /** Builds the scoped persistence key matching StorageKeys.BuildKey(tenantId, userId, feature). */
  static storageKey(tenant: TenantContext, feature: 'theme' | 'density'): string {
    return `${SettingsPage.tenantSegment(tenant.tenantId)}:${SettingsPage.userSegment(tenant.userId)}:${feature}`;
  }

  private static tenantSegment(value: string): string {
    return SettingsPage.escapeDataString(SettingsPage.dotnetTrim(value).normalize('NFC'));
  }

  private static userSegment(value: string): string {
    const normalized = SettingsPage.dotnetTrim(value).normalize('NFC');
    return SettingsPage.escapeDataString(normalized.includes('@') ? normalized.toLowerCase() : normalized);
  }

  /**
   * Mirrors .NET `Uri.EscapeDataString` (RFC 3986 unreserved set): `encodeURIComponent`
   * leaves `!'()*` unescaped, so encode them explicitly to keep this helper byte-identical
   * to FrontComposerStorageKey canonicalization.
   */
  private static escapeDataString(value: string): string {
    return encodeURIComponent(value).replace(
      /[!'()*]/g,
      (c) => `%${c.charCodeAt(0).toString(16).toUpperCase()}`,
    );
  }

  /**
   * Mirrors .NET `string.Trim()` (`char.IsWhiteSpace`): JS `trim()` strips BOM (U+FEFF,
   * which .NET does not treat as whitespace) but misses NEL (U+0085). Trim exactly the
   * .NET whitespace set so keys match the runtime for edge-whitespace identities.
   */
  private static dotnetTrim(value: string): string {
    return value.replace(
      /^[\t-\r \u0085\u00A0\u1680\u2000-\u200A\u2028\u2029\u202F\u205F\u3000]+|[\t-\r \u0085\u00A0\u1680\u2000-\u200A\u2028\u2029\u202F\u205F\u3000]+$/g,
      '',
    );
  }

  /** Reads a raw persisted localStorage value (post-drain) for the scoped feature key. */
  async storedValue(tenant: TenantContext, feature: 'theme' | 'density'): Promise<string | null> {
    const key = SettingsPage.storageKey(tenant, feature);
    return this.page.evaluate((k) => window.localStorage.getItem(k), key);
  }
}

export { ViewportBreakpoints };
