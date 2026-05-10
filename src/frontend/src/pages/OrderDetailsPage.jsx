import React, { useState, useEffect } from 'react';
import { useParams, Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ordersAPI } from '../services/api';

function OrderDetailsPage() {
  const { id } = useParams();
  const location = useLocation();
  const { t, i18n } = useTranslation();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const isEn = i18n.language === 'en';
  const fromAdmin = location.state?.from === 'admin';
  const backPath = fromAdmin ? '/admin/orders' : '/profile';
  const backText = fromAdmin ? t('order.back_to_admin_orders') : t('order.back_to_profile');

  useEffect(() => {
    const loadOrder = async () => {
      try {
        setLoading(true);
        const res = await ordersAPI.getOrder(id);
        setOrder(res.data);
      } catch (err) {
        setError(t('common.load_error') || 'Не вдалося завантажити деталі замовлення');
      } finally {
        setLoading(false);
      }
    };
    loadOrder();
  }, [id, t]);

  if (loading) return <div className="loading">{t('common.loading')}</div>;
  if (error) return <div className="error">{error}</div>;
  if (!order) return <div>{t('order.not_found')}</div>;

  return (
    <div className="order-details" style={{ maxWidth: 800, margin: '40px auto', padding: '0 20px' }}>
      <Link to={backPath} style={{ textDecoration: 'none', color: '#007bff', fontWeight: 500 }}>← {backText}</Link>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 20 }}>
        <h1>{t('order.title')} #{order.orderNumber}</h1>
        <span style={{ 
          padding: '6px 15px', 
          borderRadius: 20, 
          background: order.status === 'Paid' ? '#e6ffed' : '#e6f7ff',
          color: order.status === 'Paid' ? '#28a745' : '#1890ff',
          fontWeight: 600
        }}>
          {order.status}
        </span>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20, marginTop: 30 }}>
        <div style={{ background: '#fff', padding: 20, borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)' }}>
          <h3>{t('checkout.delivery')}</h3>
          <p><strong>{t('checkout.recipient')}:</strong> {order.shipping.recipientName}</p>
          <p><strong>{t('checkout.phone')}:</strong> {order.shipping.recipientPhone}</p>
          <p><strong>{t('checkout.address')}:</strong> {t('checkout.city_prefix')} {order.shipping.city}, {order.shipping.warehouseNumber}</p>
          {order.shipping.trackingNumber && (
            <div style={{ marginTop: 15, padding: 10, background: '#f0f7ff', borderRadius: 8, border: '1px solid #bae7ff' }}>
              <strong>{t('order.tracking_number')}:</strong> <span style={{ color: '#0056b3', fontWeight: 700 }}>{order.shipping.trackingNumber}</span>
              <p style={{ margin: '5px 0 0 0', fontSize: '0.85rem' }}>{t('order.track_on_np')}</p>
            </div>
          )}
        </div>

        <div style={{ background: '#fff', padding: 20, borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)' }}>
          <h3>{t('checkout.payment')}</h3>
          <p><strong>{t('checkout.payment_method')}:</strong> {order.payment.method === 'Card' ? t('checkout.payment_card') : t('checkout.payment_cod')}</p>
          <p><strong>{t('cart.total')}:</strong> {order.payment.amount.toFixed(2)} {t('common.currency')}</p>
          <p><strong>{t('order.payment_status')}:</strong> {order.payment.status}</p>
        </div>
      </div>

      <div style={{ background: '#fff', padding: 20, borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)', marginTop: 20 }}>
        <h3>{t('cart.items')}</h3>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid #eee', textAlign: 'left' }}>
              <th style={{ padding: '10px 0' }}>{t('order.item_name')}</th>
              <th>{t('product.price')}</th>
              <th>{t('order.quantity')}</th>
              <th style={{ textAlign: 'right' }}>{t('cart.total')}</th>
            </tr>
          </thead>
          <tbody>
            {order.items.map(item => {
              const displayName = (isEn && item.nameEn) ? item.nameEn : item.name;
              return (
                <tr key={item.productId} style={{ borderBottom: '1px solid #f9f9f9' }}>
                  <td style={{ padding: '12px 0' }}>{displayName}</td>
                  <td>{item.price.toFixed(2)} {t('common.currency')}</td>
                  <td>{item.quantity}</td>
                  <td style={{ textAlign: 'right' }}>{(item.price * item.quantity).toFixed(2)} {t('common.currency')}</td>
                </tr>
              );
            })}
          </tbody>
          <tfoot>
            <tr>
              <td colSpan="3" style={{ padding: '20px 0 5px 0', textAlign: 'right', color: '#666' }}>{t('cart.items')}:</td>
              <td style={{ padding: '20px 0 5px 0', textAlign: 'right' }}>{(order.totalAmount - (order.shippingCost || 0)).toFixed(2)} {t('common.currency')}</td>
            </tr>
            {order.shippingCost > 0 && (
              <tr>
                <td colSpan="3" style={{ padding: '5px 0', textAlign: 'right', color: '#666' }}>{t('checkout.delivery')}:</td>
                <td style={{ padding: '5px 0', textAlign: 'right' }}>{order.shippingCost.toFixed(2)} {t('common.currency')}</td>
              </tr>
            )}
            <tr>
              <td colSpan="3" style={{ padding: '10px 0', textAlign: 'right', fontWeight: 700, fontSize: '1.2rem' }}>{t('cart.total')}:</td>
              <td style={{ padding: '10px 0', textAlign: 'right', fontWeight: 700, fontSize: '1.2rem' }}>{order.totalAmount.toFixed(2)} {t('common.currency')}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  );
}

export default OrderDetailsPage;