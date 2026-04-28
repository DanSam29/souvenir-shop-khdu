import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ordersAPI } from '../services/api';

function OrderDetailsPage() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const loadOrder = async () => {
      try {
        setLoading(true);
        const res = await ordersAPI.getOrder(id);
        setOrder(res.data);
      } catch (err) {
        setError('Не вдалося завантажити деталі замовлення');
      } finally {
        setLoading(false);
      }
    };
    loadOrder();
  }, [id]);

  if (loading) return <div className="loading">Завантаження...</div>;
  if (error) return <div className="error">{error}</div>;
  if (!order) return <div>Замовлення не знайдено</div>;

  return (
    <div className="order-details" style={{ maxWidth: 800, margin: '40px auto', padding: '0 20px' }}>
      <Link to="/profile" style={{ textDecoration: 'none', color: '#666' }}>← Назад до профілю</Link>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 20 }}>
        <h1>Замовлення #{order.orderNumber}</h1>
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
          <h3>Доставка</h3>
          <p><strong>Отримувач:</strong> {order.shipping.recipientName}</p>
          <p><strong>Телефон:</strong> {order.shipping.recipientPhone}</p>
          <p><strong>Адреса:</strong> м. {order.shipping.city}, {order.shipping.warehouseNumber}</p>
          {order.shipping.trackingNumber && (
            <div style={{ marginTop: 15, padding: 10, background: '#f0f7ff', borderRadius: 8, border: '1px solid #bae7ff' }}>
              <strong>ТТН:</strong> <span style={{ color: '#0056b3', fontWeight: 700 }}>{order.shipping.trackingNumber}</span>
              <p style={{ margin: '5px 0 0 0', fontSize: '0.85rem' }}>Відстежити на сайті Нової Пошти</p>
            </div>
          )}
        </div>

        <div style={{ background: '#fff', padding: 20, borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)' }}>
          <h3>Оплата</h3>
          <p><strong>Метод:</strong> {order.payment.method === 'Card' ? 'Картка (Stripe)' : 'Накладений платіж'}</p>
          <p><strong>Сума:</strong> {order.payment.amount.toFixed(2)} грн</p>
          <p><strong>Статус оплати:</strong> {order.payment.status}</p>
        </div>
      </div>

      <div style={{ background: '#fff', padding: 20, borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)', marginTop: 20 }}>
        <h3>Товари</h3>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid #eee', textAlign: 'left' }}>
              <th style={{ padding: '10px 0' }}>Назва</th>
              <th>Ціна</th>
              <th>К-сть</th>
              <th style={{ textAlign: 'right' }}>Сума</th>
            </tr>
          </thead>
          <tbody>
            {order.items.map(item => (
              <tr key={item.productId} style={{ borderBottom: '1px solid #f9f9f9' }}>
                <td style={{ padding: '12px 0' }}>{item.name}</td>
                <td>{item.price.toFixed(2)} грн</td>
                <td>{item.quantity}</td>
                <td style={{ textAlign: 'right' }}>{(item.price * item.quantity).toFixed(2)} грн</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <td colSpan="3" style={{ padding: '20px 0 5px 0', textAlign: 'right', color: '#666' }}>Товари:</td>
              <td style={{ padding: '20px 0 5px 0', textAlign: 'right' }}>{(order.totalAmount - (order.shippingCost || 0)).toFixed(2)} грн</td>
            </tr>
            {order.shippingCost > 0 && (
              <tr>
                <td colSpan="3" style={{ padding: '5px 0', textAlign: 'right', color: '#666' }}>Доставка:</td>
                <td style={{ padding: '5px 0', textAlign: 'right' }}>{order.shippingCost.toFixed(2)} грн</td>
              </tr>
            )}
            <tr>
              <td colSpan="3" style={{ padding: '10px 0', textAlign: 'right', fontWeight: 700, fontSize: '1.2rem' }}>Разом:</td>
              <td style={{ padding: '10px 0', textAlign: 'right', fontWeight: 700, fontSize: '1.2rem' }}>{order.totalAmount.toFixed(2)} грн</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  );
}

export default OrderDetailsPage;