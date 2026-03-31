import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { usersAPI, ordersAPI, promotionsAPI } from '../services/api';

function ProfilePage() {
  const [user, setUser] = useState(null);
  const [orders, setOrders] = useState([]);
  const [promos, setPromos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [formData, setForm] = useState({ firstName: '', lastName: '', phone: '' });
  const [passData, setPass] = useState({ oldPassword: '', newPassword: '' });
  const [msg, setMsg] = useState({ type: '', text: '' });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [uRes, oRes, pRes] = await Promise.all([
        usersAPI.getCurrentUser(),
        ordersAPI.getOrders(),
        promotionsAPI.getMy()
      ]);
      setUser(uRes.data);
      setOrders(oRes.data);
      setPromos(pRes.data);
      setForm({
        firstName: uRes.data.firstName,
        lastName: uRes.data.lastName,
        phone: uRes.data.phone || ''
      });
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateProfile = async (e) => {
    e.preventDefault();
    try {
      await usersAPI.updateProfile(formData);
      setMsg({ type: 'success', text: 'Профіль успішно оновлено!' });
      setEditing(false);
      loadData();
    } catch (err) {
      setMsg({ type: 'error', text: 'Помилка при оновленні профілю' });
    }
  };

  const handleChangePassword = async (e) => {
    e.preventDefault();
    try {
      await usersAPI.changePassword(passData);
      setMsg({ type: 'success', text: 'Пароль успішно змінено!' });
      setPass({ oldPassword: '', newPassword: '' });
    } catch (err) {
      setMsg({ type: 'error', text: err.response?.data?.error || 'Помилка при зміні пароля' });
    }
  };

  if (loading) return <div className="loading">Завантаження...</div>;

  return (
    <div className="profile-page" style={{ maxWidth: 1000, margin: '40px auto', padding: '0 20px' }}>
      <h1>Особистий кабінет</h1>
      
      {msg.text && (
        <div style={{ padding: 15, marginBottom: 20, borderRadius: 8, background: msg.type === 'success' ? '#e6ffed' : '#fff1f0', border: `1px solid ${msg.type === 'success' ? '#b7eb8f' : '#ffa39e'}`, color: msg.type === 'success' ? '#1e4620' : '#cf1322' }}>
          {msg.text}
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: 30 }}>
        {/* Ліва колонка: Профіль */}
        <aside>
          <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', marginBottom: 20 }}>
            <h2 style={{ fontSize: '1.2rem', marginBottom: 20 }}>Мій профіль</h2>
            
            {!editing ? (
              <div>
                <p><strong>Ім'я:</strong> {user.firstName} {user.lastName}</p>
                <p><strong>Email:</strong> {user.email}</p>
                <p><strong>Телефон:</strong> {user.phone || 'не вказано'}</p>
                <p><strong>Статус:</strong> <span style={{ color: user.studentStatus !== 'NONE' ? '#28a745' : '#666', fontWeight: 600 }}>{user.studentStatus}</span></p>
                {user.studentExpiresAt && <p><small>Дійсний до: {new Date(user.studentExpiresAt).toLocaleDateString()}</small></p>}
                <button onClick={() => setEditing(true)} style={{ width: '100%', marginTop: 15, padding: '10px', borderRadius: 6, border: '1px solid #007bff', background: 'none', color: '#007bff', cursor: 'pointer' }}>Редагувати</button>
              </div>
            ) : (
              <form onSubmit={handleUpdateProfile}>
                <input type="text" value={formData.firstName} onChange={e => setForm({...formData, firstName: e.target.value})} placeholder="Ім'я" style={{ width: '100%', padding: 10, marginBottom: 10, borderRadius: 6, border: '1px solid #ddd' }} required />
                <input type="text" value={formData.lastName} onChange={e => setForm({...formData, lastName: e.target.value})} placeholder="Прізвище" style={{ width: '100%', padding: 10, marginBottom: 10, borderRadius: 6, border: '1px solid #ddd' }} required />
                <input type="text" value={formData.phone} onChange={e => setForm({...formData, phone: e.target.value})} placeholder="Телефон" style={{ width: '100%', padding: 10, marginBottom: 10, borderRadius: 6, border: '1px solid #ddd' }} />
                <div style={{ display: 'flex', gap: 10 }}>
                  <button type="submit" style={{ flex: 1, padding: 10, borderRadius: 6, border: 'none', background: '#28a745', color: '#fff', cursor: 'pointer' }}>Зберегти</button>
                  <button type="button" onClick={() => setEditing(false)} style={{ flex: 1, padding: 10, borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: 'pointer' }}>Скасувати</button>
                </div>
              </form>
            )}
          </div>

          <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', marginBottom: 20 }}>
            <h2 style={{ fontSize: '1.2rem', marginBottom: 20 }}>Зміна пароля</h2>
            <form onSubmit={handleChangePassword}>
              <input type="password" value={passData.oldPassword} onChange={e => setPass({...passData, oldPassword: e.target.value})} placeholder="Старий пароль" style={{ width: '100%', padding: 10, marginBottom: 10, borderRadius: 6, border: '1px solid #ddd' }} required />
              <input type="password" value={passData.newPassword} onChange={e => setPass({...passData, newPassword: e.target.value})} placeholder="Новий пароль" style={{ width: '100%', padding: 10, marginBottom: 15, borderRadius: 6, border: '1px solid #ddd' }} required />
              <button type="submit" style={{ width: '100%', padding: 10, borderRadius: 6, border: 'none', background: '#666', color: '#fff', cursor: 'pointer' }}>Оновити пароль</button>
            </form>
          </div>

          <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)' }}>
            <h2 style={{ fontSize: '1.2rem', marginBottom: 20 }}>Мої знижки</h2>
            {promos.length === 0 ? <p style={{ color: '#888' }}>Активних знижок немає</p> : (
              <ul style={{ listStyle: 'none', padding: 0 }}>
                {promos.map(p => (
                  <li key={p.promotionId} style={{ marginBottom: 12, padding: 10, background: '#f8f9fa', borderRadius: 8, borderLeft: '4px solid #28a745' }}>
                    <strong>{p.name}</strong>
                    <p style={{ margin: '4px 0', fontSize: '0.9rem' }}>{p.description}</p>
                    <span style={{ color: '#28a745', fontWeight: 700 }}>-{p.type === 'PERCENTAGE' ? `${p.value}%` : `${p.value} грн`}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </aside>

        {/* Права колонка: Замовлення */}
        <main>
          <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)' }}>
            <h2 style={{ fontSize: '1.2rem', marginBottom: 20 }}>Історія замовлень</h2>
            {orders.length === 0 ? (
              <div style={{ textAlign: 'center', padding: '40px 0' }}>
                <p style={{ color: '#888' }}>Ви ще не робили замовлень</p>
                <Link to="/" style={{ color: '#007bff' }}>Перейти до покупок</Link>
              </div>
            ) : (
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ borderBottom: '2px solid #eee', textAlign: 'left' }}>
                      <th style={{ padding: '12px 8px' }}>Номер</th>
                      <th style={{ padding: '12px 8px' }}>Дата</th>
                      <th style={{ padding: '12px 8px' }}>Сума</th>
                      <th style={{ padding: '12px 8px' }}>Статус</th>
                      <th style={{ padding: '12px 8px' }}>Дії</th>
                    </tr>
                  </thead>
                  <tbody>
                    {orders.map(order => (
                      <tr key={order.orderId} style={{ borderBottom: '1px solid #f0f0f0' }}>
                        <td style={{ padding: '15px 8px', fontWeight: 500 }}>{order.orderNumber}</td>
                        <td style={{ padding: '15px 8px', color: '#666' }}>{new Date(order.createdAt).toLocaleDateString()}</td>
                        <td style={{ padding: '15px 8px', fontWeight: 600 }}>{order.totalAmount.toFixed(2)} грн</td>
                        <td style={{ padding: '15px 8px' }}>
                          <span style={{ 
                            padding: '4px 10px', 
                            borderRadius: 20, 
                            fontSize: '0.85rem',
                            background: order.status === 'Paid' ? '#e6ffed' : (order.status === 'Cancelled' ? '#fff1f0' : '#e6f7ff'),
                            color: order.status === 'Paid' ? '#28a745' : (order.status === 'Cancelled' ? '#cf1322' : '#1890ff'),
                            border: `1px solid ${order.status === 'Paid' ? '#b7eb8f' : (order.status === 'Cancelled' ? '#ffa39e' : '#91d5ff')}`
                          }}>
                            {order.status}
                          </span>
                        </td>
                        <td style={{ padding: '15px 8px' }}>
                          <Link to={`/order/${order.orderId}`} style={{ color: '#007bff', textDecoration: 'none', fontSize: '0.9rem' }}>Деталі →</Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </main>
      </div>
    </div>
  );
}

export default ProfilePage;