import React, { useState, useEffect } from 'react';
import { useParams, Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ordersAPI, novaPoshtaAPI } from '../services/api';

// Лінійний життєвий цикл замовлення (без врахування скасування)
const ORDER_STEPS = ['Processing', 'Paid', 'Shipped', 'Delivered'];
// Публічна сторінка відстеження Нової Пошти за номером ТТН
const npTrackingUrl = (ttn) => `https://novaposhta.ua/tracking/?cargo_number=${encodeURIComponent(ttn)}`;
const STATUS_COLORS = {
  Processing: { bg: '#e6f7ff', fg: '#1890ff', border: '#91d5ff' },
  Pending: { bg: '#fff7e6', fg: '#d46b08', border: '#ffd591' },
  Paid: { bg: '#e6ffed', fg: '#28a745', border: '#b7eb8f' },
  Shipped: { bg: '#e6f7ff', fg: '#1890ff', border: '#91d5ff' },
  Delivered: { bg: '#e6ffed', fg: '#28a745', border: '#b7eb8f' },
  Cancelled: { bg: '#fff1f0', fg: '#cf1322', border: '#ffa39e' }
};

// Прогрес-бар статусів замовлення
function StatusProgress({ status, t }) {
  if (status === 'Cancelled') {
    return (
      <div style={{ padding: 12, borderRadius: 8, background: '#fff1f0', border: '1px solid #ffa39e', color: '#cf1322', textAlign: 'center', fontWeight: 600 }}>
        {t('order.status_Cancelled')}
      </div>
    );
  }
  const currentIndex = ORDER_STEPS.indexOf(status);
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 4, width: '100%' }}>
      {ORDER_STEPS.map((step, i) => {
        const done = currentIndex >= 0 && i <= currentIndex;
        return (
          <React.Fragment key={step}>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minWidth: 70 }}>
              <div style={{
                width: 24, height: 24, borderRadius: '50%', fontSize: '0.8rem',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                background: done ? '#28a745' : '#e0e0e0', color: '#fff'
              }}>
                {done ? '✓' : ''}
              </div>
              <small style={{ fontSize: '0.75rem', textAlign: 'center', color: done ? '#28a745' : '#999' }}>
                {t(`order.status_${step}`)}
              </small>
            </div>
            {i < ORDER_STEPS.length - 1 && (
              <div style={{ flex: 1, height: 2, minWidth: 10, background: i < currentIndex ? '#28a745' : '#e0e0e0' }} />
            )}
          </React.Fragment>
        );
      })}
    </div>
  );
}

