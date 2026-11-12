import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { analyticsAPI } from '../services/api';

// ============================ Допоміжні функції ============================
const fmtMoney = (v) => Number(v || 0).toLocaleString('uk-UA', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fmtInt = (v) => Math.round(Number(v) || 0).toLocaleString('uk-UA');
const fmtPercent = (v) => `${(Number(v || 0) * 100).toFixed(1)}%`;

// Компактний формат для підписів на осі графіка
const fmtCompact = (v) => {
  const n = Number(v) || 0;
  if (Math.abs(n) >= 1000000) return `${(n / 1000000).toFixed(1)}M`;
  if (Math.abs(n) >= 1000) return `${(n / 1000).toFixed(1)}k`;
  return String(Math.round(n));
};
// Витягування тексту помилки з ApiResponse
const errMessage = (err, fallback) => err?.response?.data?.errors?.[0] || err?.response?.data?.message || fallback;
// Завантаження бінарного файлу з response.data (Blob)
const downloadBlob = (blob, filename) => {
  const url = window.URL.createObjectURL(new Blob([blob]));
  const link = document.createElement('a');
  link.href = url;
  link.setAttribute('download', filename);
  document.body.appendChild(link);
  link.click();
  window.URL.revokeObjectURL(url);
  document.body.removeChild(link);
};

// ============================ Спільні стилі ============================
const cardStyle = { background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)' };
const kpiStyle = { ...cardStyle, textAlign: 'center' };
const tabsStyle = { display: 'flex', gap: 8, marginBottom: 24, flexWrap: 'wrap' };
const tableStyle = { width: '100%', borderCollapse: 'collapse' };
const thStyle = { padding: '12px 16px', textAlign: 'left', borderBottom: '1px solid #eee', cursor: 'pointer', userSelect: 'none', whiteSpace: 'nowrap' };
const tdStyle = { padding: '12px 16px', borderBottom: '1px solid #f0f0f0' };
const selectStyle = { padding: '8px 12px', borderRadius: 8, border: '1px solid #ddd', background: '#fff' };
const btnStyle = { padding: '8px 14px', border: 'none', borderRadius: 8, cursor: 'pointer', color: '#fff', fontWeight: 500 };
const emptyChartStyle = { display: 'flex', alignItems: 'center', justifyContent: 'center', height: 200, color: '#8a94a6' };

// ============================ Графіки (без зовнішніх бібліотек) ============================

// Лінійний графік (SVG): data = [{ label, value }]
function LineChart({ data, height = 260, color = '#165DFF' }) {
  if (!data || data.length === 0) return <div style={emptyChartStyle}>Даних за цей період немає</div>;
  const width = 680;
  const padL = 56, padR = 20, padT = 20, padB = 36;
  const innerW = width - padL - padR;
  const innerH = height - padT - padB;
  const max = Math.max(1, ...data.map((d) => Number(d.value) || 0));
  const stepX = data.length > 1 ? innerW / (data.length - 1) : 0;
  const xAt = (i) => padL + stepX * i;
  const yAt = (v) => padT + innerH - ((Number(v) || 0) / max) * innerH;
  const line = data.map((d, i) => `${xAt(i)},${yAt(d.value)}`).join(' ');
  const area = `${padL},${padT + innerH} ${line} ${xAt(data.length - 1)},${padT + innerH}`;
  const labelStep = Math.max(1, Math.ceil(data.length / 8));
  return (
    <svg viewBox={`0 0 ${width} ${height}`} width="100%" height={height} preserveAspectRatio="xMidYMid meet">
      {[0, 0.25, 0.5, 0.75, 1].map((t, i) => {
        const y = padT + innerH - t * innerH;
        return (
          <g key={i}>
            <line x1={padL} y1={y} x2={width - padR} y2={y} stroke="#eef0f3" strokeWidth="1" />
            <text x={padL - 8} y={y + 4} textAnchor="end" fontSize="11" fill="#8a94a6">{fmtCompact(max * t)}</text>
          </g>
        );
      })}
      <polygon points={area} fill={color} opacity="0.08" />
      <polyline points={line} fill="none" stroke={color} strokeWidth="2.5" strokeLinejoin="round" />
      {data.map((d, i) => (
        <g key={i}>
          <circle cx={xAt(i)} cy={yAt(d.value)} r="3.5" fill="#fff" stroke={color} strokeWidth="2">
            <title>{`${d.label}: ${fmtMoney(d.value)} грн`}</title>
          </circle>
          {i % labelStep === 0 && (
            <text x={xAt(i)} y={height - 12} textAnchor="middle" fontSize="10" fill="#8a94a6">{d.label}</text>
          )}
        </g>
      ))}
    </svg>
  );
}

// Стовпчиковий графік (горизонтальні смуги): data = [{ label, value }]
function BarChart({ data, color = '#165DFF', maxItems = 10 }) {
  const items = (data || []).slice(0, maxItems);
  if (items.length === 0) return <div style={emptyChartStyle}>Даних за цей період немає</div>;
  const max = Math.max(1, ...items.map((d) => Number(d.value) || 0));
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      {items.map((d, i) => (
        <div key={i} style={{ display: 'grid', gridTemplateColumns: '160px 1fr 120px', alignItems: 'center', gap: 12 }}>
          <span style={{ fontSize: '0.85rem', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={d.label}>{d.label}</span>
          <div style={{ background: '#f0f2f5', borderRadius: 6, height: 18 }}>
            <div style={{ width: `${((Number(d.value) || 0) / max) * 100}%`, height: '100%', background: color, borderRadius: 6, minWidth: 2 }} />
          </div>
          <span style={{ fontSize: '0.85rem', textAlign: 'right' }}>{fmtMoney(d.value)}</span>
        </div>
      ))}
    </div>
  );
}

// Кругова діаграма (SVG): data = [{ label, value, color }]
function PieChart({ data }) {
  const slices = (data || []).filter((d) => (Number(d.value) || 0) > 0);
  const total = slices.reduce((s, d) => s + (Number(d.value) || 0), 0);
  if (total === 0) return <div style={emptyChartStyle}>Даних за цей період немає</div>;
  const size = 180;
  const r = size / 2;
  const cx = r, cy = r;
  const polar = (a) => [cx + r * Math.cos(a), cy + r * Math.sin(a)];
  let angle = -Math.PI / 2;
  const paths = slices.map((d) => {
    const value = Number(d.value) || 0;
    const sweep = (value / total) * Math.PI * 2;
    const start = angle;
    const end = angle + sweep;
    angle = end;
    const [x1, y1] = polar(start);
    const [x2, y2] = polar(end);
    const largeArc = sweep > Math.PI ? 1 : 0;
    const isFull = sweep >= Math.PI * 2 - 0.0001;
    const path = isFull
      ? `M ${cx} ${cy - r} A ${r} ${r} 0 1 1 ${cx - 0.01} ${cy - r} Z`
      : `M ${cx} ${cy} L ${x1} ${y1} A ${r} ${r} 0 ${largeArc} 1 ${x2} ${y2} Z`;
    return { ...d, path, percent: (value / total) * 100 };
  });
  return (
    <div style={{ display: 'flex', gap: 28, alignItems: 'center', flexWrap: 'wrap', justifyContent: 'center' }}>
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
        {paths.map((s, i) => (
          <path key={i} d={s.path} fill={s.color} stroke="#fff" strokeWidth="1.5">
            <title>{`${s.label}: ${fmtInt(s.value)} (${s.percent.toFixed(1)}%)`}</title>
          </path>
        ))}
      </svg>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {paths.map((s, i) => (
          <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ width: 14, height: 14, borderRadius: 4, background: s.color, display: 'inline-block' }} />
            <span style={{ fontSize: '0.9rem' }}>{s.label}</span>
            <b style={{ marginLeft: 6 }}>{s.percent.toFixed(1)}%</b>
          </div>
        ))}
      </div>
    </div>
  );
}

