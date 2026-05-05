import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { companiesAPI } from '../../services/api';

function CompaniesAdmin() {
  const [companies, setCompanies] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingCompany, setEditingCompany] = useState(null);
  const [form, setForm] = useState({
    name: '',
    contactPerson: '',
    phone: '',
    email: '',
    address: '',
    notes: '',
    isActive: true
  });

  useEffect(() => {
    loadCompanies();
  }, []);

  const loadCompanies = async () => {
    try {
      const res = await companiesAPI.getAll();
      setCompanies(res.data);
    } catch (err) {
      console.error('Не вдалося завантажити компанії', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingCompany) {
        await companiesAPI.update(editingCompany.companyId, form);
      } else {
        await companiesAPI.create(form);
      }
      setShowForm(false);
      setEditingCompany(null);
      resetForm();
      loadCompanies();
    } catch (err) {
      alert(err.response?.data?.errors?.[0] || 'Не вдалося зберегти компанію');
    }
  };

  const handleEdit = (company) => {
    setEditingCompany(company);
    setForm({
      name: company.name,
      contactPerson: company.contactPerson || '',
      phone: company.phone || '',
      email: company.email || '',
      address: company.address || '',
      notes: company.notes || '',
      isActive: company.isActive
    });
    setShowForm(true);
  };

  const handleDelete = async (companyId) => {
    if (!window.confirm('Ви впевнені, що хочете видалити/деактивувати цю компанію?')) return;
    try {
      await companiesAPI.delete(companyId);
      loadCompanies();
    } catch (err) {
      alert('Не вдалося видалити компанію');
    }
  };

  const resetForm = () => {
    setForm({
      name: '',
      contactPerson: '',
      phone: '',
      email: '',
      address: '',
      notes: '',
      isActive: true
    });
  };

  if (loading) return <div>Завантаження...</div>;

  return (
    <div style={{ padding: 20, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 20 }}>
        <Link to="/admin" style={{ textDecoration: 'none', color: '#666', display: 'block', marginBottom: 10 }}>
          ← Повернутися до дашборду
        </Link>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h1>Компанії-постачальники</h1>
          <button 
            onClick={() => { setShowForm(true); setEditingCompany(null); resetForm(); }}
            style={{ padding: '10px 20px', background: '#007bff', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer' }}
          >
            + Додати компанію
          </button>
        </div>
      </div>

      {showForm && (
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.08)', marginBottom: 30 }}>
          <h2 style={{ marginBottom: 20 }}>{editingCompany ? 'Редагувати компанію' : 'Нова компанія'}</h2>
          <form onSubmit={handleSubmit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
              <div>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Назва компанії *</label>
                <input
                  type="text"
                  required
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                  style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #ddd' }}
                />
              </div>
              <div>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Контактна особа</label>
                <input
                  type="text"
                  value={form.contactPerson}
                  onChange={(e) => setForm({ ...form, contactPerson: e.target.value })}
                  style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #ddd' }}
                />
              </div>
              <div>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Телефон</label>
                <input
                  type="text"
                  value={form.phone}
                  onChange={(e) => setForm({ ...form, phone: e.target.value })}
                  placeholder="+380XXXXXXXXX"
                  style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #ddd' }}
                />
              </div>
              <div>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Email</label>
                <input
                  type="email"
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                  style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #ddd' }}
                />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Адреса</label>
                <input
                  type="text"
                  value={form.address}
                  onChange={(e) => setForm({ ...form, address: e.target.value })}
                  style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #ddd' }}
                />
              </div>
              <div style={{ gridColumn: '1 / -1' }}>
                <label style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Примітки</label>
                <textarea
                  value={form.notes}
                  onChange={(e) => setForm({ ...form, notes: e.target.value })}
                  rows={3}
                  style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #ddd' }}
                />
              </div>
              <div>
                <label style={{ display: 'flex', alignItems: 'center', cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={form.isActive}
                    onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                    style={{ marginRight: 8 }}
                  />
                  Активна
                </label>
              </div>
            </div>
            <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
              <button
                type="submit"
                style={{ padding: '12px 24px', background: '#28a745', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer' }}
              >
                {editingCompany ? 'Зберегти зміни' : 'Створити компанію'}
              </button>
              <button
                type="button"
                onClick={() => { setShowForm(false); setEditingCompany(null); }}
                style={{ padding: '12px 24px', background: '#6c757d', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer' }}
              >
                Скасувати
              </button>
            </div>
          </form>
        </div>
      )}

      <div style={{ background: '#fff', borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead style={{ background: '#f8f9fa' }}>
            <tr>
              <th style={{ padding: 16, textAlign: 'left', borderBottom: '1px solid #eee' }}>Назва</th>
              <th style={{ padding: 16, textAlign: 'left', borderBottom: '1px solid #eee' }}>Контакт</th>
              <th style={{ padding: 16, textAlign: 'left', borderBottom: '1px solid #eee' }}>Телефон</th>
              <th style={{ padding: 16, textAlign: 'left', borderBottom: '1px solid #eee' }}>Email</th>
              <th style={{ padding: 16, textAlign: 'left', borderBottom: '1px solid #eee' }}>Статус</th>
              <th style={{ padding: 16, textAlign: 'left', borderBottom: '1px solid #eee' }}>Дії</th>
            </tr>
          </thead>
          <tbody>
            {companies.map((company) => (
              <tr key={company.companyId} style={{ borderBottom: '1px solid #f0f0f0' }}>
                <td style={{ padding: 16 }}>
                  <strong>{company.name}</strong>
                </td>
                <td style={{ padding: 16 }}>{company.contactPerson || '-'}</td>
                <td style={{ padding: 16 }}>{company.phone || '-'}</td>
                <td style={{ padding: 16 }}>{company.email || '-'}</td>
                <td style={{ padding: 16 }}>
                  <span style={{ 
                    padding: '4px 12px', 
                    borderRadius: 20, 
                    fontSize: '0.85rem',
                    background: company.isActive ? '#d4edda' : '#f8d7da',
                    color: company.isActive ? '#155724' : '#721c24'
                  }}>
                    {company.isActive ? 'Активна' : 'Деактивована'}
                  </span>
                </td>
                <td style={{ padding: 16 }}>
                  <button
                    onClick={() => handleEdit(company)}
                    style={{ padding: '6px 12px', marginRight: 8, border: '1px solid #007bff', background: 'transparent', color: '#007bff', borderRadius: 6, cursor: 'pointer' }}
                  >
                    Редагувати
                  </button>
                  <button
                    onClick={() => handleDelete(company.companyId)}
                    style={{ padding: '6px 12px', border: '1px solid #dc3545', background: 'transparent', color: '#dc3545', borderRadius: 6, cursor: 'pointer' }}
                  >
                    Видалити
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {companies.length === 0 && (
          <div style={{ padding: 40, textAlign: 'center', color: '#666' }}>
            Компаній поки що немає
          </div>
        )}
      </div>
    </div>
  );
}

export default CompaniesAdmin;
