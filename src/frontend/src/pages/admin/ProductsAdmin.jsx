import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { productsAPI, categoriesAPI, adminProductsAPI } from '../../services/api';

function ProductsAdmin() {
  const { user, isAuthenticated, loading } = useAuth();
  const navigate = useNavigate();

  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [pageLoading, setPageLoading] = useState(true);
  const [error, setError] = useState(null);

  const [form, setForm] = useState({
    name: '',
    description: '',
    price: '',
    weight: '',
    categoryId: '',
    stock: 0
  });
  const [editId, setEditId] = useState(null);

  useEffect(() => {
    if (loading) return;
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    if (user?.role !== 'Manager' && user?.role !== 'Administrator') {
      navigate('/');
      return;
    }
    loadData();
  }, [loading, isAuthenticated, user, navigate]);

  const loadData = async () => {
    try {
      setPageLoading(true);
      const [prodRes, catRes] = await Promise.all([
        productsAPI.getAll(),
        categoriesAPI.getAll()
      ]);
      setProducts(prodRes.data);
      setCategories(catRes.data);
      setError(null);
    } catch (e) {
      setError('Помилка завантаження даних');
    } finally {
      setPageLoading(false);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const resetForm = () => {
    setForm({
      name: '',
      description: '',
      price: '',
      weight: '',
      categoryId: '',
      stock: 0
    });
    setEditId(null);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const payload = {
        name: form.name,
        description: form.description,
        price: Number(form.price),
        weight: Number(form.weight),
        categoryId: Number(form.categoryId),
        stock: Number(form.stock) || 0
      };
      if (editId) {
        await adminProductsAPI.update(editId, payload);
      } else {
        await adminProductsAPI.create(payload);
      }
      await loadData();
      resetForm();
    } catch (err) {
      alert(err.response?.data?.error || 'Помилка збереження товару');
    }
  };

  const startEdit = (p) => {
    setEditId(p.productId);
    setForm({
      name: p.name,
      description: p.description,
      price: p.price,
      weight: p.weight,
      categoryId: p.categoryId,
      stock: p.stock
    });
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Видалити товар?')) return;
    try {
      await adminProductsAPI.delete(id);
      await loadData();
    } catch (err) {
      alert(err.response?.data?.error || 'Помилка видалення товару');
    }
  };

  if (loading) return <div>Завантаження...</div>;
  if (pageLoading) return <div>Завантаження...</div>;
  if (error) return <div>{error}</div>;

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: 20 }}>
      <div style={{ marginBottom: 30 }}>
        <Link to="/admin" style={{ textDecoration: 'none', color: '#666', display: 'block', marginBottom: 10 }}>
          ← Назад до дашборду
        </Link>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h1>Керування товарами</h1>
          <button 
            onClick={() => { resetForm(); setEditId(null); }}
            style={{ padding: '10px 20px', background: editId ? '#ffc107' : '#28a745', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer' }}
          >
            {editId ? 'Скасувати редагування' : '+ Додати товар'}
          </button>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: 30 }}>
        {/* Форма */}
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', height: 'fit-content' }}>
          <h3 style={{ marginBottom: 20 }}>{editId ? 'Редагувати товар' : 'Новий товар'}</h3>
          <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 15 }}>
            <div className="form-group">
              <label style={labelStyle}>Назва *</label>
              <input name="name" value={form.name} onChange={handleChange} placeholder="Назва товару" style={inputStyle} required />
            </div>
            
            <div className="form-group">
              <label style={labelStyle}>Опис</label>
              <textarea name="description" value={form.description} onChange={handleChange} placeholder="Опис товару" rows={4} style={inputStyle} />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 15 }}>
              <div className="form-group">
                <label style={labelStyle}>Ціна (грн) *</label>
                <input name="price" type="number" step="0.01" value={form.price} onChange={handleChange} style={inputStyle} required />
              </div>
              <div className="form-group">
                <label style={labelStyle}>Вага (кг) *</label>
                <input name="weight" type="number" step="0.001" value={form.weight} onChange={handleChange} style={inputStyle} required />
              </div>
            </div>

            <div className="form-group">
              <label style={labelStyle}>Категорія *</label>
              <select name="categoryId" value={form.categoryId} onChange={handleChange} style={inputStyle} required>
                <option value="">Оберіть категорію</option>
                {categories.map((c) => (
                  <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label style={labelStyle}>Залишок на складі</label>
              <input name="stock" type="number" value={form.stock} onChange={handleChange} style={inputStyle} />
            </div>

            <button type="submit" style={{ padding: '12px', background: '#007bff', color: '#fff', border: 'none', borderRadius: 8, fontWeight: 600, cursor: 'pointer', marginTop: 10 }}>
              {editId ? 'Зберегти зміни' : 'Створити товар'}
            </button>
          </form>
        </div>

        {/* Таблиця */}
        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)' }}>
          <h3 style={{ marginBottom: 20 }}>Список товарів ({products.length})</h3>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '2px solid #eee', textAlign: 'left' }}>
                  <th style={thStyle}>Товар</th>
                  <th style={thStyle}>Ціна</th>
                  <th style={thStyle}>Склад</th>
                  <th style={thStyle}>Дії</th>
                </tr>
              </thead>
              <tbody>
                {products.map((p) => (
                  <tr key={p.productId} style={{ borderBottom: '1px solid #f0f0f0' }}>
                    <td style={tdStyle}>
                      <div style={{ fontWeight: 500 }}>{p.name}</div>
                      <div style={{ fontSize: '0.8rem', color: '#888' }}>ID: {p.productId}</div>
                    </td>
                    <td style={tdStyle}>{p.price.toFixed(2)} грн</td>
                    <td style={tdStyle}>
                      <span style={{ 
                        padding: '2px 8px', 
                        borderRadius: 4, 
                        fontSize: '0.85rem',
                        background: p.stock > 0 ? '#e6ffed' : '#fff1f0',
                        color: p.stock > 0 ? '#28a745' : '#cf1322'
                      }}>
                        {p.stock} шт
                      </span>
                    </td>
                    <td style={tdStyle}>
                      <div style={{ display: 'flex', gap: 10 }}>
                        <button onClick={() => startEdit(p)} style={actionBtnStyle('#ffc107')}>✎</button>
                        <button onClick={() => handleDelete(p.productId)} style={actionBtnStyle('#dc3545')}>✕</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}

const inputStyle = {
  width: '100%',
  padding: '10px',
  borderRadius: 6,
  border: '1px solid #ddd',
  fontSize: '0.95rem'
};

const labelStyle = {
  display: 'block',
  marginBottom: 5,
  fontSize: '0.9rem',
  fontWeight: 600,
  color: '#555'
};

const thStyle = { padding: '12px 8px', color: '#666', fontWeight: 600 };
const tdStyle = { padding: '15px 8px' };

const actionBtnStyle = (color) => ({
  background: 'none',
  border: `1px solid ${color}`,
  color: color,
  padding: '4px 8px',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: '1rem'
});

export default ProductsAdmin;