// ============================ Таблиця з сортуванням і пагінацією ============================
function MetricsTable({ columns, rows, pageSize = 10 }) {
  const [sortKey, setSortKey] = useState(null);
  const [sortDir, setSortDir] = useState('desc');
  const [page, setPage] = useState(1);

  useEffect(() => { setPage(1); }, [rows]);

  const sorted = useMemo(() => {
    if (!sortKey) return rows;
    const copy = [...rows];
    copy.sort((a, b) => {
      const av = a[sortKey], bv = b[sortKey];
      let cmp;
      if (typeof av === 'number' && typeof bv === 'number') cmp = av - bv;
      else cmp = String(av ?? '').localeCompare(String(bv ?? ''), 'uk');
      return sortDir === 'asc' ? cmp : -cmp;
    });
    return copy;
  }, [rows, sortKey, sortDir]);

  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize));
  const current = Math.min(page, totalPages);
  const pageRows = sorted.slice((current - 1) * pageSize, current * pageSize);

  const toggleSort = (key) => {
    if (sortKey === key) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    else { setSortKey(key); setSortDir('desc'); }
  };

  if (!rows || rows.length === 0) return <div style={{ ...emptyChartStyle, height: 120 }}>Даних за цей період немає</div>;

  return (
    <div>
      <div style={{ overflowX: 'auto' }}>
        <table style={tableStyle}>
          <thead>
            <tr>
              {columns.map((c) => (
                <th key={c.key} style={{ ...thStyle, textAlign: c.align || 'left' }} onClick={() => toggleSort(c.key)}>
                  {c.label}{sortKey === c.key ? (sortDir === 'asc' ? ' ▲' : ' ▼') : ''}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {pageRows.map((row, i) => (
              <tr key={i}>
                {columns.map((c) => (
                  <td key={c.key} style={{ ...tdStyle, textAlign: c.align || 'left' }}>
                    {c.render ? c.render(row) : row[c.key]}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {totalPages > 1 && (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 12, marginTop: 16 }}>
          <button style={{ ...btnStyle, background: '#6c757d' }} disabled={current <= 1} onClick={() => setPage(current - 1)}>←</button>
          <span style={{ fontSize: '0.9rem' }}>Сторінка {current} з {totalPages}</span>
          <button style={{ ...btnStyle, background: '#6c757d' }} disabled={current >= totalPages} onClick={() => setPage(current + 1)}>→</button>
        </div>
      )}
    </div>
  );
}

// ============================ Кнопки експорту ============================
function ExportButtons({ type, range }) {
  const [busy, setBusy] = useState(null);
  const handle = async (format) => {
    try {
      setBusy(format);
      const res = await analyticsAPI.exportReport({
        type,
        format,
        from: range.from.toISOString(),
        to: range.to.toISOString(),
      });
      downloadBlob(res.data, `${type}_report.${format}`);
    } catch (err) {
      console.error('Помилка експорту звіту', err);
      alert('Помилка експорту звіту');
    } finally {
      setBusy(null);
    }
  };
  return (
    <div style={{ display: 'flex', gap: 8 }}>
      {['csv', 'xlsx', 'pdf'].map((f) => (
        <button key={f} style={{ ...btnStyle, background: '#28a745', opacity: busy ? 0.6 : 1 }} disabled={!!busy} onClick={() => handle(f)}>
          {busy === f ? '...' : `⤓ ${f.toUpperCase()}`}
        </button>
      ))}
    </div>
  );
}

const TABS = [
  { key: 'sales', label: 'Продажі' },
  { key: 'products', label: 'Товари' },
  { key: 'categories', label: 'Категорії' },
  { key: 'promotions', label: 'Акції' },
  { key: 'financial', label: 'Фінанси' },
];

const PERIODS = [
  { value: 'today', label: 'Сьогодні' },
  { value: 'yesterday', label: 'Вчора' },
  { value: '7d', label: 'Останні 7 днів' },
  { value: '30d', label: 'Останні 30 днів' },
  { value: 'custom', label: 'Довільний період' },
];

const PAYMENT_COLORS = ['#165DFF', '#FF7D00'];

// ============================ Сторінка ============================
function AdminAnalyticsPage() {
  const [period, setPeriod] = useState('30d');
  const [customFrom, setCustomFrom] = useState('');
  const [customTo, setCustomTo] = useState('');
  const [activeTab, setActiveTab] = useState('sales');

  const [overview, setOverview] = useState(null);
  const [tabData, setTabData] = useState({});
  const [loading, setLoading] = useState(true);
  const [tabLoading, setTabLoading] = useState(false);
  const [error, setError] = useState('');

  // Розрахунок діапазону дат за обраним періодом
  const range = useMemo(() => {
    const now = new Date();
    const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0);
    const endOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
    switch (period) {
      case 'today':
        return { from: startOfToday, to: now };
      case 'yesterday': {
        const from = new Date(startOfToday); from.setDate(from.getDate() - 1);
        const to = new Date(startOfToday); to.setMilliseconds(-1);
        return { from, to };
      }
      case '7d': {
        const from = new Date(startOfToday); from.setDate(from.getDate() - 6);
        return { from, to: endOfToday };
      }
      case 'custom': {
        const from = customFrom ? new Date(`${customFrom}T00:00:00`) : startOfToday;
        const to = customTo ? new Date(`${customTo}T23:59:59`) : endOfToday;
        return { from, to };
      }
      case '30d':
      default: {
        const from = new Date(startOfToday); from.setDate(from.getDate() - 29);
        return { from, to: endOfToday };
      }
    }
  }, [period, customFrom, customTo]);

  const params = useMemo(
    () => ({ from: range.from.toISOString(), to: range.to.toISOString() }),
    [range]
  );

  // Завантаження зведеного огляду (KPI, динаміка, оплати)
  const loadOverview = useCallback(async () => {
    try {
      setLoading(true);
      setError('');
      const res = await analyticsAPI.getOverview(params);
      setOverview(res.data);
    } catch (err) {
      console.error('Не вдалося завантажити аналітику', err);
      setError(errMessage(err, 'Не вдалося завантажити аналітику'));
      setOverview(null);
    } finally {
      setLoading(false);
    }
  }, [params]);

  useEffect(() => { loadOverview(); }, [loadOverview]);

  // Завантаження даних активної вкладки
  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setTabLoading(true);
        let payload;
        if (activeTab === 'sales') payload = (await analyticsAPI.getSales({ ...params, groupBy: 'day' })).data;
        else if (activeTab === 'products') payload = (await analyticsAPI.getProducts({ ...params, top: 20 })).data;
        else if (activeTab === 'categories') payload = (await analyticsAPI.getCategories({ ...params, top: 20 })).data;
        else if (activeTab === 'promotions') payload = (await analyticsAPI.getPromotions(params)).data;
        else payload = (await analyticsAPI.getFinancial(params)).data;
        if (alive) setTabData((prev) => ({ ...prev, [activeTab]: payload }));
      } catch (err) {
        console.error('Не вдалося завантажити дані вкладки', err);
      } finally {
        if (alive) setTabLoading(false);
      }
    })();
    return () => { alive = false; };
  }, [params, activeTab]);

  const paymentData = overview
    ? [
        { label: 'Картка', value: overview.payments?.cardCount || 0, color: PAYMENT_COLORS[0] },
        { label: 'Післяплата', value: overview.payments?.codCount || 0, color: PAYMENT_COLORS[1] },
      ]
    : [];

  return (
    <div className="admin-analytics" style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 16, marginBottom: 30 }}>
        <div>
          <Link to="/admin" className="back-link">← Назад до дашборду</Link>
          <h1 style={{ marginTop: 8 }}>Аналітика та звіти</h1>
        </div>
        <div style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap', marginTop: '1.5rem' }}>
          <select style={selectStyle} value={period} onChange={(e) => setPeriod(e.target.value)}>
            {PERIODS.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
          </select>
          {period === 'custom' && (
            <>
              <input type="date" style={selectStyle} value={customFrom} onChange={(e) => setCustomFrom(e.target.value)} />
              <span>—</span>
              <input type="date" style={selectStyle} value={customTo} onChange={(e) => setCustomTo(e.target.value)} />
            </>
          )}
        </div>
      </div>

      {error && <div style={{ ...cardStyle, color: '#dc3545', marginBottom: 20 }}>{error}</div>}
      {loading && !overview && <div style={{ textAlign: 'center', padding: 40, color: '#666' }}>Завантаження аналітики...</div>}

      {overview && (
        <>
          {/* KPI картки */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 20 }}>
            <div style={kpiStyle}>
              <span style={{ color: '#666', fontSize: '0.9rem' }}>Загальна виручка</span>
              <h2 style={{ margin: '10px 0', color: '#28a745' }}>{fmtMoney(overview.totalRevenue)} грн</h2>
            </div>
            <div style={kpiStyle}>
              <span style={{ color: '#666', fontSize: '0.9rem' }}>Замовлень</span>
              <h2 style={{ margin: '10px 0', color: '#007bff' }}>{fmtInt(overview.totalOrders)}</h2>
            </div>
            <div style={kpiStyle}>
              <span style={{ color: '#666', fontSize: '0.9rem' }}>Середній чек (AOV)</span>
              <h2 style={{ margin: '10px 0' }}>{fmtMoney(overview.averageOrderValue)} грн</h2>
            </div>
            <div style={kpiStyle}>
              <span style={{ color: '#666', fontSize: '0.9rem' }}>Маржа</span>
              <h2 style={{ margin: '10px 0', color: '#6f42c1' }}>{fmtPercent(overview.margin)}</h2>
            </div>
          </div>

          {/* Динаміка продажів + способи оплати */}
          <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 24, marginTop: 24 }}>
            <div style={cardStyle}>
              <h3 style={{ marginTop: 0, marginBottom: 16 }}>📈 Динаміка продажів</h3>
              <LineChart data={(overview.salesByDay || []).map((d) => ({ label: d.date, value: d.amount }))} />
            </div>
            <div style={cardStyle}>
              <h3 style={{ marginTop: 0, marginBottom: 16 }}>💳 Способи оплати</h3>
              <PieChart data={paymentData} />
            </div>
          </div>

          {/* Вкладки */}
          <div style={{ ...tabsStyle, marginTop: 32 }}>
            {TABS.map((tab) => (
              <button
                key={tab.key}
                onClick={() => setActiveTab(tab.key)}
                style={{
                  ...btnStyle,
                  padding: '10px 20px',
                  background: activeTab === tab.key ? '#165DFF' : '#e9ecef',
                  color: activeTab === tab.key ? '#fff' : '#333',
                }}
              >
                {tab.label}
              </button>
            ))}
          </div>

          <div style={cardStyle}>
            {tabLoading && <div style={{ color: '#666', marginBottom: 12 }}>Завантаження...</div>}
            {activeTab === 'sales' && <SalesTab data={tabData.sales} range={range} />}
            {activeTab === 'products' && <ProductsTab data={tabData.products} range={range} />}
            {activeTab === 'categories' && <CategoriesTab data={tabData.categories} />}
            {activeTab === 'promotions' && <PromotionsTab data={tabData.promotions} range={range} />}
            {activeTab === 'financial' && <FinancialTab data={tabData.financial} range={range} />}
          </div>
        </>
      )}
    </div>
  );
}

