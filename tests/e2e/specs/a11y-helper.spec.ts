import { expect, test } from '../fixtures/index.js';
import { partitionAxeViolations, type AxeViolation } from '../helpers/a11y.js';

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
});

const violation = (id: string, impact: 'minor' | 'moderate' | 'serious' | 'critical'): AxeViolation => ({
  id,
  impact,
  help: id,
  helpUrl: `https://dequeuniversity.com/rules/axe/4.10/${id}`,
  nodes: [{ target: [`[data-testid='${id}']`] }],
} as AxeViolation);
