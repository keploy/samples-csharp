// A small single-page UI: #/ lists the leads, #/new creates one with custom
// fields, #/lead/<id> shows one and takes a payment and an enrollment. Every
// value it shows comes from the backend's /api.
const view = document.getElementById('view');

const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);

async function api(method, path, body) {
  const res = await fetch(path, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : {},
    body: body ? JSON.stringify(body) : undefined,
  });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(data.error || `${method} ${path} answered ${res.status}`);
  return data;
}

async function listPage() {
  const leads = await api('GET', '/api/leads');
  view.innerHTML = `
    <h1>Leads</h1>
    <p><a href="#/new" id="new-lead">New lead</a></p>
    <table aria-label="Leads">
      <thead><tr><th>Name</th><th>Email</th><th>Source</th><th>Status</th></tr></thead>
      <tbody>${leads.map((l) => `
        <tr><td><a href="#/lead/${esc(l.id)}">${esc(l.name)}</a></td><td>${esc(l.email)}</td><td>${esc(l.source)}</td><td>${esc(l.status)}</td></tr>`).join('')}
      </tbody>
    </table>`;
}

function newPage() {
  view.innerHTML = `
    <h1>New lead</h1>
    <form id="lead-form">
      <label for="name">Name</label><input id="name" name="name">
      <label for="email">Email</label><input id="email" name="email">
      <label for="phone">Phone</label><input id="phone" name="phone">
      <label for="source">Source</label>
      <select id="source" name="source"><option>Website</option><option>Referral</option><option>Campaign</option></select>
      <label for="budget">Budget</label><input id="budget" name="budget" type="number">
      <fieldset id="custom"><legend>Custom fields</legend></fieldset>
      <button type="button" id="add-field">Add custom field</button>
      <p class="error" id="form-error" role="alert"></p>
      <button type="submit">Create lead</button>
    </form>`;
  const custom = document.getElementById('custom');
  document.getElementById('add-field').onclick = () => {
    const n = custom.querySelectorAll('.row').length + 1;
    const row = document.createElement('div');
    row.className = 'row';
    row.innerHTML = `<input aria-label="Custom field ${n} name" class="ck"><input aria-label="Custom field ${n} value" class="cv">`;
    custom.appendChild(row);
  };
  document.getElementById('lead-form').onsubmit = async (e) => {
    e.preventDefault();
    const f = e.target;
    const fields = {};
    custom.querySelectorAll('.row').forEach((r) => {
      const k = r.querySelector('.ck').value.trim();
      if (k) fields[k] = r.querySelector('.cv').value;
    });
    try {
      const lead = await api('POST', '/api/leads', {
        name: f.name.value, email: f.email.value, phone: f.phone.value, source: f.source.value,
        budget: f.budget.value === '' ? null : Number(f.budget.value), custom: fields,
      });
      location.hash = `#/lead/${lead.id}`;
    } catch (err) {
      document.getElementById('form-error').textContent = err.message;
    }
  };
}

async function leadPage(id) {
  const l = await api('GET', `/api/leads/${encodeURIComponent(id)}`);
  view.innerHTML = `
    <h1 id="lead-name">${esc(l.name)}</h1>
    <p>Email: <span id="lead-email">${esc(l.email)}</span> · Status: <span id="lead-status">${esc(l.status)}</span></p>
    <div class="card"><h2>Custom fields</h2><dl id="lead-custom">${Object.entries(l.custom || {}).map(([k, v]) => `<dt>${esc(k)}</dt><dd>${esc(v)}</dd>`).join('')}</dl></div>
    <div class="card"><h2>Payment</h2>
      <div id="payment">${l.payment ? `Charge ${esc(l.payment.chargeId)}: ${esc(l.payment.status)}` : `
        <label for="amount">Amount (cents)</label><input id="amount" type="number" value="4999">
        <button id="pay">Take payment</button>`}</div></div>
    <div class="card"><h2>Course</h2>
      <div id="enrollment">${l.enrollment ? `Enrolled in ${esc(l.enrollment.courseId)}: ${esc(l.enrollment.status)}` : `
        <label for="course">Course</label><select id="course"><option value="crm-101">CRM basics</option><option value="sales-201">Sales pipeline</option></select>
        <button id="enroll">Enroll</button>`}</div></div>
    <div class="card"><h2>Activity</h2><ul id="activity">${(l.activity || []).map((a) => `<li>${esc(a.type)}</li>`).join('')}</ul>
      <button id="refresh">Refresh</button></div>
    <p class="error" id="lead-error" role="alert"></p>`;
  const fail = (err) => { document.getElementById('lead-error').textContent = err.message; };
  const pay = document.getElementById('pay');
  if (pay) pay.onclick = () => api('POST', `/api/leads/${id}/payments`, { amount: Number(document.getElementById('amount').value), currency: 'USD' }).then(() => leadPage(id), fail);
  const enroll = document.getElementById('enroll');
  if (enroll) enroll.onclick = () => api('POST', `/api/leads/${id}/enrollments`, { courseId: document.getElementById('course').value }).then(() => leadPage(id), fail);
  document.getElementById('refresh').onclick = () => leadPage(id).catch(fail);
}

async function route() {
  const hash = location.hash || '#/';
  try {
    if (hash === '#/new') newPage();
    else if (hash.startsWith('#/lead/')) await leadPage(decodeURIComponent(hash.slice('#/lead/'.length)));
    else await listPage();
  } catch (err) {
    view.innerHTML = `<p class="error" role="alert">${esc(err.message)}</p>`;
  }
}

window.addEventListener('hashchange', route);
route();