// ============================ Вміст вкладок ============================
function SalesTab({ data, range }) {
  const series = data?.series || [];
  const metrics = data?.metrics;
  const rows = series.map((s) => ({ date: s.date, count: s.count, amount: s.amount }));
  const columns = [
    { key: 'date', label: 'Дата' },
    { key: 'count', label: 'Замовлень', align: 'right' },
    { key: 'amount', label: 'Виручка, грн', align: 'right', render: (r) => fmtMoney(r.amount) },
  ];
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12, marginBottom: 20 }}>
        <h3 style={{ margin: 0 }}>Продажі за період</h3>
        <ExportButtons type="sales" range={range} />
      </div>
      {metrics && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 16, marginBottom: 24 }}>
          <div style={{ background: '#f8f9fb', padding: 16, borderRadius: 10 }}>
            <div style={{ color: '#666', fontSize: '0.85rem' }}>Замовлень</div>
            <b style={{ fontSize: '1.3rem' }}>{fmtInt(metrics.totalOrders)}</b>
          </div>
          <div style={{ background: '#f8f9fb', padding: 16, borderRadius: 10 }}>
            <div style={{ color: '#666', fontSize: '0.85rem' }}>Виручка</div>
            <b style={{ fontSize: '1.3rem' }}>{fmtMoney(metrics.totalRevenue)} грн</b>
          </div>
          <div style={{ background: '#f8f9fb', padding: 16, borderRadius: 10 }}>
            <div style={{ color: '#666', fontSize: '0.85rem' }}>Середній чек</div>
            <b style={{ fontSize: '1.3rem' }}>{fmtMoney(metrics.averageOrderValue)} грн</b>
          </div>
        </div>
      )}
      <LineChart data={series.map((s) => ({ label: s.date, value: s.amount }))} />
      <div style={{ marginTop: 24 }}>
        <MetricsTable columns={columns} rows={rows} />
      </div>
    </div>
  );
}

