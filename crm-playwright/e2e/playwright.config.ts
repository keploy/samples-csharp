import { defineConfig } from '@playwright/test';
import path from 'node:path';

const baseURL = process.env.CRM_URL ?? 'http://127.0.0.1:5080';

// One worker and no retries: keploy serves one test at a time, and a test it
// cannot prove is set aside with its reason, not retried. Playwright starts
// the app itself (webServer), so the app runs inside the command keploy
// wraps and keploy sees the app's calls to its dependencies.
export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],
  use: { baseURL, trace: 'retain-on-failure' },
  webServer: {
    command: 'dotnet CrmLeads.dll',
    cwd: path.join(__dirname, '..', 'app', 'out'),
    url: `${baseURL}/`,
    reuseExistingServer: false,
    stdout: 'pipe',
    stderr: 'pipe',
    timeout: 120_000,
  },
});
