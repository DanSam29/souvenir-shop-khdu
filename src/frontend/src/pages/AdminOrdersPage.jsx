import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ordersAPI } from '../services/api';

function AdminOrdersPage() {
  const navigate = useNavigate();
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState(false);

  useEffect(() => {
    loadOrders();
  }, []);

  const loadOrders = async () => {
    try {
      setLoading(true);
      const res = await ordersAPI.getAll();
      setOrders(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = async (id, newStatus) => {
    const trackingNumber = newStatus === 'Shipped' ? prompt('Введіть номер ТТН Нової Пошти:') : null;
    if (newStatus === 'Shipped' && trackingNumber === null) return;

    try {
      setUpdating(true);
      await ordersAPI.updateStatus(id, { status: newStatus, trackingNumber });
      alert('Статус оновлено');
      loadOrders();
    } catch (err) {
      alert('Помилка при оновленні статусу');
    } finally {
      setUpdating(false);
    }
  };

  if (loading) return <div className="loading">Завантаження...</div>;

  return (
    <div className="admin-orders" style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 20 }}>
        <Link to="/admin" className="back-link">
          ← Назад до дашборду
        </Link>
        <h1>Керування замовленнями</h1>
      </div>
      <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: 20, background: '#fff', borderRadius: 8, overflow: 'hidden' }}>
        <thead style={{ background: '#f8f9fa' }}>
          <tr>
            <th style={thStyle}>Номер</th>
            <th style={thStyle}>Клієнт</th>
            <th style={thStyle}>Сума</th>
            <th style={thStyle}>Дата</th>
            <th style={thStyle}>Статус</th>
            <th style={thStyle}>Дії</th>
          </tr>
        </thead>
        <tbody>
          {orders.map(o => (
            <tr key={o.orderId} style={{ borderBottom: '1px solid #eee' }}>
              <td style={tdStyle}>{o.orderNumber}</td>
              <td style={tdStyle}>{o.userName}<br/><small style={{ color: '#888' }}>{o.userEmail}</small></td>
              <td style={tdStyle}>{o.totalAmount.toFixed(2)} грн</td>
              <td style={tdStyle}>{new Date(o.createdAt).toLocaleDateString()}</td>
              <td style={tdStyle}>
                <span style={statusBadgeStyle(o.status)}>{o.status}</span>
              </td>
              <td style={tdStyle}>
                <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                  <button 
                    onClick={() => navigate(`/order/${o.orderId}`)}
                    style={{ padding: '5px 10px', background: '#007bff', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}
                  >
                    Деталі
                  </button>
                  <select 
                    value={o.status} 
                    onChange={(e) => handleStatusChange(o.orderId, e.target.value)}
                    disabled={updating || o.status === 'Cancelled' || o.status === 'Delivered'}
                    style={{ padding: '5px', borderRadius: 4 }}
                  >
                    <option value="Processing">Processing</option>
                    <option value="Paid">Paid</option>
                    <option value="Shipped">Shipped</option>
                    <option value="Delivered">Delivered</option>
                    <option value="Cancelled">Cancelled</option>
                  </select>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

const thStyle = { padding: '12px', textAlign: 'left', borderBottom: '2px solid #dee2e6' };
const tdStyle = { padding: '12px' };

const statusBadgeStyle = (status) => ({
  padding: '4px 8px',
  borderRadius: '12px',
  fontSize: '0.85rem',
  background: status === 'Paid' ? '#e6ffed' : (status === 'Cancelled' ? '#fff1f0' : '#e6f7ff'),
  color: status === 'Paid' ? '#28a745' : (status === 'Cancelled' ? '#cf1322' : '#1890ff'),
  border: `1px solid ${status === 'Paid' ? '#b7eb8f' : (status === 'Cancelled' ? '#ffa39e' : '#91d5ff')}`
});

export default AdminOrdersPage;