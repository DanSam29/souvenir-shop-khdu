import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { warehouseAPI, companiesAPI, productsAPI } from '../services/api';

// Спільні inline-стилі (у стилі інших адмін-сторінок проєкту)
const tabsStyle = { display: 'flex', gap: 8, marginBottom: 20, flexWrap: 'wrap' };
const cardStyle = { background: '#fff', borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', overflowX: 'auto' };
const tableStyle = { width: '100%', borderCollapse: 'collapse' };
const thStyle = { padding: 16, textAlign: 'left', borderBottom: '1px solid #eee' };
const tdStyle = { padding: 16, borderBottom: '1px solid #f0f0f0' };
const inputStyle = { width: '100%', padding: 10, borderRadius: 8, border: '1px solid #ddd' };
const labelStyle = { display: 'block', marginBottom: 8, fontWeight: 500 };
const btnStyle = { padding: '10px 20px', border: 'none', borderRadius: 8, cursor: 'pointer', color: '#fff' };
const btnSmall = { padding: '6px 12px', marginRight: 8, borderRadius: 6, cursor: 'pointer' };
const emptyRowStyle = { padding: 40, textAlign: 'center', color: '#666' };

// Допустимі причини (узгоджені з бекендом-валідатором)
const INCOMING_REASON_VALUES = ['INCOMING', 'RETURN'];
const OUTGOING_REASON_VALUES = ['Damaged', 'Lost', 'Return', 'Inventory', 'Other'];

const emptyIncomingForm = { productId: '', quantity: 1, purchasePrice: '', reason: 'INCOMING', companyId: '', documentDate: '', notes: '' };
const emptyOutgoingForm = { productId: '', quantity: 1, reason: 'Damaged', companyId: '', documentDate: '', notes: '' };
const emptyCompanyForm = { name: '', nameEn: '', contactPerson: '', phone: '', email: '', address: '', notes: '', isActive: true };

// Витягування тексту помилки з ApiResponse
const errMessage = (err, fallback) => err?.response?.data?.errors?.[0] || err?.response?.data?.message || fallback;
// Обрізання дати для input[type=date]
const toDateInput = (value) => (value ? String(value).slice(0, 10) : '');
// Завантаження бінарного файлу з response.data (Blob)
const downloadBlob = (blob, filename) => {
  const url = window.URL.createObjectURL(new Blob([blob]));
  const link = document.createElement('a');
  link.href = url;
  link.setAttribute('download', filename);
  document.body.appendChild(link);
  link.click();
  window.URL.revokeObjectURL(url);
  document.body.removeChild(link);
};

// ======================= Таб: Прибуткові накладні =======================
function IncomingTab({ products, companies }) {
  const { t } = useTranslation();
  const INCOMING_REASONS = INCOMING_REASON_VALUES.map((value) => ({ value, label: t('admin.warehouse.reason_' + (value === 'INCOMING' ? 'incoming' : 'return')) }));
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(emptyIncomingForm);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const res = await warehouseAPI.getIncoming({ pageNumber: 1, pageSize: 100 });
      setItems(res.data?.items || []);
    } catch (err) {
      console.error('Не вдалося завантажити прибуткові накладні', err);
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const resetForm = () => { setForm(emptyIncomingForm); setEditingId(null); };

  const handleEdit = (doc) => {
    setEditingId(doc.documentId);
    setForm({
      productId: doc.productId || '',
      quantity: doc.quantity || 1,
      purchasePrice: doc.purchasePrice ?? '',
      reason: doc.reason || 'INCOMING',
      companyId: doc.companyId || '',
      documentDate: toDateInput(doc.documentDate),
      notes: doc.notes || '',
    });
    setShowForm(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const payload = {
      productId: Number(form.productId),
      quantity: Number(form.quantity),
      purchasePrice: Number(form.purchasePrice),
      reason: form.reason,
      companyId: form.companyId ? Number(form.companyId) : null,
      documentDate: form.documentDate || undefined,
      notes: form.notes || null,
    };
    try {
      if (editingId) await warehouseAPI.updateIncoming(editingId, payload);
      else await warehouseAPI.createIncoming(payload);
      setShowForm(false);
      resetForm();
      load();
    } catch (err) {
      alert(errMessage(err, t('admin.warehouse.save_incoming_error')));
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm(t('admin.warehouse.confirm_delete_incoming'))) return;
    try {
      await warehouseAPI.deleteIncoming(id);
      load();
    } catch (err) {
      alert(errMessage(err, t('admin.warehouse.delete_error')));
    }
  };

  const companyRequired = form.reason !== 'RETURN';

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <button type="button" style={{ ...btnStyle, background: '#007bff' }}
          onClick={() => { resetForm(); setShowForm(true); }}>
          {t('admin.warehouse.create_document')}
        </button>
      </div>

      {showForm && (
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)', marginBottom: 30 }}>
          <h3 style={{ marginBottom: 20 }}>{editingId ? t('admin.warehouse.edit_incoming') : t('admin.warehouse.new_incoming')}</h3>
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div>
                <label htmlFor="incoming-product" style={labelStyle}>{t('admin.common.product')} *</label>
                <select id="incoming-product" required value={form.productId} style={inputStyle}
                  onChange={(e) => setForm({ ...form, productId: e.target.value })}>
                  <option value="">{t('admin.warehouse.select_product')}</option>
                  {products.map((p) => <option key={p.productId} value={p.productId}>{p.name}</option>)}
                </select>
              </div>
              <div>
                <label htmlFor="incoming-quantity" style={labelStyle}>{t('admin.common.quantity')} *</label>
                <input id="incoming-quantity" type="number" min="1" required value={form.quantity} style={inputStyle}
                  onChange={(e) => setForm({ ...form, quantity: e.target.value })} />
              </div>
              <div>
                <label htmlFor="incoming-purchase-price" style={labelStyle}>{t('admin.warehouse.purchase_price')} *</label>
                <input id="incoming-purchase-price" type="number" min="0.01" step="0.01" required value={form.purchasePrice} style={inputStyle}
                  onChange={(e) => setForm({ ...form, purchasePrice: e.target.value })} />
              </div>
              <div>
                <label htmlFor="incoming-reason" style={labelStyle}>{t('admin.common.reason')} *</label>
                <select id="incoming-reason" value={form.reason} style={inputStyle}
                  onChange={(e) => setForm({ ...form, reason: e.target.value })}>
                  {INCOMING_REASONS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
                </select>
              </div>
              <div>
                <label htmlFor="incoming-company" style={labelStyle}>{t('admin.warehouse.supplier_company')} {companyRequired ? '*' : t('admin.common.optional')}</label>
                <select id="incoming-company" required={companyRequired} value={form.companyId} style={inputStyle}
                  onChange={(e) => setForm({ ...form, companyId: e.target.value })}>
                  <option value="">{t('admin.warehouse.select_company')}</option>
                  {companies.filter((c) => c.isActive).map((c) => <option key={c.companyId} value={c.companyId}>{c.name}</option>)}
                </select>
              </div>
              <div>
                <label htmlFor="incoming-document-date" style={labelStyle}>{t('admin.warehouse.document_date')}</label>
                <input id="incoming-document-date" type="date" value={form.documentDate} style={inputStyle}
                  onChange={(e) => setForm({ ...form, documentDate: e.target.value })} />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label htmlFor="incoming-notes" style={labelStyle}>{t('admin.common.notes')}</label>
                <textarea id="incoming-notes" rows={2} value={form.notes} style={inputStyle}
                  onChange={(e) => setForm({ ...form, notes: e.target.value })} />
              </div>
            </div>
            <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
              <button type="submit" style={{ ...btnStyle, background: '#28a745' }}>
                {editingId ? t('admin.common.save_changes') : t('admin.common.create')}
              </button>
              <button type="button" style={{ ...btnStyle, background: '#6c757d' }}
                onClick={() => { setShowForm(false); resetForm(); }}>
                {t('common.cancel')}
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? <div style={emptyRowStyle}>{t('common.loading')}</div> : (
        <div style={cardStyle}>
          <table style={tableStyle}>
            <thead style={{ background: '#f8f9fa' }}>
              <tr>
                <th style={thStyle}>{t('admin.common.product')}</th>
                <th style={thStyle}>{t('admin.common.quantity')}</th>
                <th style={thStyle}>{t('admin.warehouse.purchase_price_short')}</th>
                <th style={thStyle}>{t('admin.common.reason')}</th>
                <th style={thStyle}>{t('admin.common.company')}</th>
                <th style={thStyle}>{t('admin.common.date')}</th>
                <th style={thStyle}>{t('admin.common.actions')}</th>
              </tr>
            </thead>
            <tbody>
              {items.map((doc) => (
                <tr key={doc.documentId}>
                  <td style={tdStyle}><strong>{doc.product?.name || `#${doc.productId}`}</strong></td>
                  <td style={tdStyle}>{doc.quantity}</td>
                  <td style={tdStyle}>{Number(doc.purchasePrice).toFixed(2)} ₴</td>
                  <td style={tdStyle}>{INCOMING_REASONS.find((r) => r.value === doc.reason)?.label || doc.reason}</td>
                  <td style={tdStyle}>{doc.company?.name || '-'}</td>
                  <td style={tdStyle}>{toDateInput(doc.documentDate)}</td>
                  <td style={tdStyle}>
                    <button type="button" style={{ ...btnSmall, border: '1px solid #007bff', background: 'transparent', color: '#007bff' }}
                      onClick={() => handleEdit(doc)}>{t('admin.common.edit')}</button>
                    <button type="button" style={{ ...btnSmall, border: '1px solid #dc3545', background: 'transparent', color: '#dc3545', marginRight: 0 }}
                      onClick={() => handleDelete(doc.documentId)}>{t('admin.common.delete')}</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {items.length === 0 && <div style={emptyRowStyle}>{t('admin.warehouse.no_incoming')}</div>}
        </div>
      )}
    </div>
  );
}

// ======================= Таб: Видаткові накладні =======================
function OutgoingTab({ products, companies }) {
  const { t } = useTranslation();
  const OUTGOING_REASONS = OUTGOING_REASON_VALUES.map((value) => ({ value, label: t('admin.warehouse.reason_' + ({Damaged:'damaged',Lost:'lost',Return:'return',Inventory:'inventory',Other:'other'}[value])) }));
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(emptyOutgoingForm);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const res = await warehouseAPI.getOutgoing({ pageNumber: 1, pageSize: 100 });
      setItems(res.data?.items || []);
    } catch (err) {
      console.error('Не вдалося завантажити видаткові накладні', err);
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const resetForm = () => { setForm(emptyOutgoingForm); setEditingId(null); };

  const handleEdit = (doc) => {
    setEditingId(doc.documentId);
    setForm({
      productId: doc.productId || '',
      quantity: doc.quantity || 1,
      reason: doc.reason || 'Damaged',
      companyId: doc.companyId || '',
      documentDate: toDateInput(doc.documentDate),
      notes: doc.notes || '',
    });
    setShowForm(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const payload = {
      productId: Number(form.productId),
      quantity: Number(form.quantity),
      reason: form.reason,
      companyId: form.companyId ? Number(form.companyId) : null,
      documentDate: form.documentDate || undefined,
      notes: form.notes || null,
    };
    try {
      if (editingId) await warehouseAPI.updateOutgoing(editingId, payload);
      else await warehouseAPI.createOutgoing(payload);
      setShowForm(false);
      resetForm();
      load();
    } catch (err) {
      alert(errMessage(err, t('admin.warehouse.save_outgoing_error')));
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm(t('admin.warehouse.confirm_delete_outgoing'))) return;
    try {
      await warehouseAPI.deleteOutgoing(id);
      load();
    } catch (err) {
      alert(errMessage(err, t('admin.warehouse.delete_error')));
    }
  };

  const companyRequired = form.reason === 'Return';

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <button type="button" style={{ ...btnStyle, background: '#007bff' }}
          onClick={() => { resetForm(); setShowForm(true); }}>
          {t('admin.warehouse.create_document')}
        </button>
      </div>

      {showForm && (
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)', marginBottom: 30 }}>
          <h3 style={{ marginBottom: 20 }}>{editingId ? t('admin.warehouse.edit_outgoing') : t('admin.warehouse.new_outgoing')}</h3>
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div>
                <label htmlFor="outgoing-product" style={labelStyle}>{t('admin.common.product')} *</label>
                <select id="outgoing-product" required value={form.productId} style={inputStyle}
                  onChange={(e) => setForm({ ...form, productId: e.target.value })}>
                  <option value="">{t('admin.warehouse.select_product')}</option>
                  {products.map((p) => <option key={p.productId} value={p.productId}>{p.name}</option>)}
                </select>
              </div>
              <div>
                <label htmlFor="outgoing-quantity" style={labelStyle}>{t('admin.common.quantity')} *</label>
                <input id="outgoing-quantity" type="number" min="1" required value={form.quantity} style={inputStyle}
                  onChange={(e) => setForm({ ...form, quantity: e.target.value })} />
              </div>
              <div>
                <label htmlFor="outgoing-reason" style={labelStyle}>{t('admin.common.reason')} *</label>
                <select id="outgoing-reason" value={form.reason} style={inputStyle}
                  onChange={(e) => setForm({ ...form, reason: e.target.value })}>
                  {OUTGOING_REASONS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
                </select>
              </div>
              <div>
                <label htmlFor="outgoing-company" style={labelStyle}>{t('admin.common.company')} {companyRequired ? '*' : t('admin.common.optional')}</label>
                <select id="outgoing-company" required={companyRequired} value={form.companyId} style={inputStyle}
                  onChange={(e) => setForm({ ...form, companyId: e.target.value })}>
                  <option value="">{t('admin.warehouse.select_company')}</option>
                  {companies.filter((c) => c.isActive).map((c) => <option key={c.companyId} value={c.companyId}>{c.name}</option>)}
                </select>
              </div>
              <div>
                <label htmlFor="outgoing-document-date" style={labelStyle}>{t('admin.warehouse.document_date')}</label>
                <input id="outgoing-document-date" type="date" value={form.documentDate} style={inputStyle}
                  onChange={(e) => setForm({ ...form, documentDate: e.target.value })} />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label htmlFor="outgoing-notes" style={labelStyle}>{t('admin.common.notes')}</label>
                <textarea id="outgoing-notes" rows={2} value={form.notes} style={inputStyle}
                  onChange={(e) => setForm({ ...form, notes: e.target.value })} />
              </div>
            </div>
            <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
              <button type="submit" style={{ ...btnStyle, background: '#28a745' }}>
                {editingId ? t('admin.common.save_changes') : t('admin.common.create')}
              </button>
              <button type="button" style={{ ...btnStyle, background: '#6c757d' }}
                onClick={() => { setShowForm(false); resetForm(); }}>
                {t('common.cancel')}
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? <div style={emptyRowStyle}>{t('common.loading')}</div> : (
        <div style={cardStyle}>
          <table style={tableStyle}>
            <thead style={{ background: '#f8f9fa' }}>
              <tr>
                <th style={thStyle}>{t('admin.common.product')}</th>
                <th style={thStyle}>{t('admin.common.quantity')}</th>
                <th style={thStyle}>{t('admin.common.reason')}</th>
                <th style={thStyle}>{t('admin.common.company')}</th>
                <th style={thStyle}>{t('admin.common.date')}</th>
                <th style={thStyle}>{t('admin.common.actions')}</th>
              </tr>
            </thead>
            <tbody>
              {items.map((doc) => (
                <tr key={doc.documentId}>
                  <td style={tdStyle}><strong>{doc.product?.name || `#${doc.productId}`}</strong></td>
                  <td style={tdStyle}>{doc.quantity}</td>
                  <td style={tdStyle}>{OUTGOING_REASONS.find((r) => r.value === doc.reason)?.label || doc.reason}</td>
                  <td style={tdStyle}>{doc.company?.name || '-'}</td>
                  <td style={tdStyle}>{toDateInput(doc.documentDate)}</td>
                  <td style={tdStyle}>
                    {doc.reason === 'Order' ? (
                      <span style={{ color: '#595959', fontSize: '0.85rem' }}>{t('admin.warehouse.created_by_system')}</span>
                    ) : (
                      <>
                        <button type="button" style={{ ...btnSmall, border: '1px solid #007bff', background: 'transparent', color: '#007bff' }}
                          onClick={() => handleEdit(doc)}>{t('admin.common.edit')}</button>
                        <button type="button" style={{ ...btnSmall, border: '1px solid #dc3545', background: 'transparent', color: '#dc3545', marginRight: 0 }}
                          onClick={() => handleDelete(doc.documentId)}>{t('admin.common.delete')}</button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {items.length === 0 && <div style={emptyRowStyle}>{t('admin.warehouse.no_outgoing')}</div>}
        </div>
      )}
    </div>
  );
}

// ======================= Таб: Поточні залишки =======================
function StockTab() {
  const { t } = useTranslation();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [exporting, setExporting] = useState('');

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const res = await warehouseAPI.getStockCurrent();
      setItems(res.data || []);
    } catch (err) {
      console.error('Не вдалося завантажити залишки', err);
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleExport = async (format) => {
    try {
      setExporting(format);
      const res = await warehouseAPI.exportStock(format);
      const stamp = new Date().toISOString().split('T')[0].replace(/-/g, '');
      downloadBlob(res.data, `stock_report_${stamp}.${format}`);
    } catch (err) {
      alert(t('admin.warehouse.export_error'));
    } finally {
      setExporting('');
    }
  };

  const visible = lowStockOnly ? items.filter((i) => i.lowStock) : items;

  const stockColor = (current) => {
    if (current <= 0) return '#dc3545';
    if (current <= 5) return '#ffc107';
    return '#28a745';
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16, flexWrap: 'wrap', gap: 12 }}>
        <label style={{ display: 'flex', alignItems: 'center', cursor: 'pointer', fontWeight: 500 }}>
          <input type="checkbox" checked={lowStockOnly} onChange={(e) => setLowStockOnly(e.target.checked)} style={{ marginRight: 8 }} />
          {t('admin.warehouse.low_stock_only')}
        </label>
        <div style={{ display: 'flex', gap: 8 }}>
          <button type="button" disabled={!!exporting} style={{ ...btnStyle, background: '#17a2b8', opacity: exporting ? 0.6 : 1 }}
            onClick={() => handleExport('csv')}>Export CSV</button>
          <button type="button" disabled={!!exporting} style={{ ...btnStyle, background: '#28a745', opacity: exporting ? 0.6 : 1 }}
            onClick={() => handleExport('xlsx')}>Export XLSX</button>
          <button type="button" disabled={!!exporting} style={{ ...btnStyle, background: '#dc3545', opacity: exporting ? 0.6 : 1 }}
            onClick={() => handleExport('pdf')}>Export PDF</button>
        </div>
      </div>

      {loading ? <div style={emptyRowStyle}>{t('common.loading')}</div> : (
        <div style={cardStyle}>
          <table style={tableStyle}>
            <thead style={{ background: '#f8f9fa' }}>
              <tr>
                <th style={thStyle}>{t('admin.common.product')}</th>
                <th style={thStyle}>{t('admin.warehouse.col_total_incoming')}</th>
                <th style={thStyle}>{t('admin.warehouse.col_total_outgoing')}</th>
                <th style={thStyle}>{t('admin.warehouse.col_current_stock')}</th>
                <th style={thStyle}>{t('admin.common.status')}</th>
              </tr>
            </thead>
            <tbody>
              {visible.map((item) => (
                <tr key={item.productId}>
                  <td style={tdStyle}><strong>{item.productName}</strong></td>
                  <td style={tdStyle}>{item.totalIncoming}</td>
                  <td style={tdStyle}>{item.totalOutgoing}</td>
                  <td style={{ ...tdStyle, fontWeight: 'bold', color: stockColor(item.currentStock) }}>{item.currentStock}</td>
                  <td style={tdStyle}>
                    {item.lowStock ? (
                      <span style={{ padding: '4px 12px', borderRadius: 20, fontSize: '0.85rem', background: '#fff3cd', color: '#856404' }}>
                        {t('admin.warehouse.low_stock')}
                      </span>
                    ) : (
                      <span style={{ padding: '4px 12px', borderRadius: 20, fontSize: '0.85rem', background: '#d4edda', color: '#155724' }}>
                        {t('admin.warehouse.enough')}
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {visible.length === 0 && (
            <div style={emptyRowStyle}>{lowStockOnly ? t('admin.warehouse.no_low_stock') : t('admin.warehouse.no_stock_data')}</div>
          )}
        </div>
      )}
    </div>
  );
}

// ======================= Таб: Компанії =======================
function CompaniesTab({ companies, reloadCompanies }) {
  const { t } = useTranslation();
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(emptyCompanyForm);

  const resetForm = () => { setForm(emptyCompanyForm); setEditingId(null); };

  const handleEdit = (company) => {
    setEditingId(company.companyId);
    setForm({
      name: company.name || '',
      nameEn: company.nameEn || '',
      contactPerson: company.contactPerson || '',
      phone: company.phone || '',
      email: company.email || '',
      address: company.address || '',
      notes: company.notes || '',
      isActive: company.isActive,
    });
    setShowForm(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const payload = {
      name: form.name,
      nameEn: form.nameEn || null,
      contactPerson: form.contactPerson || null,
      phone: form.phone || null,
      email: form.email || null,
      address: form.address || null,
      notes: form.notes || null,
      isActive: form.isActive,
    };
    try {
      if (editingId) await companiesAPI.update(editingId, payload);
      else await companiesAPI.create(payload);
      setShowForm(false);
      resetForm();
      reloadCompanies();
    } catch (err) {
      alert(errMessage(err, t('admin.warehouse.save_company_error')));
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm(t('admin.warehouse.confirm_delete_company'))) return;
    try {
      await companiesAPI.delete(id);
      reloadCompanies();
    } catch (err) {
      alert(errMessage(err, t('admin.warehouse.delete_company_error')));
    }
  };

  const toggleActive = async (company) => {
    try {
      if (company.isActive) await companiesAPI.deactivate(company.companyId);
      else await companiesAPI.activate(company.companyId);
      reloadCompanies();
    } catch (err) {
      alert(errMessage(err, t('admin.warehouse.toggle_company_error')));
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <button type="button" style={{ ...btnStyle, background: '#007bff' }}
          onClick={() => { resetForm(); setShowForm(true); }}>
          {t('admin.warehouse.add_company')}
        </button>
      </div>

      {showForm && (
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)', marginBottom: 30 }}>
          <h3 style={{ marginBottom: 20 }}>{editingId ? t('admin.warehouse.edit_company') : t('admin.warehouse.new_company')}</h3>
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div>
                <label htmlFor="warehouse-company-name" style={labelStyle}>{t('admin.companies.label_name')}</label>
                <input id="warehouse-company-name" type="text" required value={form.name} style={inputStyle}
                  onChange={(e) => setForm({ ...form, name: e.target.value })} />
              </div>
              <div>
                <label htmlFor="warehouse-company-name-en" style={labelStyle}>{t('admin.common.name_en')}</label>
                <input id="warehouse-company-name-en" type="text" value={form.nameEn} style={inputStyle}
                  onChange={(e) => setForm({ ...form, nameEn: e.target.value })} />
              </div>
              <div>
                <label htmlFor="warehouse-company-contact" style={labelStyle}>{t('admin.common.contact_person')}</label>
                <input id="warehouse-company-contact" type="text" value={form.contactPerson} style={inputStyle}
                  onChange={(e) => setForm({ ...form, contactPerson: e.target.value })} />
              </div>
              <div>
                <label htmlFor="warehouse-company-phone" style={labelStyle}>{t('admin.common.phone')}</label>
                <input id="warehouse-company-phone" type="text" placeholder="+380XXXXXXXXX" value={form.phone} style={inputStyle}
                  onChange={(e) => setForm({ ...form, phone: e.target.value })} />
              </div>
              <div>
                <label htmlFor="warehouse-company-email" style={labelStyle}>{t('admin.common.email')}</label>
                <input id="warehouse-company-email" type="email" value={form.email} style={inputStyle}
                  onChange={(e) => setForm({ ...form, email: e.target.value })} />
              </div>
              <div>
                <label htmlFor="warehouse-company-address" style={labelStyle}>{t('admin.common.address')}</label>
                <input id="warehouse-company-address" type="text" value={form.address} style={inputStyle}
                  onChange={(e) => setForm({ ...form, address: e.target.value })} />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label htmlFor="warehouse-company-notes" style={labelStyle}>{t('admin.common.notes')}</label>
                <textarea id="warehouse-company-notes" rows={2} value={form.notes} style={inputStyle}
                  onChange={(e) => setForm({ ...form, notes: e.target.value })} />
              </div>
              <div>
                <label style={{ display: 'flex', alignItems: 'center', cursor: 'pointer' }}>
                  <input type="checkbox" checked={form.isActive} style={{ marginRight: 8 }}
                    onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
                  {t('admin.common.active')}
                </label>
              </div>
            </div>
            <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
              <button type="submit" style={{ ...btnStyle, background: '#28a745' }}>
                {editingId ? t('admin.common.save_changes') : t('admin.warehouse.create_company')}
              </button>
              <button type="button" style={{ ...btnStyle, background: '#6c757d' }}
                onClick={() => { setShowForm(false); resetForm(); }}>
                {t('common.cancel')}
              </button>
            </div>
          </form>
        </div>
      )}

      <div style={cardStyle}>
        <table style={tableStyle}>
          <thead style={{ background: '#f8f9fa' }}>
            <tr>
              <th style={thStyle}>{t('admin.companies.col_name')}</th>
              <th style={thStyle}>{t('admin.companies.col_contact')}</th>
              <th style={thStyle}>{t('admin.common.phone')}</th>
              <th style={thStyle}>{t('admin.common.email')}</th>
              <th style={thStyle}>{t('admin.common.status')}</th>
              <th style={thStyle}>{t('admin.common.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {companies.map((company) => (
              <tr key={company.companyId}>
                <td style={tdStyle}><strong>{company.name}</strong></td>
                <td style={tdStyle}>{company.contactPerson || '-'}</td>
                <td style={tdStyle}>{company.phone || '-'}</td>
                <td style={tdStyle}>{company.email || '-'}</td>
                <td style={tdStyle}>
                  <span style={{
                    padding: '4px 12px', borderRadius: 20, fontSize: '0.85rem',
                    background: company.isActive ? '#d4edda' : '#f8d7da',
                    color: company.isActive ? '#155724' : '#721c24',
                  }}>
                    {company.isActive ? t('admin.common.active') : t('admin.common.inactive')}
                  </span>
                </td>
                <td style={tdStyle}>
                  <button type="button" style={{ ...btnSmall, border: '1px solid #007bff', background: 'transparent', color: '#007bff' }}
                    onClick={() => handleEdit(company)}>{t('admin.common.edit')}</button>
                  <button type="button" style={{ ...btnSmall, border: `1px solid ${company.isActive ? '#6c757d' : '#28a745'}`, background: 'transparent', color: company.isActive ? '#6c757d' : '#28a745' }}
                    onClick={() => toggleActive(company)}>{company.isActive ? t('admin.common.deactivate') : t('admin.common.activate')}</button>
                  <button type="button" style={{ ...btnSmall, border: '1px solid #dc3545', background: 'transparent', color: '#dc3545', marginRight: 0 }}
                    onClick={() => handleDelete(company.companyId)}>{t('admin.common.delete')}</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {companies.length === 0 && <div style={emptyRowStyle}>{t('admin.warehouse.empty_companies')}</div>}
      </div>
    </div>
  );
}

// ======================= Головна сторінка =======================
function AdminWarehousePage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState('incoming');
  const [products, setProducts] = useState([]);
  const [companies, setCompanies] = useState([]);
  const [loading, setLoading] = useState(true);

  const loadCompanies = useCallback(async () => {
    try {
      const res = await companiesAPI.getAll();
      setCompanies(res.data?.items || res.data || []);
    } catch (err) {
      console.error('Не вдалося завантажити компанії', err);
      setCompanies([]);
    }
  }, []);

  useEffect(() => {
    (async () => {
      setLoading(true);
      try {
        const [prodRes, compRes] = await Promise.all([
          productsAPI.getAll({ pageNumber: 1, pageSize: 500 }),
          companiesAPI.getAll(),
        ]);
        setProducts(prodRes.data?.items || prodRes.data || []);
        setCompanies(compRes.data?.items || compRes.data || []);
      } catch (err) {
        console.error('Не вдалося завантажити довідники', err);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const tabs = [
    { key: 'incoming', label: t('admin.warehouse.tab_incoming') },
    { key: 'outgoing', label: t('admin.warehouse.tab_outgoing') },
    { key: 'stock', label: t('admin.warehouse.tab_stock') },
    { key: 'companies', label: t('admin.warehouse.tab_companies') },
  ];

  return (
    <div className="admin-warehouse" style={{ padding: 20, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 20 }}>
        <Link to="/admin" className="back-link" style={{ textDecoration: 'none', color: '#666', display: 'block', marginBottom: 10 }}>
          {t('admin.common.back_to_dashboard')}
        </Link>
        <h1>{t('admin.warehouse.title')}</h1>
      </div>

      <div style={tabsStyle}>
        {tabs.map((tab) => (
          <button key={tab.key} type="button"
            onClick={() => setActiveTab(tab.key)}
            style={{
              padding: '10px 20px', borderRadius: 8, cursor: 'pointer',
              border: activeTab === tab.key ? '1px solid #007bff' : '1px solid #ddd',
              background: activeTab === tab.key ? '#007bff' : '#fff',
              color: activeTab === tab.key ? '#fff' : '#333',
              fontWeight: 500,
            }}>
            {tab.label}
          </button>
        ))}
      </div>

      {loading && <div style={emptyRowStyle}>{t('common.loading')}</div>}

      {!loading && activeTab === 'incoming' && <IncomingTab products={products} companies={companies} />}
      {!loading && activeTab === 'outgoing' && <OutgoingTab products={products} companies={companies} />}
      {!loading && activeTab === 'stock' && <StockTab />}
      {!loading && activeTab === 'companies' && <CompaniesTab companies={companies} reloadCompanies={loadCompanies} />}
    </div>
  );
}

export default AdminWarehousePage;
