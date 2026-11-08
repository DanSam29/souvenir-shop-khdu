import React, { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../components/Toast';
import { cartAPI, ordersAPI, usersAPI, novaPoshtaAPI, featuresAPI, promotionsAPI } from '../services/api';

const TOTAL_STEPS = 4;
const NP_DEBOUNCE_MS = 300;
const PHONE_REGEX = /^\+380\d{9}$/;
const PAYMENT_METHODS = [
  { value: 'Card', labelKey: 'checkout.payment_card', icon: '💳' },
  { value: 'CashOnDelivery', labelKey: 'checkout.payment_cod', icon: '📦' }
];

function CheckoutPage() {
  const { t, i18n } = useTranslation();
  const { isAuthenticated } = useAuth();
  const { showToast } = useToast();
  const navigate = useNavigate();
  const [step, setStep] = useState(1);
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [orderResult, setOrderResult] = useState(null);
  const [calcPreview, setCalcPreview] = useState(null);
  // Промокод: стан валідації
  const [appliedPromo, setAppliedPromo] = useState(null);
  const [promoError, setPromoError] = useState(null);
  const [promoValidating, setPromoValidating] = useState(false);
  const [features, setFeatures] = useState(null);
  const [savedAddresses, setSavedAddresses] = useState([]);
  const isEn = i18n.language === 'en';
  const isNpEnabled = !!features?.novaPoshtaEnabled;
  // Nova Poshta: стани
  const [cities, setCities] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [citySearch, setCitySearch] = useState('');
  const [warehouseSearch, setWarehouseSearch] = useState('');
  const [showCityDropdown, setShowCityDropdown] = useState(false);
  const [showWarehouseDropdown, setShowWarehouseDropdown] = useState(false);
  const [shippingCost, setShippingCost] = useState(0);
  const [estimatedDays, setEstimatedDays] = useState(null);
  // Якщо API Нової Пошти недоступне — перемикаємось на ручний ввід адреси
  const [npFallback, setNpFallback] = useState(false);
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
    loadAddresses();
  }, [isAuthenticated, navigate]);
  const loadFeatures = async () => {
    try {
      const res = await featuresAPI.getStatus();
      setFeatures(res.data);
    } catch (err) {
      console.error('Не вдалося завантажити статуси фічефлагів', err);
    }
  };
  // Якщо Stripe вимкнено фічефлагом — картка недоступна, перемикаємось на накладений платіж
  useEffect(() => {
    if (features && !features.stripeEnabled) {
      setForm((prev) => (prev.paymentMethod === 'Card'
        ? { ...prev, paymentMethod: 'CashOnDelivery' }
        : prev));
    }
  }, [features]);
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
  const loadAddresses = async () => {
    try {
      const res = await usersAPI.getAddresses();
      setSavedAddresses(res.data || []);
    } catch {
      setSavedAddresses([]);
    }
  };
  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };
  // Nova Poshta: пошук міста
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
        // API недоступне — перемикаємось на ручний ввід
        setNpFallback(true);
      }
    }, NP_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [citySearch, form.cityRef]);
  // Nova Poshta: пошук відділення
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
        setNpFallback(true);
      }
    }, NP_DEBOUNCE_MS);
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
  // Вибір збереженої адреси
  const handleAddressSelect = (a) => {
    setForm(prev => ({
      ...prev,
      city: a.city,
      cityRef: a.cityRef || '',
      warehouseNumber: a.warehouseNumber,
      warehouseRef: a.warehouseRef || ''
    }));
    setCitySearch(a.city);
    setWarehouseSearch(a.warehouseNumber);
    setShowCityDropdown(false);
    setShowWarehouseDropdown(false);
  };
  // Nova Poshta: обчислення вартості доставки
  useEffect(() => {
    if (form.cityRef && cart) {
      const calculateShipping = async () => {
        try {
          const items = calcPreview?.items ?? cart.items;
          // Використання ваги з БД, якщо вона є, інакше 0.5кг (fallback)
          const totalWeight = items.reduce((acc, item) => {
            const w = item.weight !== undefined ? item.weight : 0.5;
            return acc + w * item.quantity;
          }, 0);
          const currentTotal = calcPreview?.totalAmount ?? cart.totalAmount;
          const res = await novaPoshtaAPI.calculate({
            cityRef: form.cityRef,
            warehouseRef: form.warehouseRef,
            weight: totalWeight,
            totalAmount: currentTotal
          });
          setShippingCost(res.data.cost);
          setEstimatedDays(res.data.estimatedDays ?? null);
        } catch (err) {
          console.error('Error calculating shipping', err);
          setNpFallback(true);
          setShippingCost(0);
          setEstimatedDays(null);
        }
      };
      calculateShipping();
    }
  }, [form.cityRef, form.warehouseRef, cart, calcPreview]);
  // Зміна поля промокоду — скидання попереднього результату валідації
  const handlePromoChange = (e) => {
    setForm((prev) => ({ ...prev, promoCode: e.target.value }));
    setAppliedPromo(null);
    setPromoError(null);
    setCalcPreview(null);
  };
  // Застосування промокоду: валідація (6 правил) → оновлення тоталів
  const handleApplyPromo = async () => {
    const code = form.promoCode?.trim();
    setPromoError(null);
    setAppliedPromo(null);
    if (!code) {
      setCalcPreview(null);
      return;
    }
    try {
      setPromoValidating(true);
      const res = await promotionsAPI.validatePromoCode(code);
      const validation = res.data;
      if (validation?.valid) {
        setAppliedPromo(validation.promotion);
        const calcRes = await ordersAPI.calculate({ promoCode: code, cityRef: form.cityRef });
        setCalcPreview(calcRes.data);
        if (calcRes.data?.promoCodeError) {
          setPromoError(calcRes.data.promoCodeError);
        } else {
          showToast(t('checkout.promo_applied'), 'success');
        }
      } else {
        setPromoError(validation?.error || t('checkout.promo_invalid'));
        setCalcPreview(null);
      }
    } catch (err) {
      setPromoError(err.response?.data?.error || t('checkout.promo_invalid'));
      setCalcPreview(null);
    } finally {
      setPromoValidating(false);
    }
  };
  // Селектори Нової Пошти активні лише коли інтеграція увімкнена і API не повернуло помилку
  const isNpMode = isNpEnabled && !npFallback;
  // Валідація окремого кроку
  const validateStep = (current) => {
    if (current === 1) {
      if ((form.recipientName || '').trim().length < 2) {
        showToast(t('checkout.name_required'), 'error');
        return false;
      }
      if (!PHONE_REGEX.test((form.recipientPhone || '').trim())) {
        showToast(t('checkout.phone_invalid'), 'error');
        return false;
      }
      return true;
    }
    if (current === 2) {
      if (isNpMode) {
        if (!form.cityRef || !form.warehouseRef) {
          showToast(t('checkout.select_city_warehouse'), 'error');
          return false;
        }
      } else if (!form.city || !form.warehouseNumber) {
        showToast(t('checkout.select_city_warehouse'), 'error');
        return false;
      }
      return true;
    }
    return true;
  };
  const handleNext = () => {
    if (validateStep(step)) {
      setStep((s) => Math.min(s + 1, TOTAL_STEPS));
    }
  };
  const handleBack = () => setStep((s) => Math.max(s - 1, 1));
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateStep(1) || !validateStep(2)) {
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
        showToast(t('checkout.redirecting_to_payment'), 'info');
        window.location.href = res.data.paymentUrl;
        return;
      }
      setOrderResult(res.data);
      showToast(t('checkout.success'), 'success');
      await loadCart();
    } catch (err) {
      showToast(err.response?.data?.error || t('checkout.failed'), 'error');
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
  const promoDiscount = Math.max(0, cartTotal - previewTotal);
  const finalTotal = previewTotal + (shippingCost || 0);
  const stepLabels = [
    t('checkout.step_contacts'),
    t('checkout.delivery'),
    t('checkout.payment'),
    t('checkout.step_confirmation')
  ];
  const summaryItems = calcPreview?.items ?? cart.items;
  return (
    <div className="checkout-page" style={{ maxWidth: 900, margin: '0 auto', padding: 20 }}>
      <Link to="/cart" className="back-link" style={{ textDecoration: 'none', color: '#007bff', fontWeight: 500 }}>← {t('product.back_to_cart')}</Link>
      <h1 style={{ marginBottom: 20 }}>{t('checkout.title')}</h1>
      {/* Індикатор кроків */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 30, flexWrap: 'wrap' }}>
        {stepLabels.map((label, i) => {
          const n = i + 1;
          const active = step === n;
          const done = step > n;
          return (
            <div key={n} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <div style={{
                width: 28, height: 28, borderRadius: '50%', fontSize: '0.85rem',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                background: active ? '#007bff' : done ? '#28a745' : '#e0e0e0',
                color: '#fff', fontWeight: 600, flexShrink: 0
              }}>
                {done ? '✓' : n}
              </div>
              <span style={{ color: active ? '#000' : '#888', fontWeight: active ? 600 : 400, fontSize: '0.9rem' }}>{label}</span>
              {n < TOTAL_STEPS && <span style={{ color: '#ccc' }}>—</span>}
            </div>
          );
        })}
      </div>
      <p style={{ color: '#888', marginTop: -20, marginBottom: 20, fontSize: '0.9rem' }}>
        {t('checkout.step_of', { current: step, total: TOTAL_STEPS })}
      </p>
      <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.8fr', gap: 30 }}>
        <form onSubmit={handleSubmit} style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)' }}>
          {/* Крок 1: Контактні дані */}
          {step === 1 && (
            <>
              <h2 style={{ fontSize: '1.2rem', marginBottom: 20, borderBottom: '1px solid #eee', paddingBottom: 10 }}>{t('checkout.step_contacts')}</h2>
              <div style={{ marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.full_name')}</label>
                <input
                  type="text"
                  name="recipientName"
                  value={form.recipientName}
                  onChange={handleChange}
                  style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' }}
                />
              </div>
              <div style={{ marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.phone')}</label>
                <input
                  type="tel"
                  name="recipientPhone"
                  value={form.recipientPhone}
                  onChange={handleChange}
                  placeholder="+380XXXXXXXXX"
                  style={{ width: '100%', padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' }}
                />
              </div>
            </>
          )}
          {/* Крок 2: Доставка */}
          {step === 2 && (
            <>
              <h2 style={{ fontSize: '1.2rem', marginBottom: 20, borderBottom: '1px solid #eee', paddingBottom: 10 }}>{t('checkout.delivery')}</h2>
              {savedAddresses.length > 0 && (
                <div style={{ marginBottom: 20 }}>
                  <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.addresses')}</label>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                    {savedAddresses.map((a) => {
                      const isSelected = form.cityRef && a.cityRef === form.cityRef && a.warehouseRef === form.warehouseRef;
                      return (
                        <button
                          type="button"
                          key={a.shippingId}
                          onClick={() => handleAddressSelect(a)}
                          style={{
                            textAlign: 'left',
                            padding: '10px 12px',
                            borderRadius: 8,
                            border: isSelected ? '2px solid #007bff' : '1px solid #ddd',
                            background: isSelected ? '#f0f7ff' : '#fff',
                            cursor: 'pointer'
                          }}
                        >
                          <strong>{a.city}</strong>, {a.warehouseNumber}
                          {a.isDefault && <span style={{ marginLeft: 8, color: '#28a745', fontSize: '0.8rem' }}>★</span>}
                        </button>
                      );
                    })}
                  </div>
                </div>
              )}
              {isNpEnabled && npFallback && (
                <div style={{ marginBottom: 20, padding: 12, background: '#fff7e6', border: '1px solid #ffd591', borderRadius: 8, color: '#d46b08' }}>
                  {t('checkout.np_unavailable')}
                </div>
              )}
              {isNpMode ? (
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
                  {form.warehouseRef && (
                    <div style={{ marginBottom: 20, padding: 12, background: '#f6ffed', borderRadius: 8, border: '1px solid #b7eb8f' }}>
                      <p style={{ margin: 0 }}>
                        <strong>{t('checkout.delivery_cost')}:</strong>{' '}
                        {shippingCost > 0 ? `${shippingCost.toFixed(2)} ${t('common.currency')}` : t('checkout.will_be_calculated')}
                      </p>
                      <p style={{ margin: '6px 0 0 0', color: '#666', fontSize: '0.9rem' }}>
                        {t('checkout.estimated_days')}: {estimatedDays
                          ? t('checkout.estimated_days_value_dynamic', { days: estimatedDays })
                          : t('checkout.estimated_days_value')}
                      </p>
                    </div>
                  )}
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
                    />
                  </div>
                </>
              )}
            </>
          )}
          {/* Крок 3: Оплата */}
          {step === 3 && (
            <>
              <h2 style={{ fontSize: '1.2rem', marginBottom: 20, borderBottom: '1px solid #eee', paddingBottom: 10 }}>{t('checkout.payment_promo')}</h2>
              <div style={{ marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.payment_method')}</label>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                  {PAYMENT_METHODS
                    .filter((method) => method.value !== 'Card' || features?.stripeEnabled)
                    .map((method) => {
                      const checked = form.paymentMethod === method.value;
                      return (
                        <label
                          key={method.value}
                          htmlFor={`payment-${method.value}`}
                          style={{
                            display: 'flex', alignItems: 'flex-start', gap: 10,
                            padding: '12px 14px', borderRadius: 8, cursor: 'pointer',
                            border: checked ? '2px solid #007bff' : '1px solid #ddd',
                            background: checked ? '#f0f7ff' : '#fff'
                          }}
                        >
                          <input
                            id={`payment-${method.value}`}
                            type="radio"
                            name="paymentMethod"
                            value={method.value}
                            checked={checked}
                            onChange={handleChange}
                            style={{ marginTop: 3 }}
                          />
                          <span>
                            <span style={{ display: 'block', fontWeight: 500 }}>{method.icon} {t(method.labelKey)}</span>
                            <span style={{ display: 'block', color: '#888', fontSize: '0.85rem' }}>
                              {t(method.value === 'Card' ? 'checkout.payment_card_hint' : 'checkout.payment_cod_hint')}
                            </span>
                          </span>
                        </label>
                      );
                    })}
                </div>
              </div>
              <div style={{ marginBottom: 20 }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{t('checkout.promo_code')}</label>
                <div style={{ display: 'flex', gap: 10 }}>
                  <input
                    type="text"
                    name="promoCode"
                    value={form.promoCode}
                    onChange={handlePromoChange}
                    placeholder={t('checkout.promo_placeholder')}
                    style={{ flex: 1, padding: '12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' }}
                  />
                  <button
                    type="button"
                    onClick={handleApplyPromo}
                    disabled={promoValidating || !form.promoCode?.trim()}
                    style={{
                      padding: '12px 20px', borderRadius: 8, border: 'none',
                      background: (promoValidating || !form.promoCode?.trim()) ? '#b9d4f5' : '#007bff',
                      color: '#fff', fontWeight: 600, cursor: (promoValidating || !form.promoCode?.trim()) ? 'default' : 'pointer'
                    }}
                  >
                    {promoValidating ? t('common.loading') : t('checkout.apply_promo')}
                  </button>
                </div>
                {appliedPromo && (
                  <p style={{ margin: '8px 0 0 0', color: '#28a745', fontSize: '0.9rem' }}>
                    ✅ {t('checkout.promo_applied')}: {isEn && appliedPromo.nameEn ? appliedPromo.nameEn : appliedPromo.name}
                  </p>
                )}
                {promoError && (
                  <p style={{ margin: '8px 0 0 0', color: '#cf1322', fontSize: '0.9rem' }}>
                    ❌ {promoError}
                  </p>
                )}
              </div>
            </>
          )}
          {/* Крок 4: Підтвердження */}
          {step === 4 && (
            <>
              <h2 style={{ fontSize: '1.2rem', marginBottom: 20, borderBottom: '1px solid #eee', paddingBottom: 10 }}>{t('checkout.step_confirmation')}</h2>
              <div style={{ marginBottom: 16 }}>
                <div style={{ color: '#888', fontSize: '0.85rem' }}>{t('checkout.recipient')}</div>
                <div>{form.recipientName} · {form.recipientPhone}</div>
              </div>
              <div style={{ marginBottom: 16 }}>
                <div style={{ color: '#888', fontSize: '0.85rem' }}>{t('checkout.address')}</div>
                <div>{t('checkout.city_prefix')} {form.city}, {form.warehouseNumber}</div>
              </div>
              <div style={{ marginBottom: 16 }}>
                <div style={{ color: '#888', fontSize: '0.85rem' }}>{t('checkout.payment_method')}</div>
                <div>{form.paymentMethod === 'Card' ? t('checkout.payment_card') : t('checkout.payment_cod')}</div>
              </div>
              <div style={{ marginTop: 20, paddingTop: 15, borderTop: '1px solid #eee' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                  <span style={{ color: '#666' }}>{t('cart.items')}</span>
                  <span>{previewTotal.toFixed(2)} {t('common.currency')}</span>
                </div>
                {promoDiscount > 0 && (
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                    <span style={{ color: '#28a745' }}>{t('cart.discount')}</span>
                    <span style={{ color: '#28a745' }}>-{promoDiscount.toFixed(2)} {t('common.currency')}</span>
                  </div>
                )}
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                  <span style={{ color: '#666' }}>{t('checkout.delivery_cost')}</span>
                  <span style={{ color: shippingCost > 0 ? '#000' : '#888' }}>{shippingCost > 0 ? `${shippingCost.toFixed(2)} ${t('common.currency')}` : t('checkout.will_be_calculated')}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '1.2rem', fontWeight: 700, marginTop: 10, color: '#000' }}>
                  <span>{t('cart.total')}</span>
                  <span>{finalTotal.toFixed(2)} {t('common.currency')}</span>
                </div>
              </div>
            </>
          )}
          {/* Навігація кроками */}
          <div style={{ display: 'flex', gap: 10, marginTop: 24 }}>
            {step > 1 && (
              <button
                type="button"
                onClick={handleBack}
                style={{ padding: '14px 24px', borderRadius: 8, border: '1px solid #ddd', background: '#fff', fontSize: '1rem', fontWeight: 600, cursor: 'pointer' }}
              >
                {t('checkout.back')}
              </button>
            )}
            {step < TOTAL_STEPS ? (
              <button
                type="button"
                onClick={handleNext}
                style={{ flex: 1, padding: '14px', borderRadius: 8, border: 'none', background: '#007bff', color: '#fff', fontSize: '1.1rem', fontWeight: 600, cursor: 'pointer' }}
              >
                {t('checkout.next')}
              </button>
            ) : (
              <button
                type="submit"
                disabled={submitting}
                style={{
                  flex: 1, padding: '14px', borderRadius: 8, border: 'none',
                  background: submitting ? '#ccc' : '#007bff', color: '#fff',
                  fontSize: '1.1rem', fontWeight: 600, cursor: submitting ? 'default' : 'pointer'
                }}
              >
                {submitting ? t('common.submitting') : t('checkout.confirm_order')}
              </button>
            )}
          </div>
        </form>
        {/* Підсумок замовлення */}
        <div style={{ position: 'sticky', top: 20 }}>
          <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)' }}>
            <h2 style={{ fontSize: '1.2rem', marginBottom: 20, borderBottom: '1px solid #eee', paddingBottom: 10 }}>{t('checkout.your_order')}</h2>
            <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
              {summaryItems.map((item) => {
                // Визначення ціни: з calcPreview (якщо є) або з кошика
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
              {promoDiscount > 0 && (
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                  <span style={{ color: '#28a745' }}>{t('cart.discount')}</span>
                  <span style={{ color: '#28a745' }}>-{promoDiscount.toFixed(2)} {t('common.currency')}</span>
                </div>
              )}
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
