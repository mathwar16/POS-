let currentPage = 1;
let charts = {};
let bestSellingProductsData = [];

window.onload = async () => {
  if (!checkAuth()) {
    window.location.href = 'login.html';
    return;
  }
  initDashboard();
};

function initDashboard() {
  initProfile();
  loadStats();
}

// Profile initialization is now in common.js

function handleDateRangeChange() {
  const range = document.getElementById('dateRange').value;
  const customDiv = document.getElementById('customDateRange');

  if (range === 'custom') {
    customDiv.classList.add('show');
  } else {
    customDiv.classList.remove('show');
    currentPage = 1;
    loadStats();
  }
}

async function loadStats(page = 1) {
  currentPage = page;
  const range = document.getElementById('dateRange').value;
  let startDate = null;
  let endDate = null;

  const now = new Date();
  const today = now.toISOString().split('T')[0];

  if (range === 'today') {
    startDate = today;
    endDate = today;
  } else if (range === 'yesterday') {
    const yesterday = new Date(now);
    yesterday.setDate(now.getDate() - 1);
    startDate = yesterday.toISOString().split('T')[0];
    endDate = startDate;
  } else if (range === 'last7') {
    const last7 = new Date(now);
    last7.setDate(now.getDate() - 7);
    startDate = last7.toISOString().split('T')[0];
    endDate = today;
  } else if (range === 'thisMonth') {
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    startDate = firstDay.toISOString().split('T')[0];
    endDate = today;
  } else if (range === 'lastYear') {
    const lastYear = new Date(now);
    lastYear.setFullYear(now.getFullYear() - 1);
    startDate = lastYear.toISOString().split('T')[0];
    endDate = today;
  } else if (range === 'custom') {
    startDate = document.getElementById('startDate').value;
    endDate = document.getElementById('endDate').value;
  }

  try {
    const response = await getDashboardStats(startDate, endDate, currentPage, 8);
    renderDashboard(response);
  } catch (error) {
    console.error('Error loading stats:', error);
  }
}

function renderDashboard(data) {
  if (!data || !data.summary) {
    console.error("Dashboard data is missing or malformed", data);
    return;
  }
  const { summary, pagination } = data;

  // 1. Update KPI Cards
  updateKPI('revenue', `₹${(summary.totalRevenue || 0).toLocaleString()}`, summary.revenueTrend || 0, 'revenueTrend');
  updateKPI('orders', summary.totalOrders || 0, summary.ordersTrend || 0, 'ordersTrend');
  updateKPI('avgOrder', `₹${(summary.avgOrderValue || 0).toFixed(2)}`, summary.aovTrend || 0, 'aovTrend');

  if (document.getElementById('grossRevenue')) {
    document.getElementById('grossRevenue').innerText = `Gross: ₹${(summary.grossRevenue || 0).toLocaleString()}`;
  }
  if (document.getElementById('peakTime')) {
    document.getElementById('peakTime').innerText = summary.peakOrderTime || 'N/A';
  }

  // 2. Render Charts
  renderRevenueChart(summary.revenueChart);
  renderOrderVolumeChart(summary.orderVolumeChart);
  renderPlatformChart(summary.platformBreakdown);
  renderPaymentModeChart(summary.paymentMethods);

  // 3. Best Selling Products
  bestSellingProductsData = summary.bestSellingProducts || [];
  renderBestSellingList(bestSellingProductsData.slice(0, 5));

  // 4. Recent Orders Table
  const recentOrdersEl = document.getElementById("recentOrders");
  if (recentOrdersEl) {
    if (summary.recentOrders.length > 0) {
      recentOrdersEl.innerHTML = summary.recentOrders.map(order => `
                <tr style="border-bottom: 1px solid var(--border); transition: background 0.2s;" onmouseover="this.style.background='rgba(255,255,255,0.03)'" onmouseout="this.style.background='none'">
                    <td style="padding: 1rem; font-weight: 500; color: var(--primary-light);">#${order.billNumber}</td>
                    <td style="padding: 1rem; font-size: 0.85rem; color: var(--text-secondary);">${formatToIST(order.date)}</td>
                    <td style="padding: 1rem;">
                        <span style="background: rgba(99, 102, 241, 0.1); padding: 0.25rem 0.6rem; border-radius: 0.5rem; font-size: 0.75rem;">${order.platform}</span>
                    </td>
                    <td style="padding: 1rem; font-size: 0.85rem;">${order.paymentMethod}</td>
                    <td style="padding: 1rem; color: var(--text-muted);">₹${order.subtotal.toFixed(0)}</td>
                    <td style="padding: 1rem; font-weight: 700; color: var(--success);">₹${order.total.toFixed(0)}</td>
                </tr>
            `).join('');
    } else {
      recentOrdersEl.innerHTML = '<tr><td colspan="6" style="padding: 3rem; text-align: center; color: var(--text-muted);">No transactions found for this period</td></tr>';
    }
  }

  renderPagination(pagination);
}

