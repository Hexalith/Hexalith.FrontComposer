import { expect, test } from '../fixtures/index.js';
import { analyzeAccessibility, WCAG_AA_TAGS, partitionAxeViolations, type AxeViolation } from '../helpers/a11y.js';

test.describe('a11y helper impact partitioning', () => {
  test('serious and critical violations block while minor and moderate remain report-only', () => {
    const partition = partitionAxeViolations([
      violation('color-contrast', 'serious'),
      violation('aria-valid-attr', 'critical'),
      violation('region', 'moderate'),
      violation('landmark-one-main', 'minor'),
    ]);

    expect(partition.blocking.map((v) => v.id)).toEqual(['color-contrast', 'aria-valid-attr']);
    expect(partition.reportOnly.map((v) => v.id)).toEqual(['region', 'landmark-one-main']);
    expect(partition.unknown).toEqual([]);
  });

  test('unknown axe impacts are separated for explicit triage', () => {
    const partition = partitionAxeViolations([
      { ...violation('future-impact', 'minor'), impact: undefined },
    ]);

    expect(partition.blocking).toEqual([]);
    expect(partition.reportOnly).toEqual([]);
    expect(partition.unknown.map((v) => v.id)).toEqual(['future-impact']);
  });
});

const violation = (id: string, impact: 'minor' | 'moderate' | 'serious' | 'critical'): AxeViolation => ({
  id,
  impact,
  help: id,
  helpUrl: `https://dequeuniversity.com/rules/axe/4.10/${id}`,
  nodes: [{ target: [`[data-testid='${id}']`] }],
} as AxeViolation);

test('default scanner retains prior tags and executes the WCAG 2.2 target-size rule', async ({ page }) => {
  expect(WCAG_AA_TAGS).toEqual(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']);
  await page.setContent('<html lang="en"><title>Geometry fixture</title><main><h1>Targets</h1><button style="width:10px;height:10px;padding:0">A</button><button style="width:10px;height:10px;padding:0">B</button></main></html>');
  const results = await analyzeAccessibility(page);
  expect([...results.violations, ...results.incomplete, ...results.passes].map(rule => rule.id)).toContain('target-size');
  expect(results.violations.find(rule => rule.id === 'target-size')?.tags).toContain('wcag22aa');
});
