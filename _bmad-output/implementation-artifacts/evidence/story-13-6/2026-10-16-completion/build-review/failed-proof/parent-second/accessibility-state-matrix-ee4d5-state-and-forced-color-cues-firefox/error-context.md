# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> AE complete state matrix dark >> navigation-tabs: composed text, targets, current-state and forced-color cues
- Location: specs/accessibility-state-matrix.spec.ts:235:7

# Error details

```
Error: page.evaluate: Test ended.
 Please check out https://github.com/dequelabs/axe-core-npm/blob/develop/packages/playwright/error-handling.md
```

# Test source

```ts
  1   | import AxeBuilder from '@axe-core/playwright';
  2   | import { expect, type Page } from '@playwright/test';
  3   | import { mkdir, writeFile } from 'node:fs/promises';
  4   | import { dirname } from 'node:path';
  5   | 
  6   | export interface A11yOptions {
  7   |   tags?: string[];
  8   |   disableRules?: string[];
  9   |   include?: string[];
  10  |   exclude?: string[];
  11  |   route?: string;
  12  |   artifactPath?: string;
  13  |   requiredSelectors?: string[];
  14  | }
  15  | 
  16  | export const WCAG_AA_TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] as const;
  17  | const BLOCKING_IMPACTS = new Set(['serious', 'critical']);
  18  | const REPORT_ONLY_IMPACTS = new Set(['minor', 'moderate']);
  19  | 
  20  | /**
  21  |  * Runs axe-core against the current page and asserts zero violations.
  22  |  * Architecture Row 5: WCAG 2.2 AA enforced at Playwright level (test-time).
  23  |  */
  24  | export const expectNoAxeViolations = async (page: Page, options: A11yOptions = {}): Promise<void> => {
  25  |   const result = await analyzeAccessibility(page, options);
  26  |   expect.soft(result.violations, formatViolations(result.violations)).toEqual([]);
  27  | };
  28  | 
  29  | export const expectNoBlockingAxeViolations = async (page: Page, options: A11yOptions = {}): Promise<void> => {
  30  |   for (const selector of options.requiredSelectors ?? []) {
  31  |     const count = await page.locator(selector).count();
  32  |     expect(count, `${options.route ?? page.url()} missing required selector ${selector}`).toBeGreaterThan(0);
  33  |   }
  34  | 
> 35  |   const result = await analyzeAccessibility(page, options);
      |                  ^ Error: page.evaluate: Test ended.
  36  |   const scannedNodes = result.passes.reduce((count, pass) => count + pass.nodes.length, 0)
  37  |     + result.incomplete.reduce((count, incomplete) => count + incomplete.nodes.length, 0)
  38  |     + result.violations.reduce((count, violation) => count + violation.nodes.length, 0);
  39  |   expect(scannedNodes, `${options.route ?? page.url()} axe scan included zero target nodes`).toBeGreaterThan(0);
  40  | 
  41  |   const partitioned = partitionAxeViolations(result.violations);
  42  |   if (options.artifactPath) {
  43  |     await writeAxeSummary(options.artifactPath, {
  44  |       route: options.route ?? page.url(),
  45  |       blocking: partitioned.blocking,
  46  |       reportOnly: partitioned.reportOnly,
  47  |       unknown: partitioned.unknown,
  48  |     });
  49  |   }
  50  | 
  51  |   expect(partitioned.blocking, formatViolations(partitioned.blocking)).toEqual([]);
  52  |   expect(partitioned.unknown, `Unexpected axe impact values must be triaged explicitly:\n${formatViolations(partitioned.unknown)}`).toEqual([]);
  53  | };
  54  | 
  55  | /** Shared scanner: both assertion policies use the same complete WCAG AA default. */
  56  | export const analyzeAccessibility = async (page: Page, options: A11yOptions = {}) => {
  57  |   let builder = new AxeBuilder({ page }).withTags(options.tags ?? [...WCAG_AA_TAGS]);
  58  |   if (options.disableRules?.length) builder = builder.disableRules(options.disableRules);
  59  |   for (const selector of options.include ?? []) builder = builder.include(selector);
  60  |   for (const selector of options.exclude ?? []) builder = builder.exclude(selector);
  61  |   return builder.analyze();
  62  | };
  63  | 
  64  | export type AxeViolation = Awaited<ReturnType<AxeBuilder['analyze']>>['violations'][number];
  65  | 
  66  | export interface AxeViolationPartition {
  67  |   blocking: AxeViolation[];
  68  |   reportOnly: AxeViolation[];
  69  |   unknown: AxeViolation[];
  70  | }
  71  | 
  72  | export const partitionAxeViolations = (violations: AxeViolation[]): AxeViolationPartition => ({
  73  |   blocking: violations.filter((violation) => BLOCKING_IMPACTS.has(violation.impact ?? '')),
  74  |   reportOnly: violations.filter((violation) => REPORT_ONLY_IMPACTS.has(violation.impact ?? '')),
  75  |   unknown: violations.filter((violation) => !BLOCKING_IMPACTS.has(violation.impact ?? '') && !REPORT_ONLY_IMPACTS.has(violation.impact ?? '')),
  76  | });
  77  | 
  78  | const formatViolations = (violations: AxeViolation[]): string => {
  79  |   if (violations.length === 0) return 'no a11y violations';
  80  |   return violations
  81  |     .map((v) => `${v.id} [${v.impact ?? 'unknown'}]: ${v.help} (${v.nodes.length} node(s))`)
  82  |     .join('\n');
  83  | };
  84  | 
  85  | interface AxeSummary {
  86  |   route: string;
  87  |   blocking: AxeViolation[];
  88  |   reportOnly: AxeViolation[];
  89  |   unknown: AxeViolation[];
  90  | }
  91  | 
  92  | const writeAxeSummary = async (artifactPath: string, summary: AxeSummary): Promise<void> => {
  93  |   await mkdir(dirname(artifactPath), { recursive: true });
  94  |   await writeFile(
  95  |     artifactPath,
  96  |     `${JSON.stringify({
  97  |       route: summary.route,
  98  |       blocking: summarizeViolations(summary.blocking),
  99  |       reportOnly: summarizeViolations(summary.reportOnly),
  100 |       unknown: summarizeViolations(summary.unknown),
  101 |     }, null, 2)}\n`,
  102 |     'utf8',
  103 |   );
  104 | };
  105 | 
  106 | const summarizeViolations = (violations: AxeViolation[]) =>
  107 |   violations.map((violation) => ({
  108 |     id: violation.id,
  109 |     impact: violation.impact ?? 'unknown',
  110 |     help: violation.help,
  111 |     helpUrl: violation.helpUrl,
  112 |     selectors: violation.nodes.slice(0, 10).map((node) => node.target.join(' ')),
  113 |     truncated: violation.nodes.length > 10,
  114 |   }));
  115 | 
```