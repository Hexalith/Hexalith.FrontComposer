import AxeBuilder from '@axe-core/playwright';
import { expect, type Page } from '@playwright/test';

export interface A11yOptions {
  tags?: string[];
  disableRules?: string[];
  include?: string[];
  exclude?: string[];
}

/**
 * Runs axe-core against the current page and asserts zero violations.
 * Architecture Row 5: WCAG 2.1 AA enforced at Playwright level (test-time).
 */
export const expectNoAxeViolations = async (page: Page, options: A11yOptions = {}): Promise<void> => {
  let builder = new AxeBuilder({ page }).withTags(options.tags ?? ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']);

  if (options.disableRules?.length) builder = builder.disableRules(options.disableRules);
  for (const selector of options.include ?? []) builder = builder.include(selector);
  for (const selector of options.exclude ?? []) builder = builder.exclude(selector);

  const result = await builder.analyze();
  expect.soft(result.violations, formatViolations(result.violations)).toEqual([]);
};

type AxeViolation = Awaited<ReturnType<AxeBuilder['analyze']>>['violations'][number];

const formatViolations = (violations: AxeViolation[]): string => {
  if (violations.length === 0) return 'no a11y violations';
  return violations
    .map((v) => `${v.id} [${v.impact ?? 'unknown'}]: ${v.help} (${v.nodes.length} node(s))`)
    .join('\n');
};
