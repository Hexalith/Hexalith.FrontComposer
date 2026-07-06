import { expect, type Locator } from '@playwright/test';

export const fieldByLabel = (root: Locator, label: string): Locator =>
  root.getByLabel(label, { exact: true });

export const fieldEditorByLabel = (root: Locator, label: string): Locator =>
  fieldByLabel(root, label).locator('input, textarea').first();

export const waitForGeneratedFormReady = async (root: Locator): Promise<void> => {
  const isGeneratedCommandForm = await root
    .evaluate((element) => element.classList.contains('fc-command-form'))
    .catch(() => false);

  if (isGeneratedCommandForm) {
    await expect(root).toHaveAttribute('data-fc-interactive', 'true');
  }
};

export const fillFieldByLabel = async (root: Locator, label: string, value: string): Promise<void> => {
  await waitForGeneratedFormReady(root);

  const field = fieldByLabel(root, label);
  const tagName = await field.evaluate((element) => element.tagName.toLowerCase());

  if (tagName.startsWith('fluent-')) {
    const editor = field.locator('input, textarea').first();
    await editor.waitFor({ state: 'visible' });
    await editor.fill(value);
    await editor.blur();
    await field.evaluate((element, nextValue) => {
      const host = element as HTMLInputElement;
      host.value = nextValue;
      host.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
      host.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
    }, value);
    return;
  }

  await field.fill(value);
  await field.blur();
};

export const expectFieldValue = async (root: Locator, label: string, value: string): Promise<void> => {
  await waitForGeneratedFormReady(root);

  const field = fieldByLabel(root, label);
  const tagName = await field.evaluate((element) => element.tagName.toLowerCase());

  if (tagName.startsWith('fluent-')) {
    const editor = field.locator('input, textarea').first();
    await editor.waitFor({ state: 'visible' });
    await expect(editor).toHaveValue(value);
    return;
  }

  await expect(field).toHaveValue(value);
};