function updateKPI(id, value, trend, trendId) {
  const valEl = document.getElementById(id);
  const trendEl = document.getElementById(trendId);
  if (valEl) valEl.innerText = value;

  if (trendEl) {
    const t = trend || 0;
    const isUp = t > 0;
    const absTrend = Math.abs(t);
    trendEl.className = `trend-badge ${t > 0 ? 'trend-up' : (t < 0 ? 'trend-down' : 'trend-neutral')}`;
    trendEl.innerText = `${isUp ? '↑' : (t < 0 ? '↓' : '')} ${absTrend.toFixed(1)}%`;
  }
}

const commonOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: false,
      labels: { boxWidth: 10, font: { size: 10 }, color: '#94a3b8' }
    }
  },
  scales: {
    y: { grid: { color: 'rgba(148, 163, 184, 0.05)' }, ticks: { font: { size: 10 }, color: '#94a3b8' } },
    x: { grid: { display: false }, ticks: { font: { size: 10 }, color: '#94a3b8' } }
  }
};

function renderRevenueChart(data) {
  const canvas = document.getElementById('revenueChart');
  if (!canvas || typeof Chart === 'undefined') return;
  const ctx = canvas.getContext('2d');
  const labels = (data || []).map(d => d.label);
  const values = (data || []).map(d => d.value);

  if (charts.revenue) charts.revenue.destroy();

  charts.revenue = new Chart(ctx, {
    type: 'line',
    data: {
      labels: labels,
      datasets: [{
        label: 'Revenue',
        data: values,
        borderColor: '#6366f1',
        backgroundColor: 'rgba(99, 102, 241, 0.05)',
        fill: true,
        tension: 0.4,
        pointRadius: 2,
        borderWidth: 2
      }]
    },
    options: commonOptions
  });
}

function renderOrderVolumeChart(data) {
  const canvas = document.getElementById('orderVolumeChart');
  if (!canvas || typeof Chart === 'undefined') {
    console.error('Order Volume Chart: Canvas element or Chart.js not found');
    return;
  }

  const ctx = canvas.getContext('2d');
  console.log('Order Volume Chart Data:', data);

  const labels = (data || []).map(d => d.label);
  const counts = (data || []).map(d => d.count);

  if (charts.orders) charts.orders.destroy();

  charts.orders = new Chart(ctx, {
    type: 'bar',
    data: {
      labels: labels,
      datasets: [{
        label: 'Orders',
        data: counts,
        backgroundColor: 'rgba(16, 185, 129, 0.6)',
        borderRadius: 4,
        barThickness: 12
      }]
    },
    options: commonOptions
  });
}

function renderPlatformChart(data) {
  const canvas = document.getElementById('platformChart');
  if (!canvas || typeof Chart === 'undefined') {
    console.error('Platform Chart: Canvas element or Chart.js not found');
    return;
  }

  const ctx = canvas.getContext('2d');
  console.log('Platform Chart Data:', data);

  const chartData = data && data.length > 0 ? data : [
    { name: 'Direct', amount: 0 }
  ];

  const labels = chartData.map(d => d.name);
  const values = chartData.map(d => d.amount);

  if (charts.platform) charts.platform.destroy();

  charts.platform = new Chart(ctx, {
    type: 'doughnut',
    data: {
      labels: labels,
      datasets: [{
        data: values,
        backgroundColor: ['#6366f1', '#f59e0b', '#3b82f6', '#10b981'],
        borderWidth: 0
      }]
    },
    options: {
      ...commonOptions,
      plugins: {
        ...commonOptions.plugins,
        legend: { display: true, position: 'bottom', labels: { boxWidth: 8, font: { size: 9 }, color: '#94a3b8' } }
      },
      cutout: '75%'
    }
  });
}

