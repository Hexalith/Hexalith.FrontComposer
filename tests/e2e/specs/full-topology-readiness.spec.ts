import { expect, test, type Page } from '@playwright/test';

// This lane has no specimen authentication, seeded data, or mock gateway. Only
// the owned rehearsal supplies these endpoints and private realm credentials.
const live = process.env.FC_FULL_TOPOLOGY_LIVE === 'true';
test.skip(!live, 'Requires the disposable full-topology readiness runner.');
test.use({ trace: 'off', screenshot: 'off', video: 'off', locale: 'en-US' });

function required(name: string): string {
  const value = process.env[name];
  if (!value) throw new Error('The full-topology runner did not supply its private navigation configuration.');
  return value;
}

function loopback(name: string): string {
  const url = new URL(required(name));
  if (!['http:', 'https:'].includes(url.protocol)
      || !['localhost', '127.0.0.1', '[::1]'].includes(url.hostname)
      || !url.port || url.username || url.password || url.search || url.hash || url.pathname !== '/') {
    throw new Error('Full-topology navigation refuses a non-loopback or ambiguous endpoint.');
  }
  return url.origin;
}

async function signIn(page: Page, base: string): Promise<void> {
  const authority = loopback('FC_FULL_TOPOLOGY_AUTHORITY');
  await page.goto(`${base}/authentication/challenge?returnUrl=${encodeURIComponent('/tenants/tenant-summary-projection')}`);
  await expect.poll(() => new URL(page.url()).origin).toBe(authority);
  // Check the origin before typing; credentials must never follow an unexpected redirect.
  await page.locator('#username').fill(required('FC_FULL_TOPOLOGY_USERNAME'));
  await page.locator('#password').fill(required('FC_FULL_TOPOLOGY_PASSWORD'));
  await page.locator('#kc-login').click();
  await expect.poll(() => new URL(page.url()).origin).toBe(base);
}

async function tenantSurface(page: Page): Promise<void> {
  const heading = page.getByRole('heading', { name: 'Tenants', level: 1, exact: true });
  await expect(heading).toHaveCount(1);
  await expect(heading).toBeVisible();
  await expect(heading).toBeFocused();
  // The primary region must reach an authorized data/empty surface, never pass
  // merely because a heading was rendered while its query is denied or pending.
  await expect(page.getByText('Generated view of the first authorized tenant page.', { exact: false })).toBeVisible();
  await expect(page.locator('[data-fc-route-denied], [data-fc-route-authorizing], [data-testid$="-unauthorized"], [data-testid$="-stale"], [data-testid$="-error"]')).toHaveCount(0);
  await expect(page.getByRole('alert').filter({ hasText: /unauthorized|forbidden|stale|unavailable|failed/i })).toHaveCount(0);
  await expect(page.getByText(/latest freshness evidence says this list is stale|Some tenant evidence is unavailable|Sign in to view tenants|Tenant data could not be loaded/i)).toHaveCount(0);
  const account = page.locator('#fc-account-avatar');
  await account.click();
  await expect(page.getByTestId('fc-account-sign-out')).toBeVisible();
  await expect(page.getByTestId('fc-account-sign-in')).toHaveCount(0);
  await page.keyboard.press('Escape');
}

for (const [name, variable] of [
  ['combined UI', 'FC_FULL_TOPOLOGY_COMBINED_URL'],
  ['Tenants UI', 'FC_FULL_TOPOLOGY_TENANTS_URL'],
] as const) {
  test(`${name} authenticates and navigates to an authorized focused tenant surface`, async ({ page }) => {
    const base = loopback(variable);
    await signIn(page, base);
    await tenantSurface(page);
    await page.goto(`${base}/tenants`);
    await expect(page.getByRole('heading', { level: 1, name: 'Tenants', exact: true })).toBeFocused();
    await expect(page.locator('[data-fc-route-denied], [data-testid$="-unauthorized"], [data-testid$="-stale"]')).toHaveCount(0);
  });
}

test('Admin UI navigates with its ordinary authenticated Admin API session', async ({ page }) => {
  await page.goto(`${loopback('FC_FULL_TOPOLOGY_ADMIN_URL')}/tenants`);
  const heading = page.getByRole('heading', { name: 'Tenants', level: 1, exact: true });
  await expect(heading).toHaveCount(1);
  await expect(heading).toBeVisible();
  await expect(heading).toBeFocused();
  // A fresh owned store has no tenant rows. This terminal API-backed empty
  // state proves the normal Admin client credential path completed successfully.
  await expect(page.getByText('No tenants configured.', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Create tenant', exact: true })).toBeVisible();
  await expect(page.getByText(/Unable to load tenant data|session expired|not authorized|stale/i)).toHaveCount(0);
  await expect(page.getByRole('alert')).toHaveCount(0);
});