function ProductsTab({ data, range }) {
  const list = data || [];
  const rows = list.map((p) => ({ id: p.productId, name: p.name, quantity: p.quantity, revenue: p.revenue }));
  const columns = [
    { key: 'id', label: 'ID' },
    { key: 'name', label: 'Товар' },
    { key: 'quantity', label: 'Продано, шт', align: 'right' },
    { key: 'revenue', label: 'Виручка, грн', align: 'right', render: (r) => fmtMoney(r.revenue) },
  ];
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12, marginBottom: 20 }}>
        <h3 style={{ margin: 0 }}>Топ товарів за виручкою</h3>
        <ExportButtons type="products" range={range} />
      </div>
      <BarChart data={list.map((p) => ({ label: p.name, value: p.revenue }))} />
      <div style={{ marginTop: 24 }}>
        <MetricsTable columns={columns} rows={rows} />
      </div>
    </div>
  );
}

function CategoriesTab({ data }) {
  const list = data || [];
  const rows = list.map((c) => ({ name: c.name, revenue: c.revenue }));
  const columns = [
    { key: 'name', label: 'Категорія' },
    { key: 'revenue', label: 'Виручка, грн', align: 'right', render: (r) => fmtMoney(r.revenue) },
  ];
  return (
    <div>
      <h3 style={{ marginTop: 0, marginBottom: 20 }}>Топ категорій за виручкою</h3>
      <BarChart data={list.map((c) => ({ label: c.name, value: c.revenue }))} color="#FF7D00" />
      <div style={{ marginTop: 24 }}>
        <MetricsTable columns={columns} rows={rows} />
      </div>
    </div>
  );
}

