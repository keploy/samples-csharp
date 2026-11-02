# CRM leads: an ASP.NET Core app with a Playwright UI suite under keploy

A small CRM shaped like many line-of-business apps: an ASP.NET Core 8 backend on Linux with a browser UI, tested end to end by a Playwright suite in TypeScript that uses a Page Object Model. The backend talks to:

- DynamoDB (AWS SDK for .NET): the leads table;
- SQS (AWS SDK for .NET): a `lead.created` message on each new lead;
- Pulsar (DotPulsar): a `lead.created` event, which a hosted consumer in the same app reads back to add an activity to the lead;
- a payment provider over HTTPS (`POST /v1/charges`);
- an LMS over HTTPS (`POST /api/v2/enrollments`).

`keploy mock record` runs the suite against these dependencies and records every call the app makes. It then runs each test again against its recording, keeps the tests it can prove, and sets the others aside with a reason. `keploy mock replay` runs the kept tests with every dependency stopped: keploy answers the app's calls from the recording. What that gives today is under [What record and replay give today](#what-record-and-replay-give-today).

## Layout

| Path | What it is |
| --- | --- |
| `app/` | The backend (`Program.cs`, `LeadStore.cs`, `ActivityConsumer.cs`) and the UI (`wwwroot/`) |
| `e2e/` | The Playwright suite: `pages/` (page objects), `tests/`, `fixtures.ts` (the page objects on `@keploy/playwright`'s `test`) |
| `providers/server.mjs` | Two fake third parties (payments, LMS) over HTTPS, for record |
| `docker-compose.yml` | DynamoDB local, ElasticMQ (SQS) and Pulsar standalone, for record |
| `scripts/` | `gen-certs.sh` and `trust-ca.sh` (the providers' test CA), `deps.sh up|down`, `measure.sh` (record once, replay N times, check each replay) |
| `keploy.yml` | The suite as keploy's command, recordings kept locally (`mock.local`) |

The suite makes its own data the way real suites do: every lead's name and email carry the time and a random suffix, and the backend stamps ids (`Guid.NewGuid()`) and times (`DateTime.UtcNow`) of its own.

To compare, `CRM_FIXED_DATA=1` in the environment of `keploy mock record` and `keploy mock replay` pins the suite's data: each test makes the same name, email and phone on every run, from its own title. The backend's ids and times still change.

## What you need

- .NET 8 SDK, Node.js 22 or later, Docker with Compose, `openssl`, `curl`.
- Two host names for the providers: add `127.0.0.1 payments.provider.test lms.provider.test` to `/etc/hosts`.

## Run the suite without keploy

```sh
cd crm-playwright
dotnet publish app/CrmLeads.csproj -c Release -o app/out
sh scripts/gen-certs.sh .certs
sudo sh scripts/trust-ca.sh .certs/ca.pem    # the providers' test CA, as a real provider's CA is trusted
(cd e2e && npm ci && npx playwright install --with-deps chromium)
sh scripts/deps.sh up                        # dependencies, providers, the table and the queue
(cd e2e && npx playwright test)
sh scripts/deps.sh down
```

Playwright starts the app itself (`webServer` in `e2e/playwright.config.ts`) on `http://127.0.0.1:5080`.

The test CA and the providers' certificate are valid for 30 days. `deps.sh up` starts nothing when the certificate is missing, out of date or not trusted on this machine; it says so and names the two commands to run again (`gen-certs.sh`, then `trust-ca.sh`).

## Record and replay with keploy

Install keploy (enterprise), then, from `crm-playwright/`:

```sh
sh scripts/deps.sh up
KEPLOY_PROVEN=1 keploy mock record --local
sh scripts/deps.sh down                      # every dependency and provider stopped
KEPLOY_PROVEN=1 keploy mock replay --local
```

`KEPLOY_PROVEN=1` turns on keploy's proven model: record runs each test again against its recording before it keeps it, and replay compares only the tests record kept. `--local` keeps the recordings in `keploy/` on this machine and runs without a Keploy sign-in. keploy 3.8.106 still asks for a sign-in when only `keploy.yml` sets `mock.local`, so pass the flag.

To measure the replay, `bash scripts/measure.sh 10` does the four steps above, with 10 replays. It exits 0 only when record and every replay exit 0 and each replay's own count line (`N test(s) ran: P passed, F failed ...; Q quarantined`) counts no failed test, at least one passed test, and the same counts as the first replay. `EXPECT_COUNTS=ran/passed/failed/quarantined` also pins the counts. Today it passes with 1 test compared and 6 quarantined (see below): `MIN_COMPARED=N` sets how many tests a run must compare to pass. The logs go to `measure-out/`; the last line says `MEASURE: PASS` or `MEASURE: FAIL`. `bash scripts/measure.sh record` and `bash scripts/measure.sh replay N` run the two halves.

The suite runs with one worker and no retries (`keploy.yml`): keploy serves one test at a time, and sets aside, with its reason, a test it cannot prove instead of retrying it.

## What record and replay give today

With keploy 3.8.106, record keeps 1 of the 7 tests: `refuses a lead without an email`, which makes no call to any dependency. It sets the other 6 aside as flaky (`request_changed`), and `keploy/crm-playwright/verdicts.yaml` names each one and its reason:

- Their DynamoDB calls carry values that change on every run: the lead ids and times the app makes, and the names and emails the suite makes. Pinning the suite's data with `CRM_FIXED_DATA=1` keeps no more tests.
- keploy refuses each changed call it cannot explain instead of guessing. It cannot yet explain two places these values reach: the AWS SDK signs every DynamoDB request over them (the `Authorization` header), and the SQS message each new lead sends carries them. keploy 3.8.106 also cannot follow them through the app's Pulsar messages. Following them through Pulsar alone keeps no more tests: the first two places still set the 6 aside.

`keploy mock replay` then runs that one test, which passes, and skips the 6. Its receipt says the replay did not prove the tests run with the dependencies off. So today this sample shows the record and replay flow on a .NET app with these dependencies; it does not yet show these dependencies replayed inside a test.
