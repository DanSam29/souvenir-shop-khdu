import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { analyticsAPI } from '../services/api';

function AdminAnalyticsPage() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [period, setPeriod] = useState('month');

  const getStartDate = useCallback(() => {
    let from = new Date();
    if (period === 'week') from.setDate(from.getDate() - 7);
    else if (period === 'month') from.setMonth(from.getMonth() - 1);
    else if (period === 'year') from.setFullYear(from.getFullYear() - 1);
    else if (period === 'today') from.setHours(0, 0, 0, 0);
    return from;
  }, [period]);

  const loadAnalytics = useCallback(async () => {
    try {
      setLoading(true);
      const from = getStartDate();
      const res = await analyticsAPI.getSummary({ from: from.toISOString() });
      setData(res.data);
    } catch (err) {
      console.error('Error loading analytics', err);
    } finally {
      setLoading(false);
    }
  }, [getStartDate]);

  useEffect(() => {
    loadAnalytics();
  }, [loadAnalytics]);

  const handleExport = async () => {
    try {
      const from = getStartDate();
      const to = new Date();
      
      const res = await analyticsAPI.exportSales({ 
        from: from.toISOString(),
        to: to.toISOString()
      });
      
      const startDateStr = from.toISOString().split('T')[0];
      const endDateStr = to.toISOString().split('T')[0];
      
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `sales_report_${startDateStr}_${endDateStr}.csv`);
      document.body.appendChild(link);
      link.click();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      alert('Помилка при завантаженні звіту');
    }
  };

  if (loading) return <div className="loading">Завантаження аналітики...</div>;

  return (
    <div className="admin-analytics" style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 30 }}>
        <div>
          <Link to="/admin" className="back-link">
            ← Назад до дашборду
          </Link>
          <h1>Аналітика та звіти</h1>
        </div>
        <div style={{ display: 'flex', gap: 15, marginTop: '2.5rem' }}>
          <select 
            value={period} 
            onChange={(e) => setPeriod(e.target.value)}
            style={{ padding: '8px 12px', borderRadius: 8, border: '1px solid #ddd' }}
          >
            <option value="today">Сьогодні</option>
            <option value="week">Останній тиждень</option>
            <option value="month">Останній місяць</option>
            <option value="year">Останній рік</option>
          </select>
          <button 
            onClick={handleExport}
            style={{ padding: '8px 16px', background: '#28a745', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer' }}
          >
            📥 Експорт CSV
          </button>
        </div>
      </div>

      {/* Stats Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 20 }}>
        <div style={cardStyle}>
          <span style={{ color: '#666', fontSize: '0.9rem' }}>Загальний дохід</span>
          <h2 style={{ margin: '10px 0', color: '#28a745' }}>{data.totalIncome.toFixed(2)} грн</h2>
        </div>
        <div style={cardStyle}>
          <span style={{ color: '#666', fontSize: '0.9rem' }}>Витрати (закупівля)</span>
          <h2 style={{ margin: '10px 0', color: '#dc3545' }}>{data.totalExpenses.toFixed(2)} грн</h2>
        </div>
        <div style={cardStyle}>
          <span style={{ color: '#666', fontSize: '0.9rem' }}>Чистий прибуток</span>
          <h2 style={{ margin: '10px 0', color: '#007bff' }}>{data.profit.toFixed(2)} грн</h2>
        </div>
        <div style={cardStyle}>
          <span style={{ color: '#666', fontSize: '0.9rem' }}>Середній чек</span>
          <h2 style={{ margin: '10px 0' }}>{data.avgCheck.toFixed(2)} грн</h2>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 30, marginTop: 40 }}>
        {/* Popular Products */}
        <div style={cardStyle}>
          <h3 style={{ marginBottom: 20 }}>🔥 Популярні товари</h3>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ textAlign: 'left', borderBottom: '1px solid #eee' }}>
                <th style={{ padding: '10px 0' }}>Назва</th>
                <th>К-сть</th>
                <th style={{ textAlign: 'right' }}>Дохід</th>
              </tr>
            </thead>
            <tbody>
              {data.popularProducts.map(p => (
                <tr key={p.productId} style={{ borderBottom: '1px solid #f9f9f9' }}>
                  <td style={{ padding: '12px 0' }}>{p.name}</td>
                  <td>{p.quantity}</td>
                  <td style={{ textAlign: 'right' }}>{p.revenue.toFixed(2)} грн</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Sales Chart (Simple CSS implementation) */}
        <div style={cardStyle}>
          <h3 style={{ marginBottom: 20 }}>📈 Динаміка продажів</h3>
          <div style={{ display: 'flex', alignItems: 'flex-end', height: 200, gap: 10, paddingBottom: 20, borderBottom: '2px solid #eee' }}>
            {data.salesByDay.length === 0 ? <p style={{ color: '#888', width: '100%', textAlign: 'center' }}>Даних за цей період немає</p> : 
              data.salesByDay.map((day, i) => {
                const maxAmount = Math.max(...data.salesByDay.map(d => d.amount), 1);
                const height = (day.amount / maxAmount) * 100;
                return (
                  <div key={i} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                    <div 
                      title={`${day.date}: ${day.amount} грн`}
                      style={{ width: '100%', height: `${height}%`, background: '#007bff', borderRadius: '4px 4px 0 0', minHeight: 2 }}
                    ></div>
                    <span style={{ fontSize: '0.7rem', color: '#888', marginTop: 5, transform: 'rotate(-45deg)', whiteSpace: 'nowrap' }}>
                      {day.date.split('-').slice(1).join('.')}
                    </span>
                  </div>
                );
              })
            }
          </div>
        </div>
      </div>
    </div>
  );
}

const cardStyle = {
  background: '#fff',
  padding: 24,
  borderRadius: 12,
  boxShadow: '0 4px 12px rgba(0,0,0,0.05)'
};

export default AdminAnalyticsPage;