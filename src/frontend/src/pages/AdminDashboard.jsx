import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ordersAPI, adminUsersAPI, warehouseAPI, integrationsAPI } from '../services/api';

function AdminDashboard() {
  const [stats, setStats] = useState({ orders: 0, users: 0, stock: 0 });
  const [integrations, setIntegrations] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadStats = async () => {
      try {
        const [oRes, uRes, sRes, iRes] = await Promise.all([
          ordersAPI.getAll(),
          adminUsersAPI.getAll(),
          warehouseAPI.getStock(),
          integrationsAPI.getStatus()
        ]);
        setStats({
          orders: oRes.data.length,
          users: uRes.data.length,
          stock: sRes.data.length
        });
        setIntegrations(iRes.data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    loadStats();
  }, []);

  if (loading) return <div>Завантаження...</div>;

  return (
    <div className="admin-dashboard" style={{ padding: 20 }}>
      <h1>Панель адміністратора</h1>
      
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 20, marginTop: 30 }}>
        <div style={statCardStyle}>
          <h3>Замовлення</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>{stats.orders}</p>
          <Link to="/admin/orders">Керувати →</Link>
        </div>
        <div style={statCardStyle}>
          <h3>Користувачі</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>{stats.users}</p>
          <Link to="/admin/users">Керувати →</Link>
        </div>
        <div style={statCardStyle}>
          <h3>Товари на складі</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>{stats.stock}</p>
          <Link to="/admin/warehouse">Склад →</Link>
        </div>
        <div style={statCardStyle}>
          <h3>Компанії</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>🏢</p>
          <Link to="/admin/companies">Постачальники →</Link>
        </div>
        <div style={statCardStyle}>
          <h3>Аналітика</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>📊</p>
          <Link to="/admin/analytics">Звіти →</Link>
        </div>
      </div>

      <h2 style={{ marginTop: 40 }}>Статус інтеграцій</h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: 20, marginTop: 20 }}>
        {integrations && (
          <>
            <div style={integrationCardStyle(integrations.stripe.keyConfigured)}>
              <h4>Stripe (Оплати)</h4>
              <p>Статус: {integrations.stripe.keyConfigured ? '✅ Налаштовано' : '❌ Потрібна конфігурація'}</p>
            </div>
            <div style={integrationCardStyle(integrations.novaPoshta.keyConfigured)}>
              <h4>Нова Пошта (Доставка)</h4>
              <p>Статус: {integrations.novaPoshta.keyConfigured ? '✅ Налаштовано' : '❌ Потрібна конфігурація'}</p>
            </div>
            <div style={integrationCardStyle(true)}>
              <h4>University API</h4>
              <p>Активних доменів: {integrations.university.domainsCount}</p>
            </div>
          </>
        )}
      </div>
    </div>
  );
}

const statCardStyle = {
  background: '#fff',
  padding: 20,
  borderRadius: 12,
  boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
  textAlign: 'center'
};

const integrationCardStyle = (isActive) => ({
  background: '#fff',
  padding: 20,
  borderRadius: 12,
  borderLeft: `6px solid ${isActive ? '#28a745' : '#dc3545'}`,
  boxShadow: '0 4px 12px rgba(0,0,0,0.05)'
});

export default AdminDashboard;