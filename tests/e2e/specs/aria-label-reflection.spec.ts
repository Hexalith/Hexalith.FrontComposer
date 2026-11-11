import { expect, test } from '@playwright/test';

// Playwright #43967 / Mozilla #2090055: Firefox 159 crashes when native ARIA
// array reflection is read in both the page and locator JavaScript worlds.
// These attribute-based cases protect the working cohort during future upgrades;
// they do not claim support for Playwright 1.65's script-set reflected-name locators.
for (const first of ['page', 'locator'] as const) {
  test(`native ARIA label reflection survives ${first}-world-first named actions`, async ({ page }) => {
    await page.setContent(`
      <span id="save-label">Save</span>
      <button id="save" aria-labelledby="save-label" onclick="this.dataset.clicked = 'true'"></button>
      <button>Cancel</button>
      <label for="recipient">Recipient</label><input id="recipient">
    `);
    const save = page.getByRole('button', { name: 'Save', exact: true });
    const readPageWorld = async () => {
      const labels = await page.evaluate(() => {
        const button = document.getElementById('save')!;
        return button.ariaLabelledByElements?.map(label => label.id);
      });
      expect(labels).toEqual(['save-label']);
    };
    const readLocatorWorld = async () => {
      await save.click({ trial: true });
    };

    if (first === 'page') {
      await readPageWorld();
      await readLocatorWorld();
    } else {
      await readLocatorWorld();
      await readPageWorld();
    }

    await expect(save).toHaveAccessibleName('Save');
    await save.click();
    await expect(page.locator('#save')).toHaveAttribute('data-clicked', 'true');
    await expect(page.getByRole('button', { name: 'Cancel', exact: true })).toBeVisible();
    await page.getByRole('textbox', { name: 'Recipient', exact: true }).fill('Named control');
    await expect(page.getByLabel('Recipient', { exact: true })).toHaveValue('Named control');
  });
}
