import { expect, type Locator, type Page } from '@playwright/test';

export class LeadDetailPage {
  readonly name: Locator;
  readonly email: Locator;
  readonly status: Locator;
  readonly custom: Locator;
  readonly payment: Locator;
  readonly enrollment: Locator;
  readonly activity: Locator;
  readonly error: Locator;

  constructor(private readonly page: Page) {
    this.name = page.locator('#lead-name');
    this.email = page.locator('#lead-email');
    this.status = page.locator('#lead-status');
    this.custom = page.locator('#lead-custom');
    this.payment = page.locator('#payment');
    this.enrollment = page.locator('#enrollment');
    this.activity = page.locator('#activity');
    this.error = page.locator('#lead-error');
  }

  customValue(key: string): Locator {
    return this.custom.locator('dt', { hasText: key }).locator('xpath=following-sibling::dd[1]');
  }

  async pay(amountCents: number) {
    await this.page.getByLabel('Amount (cents)').fill(String(amountCents));
    await this.page.getByRole('button', { name: 'Take payment' }).click();
  }

  async enroll(courseId: string) {
    await this.page.getByLabel('Course').selectOption(courseId);
    await this.page.getByRole('button', { name: 'Enroll' }).click();
  }

  // The activity comes from a Pulsar consumer after the lead is created, so
  // the page is refreshed until it shows it (Playwright's toPass, no sleep).
  async waitForActivity(type: string) {
    await expect(async () => {
      await this.page.getByRole('button', { name: 'Refresh' }).click();
      await expect(this.activity.getByRole('listitem').filter({ hasText: type })).toBeVisible({ timeout: 1_000 });
    }).toPass({ timeout: 30_000 });
  }
}
