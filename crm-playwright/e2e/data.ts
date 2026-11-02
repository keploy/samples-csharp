// Test data the way a real suite makes it: a fresh name and email on every
// run, stamped with the time and a random suffix. Nothing here is pinned for
// keploy's sake.
export type LeadData = {
  name: string;
  email: string;
  phone: string;
  source: 'Website' | 'Referral' | 'Campaign';
  budget: string;
  custom: Record<string, string>;
};

export function newLead(overrides: Partial<LeadData> = {}): LeadData {
  const stamp = `${Date.now()}${Math.random().toString(36).slice(2, 7)}`;
  return {
    name: `Lead ${stamp}`,
    email: `lead.${stamp}@example.com`,
    phone: `+1555${String(Math.floor(Math.random() * 1e7)).padStart(7, '0')}`,
    source: 'Website',
    budget: '25000',
    custom: { Region: 'APAC', 'Product interest': 'CRM suite' },
    ...overrides,
  };
}
