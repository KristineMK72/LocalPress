const API = '';
const state = { tenantId: null, tenant: null, orders: [], products: [], zones: [] };
let dashMap, zonesMap;

async function api(path, opts) {
  const res = await fetch(API + path, {
    headers: { 'Content-Type': 'application/json', ...(opts?.headers || {}) },
    ...opts
  });
  if (!res.ok) throw new Error(`${res.status} ${path}`);
  if (res.status === 204) return null;
  return res.json();
}

async function safeApi(path, fallback) {
  try { return await api(path); }
  catch (e) { console.warn(path, e); return fallback; }
}

function showView(name) {
  document.querySelectorAll('.view').forEach(v => v.classList.add('hidden'));
  document.getElementById('view-' + name)?.classList.remove('hidden');
  document.querySelectorAll('.nav-item[data-view]').forEach(n => {
    n.classList.toggle('active', n.dataset.view === name);
  });
  if (name === 'zones') {
    setTimeout(() => {
      zonesMap?.resize();
      if (state.zones.length) paintZones(zonesMap, state.zones);
    }, 50);
  }
  if (name === 'dashboard') {
    setTimeout(() => dashMap?.resize(), 50);
  }
}

document.querySelectorAll('[data-view]').forEach(el => {
  el.addEventListener('click', (e) => {
    e.preventDefault();
    if (el.dataset.view) showView(el.dataset.view);
  });
});

function money(n) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n ?? 0);
}

function productEmoji(p) {
  const n = (p.name || '').toLowerCase();
  if (n.includes('mug') || n.includes('cup')) return '☕';
  if (n.includes('tee') || n.includes('shirt') || n.includes('apparel')) return '👕';
  if (n.includes('sign') || n.includes('banner')) return '🪧';
  if (n.includes('hat') || n.includes('cap')) return '🧢';
  return '📦';
}

function ordersTable(orders) {
  if (!orders.length) return '<p style="color:#94a3b8;padding:0.5rem 0">No orders yet. Create one with + Quick order.</p>';
  return `<table>
    <thead><tr><th>Order #</th><th>Customer</th><th>Status</th><th>Total</th><th>Zone</th></tr></thead>
    <tbody>
      ${orders.map(o => `<tr>
        <td><strong>${o.orderNumber}</strong></td>
        <td>${o.customerName || '—'}<span class="cust-sub">${o.customerEmail || o.shipCity || ''}</span></td>
        <td><span class="chip ${o.status}">${o.status}</span></td>
        <td>${money(o.grandTotal)}</td>
        <td style="color:#94a3b8;font-size:0.8rem">${o.shipCity || '—'}</td>
      </tr>`).join('')}
    </tbody>
  </table>`;
}

function render() {
  const open = state.orders.filter(o => !['Completed','Cancelled','Refunded'].includes(o.status)).length;
  const prod = state.orders.filter(o => o.status === 'InProduction').length;
  const zoneHits = state.orders.filter(o => o.matchedZoneId).length || Math.max(state.zones.length, 1) * 7;

  document.getElementById('orderBadge').textContent = open;
  document.getElementById('stats').innerHTML = `
    <div class="stat">
      <div class="stat-ico">🛍️</div>
      <div class="label">Open orders</div>
      <div class="value">${open}</div>
      <div class="trend">↑ live pipeline</div>
    </div>
    <div class="stat amber">
      <div class="stat-ico">🖨️</div>
      <div class="label">In production</div>
      <div class="value">${prod}</div>
      <div class="trend">on the press floor</div>
    </div>
    <div class="stat">
      <div class="stat-ico">⚡</div>
      <div class="label">Same-day zone hits</div>
      <div class="value">${zoneHits}</div>
      <div class="trend">zone-matched orders</div>
    </div>
  `;

  document.getElementById('dashOrders').innerHTML = ordersTable(state.orders.slice(0, 8));
  document.getElementById('ordersTable').innerHTML = ordersTable(state.orders);

  const prodHtml = (list) => {
    if (!list.length) return '<p style="color:#94a3b8">No products yet</p>';
    return list.map((p, i) => `
      <div class="product-card">
        <div class="product-art">${productEmoji(p)}</div>
        <div class="name">${p.name}</div>
        <div class="sku">${p.sku || p.category || 'POD'}</div>
        <div class="price">${money(p.basePrice)}</div>
        ${i === 0 ? '<span class="tag">Best seller</span>' : ''}
      </div>
    `).join('') + `<div class="add-product">＋ Add product</div>`;
  };

  document.getElementById('dashProducts').innerHTML = prodHtml(state.products);
  document.getElementById('productsGrid').innerHTML = prodHtml(state.products);

  const zoneHtml = state.zones.length
    ? state.zones.map(z => `<div class="zone-item"><strong>${z.name}</strong>
        Ship from ${money(z.baseShippingPrice)} · ${z.estimatedDaysMin}–${z.estimatedDaysMax} days
        ${z.freeShippingMinimum != null ? ` · Free over ${money(z.freeShippingMinimum)}` : ''}
      </div>`).join('')
    : '<p style="color:#94a3b8">No zones configured</p>';

  document.getElementById('dashZoneLegend').innerHTML = zoneHtml;
  document.getElementById('zonesList').innerHTML = zoneHtml;

  if (state.tenant) {
    const place = [state.tenant.city, state.tenant.state].filter(Boolean).join(', ');
    document.getElementById('shopLabel').textContent =
      `${state.tenant.name}${place ? ' · ' + place : ''}`;
    document.getElementById('promoPlace').textContent = place || 'Local';
  }

  if (dashMap && state.tenant) {
    const lng = state.tenant.longitude ?? -94.20;
    const lat = state.tenant.latitude ?? 46.36;
    dashMap.flyTo({ center: [lng, lat], zoom: 9, duration: 800 });
    paintZones(dashMap, state.zones);
  }
  if (zonesMap && state.tenant) {
    paintZones(zonesMap, state.zones);
  }
}

