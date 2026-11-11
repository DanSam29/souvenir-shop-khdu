import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { warehouseAPI, companiesAPI, productsAPI } from '../services/api';

// Спільні inline-стилі (у стилі інших адмін-сторінок проєкту)
const tabsStyle = { display: 'flex', gap: 8, marginBottom: 20, flexWrap: 'wrap' };
const cardStyle = { background: '#fff', borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', overflow: 'hidden' };
const tableStyle = { width: '100%', borderCollapse: 'collapse' };
const thStyle = { padding: 16, textAlign: 'left', borderBottom: '1px solid #eee' };
const tdStyle = { padding: 16, borderBottom: '1px solid #f0f0f0' };
const inputStyle = { width: '100%', padding: 10, borderRadius: 8, border: '1px solid #ddd' };
const labelStyle = { display: 'block', marginBottom: 8, fontWeight: 500 };
const btnStyle = { padding: '10px 20px', border: 'none', borderRadius: 8, cursor: 'pointer', color: '#fff' };
const btnSmall = { padding: '6px 12px', marginRight: 8, borderRadius: 6, cursor: 'pointer' };
const emptyRowStyle = { padding: 40, textAlign: 'center', color: '#666' };

// Допустимі причини (узгоджені з бекендом-валідатором)
const INCOMING_REASONS = [
  { value: 'INCOMING', label: 'Надходження' },
  { value: 'RETURN', label: 'Повернення' },
];
const OUTGOING_REASONS = [
  { value: 'Damaged', label: 'Пошкоджено' },
  { value: 'Lost', label: 'Втрачено' },
  { value: 'Return', label: 'Повернення' },
  { value: 'Inventory', label: 'Інвентаризація' },
  { value: 'Other', label: 'Інше' },
];

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
      alert(errMessage(err, 'Не вдалося зберегти прибуткову накладну'));
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Видалити прибуткову накладну? Залишки буде перераховано.')) return;
    try {
      await warehouseAPI.deleteIncoming(id);
      load();
    } catch (err) {
      alert(errMessage(err, 'Не вдалося видалити документ'));
    }
  };

  const companyRequired = form.reason !== 'RETURN';

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <button type="button" style={{ ...btnStyle, background: '#007bff' }}
          onClick={() => { resetForm(); setShowForm(true); }}>
          + Створити накладну
        </button>
      </div>

      {showForm && (
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)', marginBottom: 30 }}>
          <h3 style={{ marginBottom: 20 }}>{editingId ? 'Редагувати прибуткову накладну' : 'Нова прибуткова накладна'}</h3>
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div>
                <label style={labelStyle}>Товар *</label>
                <select required value={form.productId} style={inputStyle}
                  onChange={(e) => setForm({ ...form, productId: e.target.value })}>
                  <option value="">— Оберіть товар —</option>
                  {products.map((p) => <option key={p.productId} value={p.productId}>{p.name}</option>)}
                </select>
              </div>
              <div>
                <label style={labelStyle}>Кількість *</label>
                <input type="number" min="1" required value={form.quantity} style={inputStyle}
                  onChange={(e) => setForm({ ...form, quantity: e.target.value })} />
              </div>
              <div>
                <label style={labelStyle}>Ціна закупівлі, ₴ *</label>
                <input type="number" min="0.01" step="0.01" required value={form.purchasePrice} style={inputStyle}
                  onChange={(e) => setForm({ ...form, purchasePrice: e.target.value })} />
              </div>
              <div>
                <label style={labelStyle}>Причина *</label>
                <select value={form.reason} style={inputStyle}
                  onChange={(e) => setForm({ ...form, reason: e.target.value })}>
                  {INCOMING_REASONS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
                </select>
              </div>
              <div>
                <label style={labelStyle}>Компанія-постачальник {companyRequired ? '*' : '(необов’язково)'}</label>
                <select required={companyRequired} value={form.companyId} style={inputStyle}
                  onChange={(e) => setForm({ ...form, companyId: e.target.value })}>
                  <option value="">— Оберіть компанію —</option>
                  {companies.filter((c) => c.isActive).map((c) => <option key={c.companyId} value={c.companyId}>{c.name}</option>)}
                </select>
              </div>
              <div>
                <label style={labelStyle}>Дата документа</label>
                <input type="date" value={form.documentDate} style={inputStyle}
                  onChange={(e) => setForm({ ...form, documentDate: e.target.value })} />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label style={labelStyle}>Примітки</label>
                <textarea rows={2} value={form.notes} style={inputStyle}
                  onChange={(e) => setForm({ ...form, notes: e.target.value })} />
              </div>
            </div>
            <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
              <button type="submit" style={{ ...btnStyle, background: '#28a745' }}>
                {editingId ? 'Зберегти зміни' : 'Створити'}
              </button>
              <button type="button" style={{ ...btnStyle, background: '#6c757d' }}
                onClick={() => { setShowForm(false); resetForm(); }}>
                Скасувати
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? <div style={emptyRowStyle}>Завантаження...</div> : (
        <div style={cardStyle}>
          <table style={tableStyle}>
            <thead style={{ background: '#f8f9fa' }}>
              <tr>
                <th style={thStyle}>Товар</th>
                <th style={thStyle}>Кількість</th>
                <th style={thStyle}>Ціна закупівлі</th>
                <th style={thStyle}>Причина</th>
                <th style={thStyle}>Компанія</th>
                <th style={thStyle}>Дата</th>
                <th style={thStyle}>Дії</th>
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
                      onClick={() => handleEdit(doc)}>Редагувати</button>
                    <button type="button" style={{ ...btnSmall, border: '1px solid #dc3545', background: 'transparent', color: '#dc3545', marginRight: 0 }}
                      onClick={() => handleDelete(doc.documentId)}>Видалити</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {items.length === 0 && <div style={emptyRowStyle}>Прибуткових накладних ще немає</div>}
        </div>
      )}
    </div>
  );
}

