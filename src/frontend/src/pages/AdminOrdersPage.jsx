import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ordersAPI } from '../services/api';
import { TableSkeleton } from '../components/ui';

// Публічна сторінка відстеження Нової Пошти за номером ТТН
const npTrackingUrl = (ttn) => `https://novaposhta.ua/tracking/?cargo_number=${encodeURIComponent(ttn)}`;

function AdminOrdersPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [orders, setOrders] = useState([]);
  const [pagination, setPagination] = useState({ pageNumber: 1, totalPages: 1 });
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState(false);
  const [trackingModalOrderId, setTrackingModalOrderId] = useState(null);
  const [trackingValue, setTrackingValue] = useState('');
  const [trackingError, setTrackingError] = useState('');
  const statusLabel = (s) => t('order.status_' + s, { defaultValue: s });
  useEffect(() => {
    loadOrders(1);
  }, []);
  const loadOrders = async (page = 1) => {
    try {
      setLoading(true);
      const res = await ordersAPI.getAll({ pageNumber: page, pageSize: 10 });
      // Повренення об'єкту з Items
      if (res.data && res.data.items) {
        setOrders(res.data.items);
        setPagination({
          pageNumber: res.data.pageNumber,
          totalPages: res.data.totalPages
        });
      } else {
        setOrders(res.data || []);
      }
    } catch (err) {
      console.error(err);
      setOrders([]);
    } finally {
      setLoading(false);
    }
  };
  const submitStatusChange = async (id, newStatus, trackingNumber) => {
    try {
      setUpdating(true);
      await ordersAPI.updateStatus(id, { status: newStatus, trackingNumber });
      alert(t('admin.orders.status_updated'));
      setTrackingModalOrderId(null);
      loadOrders();
    } catch (err) {
      alert(err?.message || t('admin.orders.status_update_error'));
    } finally {
      setUpdating(false);
    }
  };
  const handleStatusChange = (id, newStatus) => {
    // Для переходу в Shipped ТТН Nova Poshta обов'язковий — відкриваємо модальне поле вводу
    if (newStatus === 'Shipped') {
      setTrackingValue('');
      setTrackingError('');
      setTrackingModalOrderId(id);
      return;
    }
    submitStatusChange(id, newStatus, null);
  };
  const confirmShipped = () => {
    const value = trackingValue.trim();
    if (!value) {
      setTrackingError(t('admin.orders.ttn_required'));
      return;
    }
    submitStatusChange(trackingModalOrderId, 'Shipped', value);
  };
  if (loading) return (
    <div role="status" aria-live="polite" style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <span className="sr-only">{t('common.loading')}</span>
      <TableSkeleton rows={6} cols={5} />
    </div>
  );
  return (
    <div className="admin-orders" style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 20 }}>
        <Link to="/admin" className="back-link">
          {t('admin.common.back_to_dashboard')}
        </Link>
        <h1>{t('admin.orders.title')}</h1>
      </div>
      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: 20, background: '#fff', borderRadius: 8, overflow: 'hidden' }}>
          <thead style={{ background: '#f8f9fa' }}>
            <tr>
              <th style={thStyle}>{t('admin.orders.col_number')}</th>
              <th style={thStyle}>{t('admin.orders.col_customer')}</th>
              <th style={thStyle}>{t('admin.orders.col_amount')}</th>
              <th style={thStyle}>{t('admin.common.date')}</th>
              <th style={thStyle}>{t('admin.common.status')}</th>
              <th style={thStyle}>{t('admin.orders.col_ttn')}</th>
              <th style={thStyle}>{t('admin.common.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {orders.map(o => (
              <tr key={o.orderId} style={{ borderBottom: '1px solid #eee' }}>
                <td style={tdStyle}>{o.orderNumber}</td>
                <td style={tdStyle}>{o.userName}<br/><small style={{ color: '#595959' }}>{o.userEmail}</small></td>
                <td style={tdStyle}>{o.totalAmount.toFixed(2)} {t('common.currency')}</td>
                <td style={tdStyle}>{new Date(o.createdAt).toLocaleDateString()}</td>
                <td style={tdStyle}>
                  <span style={statusBadgeStyle(o.status)}>{statusLabel(o.status)}</span>
                </td>
                <td style={tdStyle}>
                  {o.trackingNumber ? (
                    <a
                      href={npTrackingUrl(o.trackingNumber)}
                      target="_blank"
                      rel="noopener noreferrer"
                      style={{ color: '#1890ff', fontWeight: 600 }}
                    >
                      {o.trackingNumber} ↗
                    </a>
                  ) : (
                    <span style={{ color: '#bbb' }}>—</span>
                  )}
                </td>
                <td style={tdStyle}>
                  <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                    <button 
                      onClick={() => navigate(`/order/${o.orderId}`, { state: { from: 'admin' } })}
                      style={{ padding: '5px 10px', background: '#007bff', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}
                    >
                      {t('admin.orders.details')}
                    </button>
                    <select 
                      value={o.status} 
                      onChange={(e) => handleStatusChange(o.orderId, e.target.value)}
                      disabled={updating || o.status === 'Cancelled' || o.status === 'Delivered'}
                      style={{ padding: '5px', borderRadius: 4 }}
                    >
                      <option value="Processing">{t('order.status_Processing')}</option>
                      <option value="Paid">{t('order.status_Paid')}</option>
                      <option value="Shipped">{t('order.status_Shipped')}</option>
                      <option value="Delivered">{t('order.status_Delivered')}</option>
                      <option value="Cancelled">{t('order.status_Cancelled')}</option>
                    </select>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {pagination.totalPages > 1 && (
        <div style={{ marginTop: 20, display: 'flex', justifyContent: 'center', gap: 10 }}>
          <button 
            onClick={() => loadOrders(pagination.pageNumber - 1)} 
            disabled={pagination.pageNumber === 1 || loading}
            style={{ padding: '8px 16px', borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: pagination.pageNumber === 1 ? 'not-allowed' : 'pointer' }}
          >
            ← {t('common.prev')}
          </button>
          <span style={{ alignSelf: 'center' }}>
            {t('admin.common.page_of', { current: pagination.pageNumber, total: pagination.totalPages })}
          </span>
          <button 
            onClick={() => loadOrders(pagination.pageNumber + 1)} 
            disabled={pagination.pageNumber === pagination.totalPages || loading}
            style={{ padding: '8px 16px', borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: pagination.pageNumber === pagination.totalPages ? 'not-allowed' : 'pointer' }}
          >
            {t('common.next')} →
          </button>
        </div>
      )}
      {trackingModalOrderId !== null && (
        <div style={modalOverlayStyle}>
          <div style={modalContentStyle}>
            <h3 style={{ marginTop: 0 }}>{t('admin.orders.shipping_title')}</h3>
            <p style={{ color: '#666', fontSize: '0.9rem' }}>
              {t('admin.orders.shipping_hint')}
            </p>
            <label htmlFor="order-ttn" style={{ display: 'block', marginBottom: 6, fontWeight: 500 }}>
              {t('admin.orders.ttn_label')}
            </label>
            <input
              id="order-ttn"
              type="text"
              value={trackingValue}
              onChange={(e) => {
                setTrackingValue(e.target.value);
                if (trackingError) setTrackingError('');
              }}
              placeholder={t('admin.orders.ttn_placeholder')}
              disabled={updating}
              autoFocus
              style={{
                width: '100%',
                padding: '8px 10px',
                borderRadius: 6,
                border: trackingError ? '1px solid #cf1322' : '1px solid #ddd',
                boxSizing: 'border-box'
              }}
            />
            {trackingError && (
              <div style={{ color: '#cf1322', fontSize: '0.85rem', marginTop: 6 }}>
                {trackingError}
              </div>
            )}
            <div style={{ marginTop: 16, display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
              <button
                onClick={() => setTrackingModalOrderId(null)}
                disabled={updating}
                style={{ padding: '8px 16px', borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: 'pointer' }}
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={confirmShipped}
                disabled={updating}
                style={{ padding: '8px 16px', borderRadius: 6, border: 'none', background: '#007bff', color: '#fff', cursor: updating ? 'not-allowed' : 'pointer' }}
              >
                {updating ? t('admin.common.saving') : t('admin.orders.confirm')}
              </button>
            </div>
          </div>
        </div>
      )}
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
const modalOverlayStyle = {
  position: 'fixed',
  inset: 0,
  background: 'rgba(0, 0, 0, 0.45)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  zIndex: 1000
};
const modalContentStyle = {
  background: '#fff',
  borderRadius: 8,
  padding: 24,
  width: '100%',
  maxWidth: 420,
  boxShadow: '0 8px 24px rgba(0, 0, 0, 0.2)'
};

export default AdminOrdersPage;