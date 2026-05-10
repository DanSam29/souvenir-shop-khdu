import React, { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, ordersAPI, usersAPI, novaPoshtaAPI, featuresAPI } from '../services/api';

function CheckoutPage() {
  const { t, i18n } = useTranslation();
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [orderResult, setOrderResult] = useState(null);
  const [calcPreview, setCalcPreview] = useState(null);
  const [features, setFeatures] = useState(null);

  const isEn = i18n.language === 'en';

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
    if (form.cityRef || citySearch.length < 2) {
      setCities([]);
      setShowCityDropdown(false);
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
  }, [citySearch, form.cityRef]);

  // Nova Poshta: Warehouse Search
  useEffect(() => {
    if (!form.cityRef || form.warehouseRef) {
      setWarehouses([]);
      setShowWarehouseDropdown(false);
      return;
    }
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
  }, [form.cityRef, form.warehouseRef, warehouseSearch]);

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
          const items = calcPreview?.items ?? cart.items;
          // Використовуємо реальну вагу з БД, якщо вона є, інакше 0.5кг (fallback)
          const totalWeight = items.reduce((acc, item) => {
            const w = item.weight !== undefined ? item.weight : 0.5;
            return acc + w * item.quantity;
          }, 0);
          
          const currentTotal = calcPreview?.totalAmount ?? cart.totalAmount;
          const res = await novaPoshtaAPI.calculate(form.cityRef, totalWeight, currentTotal);
          setShippingCost(res.data.cost);
        } catch (err) {
          console.error('Error calculating shipping', err);
        }
      };
      calculateShipping();
    }
  }, [form.cityRef, cart, calcPreview]);

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
    return <div className="checkout-page"><div className="loading">{t('common.loading')}</div></div>;
  }

  if (orderResult) {
    return (
      <div className="checkout-page" style={{ maxWidth: 600, margin: '50px auto', padding: 20, textAlign: 'center' }}>
        <div style={{ background: '#e6ffed', border: '1px solid #b7eb8f', borderRadius: 12, padding: 40 }}>
          <h1 style={{ margin: '0 0 20px 0', color: '#1e4620' }}>✅ {t('checkout.success')}</h1>
          <p style={{ fontSize: '1.1rem', margin: '10px 0' }}><strong>{t('checkout.order_number')}:</strong> {orderResult.orderNumber}</p>
          <p style={{ fontSize: '1.1rem', margin: '10px 0' }}><strong>{t('cart.total')}:</strong> {orderResult.totalAmount.toFixed(2)} {t('common.currency')}</p>
          <button 
            onClick={() => navigate('/profile')} 
            style={{ 
              marginTop: 30, 
              padding: '12px 24px', 
              borderRadius: 8, 
              border: 'none', 
              background: '#007bff', 
              color: '#fff', 
              fontSize: '1rem', 
              cursor: 'pointer' 
            }}
          >
            {t('checkout.go_to_orders')}
          </button>
        </div>
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="checkout-page">
        <div className="empty-cart">
          <h2>{t('cart.empty')}</h2>
          <Link to="/" className="back-to-catalog-btn">{t('product.back_to_catalog')}</Link>
        </div>
      </div>
    );
  }

  const cartTotal = cart?.totalAmount ?? 0;
  const previewTotal = calcPreview?.totalAmount ?? cartTotal;
  const finalTotal = previewTotal + (shippingCost || 0);

  return (
    <div className="checkout-page" style={{ maxWidth: 900, margin: '0 auto', padding: 20 }}>
      <Link to="/cart" className="back-link" style={{ textDecoration: 'none', color: '#007bff', fontWeight: 500 }}>← {t('product.back_to_cart')}</Link>
      <h1 style={{ marginBottom: 30 }}>{t('checkout.title')}</h1>

      <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.8fr', gap: 30 }}>
        <form onSubmit={handleSubmit} style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)' }}>
          <h2 style={{ fontSize: '1.2rem', marginBottom: 20, borderBottom: '1px solid #eee', paddingBottom: 10 }}>{t('checkout.delivery')}</h2>

          {features?.novaPoshtaEnabled ? (
            <>
              <div style={{ position: 'relative', marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.city_np')}</label>
                <input
                  type="text"
                  value={citySearch}
                  onChange={(e) => {
                    setCitySearch(e.target.value);
                    if (form.cityRef) setForm(p => ({ ...p, cityRef: '', warehouseRef: '' }));
                  }}
                  placeholder={t('checkout.city_placeholder')}
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
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.warehouse')}</label>
                <input
                  type="text"
                  value={warehouseSearch}
                  onChange={(e) => setWarehouseSearch(e.target.value)}
                  disabled={!form.cityRef}
                  placeholder={form.cityRef ? t('checkout.warehouse_placeholder') : t('checkout.select_city_first')}
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
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.city')}</label>
                <input
                  type="text"
                  name="city"
                  value={form.city}
                  onChange={handleChange}
                  placeholder={t('checkout.city_placeholder_manual')}
                  style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' }}
                  required
                />
              </div>
              <div style={{ marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.warehouse')}</label>
                <input
                  type="text"
                  name="warehouseNumber"
                  value={form.warehouseNumber}
                  onChange={handleChange}
                  placeholder={t('checkout.warehouse_placeholder_manual')}
                  style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' }}
                  required
                />
              </div>
            </>
          )}

          <h2 style={{ fontSize: '1.2rem', margin: '30px 0 20px', borderBottom: '1px solid #eee', paddingBottom: 10 }}>{t('checkout.payment_promo')}</h2>

          <div style={{ marginBottom: 20 }}>
            <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.payment_method')}</label>
            <select
              name="paymentMethod"
              value={form.paymentMethod}
              onChange={handleChange}
              style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', cursor: 'pointer' }}
            >
              {features?.stripeEnabled && <option value="Card">💳 {t('checkout.payment_card')}</option>}
              <option value="CashOnDelivery">📦 {t('checkout.payment_cod')}</option>
            </select>
          </div>
          
          <div style={{ marginBottom: 20 }}>
            <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.promo_code')}</label>
            <input
              type="text"
              name="promoCode"
              value={form.promoCode}
              onChange={handleChange}
              placeholder={t('checkout.promo_placeholder')}
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
            {submitting ? t('common.submitting') : t('checkout.place_order')}
          </button>
        </form>

        <div style={{ position: 'sticky', top: 20 }}>
          <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)' }}>
            <h2 style={{ fontSize: '1.2rem', marginBottom: 20, borderBottom: '1px solid #eee', paddingBottom: 10 }}>{t('checkout.your_order')}</h2>
            <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
              {(calcPreview?.items ?? cart.items).map((item) => {
                // Визначаємо ціну: беремо з calcPreview (якщо є) або з кошика
                const itemPrice = item.finalPrice ?? item.productPrice ?? item.price ?? 0;
                const quantity = item.quantity ?? 0;
                const itemSubtotal = itemPrice * quantity;
                const displayName = (isEn && (item.nameEn || item.productNameEn)) ? (item.nameEn || item.productNameEn) : (item.name || item.productName);
                
                return (
                  <li key={item.cartItemId || item.productId} style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 12, fontSize: '0.95rem' }}>
                    <span style={{ color: '#555' }}>{displayName} × {quantity}</span>
                    <span style={{ fontWeight: 500 }}>{itemSubtotal.toFixed(2)} {t('common.currency')}</span>
                  </li>
                );
              })}
            </ul>
            
            <div style={{ marginTop: 20, paddingTop: 15, borderTop: '1px solid #eee' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                <span style={{ color: '#666' }}>{t('cart.items')}</span>
                <span>{previewTotal.toFixed(2)} {t('common.currency')}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 12 }}>
                <span style={{ color: '#666' }}>{t('checkout.delivery_cost')}</span>
                <span style={{ color: shippingCost > 0 ? '#000' : '#888' }}>{shippingCost > 0 ? `${shippingCost.toFixed(2)} ${t('common.currency')}` : t('checkout.will_be_calculated')}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '1.2rem', fontWeight: 700, marginTop: 10, color: '#000' }}>
                <span>{t('cart.total')}</span>
                <span>{finalTotal.toFixed(2)} {t('common.currency')}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default CheckoutPage;