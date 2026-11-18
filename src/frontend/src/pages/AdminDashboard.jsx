import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ordersAPI, adminUsersAPI, warehouseAPI, integrationsAPI, categoriesAPI } from '../services/api';

function AdminDashboard() {
  const { t } = useTranslation();
  const [stats, setStats] = useState({ orders: 0, users: 0, stock: 0, categories: 0 });
  const [integrations, setIntegrations] = useState(null);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    const loadStats = async () => {
      try {
        const [oRes, uRes, sRes, iRes, cRes] = await Promise.all([
          ordersAPI.getAll(),
          adminUsersAPI.getAll(),
          warehouseAPI.getStock(),
          integrationsAPI.getStatus(),
          categoriesAPI.getAll()
        ]);
        // Підрахунок загальної кількості категорій (включаючи підкатегорії)
        const countCategories = (nodes) => {
          let count = 0;
          nodes.forEach(node => {
            count++;
            if (node.subCategories && node.subCategories.length > 0) {
              count += countCategories(node.subCategories);
            }
          });
          return count;
        };
        setStats({
          orders: oRes.data.items ? oRes.data.totalItems : oRes.data.length,
          users: uRes.data.items ? uRes.data.totalItems : uRes.data.length,
          stock: sRes.data.length,
          categories: countCategories(cRes.data)
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
  if (loading) return <div>{t('common.loading')}</div>;
  return (
    <div className="admin-dashboard" style={{ padding: 20 }}>
      <h1>{t('admin.dashboard.title')}</h1>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 20, marginTop: 30 }}>
        <div style={statCardStyle}>
          <h3>{t('admin.dashboard.orders')}</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>{stats.orders}</p>
          <Link to="/admin/orders">{t('admin.dashboard.manage')}</Link>
        </div>
        <div style={statCardStyle}>
          <h3>{t('admin.dashboard.products')}</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>{stats.stock}</p>
          <Link to="/admin/products">{t('admin.dashboard.manage')}</Link>
        </div>
        <div style={statCardStyle}>
          <h3>{t('admin.dashboard.categories')}</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>{stats.categories}</p>
          <Link to="/admin/categories">{t('admin.dashboard.manage')}</Link>
        </div>
        <div style={statCardStyle}>
          <h3>{t('admin.dashboard.users')}</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>{stats.users}</p>
          <Link to="/admin/users">{t('admin.dashboard.manage')}</Link>
        </div>
        <div style={statCardStyle}>
          <h3>{t('admin.dashboard.stock_items')}</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>{stats.stock}</p>
          <Link to="/admin/warehouse">{t('admin.dashboard.warehouse_link')}</Link>
        </div>
        <div style={statCardStyle}>
          <h3>{t('admin.dashboard.companies')}</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>🏢</p>
          <Link to="/admin/companies">{t('admin.dashboard.suppliers_link')}</Link>
        </div>
        <div style={statCardStyle}>
          <h3>{t('admin.dashboard.promotions')}</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>🎁</p>
          <Link to="/admin/promotions">{t('admin.dashboard.manage')}</Link>
        </div>
        <div style={statCardStyle}>
          <h3>{t('admin.dashboard.analytics')}</h3>
          <p style={{ fontSize: '2rem', fontWeight: 700 }}>📊</p>
          <Link to="/admin/analytics">{t('admin.dashboard.reports_link')}</Link>
        </div>
      </div>
      <h2 style={{ marginTop: 40 }}>{t('admin.dashboard.integrations_status')}</h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: 20, marginTop: 20 }}>
        {integrations && (
          <>
            <div style={integrationCardStyle(integrations.stripe.keyConfigured)}>
              <h4>{t('admin.dashboard.stripe')}</h4>
              <p>{t('admin.dashboard.status_prefix')} {integrations.stripe.keyConfigured ? t('admin.dashboard.configured') : t('admin.dashboard.not_configured')}</p>
            </div>
            <div style={integrationCardStyle(integrations.novaPoshta.keyConfigured)}>
              <h4>{t('admin.dashboard.nova_poshta')}</h4>
              <p>{t('admin.dashboard.status_prefix')} {integrations.novaPoshta.keyConfigured ? t('admin.dashboard.configured') : t('admin.dashboard.not_configured')}</p>
            </div>
            <div style={integrationCardStyle(true)}>
              <h4>University API</h4>
              <p>{t('admin.dashboard.active_domains')} {integrations.university.domainsCount}</p>
            </div>
            <div style={integrationCardStyle(integrations.smtp?.configured)}>
              <h4>Email (SMTP)</h4>
              <p>{t('admin.dashboard.status_prefix')} {integrations.smtp?.configured ? t('admin.dashboard.configured') : t('admin.dashboard.not_configured')}</p>
              <p>{t('admin.dashboard.sent_last_hour')} <b>{integrations.smtp?.sentLastHour ?? 0}</b></p>
              <p>{t('admin.dashboard.sent_last_day')} <b>{integrations.smtp?.sentLastDay ?? 0}</b></p>
              <p>{t('admin.dashboard.failed_last_day')} <b>{integrations.smtp?.failedLastDay ?? 0}</b></p>
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
  textAlign: 'center',
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  justifyContent: 'center',
  gap: 8
};
const integrationCardStyle = (isActive) => ({
  background: '#fff',
  padding: 20,
  borderRadius: 12,
  borderLeft: `6px solid ${isActive ? '#28a745' : '#dc3545'}`,
  boxShadow: '0 4px 12px rgba(0,0,0,0.05)'
});

export default AdminDashboard;