import { expect, test } from '../fixtures/index.js';
import { waitForGeneratedFormReady } from '../helpers/fluent-fields.js';

test('visual baseline for a generated full-page command', async ({ page }) => {
  test.skip(process.platform !== 'linux', 'The three-engine Linux lane owns this baseline.');
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection');

  const main = page.getByRole('main');
  await expect(main.getByRole('heading', { name: 'Configure Counter', exact: true, level: 1 })).toBeFocused();
  await expect(main.getByRole('navigation', { name: 'breadcrumb' })).toBeVisible();
  const form = main.locator('.fc-command-form[aria-label="Configure Counter command form"]');
  await expect(form).toBeVisible();
  await waitForGeneratedFormReady(form);
  await expect(form.getByRole('button', { name: 'Configure Counter', exact: true })).toBeVisible();

  await expect(page).toHaveScreenshot('configure-counter-command.png', { animations: 'disabled' });
});