function PromotionsTab({ data, range }) {
  const list = data || [];
  const rows = list.map((p) => ({
    name: p.name,
    uses: p.uses,
    discount: p.totalDiscountAmount,
    revenue: p.revenue,
    roi: p.roi,
  }));
  const columns = [
    { key: 'name', label: 'Акція' },
    { key: 'uses', label: 'Використань', align: 'right' },
    { key: 'discount', label: 'Знижка, грн', align: 'right', render: (r) => fmtMoney(r.discount) },
    { key: 'revenue', label: 'Виручка, грн', align: 'right', render: (r) => fmtMoney(r.revenue) },
    { key: 'roi', label: 'ROI', align: 'right', render: (r) => fmtPercent(r.roi) },
  ];
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12, marginBottom: 20 }}>
        <h3 style={{ margin: 0 }}>Ефективність акцій</h3>
        <ExportButtons type="promotions" range={range} />
      </div>
      <MetricsTable columns={columns} rows={rows} />
    </div>
  );
}

function FinancialTab({ data, range }) {
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12, marginBottom: 20 }}>
        <h3 style={{ margin: 0 }}>Фінансові показники</h3>
        <ExportButtons type="financial" range={range} />
      </div>
      {!data ? (
        <div style={{ ...emptyChartStyle, height: 120 }}>Даних за цей період немає</div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 16 }}>
          <div style={{ background: '#f8f9fb', padding: 16, borderRadius: 10 }}>
            <div style={{ color: '#666', fontSize: '0.85rem' }}>Виручка</div>
            <b style={{ fontSize: '1.3rem' }}>{fmtMoney(data.totalRevenue)} грн</b>
          </div>
          <div style={{ background: '#f8f9fb', padding: 16, borderRadius: 10 }}>
            <div style={{ color: '#666', fontSize: '0.85rem' }}>Собівартість</div>
            <b style={{ fontSize: '1.3rem', color: '#dc3545' }}>{fmtMoney(data.totalPurchaseCost)} грн</b>
          </div>
          <div style={{ background: '#f8f9fb', padding: 16, borderRadius: 10 }}>
            <div style={{ color: '#666', fontSize: '0.85rem' }}>Валовий прибуток</div>
            <b style={{ fontSize: '1.3rem', color: '#28a745' }}>{fmtMoney(data.grossProfit)} грн</b>
          </div>
          <div style={{ background: '#f8f9fb', padding: 16, borderRadius: 10 }}>
            <div style={{ color: '#666', fontSize: '0.85rem' }}>Маржа</div>
            <b style={{ fontSize: '1.3rem', color: '#6f42c1' }}>{fmtPercent(data.margin)}</b>
          </div>
        </div>
      )}
    </div>
  );
}

export default AdminAnalyticsPage;
