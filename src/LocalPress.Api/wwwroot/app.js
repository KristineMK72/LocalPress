const API = '';

const state = { tenantId: null, tenant: null, orders: [], products: [], zones: [] };

async function api(path, opts) {
  const res = await fetch(API + path, {
    headers: { 'Content-Type': 'application/json', ...(opts?.headers || {}) },
    ...opts
  });
  if (!res.ok) throw new Error(`${res.status} ${path}`);
  if (res.status === 204) return null;
  return res.json();
}

function showView(name) {
  document.querySelectorAll('.view').forEach(v => v.classList.add('hidden'));
  document.getElementById('view-' + name)?.classList.remove('hidden');
  document.querySelectorAll('.nav-item').forEach(n => {
    n.classList.toggle('active', n.dataset.view === name);
  });
  const titles = {
    dashboard: ['Dashboard', 'Overview of your local print operations'],
    orders: ['Orders', 'Production pipeline'],
    products: ['Products', 'Your catalog'],
    zones: ['Zones', 'Map-based delivery areas']
  };
  const t = titles[name] || titles.dashboard;
  document.getElementById('pageTitle').textContent = t[0];
  document.getElementById('pageSub').textContent = t[1];
}

document.querySelectorAll('[data-view]').forEach(el => {
  el.addEventListener('click', (e) => {
    e.preventDefault();
    showView(el.dataset.view);
  });
});

function money(n) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n ?? 0);
}

function ordersTable(orders) {
  if (!orders.length) return '<p style="color:#94a3b8">No orders yet.</p>';
  return `<table>
    <thead><tr><th>Order</th><th>Customer</th><th>Status</th><th>Total</th></tr></thead>
    <tbody>
      ${orders.map(o => `<tr>
        <td><strong>${o.orderNumber}</strong></td>
        <td>${o.customerName || '—'}</td>
        <td><span class="chip ${o.status}">${o.status}</span></td>
        <td>${money(o.grandTotal)}</td>
      </tr>`).join('')}
    </tbody>
  </table>`;
}

function render() {
  const open = state.orders.filter(o => !['Completed','Cancelled','Refunded'].includes(o.status)).length;
  const prod = state.orders.filter(o => o.status === 'InProduction').length;
  document.getElementById('orderBadge').textContent = open;
  document.getElementById('stats').innerHTML = `
    <div class="stat"><div class="label">Open orders</div><div class="value">${open}</div></div>
    <div class="stat"><div class="label">In production</div><div class="value">${prod}</div></div>
    <div class="stat"><div class="label">Products</div><div class="value">${state.products.length}</div></div>
  `;
  document.getElementById('dashOrders').innerHTML = ordersTable(state.orders.slice(0, 5));
  document.getElementById('ordersTable').innerHTML = ordersTable(state.orders);

  const prodHtml = (list) => list.map(p => `
    <div class="product-card">
      <div class="name">${p.name}</div>
      <div class="sku">${p.sku || p.category || 'POD'}</div>
      <div class="price">${money(p.basePrice)}</div>
    </div>
  `).join('') || '<p style="color:#94a3b8">No products</p>';

  document.getElementById('dashProducts').innerHTML = prodHtml(state.products);
  document.getElementById('productsGrid').innerHTML = prodHtml(state.products);

  document.getElementById('dashZones').innerHTML = state.zones.length
    ? state.zones.map(z => `<div class="zone-item"><strong>${z.name}</strong>
        Ship from ${money(z.baseShippingPrice)} · ${z.estimatedDaysMin}–${z.estimatedDaysMax} days
        ${z.freeShippingMinimum != null ? ` · Free over ${money(z.freeShippingMinimum)}` : ''}
      </div>`).join('')
    : '<p style="color:#94a3b8">No zones</p>';

  document.getElementById('zonesList').innerHTML = document.getElementById('dashZones').innerHTML;

  if (state.tenant) {
    document.getElementById('shopLabel').textContent =
      `${state.tenant.name} · ${state.tenant.city || ''}, ${state.tenant.state || ''}`;
  }
}

async function load() {
  try {
    await api('/api/health');
    document.getElementById('apiStatus').textContent = 'API online';
    document.getElementById('apiStatus').className = 'pill ok';
  } catch {
    document.getElementById('apiStatus').textContent = 'API offline';
    document.getElementById('apiStatus').className = 'pill bad';
    return;
  }

  const tenants = await api('/api/tenants');
  state.tenant = tenants[0];
  state.tenantId = state.tenant.id;

  const tid = state.tenantId;
  const [products, orders, zones] = await Promise.all([
    api(`/api/tenants/${tid}/products`),
    api(`/api/tenants/${tid}/orders`),
    api(`/api/tenants/${tid}/zones`)
  ]);
  state.products = products;
  state.orders = orders;
  state.zones = zones;
  render();
}

document.getElementById('btnMatch').addEventListener('click', async () => {
  const lat = parseFloat(document.getElementById('testLat').value);
  const lng = parseFloat(document.getElementById('testLng').value);
  try {
    const result = await api(`/api/tenants/${state.tenantId}/zones/match`, {
      method: 'POST',
      body: JSON.stringify({ latitude: lat, longitude: lng })
    });
    document.getElementById('matchResult').textContent = result
      ? JSON.stringify(result, null, 2)
      : 'No zone matched for that point.';
  } catch (e) {
    document.getElementById('matchResult').textContent = String(e);
  }
});

document.getElementById('btnNewOrder').addEventListener('click', async () => {
  if (!state.products.length) return alert('No products');
  const p = state.products[0];
  try {
    await api(`/api/tenants/${state.tenantId}/orders`, {
      method: 'POST',
      body: JSON.stringify({
        customerName: 'Walk-in Customer',
        customerEmail: 'walkin@example.com',
        shipCity: state.tenant?.city || 'Brainerd',
        shipState: state.tenant?.state || 'MN',
        shipLatitude: 46.36,
        shipLongitude: -94.20,
        lines: [{ productName: p.name, quantity: 1, unitPrice: p.basePrice }]
      })
    });
    await load();
    showView('orders');
  } catch (e) {
    alert(e.message);
  }
});

load();
