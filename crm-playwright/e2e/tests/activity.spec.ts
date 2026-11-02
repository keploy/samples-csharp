import { newLead } from '../data';
import { test } from '../fixtures';

test('shows the welcome activity from the lead event', async ({ leadForm, leadDetail }) => {
  await leadForm.create(newLead());
  await leadDetail.waitForActivity('welcome-queued');
});
