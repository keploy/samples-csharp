import { test } from '@playwright/test';
import { createHash } from 'node:crypto';

// Test data the way a real suite makes it: a fresh name and email on every
// run, stamped with the time and a random suffix. Nothing here is pinned for
// keploy's sake.
//
// CRM_FIXED_DATA=1 pins it instead, for comparison: each test makes the same
// values on every run, from its own title and how many leads it made before.
export type LeadData = {
  name: string;
  email: string;
  phone: string;
  source: 'Website' | 'Referral' | 'Campaign';
  budget: string;
  custom: Record<string, string>;
};

const fixedData = process.env.CRM_FIXED_DATA === '1';
const madeByTest = new Map<string, number>();

function fixedStamp(): { stamp: string; digits: string } {
  const info = test.info();
  const n = madeByTest.get(info.testId) ?? 0;
  madeByTest.set(info.testId, n + 1);
  const hash = createHash('sha256').update(`${info.titlePath.join(' > ')} #${n}`).digest('hex');
  return { stamp: hash.slice(0, 18), digits: String(parseInt(hash.slice(18, 26), 16) % 1e7) };
}

export function newLead(overrides: Partial<LeadData> = {}): LeadData {
  const { stamp, digits } = fixedData
    ? fixedStamp()
    : { stamp: `${Date.now()}${Math.random().toString(36).slice(2, 7)}`, digits: String(Math.floor(Math.random() * 1e7)) };
  return {
    name: `Lead ${stamp}`,
    email: `lead.${stamp}@example.com`,
    phone: `+1555${digits.padStart(7, '0')}`,
    source: 'Website',
    budget: '25000',
    custom: { Region: 'APAC', 'Product interest': 'CRM suite' },
    ...overrides,
  };
}
