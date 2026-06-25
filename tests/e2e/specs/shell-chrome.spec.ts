import type { Page } from '@playwright/test';

import { expect, test } from '../fixtures/index.js';
import { SettingsPage, type ThemeLabel } from '../page-objects/settings.page.js';

type ChromeSnapshot = {
  actionColor: string;
  actionContrast: number;
  accentBackground: string;
  footerBackground: string;
  footerBorderColor: string;
  footerBorderStyle: string;
  footerBorderWidth: string;
  headerBackground: string;
  headerBorderColor: string;
  headerBorderStyle: string;
  headerBorderWidth: string;
  neutralBackground: string;
  neutralForeground: string;
  neutralStroke: string;
  titleColor: string;
  titleContrast: number;
};

const SPECIMEN_ROUTE = '/__frontcomposer/specimens/type';
const MIN_TEXT_CONTRAST = 4.5;

test.describe('Story 8.1: neutral shell chrome @p1', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.localStorage.clear();
      window.sessionStorage.clear();
    });

    await page.setViewportSize({ width: 1280, height: 900 });
  });

  for (const theme of ['Light', 'Dark'] as const satisfies readonly ThemeLabel[]) {
    test(`header and footer use neutral chrome tokens in ${theme.toLowerCase()} theme`, async ({
      page,
      tenant,
    }) => {
      expect(tenant.tenantId).toBeTruthy();

      await page.goto(SPECIMEN_ROUTE);
      await expect(page.getByTestId('fc-type-specimen')).toBeVisible();

      const settings = new SettingsPage(page);
      await settings.openViaButton();
      await settings.selectTheme(theme);
      await expect(page.getByTitle('Change theme').first()).toContainText(theme);

      const snapshot = await readChromeSnapshot(page);

      expect(snapshot.headerBackground).toBe(snapshot.neutralBackground);
      expect(snapshot.footerBackground).toBe(snapshot.neutralBackground);
      expect(snapshot.headerBackground).not.toBe(snapshot.accentBackground);
      expect(snapshot.headerBorderWidth).toBe('1px');
      expect(snapshot.headerBorderStyle).toBe('solid');
      expect(snapshot.headerBorderColor).toBe(snapshot.neutralStroke);
      expect(snapshot.footerBorderWidth).toBe('1px');
      expect(snapshot.footerBorderStyle).toBe('solid');
      expect(snapshot.footerBorderColor).toBe(snapshot.neutralStroke);
      expect(snapshot.titleColor).toBe(snapshot.neutralForeground);
      expect(snapshot.actionColor).not.toBe(snapshot.accentBackground);
      expect(snapshot.titleContrast).toBeGreaterThanOrEqual(MIN_TEXT_CONTRAST);
      expect(snapshot.actionContrast).toBeGreaterThanOrEqual(MIN_TEXT_CONTRAST);
    });
  }
});

const readChromeSnapshot = async (page: Page): Promise<ChromeSnapshot> => page.evaluate(() => {
  const resolveElement = (selector: string): HTMLElement => {
    const element = document.querySelector(selector);
    if (!(element instanceof HTMLElement)) {
      throw new Error(`Missing element: ${selector}`);
    }

    return element;
  };

  const nearestChrome = (start: HTMLElement, borderProperty: 'borderBlockEndWidth' | 'borderBlockStartWidth'): HTMLElement => {
    let current: HTMLElement | null = start;
    while (current) {
      if (getComputedStyle(current)[borderProperty] !== '0px') {
        return current;
      }

      current = current.parentElement;
    }

    throw new Error(`Missing chrome ancestor with ${borderProperty}`);
  };

  const textElement = (exactText: string): HTMLElement => {
    const candidates = Array.from(document.querySelectorAll('.fc-shell-root *'));
    const element = candidates.find((candidate) => candidate.textContent?.trim() === exactText);
    if (!(element instanceof HTMLElement)) {
      throw new Error(`Missing shell text: ${exactText}`);
    }

    return element;
  };

  const resolveColor = (reference: HTMLElement, cssProperty: 'backgroundColor' | 'color', value: string): string => {
    const probe = document.createElement('span');
    probe.style[cssProperty] = value;
    probe.style.display = 'none';
    reference.appendChild(probe);
    const color = getComputedStyle(probe)[cssProperty];
    probe.remove();
    return color;
  };

  const parseRgb = (color: string): [number, number, number] => {
    const channels = color.match(/\d+(\.\d+)?/g)?.slice(0, 3).map(Number);
    if (!channels || channels.length !== 3) {
      throw new Error(`Unsupported color: ${color}`);
    }

    return [channels[0], channels[1], channels[2]];
  };

  const relativeLuminance = (color: string): number => {
    const [red, green, blue] = parseRgb(color).map((channel) => {
      const normalized = channel / 255;
      return normalized <= 0.03928
        ? normalized / 12.92
        : ((normalized + 0.055) / 1.055) ** 2.4;
    });

    return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
  };

  const contrastRatio = (foreground: string, background: string): number => {
    const foregroundLuminance = relativeLuminance(foreground);
    const backgroundLuminance = relativeLuminance(background);
    const lighter = Math.max(foregroundLuminance, backgroundLuminance);
    const darker = Math.min(foregroundLuminance, backgroundLuminance);
    return (lighter + 0.05) / (darker + 0.05);
  };

  const title = textElement('Hexalith FrontComposer');
  const footerText = textElement(`Hexalith FrontComposer \u00a9 ${new Date().getFullYear()}`);
  const action = resolveElement('[data-testid="fc-settings-button"]');
  const header = nearestChrome(title, 'borderBlockEndWidth');
  const footer = nearestChrome(footerText, 'borderBlockStartWidth');
  const headerStyles = getComputedStyle(header);
  const footerStyles = getComputedStyle(footer);
  const titleStyles = getComputedStyle(title);
  const actionStyles = getComputedStyle(action);
  const neutralBackground = resolveColor(header, 'backgroundColor', 'var(--colorNeutralBackground2)');
  const neutralForeground = resolveColor(header, 'color', 'var(--colorNeutralForeground1)');
  const neutralStroke = resolveColor(header, 'color', 'var(--colorNeutralStroke2)');
  const accentBackground = resolveColor(header, 'backgroundColor', 'var(--fc-accent-base-color)');

  return {
    actionColor: actionStyles.color,
    actionContrast: contrastRatio(actionStyles.color, headerStyles.backgroundColor),
    accentBackground,
    footerBackground: footerStyles.backgroundColor,
    footerBorderColor: footerStyles.borderBlockStartColor,
    footerBorderStyle: footerStyles.borderBlockStartStyle,
    footerBorderWidth: footerStyles.borderBlockStartWidth,
    headerBackground: headerStyles.backgroundColor,
    headerBorderColor: headerStyles.borderBlockEndColor,
    headerBorderStyle: headerStyles.borderBlockEndStyle,
    headerBorderWidth: headerStyles.borderBlockEndWidth,
    neutralBackground,
    neutralForeground,
    neutralStroke,
    titleColor: titleStyles.color,
    titleContrast: contrastRatio(titleStyles.color, headerStyles.backgroundColor),
  };
});
