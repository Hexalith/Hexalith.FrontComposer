import { expect, test as base, type Locator, type Page } from '@playwright/test';

/**
 * Five-state command lifecycle contract (HFC architecture Row 2).
 * See `_bmad-output/planning-artifacts/architecture.md` for canonical state set.
 */
export const LIFECYCLE_STATES = ['idle', 'validating', 'submitting', 'success', 'error'] as const;
export type LifecycleState = (typeof LIFECYCLE_STATES)[number];

export interface LifecycleAssertions {
  locator: (commandId: string) => Locator;
  expectState: (commandId: string, state: LifecycleState) => Promise<void>;
  waitForTerminal: (commandId: string) => Promise<LifecycleState>;
}

export type LifecycleFixtures = {
  lifecycle: LifecycleAssertions;
};

const lifecycleLocator = (page: Page, commandId: string): Locator =>
  page.locator(`[data-testid="fc-lifecycle-${commandId}"]`);

const readState = async (locator: Locator): Promise<LifecycleState | null> => {
  const raw = await locator.getAttribute('data-lifecycle-state');
  if (!raw) return null;
  return LIFECYCLE_STATES.includes(raw as LifecycleState) ? (raw as LifecycleState) : null;
};

export const lifecycleTest = base.extend<LifecycleFixtures>({
  lifecycle: async ({ page }, use) => {
    const assertions: LifecycleAssertions = {
      locator: (commandId) => lifecycleLocator(page, commandId),
      expectState: async (commandId, state) => {
        const loc = lifecycleLocator(page, commandId);
        await expect(loc).toHaveAttribute('data-lifecycle-state', state);
      },
      waitForTerminal: async (commandId) => {
        const loc = lifecycleLocator(page, commandId);
        await expect(loc).toHaveAttribute('data-lifecycle-state', /^(success|error)$/);
        const state = await readState(loc);
        if (!state) throw new Error(`lifecycle state missing for commandId=${commandId}`);
        return state;
      },
    };
    await use(assertions);
  },
});
