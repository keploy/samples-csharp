import { newLead } from '../data';
import { expect, test } from '../fixtures';

test.describe('leads', () => {
  test('creates a lead with custom fields', async ({ leadForm, leadDetail }) => {
    const lead = newLead({ source: 'Referral', custom: { Region: 'EMEA', 'Preferred language': 'German' } });
    await leadForm.create(lead);
    await expect(leadDetail.email).toHaveText(lead.email);
    await expect(leadDetail.status).toHaveText('new');
    await expect(leadDetail.customValue('Region')).toHaveText('EMEA');
    await expect(leadDetail.customValue('Preferred language')).toHaveText('German');
  });

  test('lists a new lead', async ({ leadForm, leadList, leadDetail }) => {
    const lead = newLead();
    await leadForm.create(lead);
    await leadList.open();
    await expect(leadList.row(lead.name)).toContainText(lead.email);
    await leadList.openLead(lead.name);
    await expect(leadDetail.name).toHaveText(lead.name);
  });

  test('refuses a lead without an email', async ({ leadForm }) => {
    await leadForm.open();
    await leadForm.fill(newLead({ email: '' }));
    await leadForm.submit();
    await expect(leadForm.error).toHaveText('a valid email is required');
  });
});
