import { test as keployTest } from '@keploy/playwright';
import { LeadDetailPage } from './pages/LeadDetailPage';
import { LeadFormPage } from './pages/LeadFormPage';
import { LeadListPage } from './pages/LeadListPage';

// The suite's page objects, on top of @keploy/playwright's test, which tells
// keploy where each test begins and ends.
export const test = keployTest.extend<{ leadList: LeadListPage; leadForm: LeadFormPage; leadDetail: LeadDetailPage }>({
  leadList: async ({ page }, use) => use(new LeadListPage(page)),
  leadForm: async ({ page }, use) => use(new LeadFormPage(page)),
  leadDetail: async ({ page }, use) => use(new LeadDetailPage(page)),
});

export { expect } from '@keploy/playwright';