// ======================= Таб: Видаткові накладні =======================
function OutgoingTab({ products, companies }) {
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
      alert(errMessage(err, 'Не вдалося зберегти видаткову накладну'));
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Видалити видаткову накладну? Залишки буде перераховано.')) return;
    try {
      await warehouseAPI.deleteOutgoing(id);
      load();
    } catch (err) {
      alert(errMessage(err, 'Не вдалося видалити документ'));
    }
  };

  const companyRequired = form.reason === 'Return';

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <button type="button" style={{ ...btnStyle, background: '#007bff' }}
          onClick={() => { resetForm(); setShowForm(true); }}>
          + Створити накладну
        </button>
      </div>

      {showForm && (
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)', marginBottom: 30 }}>
          <h3 style={{ marginBottom: 20 }}>{editingId ? 'Редагувати видаткову накладну' : 'Нова видаткова накладна'}</h3>
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div>
                <label style={labelStyle}>Товар *</label>
                <select required value={form.productId} style={inputStyle}
                  onChange={(e) => setForm({ ...form, productId: e.target.value })}>
                  <option value="">— Оберіть товар —</option>
                  {products.map((p) => <option key={p.productId} value={p.productId}>{p.name}</option>)}
                </select>
              </div>
              <div>
                <label style={labelStyle}>Кількість *</label>
                <input type="number" min="1" required value={form.quantity} style={inputStyle}
                  onChange={(e) => setForm({ ...form, quantity: e.target.value })} />
              </div>
              <div>
                <label style={labelStyle}>Причина *</label>
                <select value={form.reason} style={inputStyle}
                  onChange={(e) => setForm({ ...form, reason: e.target.value })}>
                  {OUTGOING_REASONS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
                </select>
              </div>
              <div>
                <label style={labelStyle}>Компанія {companyRequired ? '*' : '(необов’язково)'}</label>
                <select required={companyRequired} value={form.companyId} style={inputStyle}
                  onChange={(e) => setForm({ ...form, companyId: e.target.value })}>
                  <option value="">— Оберіть компанію —</option>
                  {companies.filter((c) => c.isActive).map((c) => <option key={c.companyId} value={c.companyId}>{c.name}</option>)}
                </select>
              </div>
              <div>
                <label style={labelStyle}>Дата документа</label>
                <input type="date" value={form.documentDate} style={inputStyle}
                  onChange={(e) => setForm({ ...form, documentDate: e.target.value })} />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label style={labelStyle}>Примітки</label>
                <textarea rows={2} value={form.notes} style={inputStyle}
                  onChange={(e) => setForm({ ...form, notes: e.target.value })} />
              </div>
            </div>
            <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
              <button type="submit" style={{ ...btnStyle, background: '#28a745' }}>
                {editingId ? 'Зберегти зміни' : 'Створити'}
              </button>
              <button type="button" style={{ ...btnStyle, background: '#6c757d' }}
                onClick={() => { setShowForm(false); resetForm(); }}>
                Скасувати
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? <div style={emptyRowStyle}>Завантаження...</div> : (
        <div style={cardStyle}>
          <table style={tableStyle}>
            <thead style={{ background: '#f8f9fa' }}>
              <tr>
                <th style={thStyle}>Товар</th>
                <th style={thStyle}>Кількість</th>
                <th style={thStyle}>Причина</th>
                <th style={thStyle}>Компанія</th>
                <th style={thStyle}>Дата</th>
                <th style={thStyle}>Дії</th>
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
                      <span style={{ color: '#888', fontSize: '0.85rem' }}>створено системою</span>
                    ) : (
                      <>
                        <button type="button" style={{ ...btnSmall, border: '1px solid #007bff', background: 'transparent', color: '#007bff' }}
                          onClick={() => handleEdit(doc)}>Редагувати</button>
                        <button type="button" style={{ ...btnSmall, border: '1px solid #dc3545', background: 'transparent', color: '#dc3545', marginRight: 0 }}
                          onClick={() => handleDelete(doc.documentId)}>Видалити</button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {items.length === 0 && <div style={emptyRowStyle}>Видаткових накладних ще немає</div>}
        </div>
      )}
    </div>
  );
}

