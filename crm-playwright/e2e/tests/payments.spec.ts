import { newLead } from '../data';
import { expect, test } from '../fixtures';

test.describe('payments', () => {
  test('takes a payment for a lead', async ({ leadForm, leadDetail }) => {
    await leadForm.create(newLead());
    await leadDetail.pay(4999);
    await expect(leadDetail.payment).toContainText(/Charge ch_[0-9a-f]{16}: succeeded/);
    await expect(leadDetail.status).toHaveText('paid');
  });

  test('shows a declined payment', async ({ leadForm, leadDetail }) => {
    await leadForm.create(newLead());
    await leadDetail.pay(250000);
    await expect(leadDetail.payment).toContainText(/: declined/);
    await expect(leadDetail.status).toHaveText('payment-declined');
  });
});
