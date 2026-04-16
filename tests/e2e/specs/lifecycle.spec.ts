import { expect, test } from '../fixtures/index.js';
import { CounterPage } from '../page-objects/counter.page.js';

test.describe('command lifecycle: five-state contract (architecture Row 2)', () => {
  test('increment command transitions idle -> submitting -> success', async ({ page, lifecycle }) => {
    // Given the counter page is loaded
    const counter = new CounterPage(page);
    await counter.goto();

    // And an increment command is observable via the generated lifecycle wrapper
    const incrementCommandId = 'increment';
    const indicator = lifecycle.locator(incrementCommandId);
    await expect(indicator).toBeVisible();
    await lifecycle.expectState(incrementCommandId, 'idle');

    // When the user clicks the generated increment button
    await counter.increment();

    // Then the lifecycle wrapper advances through submitting to a terminal success
    const terminal = await lifecycle.waitForTerminal(incrementCommandId);
    expect(terminal).toBe('success');

    // And the UI reflects the new counter value
    expect(await counter.value()).toBeGreaterThanOrEqual(1);
  });
});
