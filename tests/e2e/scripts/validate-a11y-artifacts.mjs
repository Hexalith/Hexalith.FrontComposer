import { existsSync, statSync } from 'node:fs';
import { join } from 'node:path';

const required = [
  'playwright-report/index.html',
  'test-results/junit.xml',
];

const optionalEvidenceRoots = [
  'test-results',
  'playwright-report',
];

const missing = required.filter((path) => !existsSync(join(process.cwd(), path)));
if (missing.length > 0) {
  console.error(`Missing required Playwright artifact(s): ${missing.join(', ')}`);
  process.exit(1);
}

const oversized = optionalEvidenceRoots
  .map((path) => join(process.cwd(), path))
  .filter(existsSync)
  .filter((path) => statSync(path).size > 50 * 1024 * 1024);

if (oversized.length > 0) {
  console.error(`Accessibility artifact root is unexpectedly large: ${oversized.join(', ')}`);
  process.exit(1);
}

console.log('Accessibility artifact validation passed.');
