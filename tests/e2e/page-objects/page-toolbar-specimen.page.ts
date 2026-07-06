import type { Locator, Page } from '@playwright/test';

export class PageToolbarSpecimenPage {
  readonly page: Page;
  readonly root: Locator;
  readonly toolbar: Locator;
  readonly searchInput: Locator;
  readonly searchState: Locator;
  readonly filterTrigger: Locator;
  readonly filterPopover: Locator;
  readonly filterContent: Locator;
  readonly viewTrigger: Locator;
  readonly viewDensityItem: Locator;
  readonly refreshButton: Locator;
  readonly refreshState: Locator;
  readonly tabs: Locator;
  readonly activityTab: Locator;
  readonly activeTabState: Locator;

  constructor(page: Page) {
    this.page = page;
    this.root = page.getByTestId('fc-page-toolbar-specimen');
    this.toolbar = page.getByTestId('fc-page-toolbar');
    this.searchInput = page.getByTestId('fc-page-toolbar-search').locator('input');
    this.searchState = page.getByTestId('fc-page-toolbar-search-state');
    this.filterTrigger = page.getByTestId('fc-page-toolbar-filter-trigger');
    this.filterPopover = page.getByTestId('fc-page-toolbar-filter-popover');
    this.filterContent = page.getByTestId('fc-page-toolbar-filter-content');
    this.viewTrigger = page.getByTestId('fc-page-toolbar-view-trigger');
    this.viewDensityItem = page.getByTestId('fc-page-toolbar-view-density');
    this.refreshButton = page.getByTestId('fc-page-toolbar-refresh');
    this.refreshState = page.getByTestId('fc-page-toolbar-refresh-state');
    this.tabs = page.getByTestId('fc-page-toolbar-tabs');
    this.activityTab = page.getByRole('tab', { name: 'Activity' });
    this.activeTabState = page.getByTestId('fc-page-toolbar-tab-state');
  }

  async goto(): Promise<void> {
    await this.page.goto('/__frontcomposer/specimens/page-toolbar');
    await this.page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await this.root.waitFor();
  }
}
