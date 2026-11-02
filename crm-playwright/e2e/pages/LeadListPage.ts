import { expect, type Locator, type Page } from '@playwright/test';

export class LeadListPage {
  readonly table: Locator;

  constructor(private readonly page: Page) {
    this.table = page.getByRole('table', { name: 'Leads' });
  }

  async open() {
    await this.page.goto('/#/');
    await expect(this.page.getByRole('heading', { name: 'Leads' })).toBeVisible();
  }

  row(name: string): Locator {
    return this.table.getByRole('row').filter({ hasText: name });
  }

  async openLead(name: string) {
    await this.table.getByRole('link', { name }).click();
  }
}