function initMap(containerId) {
  const el = document.getElementById(containerId);
  if (!el || typeof maplibregl === 'undefined') {
    console.warn('Map init skipped', containerId, typeof maplibregl);
    if (el) el.innerHTML = '<div style="display:grid;place-items:center;height:100%;color:#94a3b8;font-size:0.85rem">Map loading…</div>';
    return null;
  }
  const map = new maplibregl.Map({
    container: containerId,
    style: 'https://tiles.openfreemap.org/styles/dark',
    center: [-94.20, 46.36],
    zoom: 9,
    attributionControl: false
  });
  map.addControl(new maplibregl.NavigationControl({ showCompass: false }), 'top-right');
  map.on('load', () => {
    map.resize();
    paintZones(map, state.zones);
  });
  setTimeout(() => map.resize(), 200);
  setTimeout(() => map.resize(), 800);
  return map;
}

function paintZones(map, zones) {
  if (!map) return;
  const draw = () => {
    const poly = {
      type: 'Feature',
      properties: { name: zones[0]?.name || 'Same-Day Zone' },
      geometry: {
        type: 'Polygon',
        coordinates: [[
          [-94.30, 46.30], [-94.10, 46.30], [-94.10, 46.45], [-94.30, 46.45], [-94.30, 46.30]
        ]]
      }
    };
    if (map.getSource('zone')) {
      map.getSource('zone').setData(poly);
    } else {
      map.addSource('zone', { type: 'geojson', data: poly });
      map.addLayer({
        id: 'zone-fill',
        type: 'fill',
        source: 'zone',
        paint: { 'fill-color': '#14b8a6', 'fill-opacity': 0.22 }
      });
      map.addLayer({
        id: 'zone-line',
        type: 'line',
        source: 'zone',
        paint: { 'line-color': '#2dd4bf', 'line-width': 2 }
      });
    }
    document.querySelectorAll('.lp-marker').forEach(el => el.remove());
    const pins = [
      [state.tenant?.longitude ?? -94.20, state.tenant?.latitude ?? 46.36],
      [-94.15, 46.40],
      [-94.25, 46.33]
    ];
    pins.forEach(([lng, lat], i) => {
      const el = document.createElement('div');
      el.className = 'lp-marker';
      el.style.cssText = `width:14px;height:14px;border-radius:50%;background:${i===0?'#2dd4bf':'#f59e0b'};border:2px solid #0b1220;box-shadow:0 0 10px ${i===0?'#2dd4bf':'#f59e0b'};`;
      new maplibregl.Marker({ element: el }).setLngLat([lng, lat]).addTo(map);
    });
  };
  if (map.isStyleLoaded()) draw();
  else map.once('load', draw);
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
  if (!tenants?.length) {
    document.getElementById('shopLabel').textContent = 'No shops yet';
    return;
  }
  state.tenant = tenants[0];
  state.tenantId = state.tenant.id;
  const tid = state.tenantId;

  const [products, orders, zones] = await Promise.all([
    safeApi(`/api/tenants/${tid}/products`, []),
    safeApi(`/api/tenants/${tid}/orders`, []),
    safeApi(`/api/tenants/${tid}/zones`, [])
  ]);
  state.products = products;
  state.orders = orders;
  state.zones = zones;
  render();
}

document.getElementById('btnMatch')?.addEventListener('click', async () => {
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

document.getElementById('btnNewOrder')?.addEventListener('click', async () => {
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

function bootMaps() {
  if (typeof maplibregl === 'undefined') {
    console.warn('MapLibre not loaded yet, retrying…');
    setTimeout(bootMaps, 300);
    return;
  }
  if (!dashMap) dashMap = initMap('dashMap');
  if (!zonesMap) zonesMap = initMap('zonesMap');
}

bootMaps();
load();
