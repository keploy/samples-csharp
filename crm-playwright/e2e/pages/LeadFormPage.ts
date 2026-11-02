import { expect, type Locator, type Page } from '@playwright/test';
import type { LeadData } from '../data';

export class LeadFormPage {
  readonly error: Locator;

  constructor(private readonly page: Page) {
    this.error = page.locator('#form-error');
  }

  async open() {
    await this.page.goto('/#/new');
    await expect(this.page.getByRole('heading', { name: 'New lead' })).toBeVisible();
  }

  async fill(lead: LeadData) {
    await this.page.getByLabel('Name', { exact: true }).fill(lead.name);
    await this.page.getByLabel('Email').fill(lead.email);
    await this.page.getByLabel('Phone').fill(lead.phone);
    await this.page.getByLabel('Source').selectOption(lead.source);
    await this.page.getByLabel('Budget').fill(lead.budget);
    let n = 0;
    for (const [key, value] of Object.entries(lead.custom)) {
      n += 1;
      await this.page.getByRole('button', { name: 'Add custom field' }).click();
      await this.page.getByLabel(`Custom field ${n} name`).fill(key);
      await this.page.getByLabel(`Custom field ${n} value`).fill(value);
    }
  }

  async submit() {
    await this.page.getByRole('button', { name: 'Create lead' }).click();
  }

  // Creates the lead and waits for its page, which names it.
  async create(lead: LeadData) {
    await this.open();
    await this.fill(lead);
    await this.submit();
    await expect(this.page.locator('#lead-name')).toHaveText(lead.name);
  }
}
