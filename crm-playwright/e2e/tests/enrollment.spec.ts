import { newLead } from '../data';
import { expect, test } from '../fixtures';

test('enrolls a lead in a course', async ({ leadForm, leadDetail }) => {
  await leadForm.create(newLead());
  await leadDetail.enroll('sales-201');
  await expect(leadDetail.enrollment).toHaveText('Enrolled in sales-201: active');
});
