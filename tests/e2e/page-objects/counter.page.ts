import type { Locator, Page } from '@playwright/test';

export class CounterPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly currentValue: Locator;
  readonly incrementButton: Locator;
  readonly decrementButton: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole('heading', { name: /counter/i });
    this.currentValue = page.getByTestId('fc-counter-value');
    this.incrementButton = page.getByTestId('fc-counter-increment');
    this.decrementButton = page.getByTestId('fc-counter-decrement');
  }

  async goto(): Promise<void> {
    await this.page.goto('/counter');
    await this.heading.waitFor();
  }

  async increment(times = 1): Promise<void> {
    for (let i = 0; i < times; i++) await this.incrementButton.click();
  }

  async decrement(times = 1): Promise<void> {
    for (let i = 0; i < times; i++) await this.decrementButton.click();
  }

  async value(): Promise<number> {
    const text = (await this.currentValue.textContent())?.trim() ?? '';
    const parsed = Number.parseInt(text, 10);
    if (Number.isNaN(parsed)) throw new Error(`counter value not numeric: "${text}"`);
    return parsed;
  }
}
