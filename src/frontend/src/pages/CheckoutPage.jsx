import React, { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, ordersAPI, usersAPI, novaPoshtaAPI, featuresAPI } from '../services/api';

function CheckoutPage() {
  useTranslation();
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [orderResult, setOrderResult] = useState(null);
  const [calcPreview, setCalcPreview] = useState(null);
  const [features, setFeatures] = useState(null);

  // Nova Poshta states
  const [cities, setCities] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [citySearch, setCitySearch] = useState('');
  const [warehouseSearch, setWarehouseSearch] = useState('');
  const [showCityDropdown, setShowCityDropdown] = useState(false);
  const [showWarehouseDropdown, setShowWarehouseDropdown] = useState(false);
  const [shippingCost, setShippingCost] = useState(0);

  const [form, setForm] = useState({
    city: '',
    cityRef: '',
    warehouseNumber: '',
    warehouseRef: '',
    paymentMethod: 'Card',
    promoCode: '',
    recipientName: '',
    recipientPhone: '',
  });

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    loadCart();
    loadUser();
    loadFeatures();
  }, [isAuthenticated, navigate]);

  const loadFeatures = async () => {
    try {
      const res = await featuresAPI.getStatus();
      setFeatures(res.data);
    } catch (err) {
      console.error('Не вдалося завантажити статуси фічефлагів', err);
    }
  };

  const loadCart = async () => {
    try {
      setLoading(true);
      const response = await cartAPI.getCart();
      setCart(response.data);
    } catch (err) {
      console.error('Не вдалося завантажити кошик', err);
    } finally {
      setLoading(false);
    }
  };

  const loadUser = async () => {
    try {
      const res = await usersAPI.getCurrentUser();
      setForm(prev => ({
        ...prev,
        recipientName: `${res.data.firstName} ${res.data.lastName}`.trim(),
        recipientPhone: res.data.phone || '',
      }));
    } catch {
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

  // Nova Poshta: City Search
  useEffect(() => {
    if (citySearch.length < 2) {
      setCities([]);
      return;
    }
    const timer = setTimeout(async () => {
      try {
        const res = await novaPoshtaAPI.getCities(citySearch);
        setCities(res.data);
        setShowCityDropdown(true);
      } catch (err) {
        console.error('Error searching cities', err);
      }
    }, 500);
    return () => clearTimeout(timer);
  }, [citySearch]);

  // Nova Poshta: Warehouse Search
  useEffect(() => {
    if (!form.cityRef) return;
    const timer = setTimeout(async () => {
      try {
        const res = await novaPoshtaAPI.getWarehouses(form.cityRef, warehouseSearch);
        setWarehouses(res.data);
        setShowWarehouseDropdown(true);
      } catch (err) {
        console.error('Error searching warehouses', err);
      }
    }, 500);
    return () => clearTimeout(timer);
  }, [form.cityRef, warehouseSearch]);

  const handleCitySelect = (city) => {
    setForm(prev => ({ 
      ...prev, 
      city: city.description, 
      cityRef: city.ref,
      warehouseNumber: '',
      warehouseRef: '' 
    }));
    setCitySearch(city.description);
    setShowCityDropdown(false);
    setWarehouses([]);
    setWarehouseSearch('');
  };

  const handleWarehouseSelect = (wh) => {
    setForm(prev => ({ 
      ...prev, 
      warehouseNumber: wh.description, 
      warehouseRef: wh.ref 
    }));
    setWarehouseSearch(wh.description);
    setShowWarehouseDropdown(false);
  };

  // Calculate Shipping Cost
  useEffect(() => {
    if (form.cityRef && cart) {
      const calculateShipping = async () => {
        try {
          const totalWeight = cart.items.reduce((acc, item) => acc + (item.weight || 0.5) * item.quantity, 0);
          const res = await novaPoshtaAPI.calculate(form.cityRef, totalWeight, cart.totalAmount);
          setShippingCost(res.data.cost);
        } catch (err) {
          console.error('Error calculating shipping', err);
        }
      };
      calculateShipping();
    }
  }, [form.cityRef, cart]);

  // Автоперерахунок при введенні промокоду
  useEffect(() => {
    const timer = setTimeout(async () => {
      const code = form.promoCode?.trim();
      try {
        if (!code) {
          setCalcPreview(null);
          return;
        }
        const payload = { promoCode: code, cityRef: form.cityRef };
        const res = await ordersAPI.calculate(payload);
        setCalcPreview(res.data);
      } catch (err) {
        setCalcPreview(null);
      }
    }, 600);
    return () => clearTimeout(timer);
  }, [form.promoCode, form.cityRef]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.cityRef || !form.warehouseRef) {
      alert('Будь ласка, виберіть місто та відділення зі списку');
      return;
    }
    try {
      setSubmitting(true);
      const payload = {
        city: form.city,
        cityRef: form.cityRef,
        warehouseNumber: form.warehouseNumber,
        warehouseRef: form.warehouseRef,
        recipientName: (form.recipientName || '').trim(),
        recipientPhone: (form.recipientPhone || '').trim(),
        paymentMethod: form.paymentMethod,
        promoCode: form.promoCode?.trim() || null,
      };
      const res = await ordersAPI.checkout(payload);
      
      if (res.data && res.data.paymentUrl) {
        window.location.href = res.data.paymentUrl;
        return;
      }

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

  const finalTotal = (calcPreview?.totalAmount ?? cart.totalAmount) + shippingCost;

  return (
    <div className="checkout-page" style={{ maxWidth: 900, margin: '0 auto', padding: 20 }}>
      <Link to="/cart" className="back-link" style={{ textDecoration: 'none', color: '#666' }}>← Назад до кошика</Link>
      <h1 style={{ marginBottom: 30 }}>Оформлення замовлення</h1>

      <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.8fr', gap: 30 }}>
        <form onSubmit={handleSubmit} style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)' }}>
          <h2 style={{ fontSize: '1.2rem', marginBottom: 20, borderBottom: '1px solid #eee', paddingBottom: 10 }}>Дані доставки</h2>

          {features?.novaPoshtaEnabled ? (
            <>
              <div style={{ position: 'relative', marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Місто (Нова Пошта)</label>
                <input
                  type="text"
                  value={citySearch}
                  onChange={(e) => {
                    setCitySearch(e.target.value);
                    if (form.cityRef) setForm(p => ({ ...p, cityRef: '', warehouseRef: '' }));
                  }}
                  placeholder="Почніть вводити назву міста..."
                  style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' }}
                />
                {showCityDropdown && cities.length > 0 && (
                  <ul style={{ position: 'absolute', top: '100%', left: 0, right: 0, background: '#fff', border: '1px solid #ddd', borderRadius: '0 0 8px 8px', listStyle: 'none', padding: 0, margin: 0, zIndex: 10, maxHeight: 200, overflowY: 'auto', boxShadow: '0 4px 12px rgba(0,0,0,0.1)' }}>
                    {cities.map(city => (
                      <li 
                        key={city.ref} 
                        onClick={() => handleCitySelect(city)}
                        style={{ padding: '10px 12px', cursor: 'pointer', borderBottom: '1px solid #eee' }}
                        onMouseEnter={(e) => e.target.style.background = '#f5f5f5'}
                        onMouseLeave={(e) => e.target.style.background = '#fff'}
                      >
                        {city.description} <small style={{ color: '#888' }}>({city.areaDescription})</small>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              <div style={{ position: 'relative', marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Відділення</label>
                <input
                  type="text"
                  value={warehouseSearch}
                  onChange={(e) => setWarehouseSearch(e.target.value)}
                  disabled={!form.cityRef}
                  placeholder={form.cityRef ? "Виберіть відділення..." : "Спочатку виберіть місто"}
                  style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box', background: form.cityRef ? '#fff' : '#f9f9f9' }}
                />
                {showWarehouseDropdown && warehouses.length > 0 && (
                  <ul style={{ position: 'absolute', top: '100%', left: 0, right: 0, background: '#fff', border: '1px solid #ddd', borderRadius: '0 0 8px 8px', listStyle: 'none', padding: 0, margin: 0, zIndex: 10, maxHeight: 200, overflowY: 'auto', boxShadow: '0 4px 12px rgba(0,0,0,0.1)' }}>
                    {warehouses.map(wh => (
                      <li 
                        key={wh.ref} 
                        onClick={() => handleWarehouseSelect(wh)}
                        style={{ padding: '10px 12px', cursor: 'pointer', borderBottom: '1px solid #eee' }}
                        onMouseEnter={(e) => e.target.style.background = '#f5f5f5'}
                        onMouseLeave={(e) => e.target.style.background = '#fff'}
                      >
                        {wh.description}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </>
          ) : (
            <>
              <div style={{ marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Місто</label>
                <input
                  type="text"
                  name="city"
                  value={form.city}
                  onChange={handleChange}
                  placeholder="Введіть назву міста"
                  style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' }}
                  required
                />
              </div>
              <div style={{ marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Відділення</label>
                <input
                  type="text"
                  name="warehouseNumber"
                  value={form.warehouseNumber}
                  onChange={handleChange}
                  placeholder="Введіть відділення"
                  style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' }}
                  required
                />
              </div>
            </>
          )}

          <h2 style={{ fontSize: '1.2rem', margin: '30px 0 20px', borderBottom: '1px solid #eee', paddingBottom: 10 }}>Оплата та промокод</h2>

          <div style={{ marginBottom: 20 }}>
            <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Спосіб оплати</label>
            <select
              name="paymentMethod"
              value={form.paymentMethod}
              onChange={handleChange}
              style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', cursor: 'pointer' }}
            >
              {features?.stripeEnabled && <option value="Card">💳 Оплата карткою (Stripe)</option>}
              <option value="CashOnDelivery">📦 Накладений платіж</option>
            </select>
          </div>
          
          <div style={{ marginBottom: 20 }}>
            <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Промокод</label>
            <input
              type="text"
              name="promoCode"
              value={form.promoCode}
              onChange={handleChange}
              placeholder="Введіть промокод (якщо є)"
              style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' }}
            />
          </div>

          <button
            type="submit"
            disabled={
              submitting || 
              (features?.novaPoshtaEnabled && (!form.cityRef || !form.warehouseRef)) ||
              (!features?.novaPoshtaEnabled && (!form.city || !form.warehouseNumber))
            }
            style={{ 
              width: '100%', 
              marginTop: 10, 
              padding: '14px', 
              borderRadius: 8, 
              border: 'none', 
              background: 
                (submitting || 
                (features?.novaPoshtaEnabled && (!form.cityRef || !form.warehouseRef)) ||
                (!features?.novaPoshtaEnabled && (!form.city || !form.warehouseNumber))) 
                  ? '#ccc' 
                  : '#007bff', 
              color: '#fff', 
              fontSize: '1.1rem', 
              fontWeight: 600, 
              cursor: 'pointer',
              transition: 'background 0.2s'
            }}
          >
            {submitting ? 'Оформлення...' : 'Підтвердити замовлення'}
          </button>
        </form>

        <div style={{ position: 'sticky', top: 20 }}>
          <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)' }}>
            <h2 style={{ fontSize: '1.2rem', marginBottom: 20, borderBottom: '1px solid #eee', paddingBottom: 10 }}>Ваше замовлення</h2>
            <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
              {(calcPreview?.items ?? cart.items).map((item) => (
                <li key={item.cartItemId || item.productId} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 12, fontSize: '0.95rem' }}>
                  <span style={{ color: '#555' }}>{item.name || item.productName} × {item.quantity}</span>
                  <span style={{ fontWeight: 500 }}>{(item.subtotal || (item.price * item.quantity)).toFixed(2)} грн</span>
                </li>
              ))}
            </ul>
            
            <div style={{ marginTop: 20, paddingTop: 15, borderTop: '1px solid #eee' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                <span style={{ color: '#666' }}>Товари</span>
                <span>{(calcPreview?.totalAmount ?? cart.totalAmount).toFixed(2)} грн</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 12 }}>
                <span style={{ color: '#666' }}>Доставка (Нова Пошта)</span>
                <span style={{ color: shippingCost > 0 ? '#000' : '#888' }}>{shippingCost > 0 ? `${shippingCost.toFixed(2)} грн` : 'буде розраховано'}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '1.2rem', fontWeight: 700, marginTop: 10, color: '#000' }}>
                <span>Разом</span>
                <span>{finalTotal.toFixed(2)} грн</span>
              </div>
            </div>

            {orderResult && (
              <div style={{ marginTop: 24, padding: 16, background: '#e6ffed', border: '1px solid #b7eb8f', borderRadius: 8 }}>
                <h3 style={{ margin: '0 0 10px 0', color: '#1e4620' }}>✅ Замовлення оформлено!</h3>
                <p style={{ margin: '4px 0' }}><strong>№:</strong> {orderResult.orderNumber}</p>
                <p style={{ margin: '4px 0' }}><strong>Сума:</strong> {orderResult.totalAmount.toFixed(2)} грн</p>
                <button 
                  onClick={() => navigate('/profile')} 
                  style={{ width: '100%', marginTop: 15, padding: '8px', borderRadius: 6, border: '1px solid #b7eb8f', background: '#fff', cursor: 'pointer' }}
                >
                  Перейти в профіль
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default CheckoutPage;