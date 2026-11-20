import { expect, test } from '../fixtures/index.js';
import { fillFieldByLabel } from '../helpers/fluent-fields.js';

test.describe('Story 3.4: command lifecycle UI', () => {
  test('generated compact command surfaces submitting -> acknowledged -> syncing -> confirmed', async ({
    page,
    lifecycle,
    tenant,
  }) => {
    expect(tenant.tenantId).toBeTruthy();

    await page.goto('/counter');
    await page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await expect(page.getByRole('heading', { name: 'Counter' })).toBeVisible();

    const commandId = 'batch-increment';
    const indicator = lifecycle.locator(commandId);
    await expect(indicator).toBeVisible();
    await lifecycle.expectState(commandId, 'idle');

    const form = page.locator('.fc-command-form[aria-label="Batch Increment command form"]');
    await expect(form).toBeVisible();
    await fillFieldByLabel(form, 'Amount', '2');
    await fillFieldByLabel(form, 'Note', 'QA story 3.4 browser lifecycle');
    // Capture short-lived UI states in the browser before a Playwright round trip can miss them.
    const readTransitions = await indicator.evaluateHandle(node => {
      const states: string[] = [];
      let acknowledgementVisible = false;
      const record = () => {
        const state = node.getAttribute('data-lifecycle-state');
        if (state && state !== states.at(-1)) states.push(state);
        const message = node.querySelector('[data-testid="fc-acknowledged"]');
        if (message?.checkVisibility({ visibilityProperty: true })) acknowledgementVisible = true;
      };
      const observer = new MutationObserver(record);
      observer.observe(node, { attributes: true, attributeFilter: ['data-lifecycle-state'], childList: true, subtree: true });
      record();
      return () => {
        observer.disconnect();
        return { states, acknowledgementVisible };
      };
    });

    await form.getByRole('button', { name: 'Batch Increment' }).click();
    expect(await lifecycle.waitForTerminal(commandId)).toBe('confirmed');
    const observed = await readTransitions.evaluate(read => read());
    expect(observed.states).toEqual(['idle', 'submitting', 'acknowledged', 'syncing', 'confirmed']);
    expect(observed.acknowledgementVisible).toBe(true);
    await expect(form.getByTestId('fc-confirmed')).toBeVisible();
    await expect(form).toBeVisible();
  });
});
