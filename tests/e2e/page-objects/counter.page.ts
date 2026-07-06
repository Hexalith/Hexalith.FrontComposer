import type { Locator, Page } from '@playwright/test';

export class CounterPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly currentValue: Locator;
  readonly incrementButton: Locator;
  readonly configureLink: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole('heading', { name: /counter/i });
    this.currentValue = page.locator("[data-fc-field='Count']").first();
    this.incrementButton = page.locator('#fc-trigger-Counter-Domain-IncrementCommand');
    this.configureLink = page.getByRole('link', { name: 'Configure Counter' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/counter');
    await this.page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await this.heading.waitFor();
  }

  async increment(times = 1): Promise<void> {
    for (let i = 0; i < times; i++) await this.incrementButton.click();
  }

  async value(): Promise<number> {
    const text = (await this.currentValue.textContent())?.trim() ?? '';
    const parsed = Number.parseInt(text, 10);
    if (Number.isNaN(parsed)) throw new Error(`counter value not numeric: "${text}"`);
    return parsed;
  }
}