function renderPaymentModeChart(data) {
  const container = document.getElementById('paymentModeStats');
  if (!container) return;

  // Handle empty data
  const stats = data && data.length > 0 ? data : [];

  if (stats.length === 0) {
    container.innerHTML = '<div style="text-align:center; color: var(--text-muted); padding: 1rem;">No payment data</div>';
    return;
  }

  container.innerHTML = stats.map(stat => `
    <div class="stat-item" style="display: flex; justify-content: space-between; align-items: center; padding: 0.75rem; background: rgba(255, 255, 255, 0.03); border-radius: 0.5rem; margin-bottom: 0.5rem;">
      <div style="display: flex; align-items: center; gap: 0.75rem;">
        <div style="width: 8px; height: 8px; border-radius: 50%; background-color: ${getPaymentColor(stat.name)}"></div>
        <span style="font-weight: 500; color: var(--text-secondary);">${stat.name}</span>
      </div>
      <div style="text-align: right;">
         <div style="font-weight: 600; color: var(--text-primary);">₹${stat.amount.toLocaleString()}</div>
         <div style="font-size: 0.75rem; color: var(--text-muted);">${stat.count} orders</div>
      </div>
    </div>
  `).join('');
}

function getPaymentColor(mode) {
  const modeLower = mode.toLowerCase();
  if (modeLower.includes('cash')) return '#10b981'; // Green
  if (modeLower.includes('upi')) return '#6366f1';  // Indigo
  if (modeLower.includes('card')) return '#f59e0b'; // Amber
  return '#94a3b8'; // Gray default
}

function renderPagination(pagination) {
  const container = document.getElementById("pagination");
  if (!container) return;

  if (pagination.totalPages <= 1) {
    container.innerHTML = "";
    return;
  }

  let html = `
        <button class="page-btn" ${pagination.page === 1 ? 'disabled' : ''} onclick="loadStats(${pagination.page - 1})">Prev</button>
    `;

  for (let i = 1; i <= pagination.totalPages; i++) {
    html += `
            <button class="page-btn ${pagination.page === i ? 'active' : ''}" onclick="loadStats(${i})">${i}</button>
        `;
  }

  html += `
        <button class="page-btn" ${pagination.page === pagination.totalPages ? 'disabled' : ''} onclick="loadStats(${pagination.page + 1})">Next</button>
    `;

  container.innerHTML = html;
}

function renderBestSellingList(products) {
  const container = document.getElementById('bestSellingList');
  if (!container) return;

  if (products.length === 0) {
    container.innerHTML = '<div style="text-align:center; color: var(--text-muted); padding: 1rem;">No data available</div>';
    return;
  }

  container.innerHTML = products.map(p => `
    <div class="stat-item" style="display: flex; justify-content: space-between; align-items: center; padding: 0.75rem; background: rgba(255, 255, 255, 0.03); border-radius: 0.5rem; margin-bottom: 0.5rem;">
      <span style="font-weight: 500; color: var(--text-secondary);">${p.name}</span>
      <span style="font-weight: 700; color: var(--accent-light);">${p.count} sold</span>
    </div>
  `).join('');
}

function showBestSellingModal() {
  const modal = document.getElementById('bestSellingModal');
  const tbody = document.getElementById('allBestSellingProducts');
  if (!modal || !tbody) return;

  tbody.innerHTML = bestSellingProductsData.map(p => `
    <tr style="border-bottom: 1px solid var(--border);">
      <td style="padding: 1rem; color: var(--text-primary);">${p.name}</td>
      <td style="padding: 1rem; color: var(--text-secondary);">${p.count}</td>
      <td style="padding: 1rem; font-weight: 700; color: var(--success);">₹${p.amount.toLocaleString()}</td>
    </tr>
  `).join('');

  modal.classList.add('show');
}

function closeBestSellingModal() {
  const modal = document.getElementById('bestSellingModal');
  if (modal) modal.classList.remove('show');
}
