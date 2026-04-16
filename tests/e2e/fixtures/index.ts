import { mergeTests } from '@playwright/test';

import { lifecycleTest } from './lifecycle.fixture.js';
import { tenantTest } from './tenant.fixture.js';

/**
 * Root composed test object. Import `test` and `expect` from here in every spec.
 * Add new fixtures by creating a file in this folder and merging below.
 */
export const test = mergeTests(tenantTest, lifecycleTest);
export { expect } from '@playwright/test';
