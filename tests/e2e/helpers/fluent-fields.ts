import { expect, type Locator } from '@playwright/test';

export const fieldByLabel = (root: Locator, label: string): Locator =>
  root.getByLabel(label, { exact: true });

export const fieldEditorByLabel = (root: Locator, label: string): Locator =>
  fieldByLabel(root, label).locator('input, textarea').first();

export const fillFieldByLabel = async (root: Locator, label: string, value: string): Promise<void> => {
  const field = fieldByLabel(root, label);
  const editor = field.locator('input, textarea').first();

  if (await editor.count()) {
    await editor.fill(value);
    await editor.blur();
    return;
  }

  await field.fill(value);
  await field.blur();
};

export const expectFieldValue = async (root: Locator, label: string, value: string): Promise<void> => {
  const field = fieldByLabel(root, label);
  const editor = field.locator('input, textarea').first();

  if (await editor.count()) {
    await expect(editor).toHaveValue(value);
    return;
  }

  await expect(field).toHaveValue(value);
};
