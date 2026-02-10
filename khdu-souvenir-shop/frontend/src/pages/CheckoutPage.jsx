import React, { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, ordersAPI, usersAPI } from '../services/api';

function CheckoutPage() {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const [orderResult, setOrderResult] = useState(null);

  const [form, setForm] = useState({
    city: '',
    warehouseNumber: '',
    paymentMethod: 'CashOnDelivery',
    promoCode: '',
  });
  const [currentUser, setCurrentUser] = useState(null);

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    loadCart();
    loadUser();
  }, [isAuthenticated, navigate]);

  const loadCart = async () => {
    try {
      setLoading(true);
      const response = await cartAPI.getCart();
      setCart(response.data);
      setError(null);
    } catch (err) {
      setError('Не вдалося завантажити кошик');
    } finally {
      setLoading(false);
    }
  };

  const loadUser = async () => {
    try {
      const res = await usersAPI.getCurrentUser();
      setCurrentUser(res.data);
      setForm(prev => ({
        ...prev,
        // заповнюємо службово для бекенду
        recipientName: `${res.data.firstName} ${res.data.lastName}`.trim(),
        recipientPhone: res.data.phone || '',
      }));
    } catch {
      // якщо не вдалося отримати користувача, все одно дозволяємо оформлення,
      // але бекенд отримає порожні значення (MVP)
      setForm(prev => ({
        ...prev,
        recipientName: '',
        recipientPhone: '',
      }));
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.city || !form.warehouseNumber) {
      alert('Заповніть місто та номер відділення');
      return;
    }
    try {
      setSubmitting(true);
      const payload = {
        city: form.city.trim(),
        warehouseNumber: form.warehouseNumber.trim(),
        recipientName: (form.recipientName || '').trim(),
        recipientPhone: (form.recipientPhone || '').trim(),
        paymentMethod: form.paymentMethod,
        promoCode: form.promoCode?.trim() || null,
      };
      const res = await ordersAPI.checkout(payload);
      setOrderResult(res.data);
      await loadCart();
    } catch (err) {
      alert(err.response?.data?.error || 'Не вдалося оформити замовлення');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <div className="checkout-page"><div className="loading">Завантаження...</div></div>;
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="checkout-page">
        <div className="empty-cart">
          <h2>Кошик порожній</h2>
          <Link to="/" className="back-to-catalog-btn">Перейти до каталогу</Link>
        </div>
      </div>
    );
  }

  return (
    <div className="checkout-page" style={{ maxWidth: 800, margin: '0 auto', padding: 20 }}>
      <Link to="/cart" className="back-link">← Назад до кошика</Link>
      <h1>Оформлення замовлення</h1>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24 }}>
        <form onSubmit={handleSubmit} style={{ background: '#fff', padding: 16, borderRadius: 8, boxShadow: '0 2px 6px rgba(0,0,0,0.05)' }}>
          <h2>Дані доставки</h2>

          <label>
            Місто
            <input
              type="text"
              name="city"
              value={form.city}
              onChange={handleChange}
              placeholder="Напр. Київ"
              style={{ width: '100%', padding: 8, marginTop: 6 }}
            />
          </label>

          <label style={{ marginTop: 12 }}>
            Номер відділення
            <input
              type="text"
              name="warehouseNumber"
              value={form.warehouseNumber}
              onChange={handleChange}
              placeholder="Напр. №12"
              style={{ width: '100%', padding: 8, marginTop: 6 }}
            />
          </label>

          {currentUser && (
            <div style={{ marginTop: 12, background: '#f7f9ff', border: '1px solid #e0e6ff', borderRadius: 6, padding: 10 }}>
              <div><strong>Одержувач:</strong> {currentUser.firstName} {currentUser.lastName}</div>
              {currentUser.phone && <div><strong>Телефон:</strong> {currentUser.phone}</div>}
            </div>
          )}

          <label style={{ marginTop: 12 }}>
            Спосіб оплати
            <select
              name="paymentMethod"
              value={form.paymentMethod}
              onChange={handleChange}
              style={{ width: '100%', padding: 8, marginTop: 6 }}
            >
              <option value="CashOnDelivery">Накладений платіж</option>
            </select>
          </label>
          
          <label style={{ marginTop: 12 }}>
            Промокод
            <input
              type="text"
              name="promoCode"
              value={form.promoCode}
              onChange={handleChange}
              placeholder="Напр. KHDU10"
              style={{ width: '100%', padding: 8, marginTop: 6 }}
            />
          </label>

          <button
            type="submit"
            disabled={submitting}
            style={{ marginTop: 16, padding: '10px 16px' }}
          >
            {submitting ? 'Оформлення...' : 'Підтвердити замовлення'}
          </button>
        </form>

        <div style={{ background: '#fff', padding: 16, borderRadius: 8, boxShadow: '0 2px 6px rgba(0,0,0,0.05)' }}>
          <h2>Ваше замовлення</h2>
          <ul style={{ listStyle: 'none', padding: 0 }}>
            {cart.items.map((item) => (
              <li key={item.cartItemId} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                <span>{item.productName} × {item.quantity}</span>
                <span>{(item.productPrice * item.quantity).toFixed(2)} грн</span>
              </li>
            ))}
          </ul>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 600, marginTop: 12 }}>
            <span>Разом</span>
            <span>{cart.totalAmount.toFixed(2)} грн</span>
          </div>

          {orderResult && (
            <div style={{ marginTop: 16, padding: 12, background: '#e6ffed', border: '1px solid #b7eb8f', borderRadius: 6 }}>
              <h3>Замовлення оформлено</h3>
              <p>Номер: {orderResult.orderNumber}</p>
              <p>Сума: {orderResult.totalAmount.toFixed(2)} грн</p>
              {typeof orderResult.discountTotal === 'number' && orderResult.discountTotal > 0 && (
                <p>Знижка застосована: −{orderResult.discountTotal.toFixed(2)} грн</p>
              )}
              <button onClick={() => navigate('/profile')} style={{ marginTop: 8 }}>
                Перейти в профіль
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default CheckoutPage;
