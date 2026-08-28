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
  readonly summaryTab: Locator;
  readonly activityTab: Locator;
  readonly archivedTab: Locator;
  readonly historyTab: Locator;
  readonly summaryPanel: Locator;
  readonly activityPanel: Locator;
  readonly archivedPanel: Locator;
  readonly historyPanel: Locator;
  readonly summaryContent: Locator;
  readonly activityContent: Locator;
  readonly historyContent: Locator;
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
    this.summaryTab = page.getByRole('tab', { name: 'Summary' });
    this.activityTab = page.getByRole('tab', { name: 'Activity' });
    this.archivedTab = page.getByRole('tab', { name: 'Archived' });
    this.historyTab = page.getByRole('tab', { name: 'History' });
    this.summaryPanel = page.locator('#summary-panel');
    this.activityPanel = page.locator('#activity-panel');
    this.archivedPanel = page.locator('#archived-panel');
    this.historyPanel = page.locator('#history-panel');
    this.summaryContent = page.getByTestId('fc-page-tabs-summary-content');
    this.activityContent = page.getByTestId('fc-page-tabs-activity-content');
    this.historyContent = page.getByTestId('fc-page-tabs-history-content');
    this.activeTabState = page.getByTestId('fc-page-toolbar-tab-state');
  }

  async goto(): Promise<void> {
    await this.page.goto('/__frontcomposer/specimens/page-toolbar');
    await this.page.locator('.fc-shell-root[data-fc-interactive="true"]').waitFor();
    await this.root.waitFor();
  }
}
