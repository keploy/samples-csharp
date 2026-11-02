# CRM leads: an ASP.NET Core app with a Playwright UI suite under keploy

A small CRM shaped like many line-of-business apps: an ASP.NET Core 8 backend on Linux with a browser UI, tested end to end by a Playwright suite in TypeScript that uses a Page Object Model. The backend talks to:

- DynamoDB (AWS SDK for .NET): the leads table;
- SQS (AWS SDK for .NET): a `lead.created` message on each new lead;
- Pulsar (DotPulsar): a `lead.created` event, which a hosted consumer in the same app reads back to add an activity to the lead;
- a payment provider over HTTPS (`POST /v1/charges`);
- an LMS over HTTPS (`POST /api/v2/enrollments`).

`keploy mock record` runs the suite against these dependencies and records every call the app makes. `keploy mock replay` runs the same suite with every dependency stopped: keploy answers the app's calls from the recording.

## Layout

| Path | What it is |
| --- | --- |
| `app/` | The backend (`Program.cs`, `LeadStore.cs`, `ActivityConsumer.cs`) and the UI (`wwwroot/`) |
| `e2e/` | The Playwright suite: `pages/` (page objects), `tests/`, `fixtures.ts` (the page objects on `@keploy/playwright`'s `test`) |
| `providers/server.mjs` | Two fake third parties (payments, LMS) over HTTPS, for record |
| `docker-compose.yml` | DynamoDB local, ElasticMQ (SQS) and Pulsar standalone, for record |
| `scripts/` | `gen-certs.sh` and `trust-ca.sh` (the providers' test CA), `deps.sh up|down` |
| `keploy.yml` | The suite as keploy's command, recordings kept locally (`mock.local`) |

The suite makes its own data the way real suites do: every lead's name and email carry the time and a random suffix, and the backend stamps ids (`Guid.NewGuid()`) and times (`DateTime.UtcNow`) of its own.

## What you need

- .NET 8 SDK, Node.js 22 or later, Docker with Compose, `openssl`, `curl`.
- Two host names for the providers: add `127.0.0.1 payments.provider.test lms.provider.test` to `/etc/hosts`.

## Run the suite without keploy

```sh
cd crm-playwright
dotnet publish app/CrmLeads.csproj -c Release -o app/out
sh scripts/gen-certs.sh .certs
sudo sh scripts/trust-ca.sh .certs/ca.pem    # the providers' test CA, as a real provider's CA is trusted
(cd e2e && npm ci && npx playwright install chromium)
sh scripts/deps.sh up                        # dependencies, providers, the table and the queue
(cd e2e && npx playwright test)
sh scripts/deps.sh down
```

Playwright starts the app itself (`webServer` in `e2e/playwright.config.ts`) on `http://127.0.0.1:5080`.

## Record and replay with keploy

Install keploy (enterprise), then, from `crm-playwright/`:

```sh
sh scripts/deps.sh up
KEPLOY_PROVEN=1 keploy mock record --local
sh scripts/deps.sh down                      # every dependency and provider stopped
KEPLOY_PROVEN=1 keploy mock replay --local
```

`--local` keeps the recordings in `keploy/` on this machine and runs without a Keploy sign-in. keploy 3.8.106 still asks for a sign-in when only `keploy.yml` sets `mock.local`, so pass the flag.

The suite runs with one worker and no retries (`keploy.yml`): keploy serves one test at a time, and sets aside, with its reason, a test it cannot prove instead of retrying it.
