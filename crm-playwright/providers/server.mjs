// Two fake third parties over HTTPS, as the app meets real ones: a payment
// provider (POST /v1/charges) and an LMS (POST /api/v2/enrollments). Each
// answers with ids and timestamps of its own, different on every call.
// Run at record only; keploy replays them with this server stopped.
//
//   node providers/server.mjs CERT_DIR
//
// CERT_DIR holds provider.pem and provider-key.pem (scripts/gen-certs.sh).
import { createServer } from 'node:https';
import { randomBytes } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const dir = process.argv[2];
if (!dir) {
  console.error('usage: node providers/server.mjs CERT_DIR');
  process.exit(2);
}
const tls = { cert: readFileSync(join(dir, 'provider.pem')), key: readFileSync(join(dir, 'provider-key.pem')) };
const id = (prefix) => `${prefix}_${randomBytes(8).toString('hex')}`;

function serve(name, port, routes) {
  createServer(tls, (req, res) => {
    let body = '';
    req.on('data', (c) => { body += c; });
    req.on('end', () => {
      const send = (status, value) => {
        res.writeHead(status, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify(value));
      };
      const route = routes[`${req.method} ${req.url}`];
      if (!route) return send(404, { error: `${name}: no route ${req.method} ${req.url}` });
      let json;
      try {
        json = JSON.parse(body || '{}');
      } catch {
        return send(400, { error: `${name}: the body is not JSON` });
      }
      const [status, value] = route(req, json);
      send(status, value);
    });
  }).listen(port, '0.0.0.0', () => console.log(`${name} listening on https://0.0.0.0:${port}`));
}

serve('payments', Number(process.env.PAYMENTS_PORT || 8443), {
  'POST /v1/charges': (req, b) => {
    if (req.headers.authorization !== 'Bearer sk_test_crm_sample') return [401, { error: 'bad api key' }];
    if (!req.headers['idempotency-key']) return [400, { error: 'Idempotency-Key is required' }];
    if (!Number.isInteger(b.amount) || b.amount <= 0) return [400, { error: 'amount must be a positive integer' }];
    const status = b.amount > 100000 ? 'declined' : 'succeeded';
    return [200, { id: id('ch'), status, amount: b.amount, currency: b.currency, created: Math.floor(Date.now() / 1000) }];
  },
});

serve('lms', Number(process.env.LMS_PORT || 9443), {
  'POST /api/v2/enrollments': (req, b) => {
    if (req.headers['x-api-key'] !== 'lms_test_crm_sample') return [401, { error: 'bad api key' }];
    if (!b.course_id || !b.learner?.email) return [400, { error: 'course_id and learner.email are required' }];
    return [201, { enrollment_id: id('enr'), status: 'active', course_id: b.course_id, enrolled_at: new Date().toISOString() }];
  },
});