function OrderDetailsPage() {
  const { id } = useParams();
  const location = useLocation();
  const { t, i18n } = useTranslation();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [tracking, setTracking] = useState(null);
  const isEn = i18n.language === 'en';
  const fromAdmin = location.state?.from === 'admin';
  const backPath = fromAdmin ? '/admin/orders' : '/profile';
  const backText = fromAdmin ? t('order.back_to_admin_orders') : t('order.back_to_profile');

  useEffect(() => {
    const loadOrder = async () => {
      try {
        setLoading(true);
        // Адміністратор бачить будь-яке замовлення; користувач — лише власне (з історією статусів)
        const res = fromAdmin ? await ordersAPI.getOrder(id) : await ordersAPI.getMyOrder(id);
        setOrder(res.data);
      } catch (err) {
        setError(t('common.load_error') || 'Не вдалося завантажити деталі замовлення');
      } finally {
        setLoading(false);
      }
    };
    loadOrder();
  }, [id, t, fromAdmin]);

  // Статус відстеження з API Нової Пошти (опціонально; сторінка працює й без нього)
  useEffect(() => {
    const ttn = order?.shipping?.trackingNumber;
    if (!ttn) {
      setTracking(null);
      return;
    }
    let active = true;
    const loadTracking = async () => {
      try {
        const res = await novaPoshtaAPI.getTracking(ttn);
        if (active) setTracking(res.data);
      } catch {
        if (active) setTracking(null);
      }
    };
    loadTracking();
    return () => { active = false; };
  }, [order]);

  if (loading) return <div className="loading">{t('common.loading')}</div>;
  if (error) return <div className="error">{error}</div>;
  if (!order) return <div>{t('order.not_found')}</div>;

  const items = (order.items || []).map((it) => {
    const originalPrice = it.originalPrice ?? it.price ?? 0;
    const finalPrice = it.finalPrice ?? it.price ?? 0;
    const unitDiscount = Math.max(0, originalPrice - finalPrice);
    return {
      productId: it.productId,
      quantity: it.quantity,
      originalPrice,
      finalPrice,
      unitDiscount,
      lineTotal: finalPrice * it.quantity,
      lineDiscount: unitDiscount * it.quantity,
      displayName: isEn
        ? (it.productNameEn || it.nameEn || it.productName || it.name || '')
        : (it.productName || it.name || '')
    };
  });
  const subtotal = items.reduce((sum, it) => sum + it.lineTotal, 0);
  const shippingCost = order.shippingCost || 0;
  const totalDiscount = items.reduce((sum, it) => sum + it.lineDiscount, 0);
  const statusColor = STATUS_COLORS[order.status] || STATUS_COLORS.Processing;
  const history = order.history || [];

  return (
    <div className="order-details" style={{ maxWidth: 800, margin: '40px auto', padding: '0 20px' }}>
      <Link to={backPath} style={{ textDecoration: 'none', color: '#007bff', fontWeight: 500 }}>← {backText}</Link>

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 20 }}>
        <h1>{t('order.title')} #{order.orderNumber}</h1>
        <span style={{
          padding: '6px 15px',
          borderRadius: 20,
          background: statusColor.bg,
          color: statusColor.fg,
          border: `1px solid ${statusColor.border}`,
          fontWeight: 600
        }}>
          {t(`order.status_${order.status}`, { defaultValue: order.status })}
        </span>
      </div>

      {/* Прогрес-бар статусів */}
      <div style={{ background: '#fff', padding: '20px 24px', borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)', marginTop: 24 }}>
        <h3 style={{ marginTop: 0 }}>{t('order.progress')}</h3>
        <StatusProgress status={order.status} t={t} />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20, marginTop: 20 }}>
        <div style={{ background: '#fff', padding: 20, borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)' }}>
          <h3>{t('checkout.delivery')}</h3>
          <p><strong>{t('checkout.recipient')}:</strong> {order.shipping?.recipientName}</p>
          <p><strong>{t('checkout.phone')}:</strong> {order.shipping?.recipientPhone}</p>
          <p><strong>{t('checkout.address')}:</strong> {t('checkout.city_prefix')} {order.shipping?.city}, {order.shipping?.warehouseNumber}</p>
          {order.shipping?.trackingNumber && (
            <div style={{ marginTop: 15, padding: 10, background: '#f0f7ff', borderRadius: 8, border: '1px solid #bae7ff' }}>
              <strong>{t('order.tracking_number')}:</strong> <span style={{ color: '#0056b3', fontWeight: 700 }}>{order.shipping.trackingNumber}</span>
              {tracking?.available && tracking?.status && (
                <p style={{ margin: '6px 0 0 0', fontWeight: 600, color: '#0056b3' }}>
                  {t('order.tracking_status')}: {tracking.status}
                </p>
              )}
              <p style={{ margin: '5px 0 0 0', fontSize: '0.85rem' }}>
                <a href={npTrackingUrl(order.shipping.trackingNumber)} target="_blank" rel="noopener noreferrer" style={{ color: '#1890ff' }}>
                  {t('order.track_on_np')} ↗
                </a>
              </p>
            </div>
          )}
        </div>
        <div style={{ background: '#fff', padding: 20, borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)' }}>
          <h3>{t('checkout.payment')}</h3>
          <p><strong>{t('checkout.payment_method')}:</strong> {order.payment?.method === 'Card' ? t('checkout.payment_card') : t('checkout.payment_cod')}</p>
          <p><strong>{t('cart.total')}:</strong> {(order.payment?.amount ?? order.totalAmount).toFixed(2)} {t('common.currency')}</p>
          <p><strong>{t('order.payment_status')}:</strong> {order.payment?.status}</p>
        </div>
      </div>

      {/* Історія (трекінг) зміни статусів */}
      {history.length > 0 && (
        <div style={{ background: '#fff', padding: 20, borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)', marginTop: 20 }}>
          <h3>{t('order.history')}</h3>
          <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
            {history.map((h) => (
              <li key={h.historyId} style={{ display: 'flex', gap: 12, padding: '10px 0', borderBottom: '1px solid #f5f5f5' }}>
                <span style={{ width: 10, height: 10, borderRadius: '50%', background: (STATUS_COLORS[h.newStatus] || STATUS_COLORS.Processing).fg, marginTop: 6, flexShrink: 0 }} />
                <div>
                  <div style={{ fontWeight: 600 }}>
                    {t(`order.status_${h.newStatus}`, { defaultValue: h.newStatus })}
                    {h.oldStatus && <span style={{ color: '#999', fontWeight: 400 }}> ({t(`order.status_${h.oldStatus}`, { defaultValue: h.oldStatus })} → )</span>}
                  </div>
                  <div style={{ color: '#888', fontSize: '0.85rem' }}>{new Date(h.timestamp).toLocaleString()}</div>
                  {h.comment && <div style={{ color: '#666', fontSize: '0.85rem' }}>{h.comment}</div>}
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}

      <div style={{ background: '#fff', padding: 20, borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)', marginTop: 20 }}>
        <h3>{t('cart.items')}</h3>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid #eee', textAlign: 'left' }}>
              <th style={{ padding: '10px 0' }}>{t('order.item_name')}</th>
              <th>{t('product.price')}</th>
              <th>{t('order.discount')}</th>
              <th>{t('order.quantity')}</th>
              <th style={{ textAlign: 'right' }}>{t('cart.total')}</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.productId} style={{ borderBottom: '1px solid #f9f9f9' }}>
                <td style={{ padding: '12px 0' }}>{item.displayName}</td>
                <td>
                  {item.unitDiscount > 0 ? (
                    <>
                      <span style={{ textDecoration: 'line-through', color: '#999', marginRight: 6 }}>{item.originalPrice.toFixed(2)}</span>
                      <span style={{ fontWeight: 600 }}>{item.finalPrice.toFixed(2)}</span>
                    </>
                  ) : (
                    <span>{item.finalPrice.toFixed(2)}</span>
                  )}{' '}
                  {t('common.currency')}
                </td>
                <td style={{ color: item.lineDiscount > 0 ? '#28a745' : '#999' }}>
                  {item.lineDiscount > 0 ? `-${item.lineDiscount.toFixed(2)} ${t('common.currency')}` : '—'}
                </td>
                <td>{item.quantity}</td>
                <td style={{ textAlign: 'right' }}>{item.lineTotal.toFixed(2)} {t('common.currency')}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <td colSpan="4" style={{ padding: '20px 0 5px 0', textAlign: 'right', color: '#666' }}>{t('order.subtotal')}:</td>
              <td style={{ padding: '20px 0 5px 0', textAlign: 'right' }}>{subtotal.toFixed(2)} {t('common.currency')}</td>
            </tr>
            {totalDiscount > 0 && (
              <tr>
                <td colSpan="4" style={{ padding: '5px 0', textAlign: 'right', color: '#28a745' }}>{t('order.discount')}:</td>
                <td style={{ padding: '5px 0', textAlign: 'right', color: '#28a745' }}>-{totalDiscount.toFixed(2)} {t('common.currency')}</td>
              </tr>
            )}
            {shippingCost > 0 && (
              <tr>
                <td colSpan="4" style={{ padding: '5px 0', textAlign: 'right', color: '#666' }}>{t('checkout.delivery')}:</td>
                <td style={{ padding: '5px 0', textAlign: 'right' }}>{shippingCost.toFixed(2)} {t('common.currency')}</td>
              </tr>
            )}
            <tr>
              <td colSpan="4" style={{ padding: '10px 0', textAlign: 'right', fontWeight: 700, fontSize: '1.2rem' }}>{t('cart.total')}:</td>
              <td style={{ padding: '10px 0', textAlign: 'right', fontWeight: 700, fontSize: '1.2rem' }}>{order.totalAmount.toFixed(2)} {t('common.currency')}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  );
}

export default OrderDetailsPage;
