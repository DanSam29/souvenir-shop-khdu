import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { promotionsAPI } from '../../services/api';
import { useToast } from '../../components/Toast';

// Дозволені значення (узгоджено з бекендом: PromotionValidators / PromotionService)
const PROMO_TYPES = ['PERCENTAGE', 'FIXED_AMOUNT', 'SPECIAL_PRICE'];
const TARGET_TYPES = ['PRODUCT', 'CATEGORY', 'CART', 'SHIPPING'];
const AUDIENCE_TYPES = ['ALL', 'STUDENTS', 'STAFF', 'ALUMNI', 'CUSTOM', 'REGULAR', 'SCHOLARSHIP', 'HIGH_ACHIEVER', 'NONE'];
const PAGE_SIZE = 10;

const emptyForm = {
  name: '',
  nameEn: '',
  description: '',
  descriptionEn: '',
  type: 'PERCENTAGE',
  value: 0,
  targetType: 'PRODUCT',
  targetId: '',
  audienceType: 'ALL',
  startDate: '',
  endDate: '',
  promoCode: '',
  minOrderAmount: '',
  minQuantity: '',
  priority: 50,
  usageLimit: ''
};

function PromotionsAdmin() {
  const { t, i18n } = useTranslation();
  const isEn = i18n.language === 'en';
  const { showToast } = useToast();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [filters, setFilters] = useState({ search: '', type: '', isActive: '' });
  const [showForm, setShowForm] = useState(false);
  const [editingPromotion, setEditingPromotion] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  // Статистика вибраної акції
  const [statsFor, setStatsFor] = useState(null);
  const [stats, setStats] = useState(null);

  const loadList = useCallback(async () => {
    try {
      setLoading(true);
      const params = { pageNumber: page, pageSize: PAGE_SIZE };
      if (filters.search) params.search = filters.search;
      if (filters.type) params.type = filters.type;
      if (filters.isActive !== '') params.isActive = filters.isActive;
      const res = await promotionsAPI.getAll(params);
      const payload = res.data || {};
      setItems(payload.items || []);
      setTotalPages(payload.totalPages || 1);
      setTotalItems(payload.totalItems || 0);
    } catch (err) {
      console.error('Не вдалося завантажити акції', err);
      showToast(t('common.load_error'), 'error');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, [page, filters, showToast, t]);

  useEffect(() => {
    loadList();
  }, [loadList]);

  const resetForm = () => setForm(emptyForm);

  const handleEdit = (p) => {
    setEditingPromotion(p);
    setForm({
      name: p.name || '',
      nameEn: p.nameEn || '',
      description: p.description || '',
      descriptionEn: p.descriptionEn || '',
      type: p.type || 'PERCENTAGE',
      value: p.value ?? 0,
      targetType: p.targetType || 'PRODUCT',
      targetId: p.targetId ?? '',
      audienceType: p.audienceType || 'ALL',
      startDate: p.startDate ? p.startDate.slice(0, 10) : '',
      endDate: p.endDate ? p.endDate.slice(0, 10) : '',
      promoCode: p.promoCode || '',
      minOrderAmount: p.minOrderAmount ?? '',
      minQuantity: p.minQuantity ?? '',
      priority: p.priority ?? 50,
      usageLimit: p.usageLimit ?? ''
    });
    setShowForm(true);
  };

  // Перетворення порожніх рядків у null для необов'язкових числових/датових полів
  const toNullableNumber = (v) => (v === '' || v === null || v === undefined ? null : Number(v));
  const toNullableDate = (v) => (v ? new Date(v).toISOString() : null);

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);
      const payload = {
        name: form.name.trim(),
        nameEn: form.nameEn || null,
        description: form.description || null,
        descriptionEn: form.descriptionEn || null,
        type: form.type,
        value: Number(form.value) || 0,
        targetType: form.targetType,
        targetId: toNullableNumber(form.targetId),
        audienceType: form.audienceType,
        startDate: toNullableDate(form.startDate),
        endDate: toNullableDate(form.endDate),
        promoCode: form.promoCode?.trim() ? form.promoCode.trim().toUpperCase() : null,
        minOrderAmount: toNullableNumber(form.minOrderAmount),
        minQuantity: toNullableNumber(form.minQuantity),
        priority: Number(form.priority) || 0,
        usageLimit: toNullableNumber(form.usageLimit)
      };
      if (editingPromotion) {
        await promotionsAPI.update(editingPromotion.promotionId, payload);
        showToast(t('admin_promotions.saved'), 'success');
      } else {
        await promotionsAPI.create(payload);
        showToast(t('admin_promotions.created'), 'success');
      }
      setShowForm(false);
      setEditingPromotion(null);
      resetForm();
      loadList();
    } catch (err) {
      showToast(err.response?.data?.error || t('common.update_error'), 'error');
    } finally {
      setSaving(false);
    }
  };

  const handleToggle = async (p) => {
    try {
      await promotionsAPI.toggleActive(p.promotionId);
      loadList();
    } catch {
      showToast(t('common.update_error'), 'error');
    }
  };

  const handleDelete = async (p) => {
    if (!window.confirm(t('admin_promotions.delete_confirm'))) return;
    try {
      await promotionsAPI.delete(p.promotionId);
      showToast(t('common.delete'), 'success');
      loadList();
    } catch {
      showToast(t('common.update_error'), 'error');
    }
  };

  const handleShowStats = async (p) => {
    try {
      const res = await promotionsAPI.getStatistics(p.promotionId);
      setStats(res.data);
      setStatsFor(p);
    } catch {
      showToast(t('common.load_error'), 'error');
    }
  };

  const handleExport = async (promotionId, format) => {
    try {
      const res = format === 'csv'
        ? await promotionsAPI.exportStatisticsCsv(promotionId)
        : await promotionsAPI.exportStatisticsPdf(promotionId);
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `promotion_${promotionId}_statistics.${format}`);
      document.body.appendChild(link);
      link.click();
      window.URL.revokeObjectURL(url);
    } catch {
      showToast(t('common.load_error'), 'error');
    }
  };

  const handleAssign = async (p) => {
    const input = window.prompt(t('admin_promotions.assign_user_prompt'));
    const userId = parseInt(input, 10);
    if (!userId || userId <= 0) return;
    try {
      await promotionsAPI.assign(p.promotionId, userId);
      showToast(t('admin_promotions.assigned'), 'success');
    } catch (err) {
      showToast(err.response?.data?.error || t('admin_promotions.assign_error'), 'error');
    }
  };

  const displayName = (p) => (isEn && p.nameEn ? p.nameEn : p.name);

  return (
    <div style={{ padding: 20, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 20 }}>
        <Link to="/admin" style={{ textDecoration: 'none', color: '#666', display: 'block', marginBottom: 10 }}>
          ← {t('admin_promotions.back_to_dashboard')}
        </Link>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h1>{t('admin_promotions.title')}</h1>
          <button
            onClick={() => { setShowForm(true); setEditingPromotion(null); resetForm(); }}
            style={{ padding: '10px 20px', background: '#007bff', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer' }}
          >
            + {t('admin_promotions.add')}
          </button>
        </div>
      </div>

      {/* Фільтри */}
      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 20 }}>
        <input
          type="text"
          value={filters.search}
          onChange={(e) => { setPage(1); setFilters({ ...filters, search: e.target.value }); }}
          placeholder={t('common.search')}
          style={{ padding: '10px 12px', borderRadius: 8, border: '1px solid #ddd', minWidth: 220 }}
        />
        <select
          value={filters.type}
          onChange={(e) => { setPage(1); setFilters({ ...filters, type: e.target.value }); }}
          style={{ padding: '10px 12px', borderRadius: 8, border: '1px solid #ddd' }}
        >
          <option value="">{t('admin_promotions.all_types')}</option>
          {PROMO_TYPES.map((tp) => <option key={tp} value={tp}>{t(`admin_promotions.type_${tp}`)}</option>)}
        </select>
        <select
          value={filters.isActive}
          onChange={(e) => { setPage(1); setFilters({ ...filters, isActive: e.target.value }); }}
          style={{ padding: '10px 12px', borderRadius: 8, border: '1px solid #ddd' }}
        >
          <option value="">{t('admin_promotions.all_statuses')}</option>
          <option value="true">{t('admin_promotions.active')}</option>
          <option value="false">{t('admin_promotions.inactive')}</option>
        </select>
      </div>

      {/* Форма створення/редагування */}
      {showForm && (
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)', marginBottom: 30 }}>
          <h2 style={{ marginBottom: 20 }}>{editingPromotion ? t('admin_promotions.edit') : t('admin_promotions.new')}</h2>
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <Field label={`${t('admin_promotions.name')} *`}>
                <input required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.name_en')}>
                <input value={form.nameEn} onChange={(e) => setForm({ ...form, nameEn: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.description')}>
                <input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.description_en')}>
                <input value={form.descriptionEn} onChange={(e) => setForm({ ...form, descriptionEn: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.type')}>
                <select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })} style={inputStyle}>
                  {PROMO_TYPES.map((tp) => <option key={tp} value={tp}>{t(`admin_promotions.type_${tp}`)}</option>)}
                </select>
              </Field>
              <Field label={t('admin_promotions.value')}>
                <input type="number" step="0.01" value={form.value} onChange={(e) => setForm({ ...form, value: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.target_type')}>
                <select value={form.targetType} onChange={(e) => setForm({ ...form, targetType: e.target.value })} style={inputStyle}>
                  {TARGET_TYPES.map((tp) => <option key={tp} value={tp}>{t(`admin_promotions.target_${tp}`)}</option>)}
                </select>
              </Field>
              <Field label={t('admin_promotions.target_id')}>
                <input type="number" value={form.targetId} onChange={(e) => setForm({ ...form, targetId: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.audience')}>
                <select value={form.audienceType} onChange={(e) => setForm({ ...form, audienceType: e.target.value })} style={inputStyle}>
                  {AUDIENCE_TYPES.map((tp) => <option key={tp} value={tp}>{t(`admin_promotions.audience_${tp}`)}</option>)}
                </select>
              </Field>
              <Field label={t('admin_promotions.promo_code')}>
                <input value={form.promoCode} onChange={(e) => setForm({ ...form, promoCode: e.target.value })} style={inputStyle} placeholder="SUMMER2026" />
              </Field>
              <Field label={t('admin_promotions.start_date')}>
                <input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.end_date')}>
                <input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.min_order_amount')}>
                <input type="number" step="0.01" value={form.minOrderAmount} onChange={(e) => setForm({ ...form, minOrderAmount: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.min_quantity')}>
                <input type="number" value={form.minQuantity} onChange={(e) => setForm({ ...form, minQuantity: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.priority')}>
                <input type="number" min="0" max="100" value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value })} style={inputStyle} />
              </Field>
              <Field label={t('admin_promotions.usage_limit')}>
                <input type="number" value={form.usageLimit} onChange={(e) => setForm({ ...form, usageLimit: e.target.value })} style={inputStyle} />
              </Field>
            </div>
            <div style={{ display: 'flex', gap: 10, marginTop: 20 }}>
              <button type="submit" disabled={saving} style={{ padding: '10px 20px', background: saving ? '#ccc' : '#28a745', color: '#fff', border: 'none', borderRadius: 8, cursor: saving ? 'default' : 'pointer' }}>
                {saving ? t('common.submitting') : t('common.save')}
              </button>
              <button type="button" onClick={() => { setShowForm(false); setEditingPromotion(null); resetForm(); }} style={{ padding: '10px 20px', background: '#fff', border: '1px solid #ddd', borderRadius: 8, cursor: 'pointer' }}>
                {t('common.cancel')}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Статистика */}
      {statsFor && stats && (
        <div style={{ background: '#f8f9fa', padding: 20, borderRadius: 12, marginBottom: 30, border: '1px solid #eee' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <h3 style={{ margin: 0 }}>{t('admin_promotions.statistics')}: {displayName(statsFor)}</h3>
            <div style={{ display: 'flex', gap: 8 }}>
              <button onClick={() => handleExport(statsFor.promotionId, 'csv')} style={smallBtn}>CSV</button>
              <button onClick={() => handleExport(statsFor.promotionId, 'pdf')} style={smallBtn}>PDF</button>
              <button onClick={() => { setStatsFor(null); setStats(null); }} style={smallBtn}>{t('common.cancel')}</button>
            </div>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 12 }}>
            <StatBox label={t('admin_promotions.usage')} value={`${stats.currentUsage} / ${stats.usageLimit ?? '∞'}`} />
            <StatBox label={t('admin_promotions.assigned_users')} value={stats.assignedUsers} />
            <StatBox label={t('admin_promotions.total_orders')} value={stats.totalOrders} />
            <StatBox label={t('admin_promotions.total_qty')} value={stats.totalQuantitySold} />
            <StatBox label={t('admin_promotions.total_discount')} value={`${Number(stats.totalDiscountGiven).toFixed(2)} ${t('common.currency')}`} />
            <StatBox label={t('admin_promotions.total_revenue')} value={`${Number(stats.totalRevenue).toFixed(2)} ${t('common.currency')}`} />
          </div>
        </div>
      )}

      {/* Таблиця */}
      {loading ? (
        <div>{t('common.loading')}</div>
      ) : items.length === 0 ? (
        <div style={{ color: '#888' }}>{t('admin_promotions.empty')}</div>
      ) : (
        <div style={{ overflowX: 'auto', background: '#fff', borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.06)' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: 900 }}>
            <thead>
              <tr style={{ background: '#f5f5f5', textAlign: 'left' }}>
                <th style={th}>{t('admin_promotions.name')}</th>
                <th style={th}>{t('admin_promotions.type')}</th>
                <th style={th}>{t('admin_promotions.value')}</th>
                <th style={th}>{t('admin_promotions.promo_code')}</th>
                <th style={th}>{t('admin_promotions.priority')}</th>
                <th style={th}>{t('admin_promotions.usage')}</th>
                <th style={th}>{t('profile.status')}</th>
                <th style={th}>{t('profile.order_actions')}</th>
              </tr>
            </thead>
            <tbody>
              {items.map((p) => (
                <tr key={p.promotionId} style={{ borderBottom: '1px solid #f0f0f0' }}>
                  <td style={td}>{displayName(p)}</td>
                  <td style={td}>{t(`admin_promotions.type_${p.type}`, { defaultValue: p.type })}</td>
                  <td style={td}>{p.type === 'PERCENTAGE' ? `${p.value}%` : `${Number(p.value).toFixed(2)}`}</td>
                  <td style={td}>{p.promoCode || '—'}</td>
                  <td style={td}>{p.priority}</td>
                  <td style={td}>{p.currentUsage} / {p.usageLimit ?? '∞'}</td>
                  <td style={td}>
                    <span style={{ color: p.isActive ? '#28a745' : '#999', fontWeight: 600 }}>
                      {p.isActive ? t('admin_promotions.active') : t('admin_promotions.inactive')}
                    </span>
                  </td>
                  <td style={td}>
                    <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                      <button onClick={() => handleEdit(p)} style={actionBtn}>✏️</button>
                      <button onClick={() => handleToggle(p)} style={actionBtn}>{p.isActive ? '⏸' : '▶️'}</button>
                      <button onClick={() => handleShowStats(p)} style={actionBtn}>📊</button>
                      <button onClick={() => handleAssign(p)} style={actionBtn}>👤+</button>
                      <button onClick={() => handleDelete(p)} style={{ ...actionBtn, color: '#cf1322' }}>🗑</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Пагінація */}
      {totalPages > 1 && (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 16, marginTop: 20 }}>
          <button disabled={page <= 1} onClick={() => setPage((p) => p - 1)} style={pageBtn(page <= 1)}>{t('common.prev')}</button>
          <span>{t('common.page')} {page} / {totalPages} ({totalItems})</span>
          <button disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)} style={pageBtn(page >= totalPages)}>{t('common.next')}</button>
        </div>
      )}
    </div>
  );
}

function Field({ label, children }) {
  return (
    <div>
      <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>{label}</label>
      {children}
    </div>
  );
}

function StatBox({ label, value }) {
  return (
    <div style={{ background: '#fff', padding: 12, borderRadius: 8, border: '1px solid #eee' }}>
      <div style={{ color: '#888', fontSize: '0.85rem' }}>{label}</div>
      <div style={{ fontWeight: 700, fontSize: '1.1rem' }}>{value}</div>
    </div>
  );
}

const inputStyle = { width: '100%', padding: '10px 12px', borderRadius: 8, border: '1px solid #ddd', boxSizing: 'border-box' };
const th = { padding: '12px 10px', fontSize: '0.9rem' };
const td = { padding: '12px 10px', fontSize: '0.9rem' };
const actionBtn = { padding: '6px 10px', borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: 'pointer' };
const smallBtn = { padding: '6px 12px', borderRadius: 6, border: '1px solid #ddd', background: '#fff', cursor: 'pointer' };
const pageBtn = (disabled) => ({ padding: '8px 14px', borderRadius: 8, border: '1px solid #ddd', background: disabled ? '#f5f5f5' : '#fff', cursor: disabled ? 'default' : 'pointer' });

export default PromotionsAdmin;
