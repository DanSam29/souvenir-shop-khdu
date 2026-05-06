import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { categoriesAPI, adminCategoriesAPI, productsAPI } from '../../services/api';

function CategoriesAdmin() {
  const { user, isAuthenticated, loading } = useAuth();
  const navigate = useNavigate();

  const [categoriesTree, setCategoriesTree] = useState([]);
  const [products, setProducts] = useState([]);
  const [pageLoading, setPageLoading] = useState(true);
  const [error, setError] = useState(null);

  const [form, setForm] = useState({
    name: '',
    parentCategoryId: '',
    description: '',
    displayOrder: 0
  });

  const [editId, setEditId] = useState(null);
  const [showForm, setShowForm] = useState(false);

  useEffect(() => {
    if (loading) return;
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    if (user?.role !== 'Manager' && user?.role !== 'Administrator' && user?.role !== 'SuperAdmin') {
      navigate('/');
      return;
    }
    loadData();
  }, [loading, isAuthenticated, user, navigate]);

  const loadData = async () => {
    try {
      setPageLoading(true);
      const [catRes, prodRes] = await Promise.all([
        categoriesAPI.getAll(),
        productsAPI.getAll()
      ]);
      setCategoriesTree(catRes.data);
      setProducts(prodRes.data);
      setError(null);
    } catch (e) {
      setError('Помилка завантаження даних');
    } finally {
      setPageLoading(false);
    }
  };

  const loadCategories = loadData; // Alias for backward compatibility in the component logic

  const getCategoryProductCount = (category) => {
    // Рахуємо товари в поточній категорії
    let count = products.filter(p => p.categoryId === category.categoryId).length;
    
    // Додаємо товари з усіх підкатегорій
    if (category.subCategories && category.subCategories.length > 0) {
      category.subCategories.forEach(sub => {
        count += getCategoryProductCount(sub);
      });
    }
    return count;
  };

  const getDirectProductCount = (categoryId) => {
    return products.filter(p => p.categoryId === categoryId).length;
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const resetForm = () => {
    setForm({
      name: '',
      parentCategoryId: '',
      description: '',
      displayOrder: 0
    });
    setEditId(null);
    setShowForm(false);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const payload = {
        name: form.name,
        parentCategoryId: form.parentCategoryId ? Number(form.parentCategoryId) : null,
        description: form.description || null,
        displayOrder: Number(form.displayOrder) || 0
      };
      if (editId) {
        await adminCategoriesAPI.update(editId, payload);
      } else {
        await adminCategoriesAPI.create(payload);
      }
      await loadCategories();
      resetForm();
    } catch (err) {
      alert(err.response?.data?.error || 'Помилка збереження категорії');
    }
  };

  const startEdit = (cat) => {
    setEditId(cat.categoryId);
    setForm({
      name: cat.name,
      parentCategoryId: cat.parentCategoryId || '',
      description: cat.description || '',
      displayOrder: cat.displayOrder || 0
    });
    setShowForm(true);
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Видалити категорію? Увага: категорія має бути порожньою.')) return;
    try {
      await adminCategoriesAPI.delete(id);
      await loadCategories();
    } catch (err) {
      alert(err.response?.data?.message || 'Помилка видалення категорії');
    }
  };

  const CategoryRow = ({ category, level = 0 }) => {
    const productCount = level === 0 ? getCategoryProductCount(category) : getDirectProductCount(category.categoryId);
    
    return (
      <React.Fragment>
        <tr style={{ borderBottom: '1px solid #f0f0f0', background: level === 0 ? '#fafafa' : '#fff' }}>
          <td style={{ padding: '12px 8px', paddingLeft: level * 30 + 10 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              {level > 0 && '↳ '}
              <span style={{ fontWeight: level === 0 ? 600 : 400 }}>{category.name}</span>
            </div>
          </td>
          <td style={{ padding: '12px 8px', color: '#666', fontSize: '0.85rem' }}>{category.description || '—'}</td>
          <td style={{ padding: '12px 8px', textAlign: 'center' }}>
            <span style={{ 
              padding: '2px 10px', 
              borderRadius: 12, 
              background: productCount > 0 ? '#e6f7ff' : '#f5f5f5',
              color: productCount > 0 ? '#1890ff' : '#bfbfbf',
              fontSize: '0.85rem',
              fontWeight: 600
            }}>
              {productCount}
            </span>
          </td>
          <td style={{ padding: '12px 8px' }}>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
              <button onClick={() => startEdit(category)} style={actionBtnStyle('#ffc107')}>✎</button>
              <button onClick={() => handleDelete(category.categoryId)} style={actionBtnStyle('#dc3545')}>✕</button>
            </div>
          </td>
        </tr>
        {category.subCategories && category.subCategories.map(sub => (
          <CategoryRow key={sub.categoryId} category={sub} level={level + 1} />
        ))}
      </React.Fragment>
    );
  };

  if (loading) return <div>Завантаження...</div>;
  if (pageLoading) return <div>Завантаження...</div>;
  if (error) return <div>{error}</div>;

  return (
    <div style={{ maxWidth: 1000, margin: '0 auto', padding: 20 }}>
      <div style={{ marginBottom: 30 }}>
        <Link to="/admin" style={{ textDecoration: 'none', color: '#007bff', fontWeight: 600, display: 'block', marginBottom: 10 }}>
          ← Назад до дашборду
        </Link>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h1>Керування категоріями</h1>
          <button 
            onClick={() => {
              if (showForm) resetForm();
              else setShowForm(true);
            }}
            style={{ padding: '10px 20px', background: showForm ? '#ffc107' : '#28a745', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer', fontWeight: 600 }}
          >
            {showForm ? '← Скасувати' : '+ Додати категорію'}
          </button>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: showForm ? '1fr 2fr' : '1fr', gap: 30 }}>
        {showForm && (
          <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', height: 'fit-content' }}>
            <h3 style={{ marginBottom: 20 }}>{editId ? 'Редагувати' : 'Нова категорія'}</h3>
            <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 15 }}>
              <div className="form-group">
                <label style={labelStyle}>Назва *</label>
                <input name="name" value={form.name} onChange={handleChange} style={inputStyle} required />
              </div>
              
              <div className="form-group">
                <label style={labelStyle}>Батьківська категорія</label>
                <select name="parentCategoryId" value={form.parentCategoryId} onChange={handleChange} style={inputStyle}>
                  <option value="">Немає (головна)</option>
                  {categoriesTree.map(c => (
                    <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label style={labelStyle}>Опис</label>
                <textarea name="description" value={form.description} onChange={handleChange} rows={3} style={inputStyle} />
              </div>

              <div className="form-group">
                <label style={labelStyle}>Порядок відображення</label>
                <input name="displayOrder" type="number" value={form.displayOrder} onChange={handleChange} style={inputStyle} />
              </div>

              <button type="submit" style={{ padding: '12px', background: '#007bff', color: '#fff', border: 'none', borderRadius: 8, fontWeight: 600, cursor: 'pointer', marginTop: 10 }}>
                {editId ? 'Зберегти зміни' : 'Створити'}
              </button>
            </form>
          </div>
        )}

        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #eee', textAlign: 'left' }}>
                <th style={thStyle}>Назва</th>
                <th style={thStyle}>Опис</th>
                <th style={{ ...thStyle, textAlign: 'center' }}>Товари</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Дії</th>
              </tr>
            </thead>
            <tbody>
              {categoriesTree.map((c) => (
                <CategoryRow key={c.categoryId} category={c} />
              ))}
            </tbody>
          </table>
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

const actionBtnStyle = (color) => ({
  background: 'none',
  border: `1px solid ${color}`,
  color: color,
  padding: '4px 8px',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: '1rem'
});

export default CategoriesAdmin;
