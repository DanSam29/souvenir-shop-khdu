import React, { useState, useEffect, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { usersAPI, ordersAPI, promotionsAPI, novaPoshtaAPI } from '../services/api';
import { useAuth } from '../contexts/AuthContext';

// Дебаунс для автокомпліту Нової Пошти
const NP_DEBOUNCE_MS = 300;

// Лінійний життєвий цикл замовлення (без врахування скасування)
const ORDER_STEPS = ['Processing', 'Paid', 'Shipped', 'Delivered'];
const STATUS_COLORS = {
  Processing: { bg: '#e6f7ff', fg: '#1890ff', border: '#91d5ff' },
  Pending: { bg: '#fff7e6', fg: '#d46b08', border: '#ffd591' },
  Paid: { bg: '#e6ffed', fg: '#28a745', border: '#b7eb8f' },
  Shipped: { bg: '#e6f7ff', fg: '#1890ff', border: '#91d5ff' },
  Delivered: { bg: '#e6ffed', fg: '#28a745', border: '#b7eb8f' },
  Cancelled: { bg: '#fff1f0', fg: '#cf1322', border: '#ffa39e' }
};

const EMPTY_ADDRESS = {
  city: '',
  cityRef: '',
  warehouseNumber: '',
  warehouseRef: '',
  recipientName: '',
  recipientPhone: '',
  isDefault: false
};

// Безпечне виконання запиту: помилка одного ендпоінта не зриває загальне завантаження
const safe = (promise) => promise.catch(() => null);

// Стилі випадаючого списку підказок Нової Пошти
const npDropdownStyle = {
  position: 'absolute', top: '100%', left: 0, right: 0, background: '#fff',
  border: '1px solid #ddd', borderRadius: '0 0 8px 8px', listStyle: 'none',
  padding: 0, margin: '-10px 0 0 0', zIndex: 10, maxHeight: 200,
  overflowY: 'auto', boxShadow: '0 4px 12px rgba(0,0,0,0.1)'
};
const npItemStyle = { padding: '10px 12px', cursor: 'pointer', borderBottom: '1px solid #eee', background: '#fff' };

// Прогрес-бар статусів замовлення
function StatusProgress({ status, t }) {
  if (status === 'Cancelled') {
    const c = STATUS_COLORS.Cancelled;
    return (
      <span style={{ padding: '4px 10px', borderRadius: 20, fontSize: '0.85rem', background: c.bg, color: c.fg, border: `1px solid ${c.border}` }}>
        {t('order.status_Cancelled')}
      </span>
    );
  }
  const currentIndex = ORDER_STEPS.indexOf(status);
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 4, width: '100%' }}>
      {ORDER_STEPS.map((step, i) => {
        const done = currentIndex >= 0 && i <= currentIndex;
        return (
          <React.Fragment key={step}>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minWidth: 62 }}>
              <div style={{
                width: 20, height: 20, borderRadius: '50%', fontSize: '0.7rem',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                background: done ? '#28a745' : '#e0e0e0', color: '#fff'
              }}>
                {done ? '✓' : ''}
              </div>
              <small style={{ fontSize: '0.7rem', textAlign: 'center', color: done ? '#28a745' : '#999' }}>
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

function ProfilePage() {
  const { logout } = useAuth();
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [orders, setOrders] = useState([]);
  const [promos, setPromos] = useState([]);
  const [promoHistory, setPromoHistory] = useState([]);
  const [addresses, setAddresses] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('profile');
  const [editing, setEditing] = useState(false);
  const [formData, setForm] = useState({ firstName: '', lastName: '', phone: '' });
  const [passData, setPass] = useState({ oldPassword: '', newPassword: '' });
  const [msg, setMsg] = useState({ type: '', text: '' });
  // Стан для керування адресами
  const [addressForm, setAddressForm] = useState(EMPTY_ADDRESS);
  const [editingAddressId, setEditingAddressId] = useState(null);
  const [showAddressForm, setShowAddressForm] = useState(false);
  const [studentBusy, setStudentBusy] = useState(false);
  // Nova Poshta: підказки міста/відділення у формі адреси
  const [npCities, setNpCities] = useState([]);
  const [npWarehouses, setNpWarehouses] = useState([]);
  const [showNpCityList, setShowNpCityList] = useState(false);
  const [showNpWarehouseList, setShowNpWarehouseList] = useState(false);
  const [cityTyping, setCityTyping] = useState(false);
  const [warehouseTyping, setWarehouseTyping] = useState(false);

  // Пошук міста Нової Пошти (лише коли користувач вводить назву вручну)
  useEffect(() => {
    if (!cityTyping || addressForm.cityRef || (addressForm.city || '').trim().length < 2) {
      setNpCities([]);
      setShowNpCityList(false);
      return;
    }
    const timer = setTimeout(async () => {
      try {
        const res = await novaPoshtaAPI.getCities(addressForm.city.trim());
        setNpCities(res.data || []);
        setShowNpCityList(true);
      } catch {
        setNpCities([]);
        setShowNpCityList(false);
      }
    }, NP_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [addressForm.city, addressForm.cityRef, cityTyping]);

  // Пошук відділення Нової Пошти за вибраним містом
  useEffect(() => {
    if (!warehouseTyping || !addressForm.cityRef || addressForm.warehouseRef) {
      setNpWarehouses([]);
      setShowNpWarehouseList(false);
      return;
    }
    const timer = setTimeout(async () => {
      try {
        const res = await novaPoshtaAPI.getWarehouses(addressForm.cityRef, addressForm.warehouseNumber || '');
        setNpWarehouses(res.data || []);
        setShowNpWarehouseList(true);
      } catch {
        setNpWarehouses([]);
        setShowNpWarehouseList(false);
      }
    }, NP_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [addressForm.cityRef, addressForm.warehouseNumber, addressForm.warehouseRef, warehouseTyping]);

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      const [uRes, oRes, pRes, hRes, aRes] = await Promise.all([
        usersAPI.getCurrentUser(),
        safe(ordersAPI.getOrders()),
        safe(promotionsAPI.getMyPromotions()),
        safe(promotionsAPI.getMyPromotionsHistory()),
        safe(usersAPI.getAddresses())
      ]);
      setUser(uRes.data);
      setOrders(oRes?.data || []);
      setPromos(pRes?.data || []);
      setPromoHistory(hRes?.data || []);
      setAddresses(aRes?.data || []);
      setForm({
        firstName: uRes.data.firstName,
        lastName: uRes.data.lastName,
        phone: uRes.data.phone || ''
      });
    } catch (err) {
      console.error('Помилка завантаження даних:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleUpdateProfile = async (e) => {
    e.preventDefault();
    try {
      await usersAPI.updateProfile(formData);
      setMsg({ type: 'success', text: t('profile.update_success') });
      setEditing(false);
      loadData();
    } catch (err) {
      setMsg({ type: 'error', text: t('profile.update_error') });
    }
  };

  const handleChangePassword = async (e) => {
    e.preventDefault();
    try {
      await usersAPI.changePassword(passData);
      setMsg({ type: 'success', text: t('profile.password_success') });
      setPass({ oldPassword: '', newPassword: '' });
    } catch (err) {
      setMsg({ type: 'error', text: err.response?.data?.error || t('profile.password_error') });
    }
  };

  // ---- Адреси ----
  const openAddressForm = (address) => {
    setCityTyping(false);
    setWarehouseTyping(false);
    setNpCities([]);
    setNpWarehouses([]);
    if (address) {
      setEditingAddressId(address.shippingId);
      setAddressForm({
        city: address.city || '',
        cityRef: address.cityRef || '',
        warehouseNumber: address.warehouseNumber || '',
        warehouseRef: address.warehouseRef || '',
        recipientName: address.recipientName || '',
        recipientPhone: address.recipientPhone || '',
        isDefault: !!address.isDefault
      });
    } else {
      setEditingAddressId(null);
      setAddressForm(EMPTY_ADDRESS);
    }
    setShowAddressForm(true);
  };

  const closeAddressForm = () => {
    setShowAddressForm(false);
    setAddressForm(EMPTY_ADDRESS);
    setEditingAddressId(null);
    setCityTyping(false);
    setWarehouseTyping(false);
    setNpCities([]);
    setNpWarehouses([]);
  };

  const handleCityInput = (value) => {
    setCityTyping(true);
    setAddressForm((prev) => ({ ...prev, city: value, cityRef: '', warehouseNumber: '', warehouseRef: '' }));
  };

  const handleCitySelect = (city) => {
    setCityTyping(false);
    setShowNpCityList(false);
    setAddressForm((prev) => ({ ...prev, city: city.description, cityRef: city.ref, warehouseNumber: '', warehouseRef: '' }));
  };

  const handleWarehouseInput = (value) => {
    setWarehouseTyping(true);
    setAddressForm((prev) => ({ ...prev, warehouseNumber: value, warehouseRef: '' }));
  };

  const handleWarehouseSelect = (wh) => {
    setWarehouseTyping(false);
    setShowNpWarehouseList(false);
    setAddressForm((prev) => ({ ...prev, warehouseNumber: wh.description, warehouseRef: wh.ref }));
  };

  const handleSaveAddress = async (e) => {
    e.preventDefault();
    if (!addressForm.city || !addressForm.warehouseNumber || !addressForm.recipientName || !addressForm.recipientPhone) {
      setMsg({ type: 'error', text: t('common.load_error') });
      return;
    }
    try {
      if (editingAddressId) {
        await usersAPI.updateAddress(editingAddressId, addressForm);
      } else {
        await usersAPI.createAddress(addressForm);
      }
      setMsg({ type: 'success', text: t('profile.address_saved') });
      closeAddressForm();
      const aRes = await safe(usersAPI.getAddresses());
      setAddresses(aRes?.data || []);
    } catch (err) {
      setMsg({ type: 'error', text: t('common.update_error') });
    }
  };

  const handleDeleteAddress = async (id) => {
    if (!window.confirm(t('profile.address_delete_confirm'))) return;
    try {
      await usersAPI.deleteAddress(id);
      setMsg({ type: 'success', text: t('profile.address_deleted') });
      const aRes = await safe(usersAPI.getAddresses());
      setAddresses(aRes?.data || []);
    } catch (err) {
      setMsg({ type: 'error', text: t('common.remove_error') });
    }
  };

  const handleSetDefaultAddress = async (id) => {
    try {
      await usersAPI.setDefaultAddress(id);
      const aRes = await safe(usersAPI.getAddresses());
      setAddresses(aRes?.data || []);
    } catch (err) {
      setMsg({ type: 'error', text: t('common.update_error') });
    }
  };

  // ---- Студентський статус ----
  const handleVerifyStudent = async () => {
    setStudentBusy(true);
    setMsg({ type: '', text: '' });
    try {
      const res = await usersAPI.verifyStudentStatus();
      setUser((prev) => ({ ...prev, ...res.data }));
      setMsg({ type: 'success', text: t('profile.student_refresh_success') });
    } catch (err) {
      setMsg({ type: 'error', text: err.response?.data?.message || t('profile.student_refresh_error') });
    } finally {
      setStudentBusy(false);
    }
  };

  if (loading || !user) return <div className="loading">{t('common.loading')}</div>;

  const isEn = i18n.language === 'en';
  const studentLabel = user.studentStatus && user.studentStatus !== 'NONE'
    ? t(`profile.student_status_${user.studentStatus}`, { defaultValue: user.studentStatus })
    : t('profile.status_none');
  // Кількість днів до закінчення студентського статусу (для нагадування у профілі)
  const daysUntilExpiry = (user.studentStatus && user.studentStatus !== 'NONE' && user.studentExpiresAt)
    ? Math.ceil((new Date(user.studentExpiresAt) - new Date()) / 86400000)
    : null;

  const cardStyle = { background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)' };
  const inputStyle = { width: '100%', padding: 10, marginBottom: 10, borderRadius: 6, border: '1px solid #ddd' };
  const tabs = [
    { id: 'profile', label: t('profile.tabs.profile') },
    { id: 'security', label: t('profile.tabs.security') },
    { id: 'orders', label: t('profile.tabs.orders') },
    { id: 'addresses', label: t('profile.tabs.addresses') },
    { id: 'discounts', label: t('profile.tabs.discounts') },
    { id: 'student', label: t('profile.tabs.student') }
  ];

  return (
    <div className="profile-page" style={{ maxWidth: 1000, margin: '40px auto', padding: '0 20px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>{t('profile.title')}</h1>
        <button onClick={handleLogout} style={{ padding: '10px 18px', borderRadius: 6, border: 'none', background: '#dc3545', color: '#fff', fontWeight: 600, cursor: 'pointer' }}>
          {t('profile.logout')}
        </button>
      </div>

      {msg.text && (
        <div style={{ padding: 15, margin: '15px 0', borderRadius: 8, background: msg.type === 'success' ? '#e6ffed' : '#fff1f0', border: `1px solid ${msg.type === 'success' ? '#b7eb8f' : '#ffa39e'}`, color: msg.type === 'success' ? '#1e4620' : '#cf1322' }}>
          {msg.text}
        </div>
      )}

      {/* Вкладки */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, margin: '20px 0', borderBottom: '1px solid #eee', paddingBottom: 12 }}>
        {tabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => { setActiveTab(tab.id); setMsg({ type: '', text: '' }); }}
            style={{
              padding: '8px 16px', borderRadius: 20, cursor: 'pointer', fontWeight: 500,
              border: activeTab === tab.id ? '1px solid #007bff' : '1px solid #ddd',
              background: activeTab === tab.id ? '#007bff' : '#fff',
              color: activeTab === tab.id ? '#fff' : '#333'
            }}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Вкладка: Профіль */}
      {activeTab === 'profile' && (
        <div style={cardStyle}>
          <h2 style={{ fontSize: '1.2rem', marginBottom: 20 }}>{t('profile.my_profile')}</h2>
          {!editing ? (
            <div>
              <p><strong>{t('profile.first_name')}:</strong> {user.firstName} {user.lastName}</p>
              <p><strong>{t('profile.email')}:</strong> {user.email}</p>
              <p><strong>{t('profile.phone')}:</strong> {user.phone || t('profile.not_specified')}</p>
              <p><strong>{t('profile.status')}:</strong>{' '}
                <span style={{ color: user.studentStatus !== 'NONE' ? '#28a745' : '#666', fontWeight: 600 }}>{studentLabel}</span>
              </p>
              <button onClick={() => setEditing(true)} style={{ marginTop: 15, padding: '10px 20px', borderRadius: 6, border: '1px solid #007bff', background: 'none', color: '#007bff', cursor: 'pointer' }}>
                {t('profile.edit')}
              </button>
            </div>
          ) : (
            <form onSubmit={handleUpdateProfile}>
              <input type="text" value={formData.firstName} onChange={e => setForm({ ...formData, firstName: e.target.value })} placeholder={t('profile.first_name')} style={inputStyle} required />
              <input type="text" value={formData.lastName} onChange={e => setForm({ ...formData, lastName: e.target.value })} placeholder={t('profile.last_name')} style={inputStyle} required />
              <input type="text" value={formData.phone} onChange={e => setForm({ ...formData, phone: e.target.value })} placeholder={t('profile.phone')} style={inputStyle} />
              <div style={{ display: 'flex', gap: 10 }}>
                <button type="submit" style={{ flex: 1, padding: 10, borderRadius: 6, border: 'none', background: '#28a745', color: '#fff', cursor: 'pointer' }}>{t('common.save')}</button>
                <button type="button" onClick={() => setEditing(false)} style={{ flex: 1, padding: 10, borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: 'pointer' }}>{t('common.cancel')}</button>
              </div>
            </form>
          )}
        </div>
      )}

      {/* Вкладка: Безпека */}
      {activeTab === 'security' && (
        <div style={cardStyle}>
          <h2 style={{ fontSize: '1.2rem', marginBottom: 20 }}>{t('profile.change_password')}</h2>
          <form onSubmit={handleChangePassword} style={{ maxWidth: 420 }}>
            <input type="password" value={passData.oldPassword} onChange={e => setPass({ ...passData, oldPassword: e.target.value })} placeholder={t('profile.old_password')} style={inputStyle} required />
            <input type="password" value={passData.newPassword} onChange={e => setPass({ ...passData, newPassword: e.target.value })} placeholder={t('profile.new_password')} style={inputStyle} minLength={8} required />
            <button type="submit" style={{ width: '100%', padding: 10, borderRadius: 6, border: 'none', background: '#666', color: '#fff', cursor: 'pointer' }}>{t('profile.update_password')}</button>
          </form>
        </div>
      )}

      {/* Вкладка: Замовлення */}
      {activeTab === 'orders' && (
        <div>
          {orders.length === 0 ? (
            <div style={{ ...cardStyle, textAlign: 'center' }}>
              <p style={{ color: '#888' }}>{t('profile.no_orders')}</p>
              <Link to="/" style={{ color: '#007bff' }}>{t('profile.go_shopping')}</Link>
            </div>
          ) : (
            <div style={{ display: 'grid', gap: 16 }}>
              {orders.map(order => {
                const c = STATUS_COLORS[order.status] || STATUS_COLORS.Processing;
                return (
                  <div key={order.orderId} style={cardStyle}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 10 }}>
                      <div>
                        <strong style={{ fontSize: '1.05rem' }}>{order.orderNumber}</strong>
                        <div style={{ color: '#888', fontSize: '0.85rem' }}>{new Date(order.createdAt).toLocaleDateString()}</div>
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                        <span style={{ fontWeight: 700 }}>{order.totalAmount.toFixed(2)} {t('common.currency')}</span>
                        <span style={{ padding: '4px 10px', borderRadius: 20, fontSize: '0.85rem', background: c.bg, color: c.fg, border: `1px solid ${c.border}` }}>
                          {t(`order.status_${order.status}`, { defaultValue: order.status })}
                        </span>
                      </div>
                    </div>
                    <div style={{ margin: '18px 0' }}>
                      <StatusProgress status={order.status} t={t} />
                    </div>
                    <Link to={`/order/${order.orderId}`} state={{ from: 'profile' }} style={{ color: '#007bff', textDecoration: 'none', fontSize: '0.9rem' }}>
                      {t('profile.details')} →
                    </Link>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* Вкладка: Адреси */}
      {activeTab === 'addresses' && (
        <div style={cardStyle}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
            <h2 style={{ fontSize: '1.2rem', margin: 0 }}>{t('common.addresses')}</h2>
            {!showAddressForm && (
              <button onClick={() => openAddressForm(null)} style={{ padding: '8px 16px', borderRadius: 6, border: 'none', background: '#007bff', color: '#fff', cursor: 'pointer' }}>
                + {t('profile.address_add')}
              </button>
            )}
          </div>

          {showAddressForm && (
            <form onSubmit={handleSaveAddress} style={{ maxWidth: 520, marginBottom: 24, padding: 16, background: '#f8f9fa', borderRadius: 8 }}>
              <div style={{ position: 'relative' }}>
                <input type="text" value={addressForm.city} onChange={e => handleCityInput(e.target.value)} placeholder={t('profile.address_city')} style={inputStyle} required />
                {showNpCityList && npCities.length > 0 && (
                  <ul style={npDropdownStyle}>
                    {npCities.map(city => (
                      <li key={city.ref} onClick={() => handleCitySelect(city)} style={npItemStyle}
                        onMouseEnter={(e) => e.target.style.background = '#f5f5f5'}
                        onMouseLeave={(e) => e.target.style.background = '#fff'}>
                        {city.description} <small style={{ color: '#888' }}>({city.areaDescription})</small>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              <div style={{ position: 'relative' }}>
                <input type="text" value={addressForm.warehouseNumber} onChange={e => handleWarehouseInput(e.target.value)} disabled={!addressForm.cityRef} placeholder={addressForm.cityRef ? t('profile.address_warehouse_search') : t('checkout.select_city_first')} style={{ ...inputStyle, background: addressForm.cityRef ? '#fff' : '#f0f0f0' }} required />
                {showNpWarehouseList && npWarehouses.length > 0 && (
                  <ul style={npDropdownStyle}>
                    {npWarehouses.map(wh => (
                      <li key={wh.ref} onClick={() => handleWarehouseSelect(wh)} style={npItemStyle}
                        onMouseEnter={(e) => e.target.style.background = '#f5f5f5'}
                        onMouseLeave={(e) => e.target.style.background = '#fff'}>
                        {wh.description}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              <input type="text" value={addressForm.recipientName} onChange={e => setAddressForm({ ...addressForm, recipientName: e.target.value })} placeholder={t('profile.address_recipient')} style={inputStyle} required />
              <input type="tel" value={addressForm.recipientPhone} onChange={e => setAddressForm({ ...addressForm, recipientPhone: e.target.value })} placeholder="+380501234567" style={inputStyle} required />
              <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
                <input type="checkbox" checked={addressForm.isDefault} onChange={e => setAddressForm({ ...addressForm, isDefault: e.target.checked })} />
                {t('profile.address_is_default')}
              </label>
              <div style={{ display: 'flex', gap: 10 }}>
                <button type="submit" style={{ flex: 1, padding: 10, borderRadius: 6, border: 'none', background: '#28a745', color: '#fff', cursor: 'pointer' }}>{t('common.save')}</button>
                <button type="button" onClick={closeAddressForm} style={{ flex: 1, padding: 10, borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: 'pointer' }}>{t('common.cancel')}</button>
              </div>
            </form>
          )}

          {addresses.length === 0 && !showAddressForm ? (
            <p style={{ color: '#888' }}>{t('profile.no_addresses')}</p>
          ) : (
            <div style={{ display: 'grid', gap: 12 }}>
              {addresses.map(a => (
                <div key={a.shippingId} style={{ padding: 16, borderRadius: 8, border: a.isDefault ? '1px solid #91d5ff' : '1px solid #eee', background: a.isDefault ? '#f0f7ff' : '#fff' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 8 }}>
                    <div>
                      <strong>{a.recipientName}</strong> · {a.recipientPhone}
                      {a.isDefault && <span style={{ marginLeft: 8, padding: '2px 8px', borderRadius: 12, fontSize: '0.75rem', background: '#e6f7ff', color: '#1890ff' }}>{t('profile.address_default')}</span>}
                      <div style={{ color: '#666', marginTop: 4 }}>{t('checkout.city_prefix')} {a.city}, {a.warehouseNumber}</div>
                    </div>
                    <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
                      {!a.isDefault && (
                        <button onClick={() => handleSetDefaultAddress(a.shippingId)} style={{ padding: '6px 10px', borderRadius: 6, border: '1px solid #007bff', background: 'none', color: '#007bff', cursor: 'pointer', fontSize: '0.85rem' }}>
                          {t('profile.address_set_default')}
                        </button>
                      )}
                      <button onClick={() => openAddressForm(a)} style={{ padding: '6px 10px', borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: 'pointer', fontSize: '0.85rem' }}>
                        {t('profile.address_edit')}
                      </button>
                      <button onClick={() => handleDeleteAddress(a.shippingId)} style={{ padding: '6px 10px', borderRadius: 6, border: '1px solid #ffa39e', background: '#fff', color: '#cf1322', cursor: 'pointer', fontSize: '0.85rem' }}>
                        {t('profile.address_delete')}
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Вкладка: Знижки */}
      {activeTab === 'discounts' && (
        <div style={{ display: 'grid', gap: 20 }}>
          <div style={cardStyle}>
            <h2 style={{ fontSize: '1.2rem', marginBottom: 20 }}>{t('profile.discounts_available')}</h2>
            {promos.length === 0 ? <p style={{ color: '#888' }}>{t('profile.no_discounts')}</p> : (
              <ul style={{ listStyle: 'none', padding: 0 }}>
                {promos.map(p => {
                  const displayName = (isEn && p.nameEn) ? p.nameEn : p.name;
                  const displayDesc = (isEn && p.descriptionEn) ? p.descriptionEn : p.description;
                  return (
                    <li key={p.promotionId} style={{ marginBottom: 12, padding: 12, background: '#f8f9fa', borderRadius: 8, borderLeft: '4px solid #28a745' }}>
                      <strong>{displayName}</strong>
                      <p style={{ margin: '4px 0', fontSize: '0.9rem' }}>{displayDesc}</p>
                      <span style={{ color: '#28a745', fontWeight: 700 }}>-{p.type === 'PERCENTAGE' ? `${p.value}%` : `${p.value} ${t('common.currency')}`}</span>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
          <div style={cardStyle}>
            <h2 style={{ fontSize: '1.2rem', marginBottom: 20 }}>{t('profile.discounts_history')}</h2>
            {promoHistory.length === 0 ? <p style={{ color: '#888' }}>{t('profile.no_discounts_history')}</p> : (
              <ul style={{ listStyle: 'none', padding: 0 }}>
                {promoHistory.map(h => {
                  const displayName = (isEn && h.nameEn) ? h.nameEn : h.name;
                  return (
                    <li key={h.userPromotionId} style={{ marginBottom: 10, padding: 12, background: '#fff', border: '1px solid #eee', borderRadius: 8 }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 8 }}>
                        <strong>{displayName}</strong>
                        <span style={{ color: '#666', fontSize: '0.85rem' }}>{t('profile.student_assigned_at')}: {h.assignedAt ? new Date(h.assignedAt).toLocaleDateString() : '—'}</span>
                      </div>
                      <div style={{ fontSize: '0.9rem', color: '#555', marginTop: 4 }}>
                        -{h.type === 'PERCENTAGE' ? `${h.value}%` : `${h.value} ${t('common.currency')}`}
                        {typeof h.usedCount === 'number' && <> · {t('profile.discount_used')}: {h.usedCount}</>}
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
        </div>
      )}

      {/* Вкладка: Студентський статус */}
      {activeTab === 'student' && (
        <div style={cardStyle}>
          <h2 style={{ fontSize: '1.2rem', marginBottom: 20 }}>{t('profile.student_title')}</h2>
          <p><strong>{t('profile.status')}:</strong>{' '}
            <span style={{ color: user.studentStatus !== 'NONE' ? '#28a745' : '#666', fontWeight: 600 }}>{studentLabel}</span>
          </p>
          {user.gpa != null && <p><strong>{t('profile.student_gpa')}:</strong> {user.gpa}</p>}
          {user.studentVerifiedAt && <p><strong>{t('profile.student_verified_at')}:</strong> {new Date(user.studentVerifiedAt).toLocaleDateString()}</p>}
          {user.studentExpiresAt && <p><strong>{t('profile.expires_at')}:</strong> {new Date(user.studentExpiresAt).toLocaleDateString()}</p>}
          {daysUntilExpiry != null && daysUntilExpiry >= 0 && (
            <p style={{ color: daysUntilExpiry <= 7 ? '#d48806' : '#666', fontWeight: daysUntilExpiry <= 7 ? 600 : 400 }}>
              {t('profile.student_expires_in_days', { days: daysUntilExpiry })}
            </p>
          )}
          <p style={{ color: '#888', fontSize: '0.9rem' }}>{t('profile.student_hint')}</p>
          <button
            onClick={handleVerifyStudent}
            disabled={studentBusy}
            style={{ marginTop: 10, padding: '10px 20px', borderRadius: 6, border: 'none', background: '#007bff', color: '#fff', cursor: 'pointer', opacity: studentBusy ? 0.6 : 1 }}
          >
            {studentBusy ? t('profile.student_refreshing') : t('profile.student_refresh')}
          </button>
        </div>
      )}
    </div>
  );
}

export default ProfilePage;