// ======================= Таб: Поточні залишки =======================
function StockTab() {
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
      alert('Не вдалося експортувати залишки');
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
          Лише низький залишок
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

      {loading ? <div style={emptyRowStyle}>Завантаження...</div> : (
        <div style={cardStyle}>
          <table style={tableStyle}>
            <thead style={{ background: '#f8f9fa' }}>
              <tr>
                <th style={thStyle}>Товар</th>
                <th style={thStyle}>Загальний прихід</th>
                <th style={thStyle}>Загальний розхід</th>
                <th style={thStyle}>Поточний залишок</th>
                <th style={thStyle}>Статус</th>
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
                        Низький залишок
                      </span>
                    ) : (
                      <span style={{ padding: '4px 12px', borderRadius: 20, fontSize: '0.85rem', background: '#d4edda', color: '#155724' }}>
                        Достатньо
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {visible.length === 0 && (
            <div style={emptyRowStyle}>{lowStockOnly ? 'Товарів із низьким залишком немає' : 'Даних по складу ще немає'}</div>
          )}
        </div>
      )}
    </div>
  );
}

// ======================= Таб: Компанії =======================
function CompaniesTab({ companies, reloadCompanies }) {
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
      alert(errMessage(err, 'Не вдалося зберегти компанію'));
    }
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Видалити/деактивувати цю компанію?')) return;
    try {
      await companiesAPI.delete(id);
      reloadCompanies();
    } catch (err) {
      alert(errMessage(err, 'Не вдалося видалити компанію'));
    }
  };

  const toggleActive = async (company) => {
    try {
      if (company.isActive) await companiesAPI.deactivate(company.companyId);
      else await companiesAPI.activate(company.companyId);
      reloadCompanies();
    } catch (err) {
      alert(errMessage(err, 'Не вдалося змінити статус компанії'));
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <button type="button" style={{ ...btnStyle, background: '#007bff' }}
          onClick={() => { resetForm(); setShowForm(true); }}>
          + Додати компанію
        </button>
      </div>

      {showForm && (
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)', marginBottom: 30 }}>
          <h3 style={{ marginBottom: 20 }}>{editingId ? 'Редагувати компанію' : 'Нова компанія'}</h3>
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div>
                <label style={labelStyle}>Назва компанії *</label>
                <input type="text" required value={form.name} style={inputStyle}
                  onChange={(e) => setForm({ ...form, name: e.target.value })} />
              </div>
              <div>
                <label style={labelStyle}>Назва (EN)</label>
                <input type="text" value={form.nameEn} style={inputStyle}
                  onChange={(e) => setForm({ ...form, nameEn: e.target.value })} />
              </div>
              <div>
                <label style={labelStyle}>Контактна особа</label>
                <input type="text" value={form.contactPerson} style={inputStyle}
                  onChange={(e) => setForm({ ...form, contactPerson: e.target.value })} />
              </div>
              <div>
                <label style={labelStyle}>Телефон</label>
                <input type="text" placeholder="+380XXXXXXXXX" value={form.phone} style={inputStyle}
                  onChange={(e) => setForm({ ...form, phone: e.target.value })} />
              </div>
              <div>
                <label style={labelStyle}>Email</label>
                <input type="email" value={form.email} style={inputStyle}
                  onChange={(e) => setForm({ ...form, email: e.target.value })} />
              </div>
              <div>
                <label style={labelStyle}>Адреса</label>
                <input type="text" value={form.address} style={inputStyle}
                  onChange={(e) => setForm({ ...form, address: e.target.value })} />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label style={labelStyle}>Примітки</label>
                <textarea rows={2} value={form.notes} style={inputStyle}
                  onChange={(e) => setForm({ ...form, notes: e.target.value })} />
              </div>
              <div>
                <label style={{ display: 'flex', alignItems: 'center', cursor: 'pointer' }}>
                  <input type="checkbox" checked={form.isActive} style={{ marginRight: 8 }}
                    onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
                  Активна
                </label>
              </div>
            </div>
            <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
              <button type="submit" style={{ ...btnStyle, background: '#28a745' }}>
                {editingId ? 'Зберегти зміни' : 'Створити компанію'}
              </button>
              <button type="button" style={{ ...btnStyle, background: '#6c757d' }}
                onClick={() => { setShowForm(false); resetForm(); }}>
                Скасувати
              </button>
            </div>
          </form>
        </div>
      )}

      <div style={cardStyle}>
        <table style={tableStyle}>
          <thead style={{ background: '#f8f9fa' }}>
            <tr>
              <th style={thStyle}>Назва</th>
              <th style={thStyle}>Контакт</th>
              <th style={thStyle}>Телефон</th>
              <th style={thStyle}>Email</th>
              <th style={thStyle}>Статус</th>
              <th style={thStyle}>Дії</th>
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
                    {company.isActive ? 'Активна' : 'Деактивована'}
                  </span>
                </td>
                <td style={tdStyle}>
                  <button type="button" style={{ ...btnSmall, border: '1px solid #007bff', background: 'transparent', color: '#007bff' }}
                    onClick={() => handleEdit(company)}>Редагувати</button>
                  <button type="button" style={{ ...btnSmall, border: `1px solid ${company.isActive ? '#6c757d' : '#28a745'}`, background: 'transparent', color: company.isActive ? '#6c757d' : '#28a745' }}
                    onClick={() => toggleActive(company)}>{company.isActive ? 'Деактивувати' : 'Активувати'}</button>
                  <button type="button" style={{ ...btnSmall, border: '1px solid #dc3545', background: 'transparent', color: '#dc3545', marginRight: 0 }}
                    onClick={() => handleDelete(company.companyId)}>Видалити</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {companies.length === 0 && <div style={emptyRowStyle}>Компаній поки що немає</div>}
      </div>
    </div>
  );
}

// ======================= Головна сторінка =======================
function AdminWarehousePage() {
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
    { key: 'incoming', label: 'Прибуткові' },
    { key: 'outgoing', label: 'Видаткові' },
    { key: 'stock', label: 'Поточні залишки' },
    { key: 'companies', label: 'Компанії' },
  ];

  return (
    <div className="admin-warehouse" style={{ padding: 20, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 20 }}>
        <Link to="/admin" className="back-link" style={{ textDecoration: 'none', color: '#666', display: 'block', marginBottom: 10 }}>
          ← Назад до дашборду
        </Link>
        <h1>Склад</h1>
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

      {loading && <div style={emptyRowStyle}>Завантаження...</div>}

      {!loading && activeTab === 'incoming' && <IncomingTab products={products} companies={companies} />}
      {!loading && activeTab === 'outgoing' && <OutgoingTab products={products} companies={companies} />}
      {!loading && activeTab === 'stock' && <StockTab />}
      {!loading && activeTab === 'companies' && <CompaniesTab companies={companies} reloadCompanies={loadCompanies} />}
    </div>
  );
}

export default AdminWarehousePage;
