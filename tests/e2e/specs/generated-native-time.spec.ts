import { expect, type Locator } from '@playwright/test';

import { submissionTest } from '../fixtures/command-submission.fixture.js';
import { waitForGeneratedFormReady } from '../helpers/fluent-fields.js';

const SPECIMEN_PATH = '/__frontcomposer/specimens/native-time';
const COMMAND_FORM = '.fc-command-form';

const nativeTimeEditor = (form: Locator, field: 'RequiredTime' | 'OptionalTime'): Locator =>
  form.locator(`input[type="time"][name="${field}"]`);

const dispatchPayloads = (output: string) =>
  Array.from(output.matchAll(/Native time specimen dispatched\. RequiredTime=(\S+) OptionalTime=(\S+)\.(?=\r?\n)/gu),
    ([, required, optional]) => ({ required, optional }));

const expectSettledDispatch = async (form: Locator, output: () => string, required: string, optional: string) => {
  await expect(form.getByTestId('fc-confirmed')).toBeVisible({ timeout: 20_000 });
  expect(dispatchPayloads(output())).toEqual([{ required, optional }]);
};

submissionTest.beforeEach(async ({ page }) => {
  await page.goto(SPECIMEN_PATH);
  await waitForGeneratedFormReady(page.locator(COMMAND_FORM));
});

submissionTest('native time editors reject invalid text and submit the existing times', async ({ page, submissionHost }) => {
  const form = page.locator(COMMAND_FORM);
  const required = nativeTimeEditor(form, 'RequiredTime');
  const optional = nativeTimeEditor(form, 'OptionalTime');
  await expect(required).toHaveValue('08:15');
  await expect(optional).toHaveValue('17:30');

  // Type into the browser's time control so its segmented editor handles the keystrokes.
  await required.pressSequentially('xyz');
  await optional.pressSequentially('xyz');
  await optional.blur();
  await expect(required).toHaveValue('08:15');
  await expect(optional).toHaveValue('17:30');

  await form.getByRole('button', { name: 'Submit native times', exact: true }).click();
  await expectSettledDispatch(form, submissionHost.output, '08:15:00.0000000', '17:30:00.0000000');
});

submissionTest('cleared native time editors block a missing required value and dispatch a null optional value', async ({ page, submissionHost }) => {
  const form = page.locator(COMMAND_FORM);
  const required = nativeTimeEditor(form, 'RequiredTime');
  const optional = nativeTimeEditor(form, 'OptionalTime');
  const submit = form.getByRole('button', { name: 'Submit native times', exact: true });
  await expect(required).toHaveValue('08:15');
  await expect(optional).toHaveValue('17:30');

  await optional.fill('');
  await required.fill('');
  await required.blur();
  await expect(required).toHaveValue('');
  await expect(optional).toHaveValue('');
  expect(await required.evaluate((input: HTMLInputElement) => input.validity.valueMissing)).toBe(true);
  expect(await optional.evaluate((input: HTMLInputElement) => input.validity.valid)).toBe(true);

  await submit.click();
  const validation = form.getByTestId('fc-validation-summary');
  await expect(validation.getByRole('link', { name: /^Required Time:/u })).toBeVisible();
  expect(dispatchPayloads(submissionHost.output())).toEqual([]);

  await required.fill('09:45');
  await required.blur();
  await expect(required).toHaveValue('09:45');
  await expect(optional).toHaveValue('');
  await submit.click();
  await expectSettledDispatch(form, submissionHost.output, '09:45:00.0000000', 'null');
});

submissionTest('generated command dispatch preserves seconds from both native time editors', async ({ page, submissionHost }) => {
  const form = page.locator(COMMAND_FORM);
  const required = nativeTimeEditor(form, 'RequiredTime');
  const optional = nativeTimeEditor(form, 'OptionalTime');

  await required.fill('23:59:42');
  await optional.fill('00:22:17');
  await optional.blur();
  await expect(required).toHaveValue('23:59:42');
  await expect(optional).toHaveValue('00:22:17');
  expect(await required.evaluate((input: HTMLInputElement) => input.validity.valid), 'required time is natively valid').toBe(true);
  expect(await optional.evaluate((input: HTMLInputElement) => input.validity.valid), 'optional time is natively valid').toBe(true);

  await form.getByRole('button', { name: 'Submit native times', exact: true }).click();
  await expectSettledDispatch(form, submissionHost.output, '23:59:42.0000000', '00:22:17.0000000');
});
