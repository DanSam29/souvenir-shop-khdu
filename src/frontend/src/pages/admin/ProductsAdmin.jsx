import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { productsAPI, categoriesAPI, adminProductsAPI, buildImageUrl } from '../../services/api';

function ProductsAdmin() {
  const { user, isAuthenticated, loading } = useAuth();
  const navigate = useNavigate();

  const [products, setProducts] = useState([]);
  const [categoriesTree, setCategoriesTree] = useState([]);
  const [pageLoading, setPageLoading] = useState(true);
  const [error, setError] = useState(null);

  const [form, setForm] = useState({
    name: '',
    description: '',
    price: '',
    weight: '',
    parentCategoryId: '',
    subCategoryId: '',
    stock: 0
  });
  const [editId, setEditId] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [selectedFiles, setSelectedFiles] = useState([]);
  const [existingImages, setExistingImages] = useState([]);

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
      const [prodRes, catRes] = await Promise.all([
        productsAPI.getAll(),
        categoriesAPI.getAll()
      ]);
      setProducts(prodRes.data);
      setCategoriesTree(catRes.data);
      setError(null);
    } catch (e) {
      setError('Помилка завантаження даних');
    } finally {
      setPageLoading(false);
    }
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => {
      const updated = { ...prev, [name]: value };
      // Якщо змінили батьківську категорію, скидаємо підкатегорію
      if (name === 'parentCategoryId') {
        updated.subCategoryId = '';
      }
      return updated;
    });
  };

  const resetForm = () => {
    setForm({
      name: '',
      description: '',
      price: '',
      weight: '',
      parentCategoryId: '',
      subCategoryId: '',
      stock: 0
    });
    setEditId(null);
    setShowForm(false);
    setSelectedFiles([]);
    setExistingImages([]);
  };

  const handleFileChange = (e) => {
    const files = Array.from(e.target.files);
    setSelectedFiles(prev => [...prev, ...files]);
  };

  const removeSelectedFile = (index) => {
    setSelectedFiles(prev => prev.filter((_, i) => i !== index));
  };

  const removeExistingImage = async (imageId) => {
    if (!window.confirm('Видалити це зображення назавжди?')) return;
    try {
      await productsAPI.deleteImage(imageId);
      setExistingImages(prev => prev.filter(img => img.imageId !== imageId));
    } catch (err) {
      alert('Помилка при видаленні зображення');
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    // Перевірка опису (мінімум 10 символів для валідатора)
    if (form.description.length < 10) {
      alert('Опис має містити мінімум 10 символів');
      return;
    }

    try {
      const categoryId = form.subCategoryId || form.parentCategoryId;
      if (!categoryId) {
        alert('Будь ласка, оберіть категорію');
        return;
      }

      const payload = {
        name: form.name,
        description: form.description,
        price: Number(form.price),
        weight: Number(form.weight),
        categoryId: Number(categoryId),
        stock: Number(form.stock) || 0
      };
      
      let productId = editId;
      if (editId) {
        await adminProductsAPI.update(editId, payload);
      } else {
        const res = await adminProductsAPI.create(payload);
        productId = res.data.productId;
      }

      // Завантаження нових зображень
      if (selectedFiles.length > 0 && productId) {
        for (const file of selectedFiles) {
          const formData = new FormData();
          formData.append('file', file);
          await productsAPI.uploadImage(productId, formData);
        }
      }

      await loadData();
      resetForm();
    } catch (err) {
      const errorMsg = err.response?.data?.errors 
        ? Object.values(err.response.data.errors).flat().join('\n')
        : err.response?.data?.message || 'Помилка збереження товару';
      alert(errorMsg);
    }
  };

  const startEdit = (p) => {
    // Знаходимо категорію та її батька
    let parentId = '';
    let subId = '';

    const findCat = (nodes, targetId, parent = null) => {
      for (const node of nodes) {
        if (node.categoryId === targetId) {
          if (parent) {
            parentId = parent.categoryId;
            subId = node.categoryId;
          } else {
            parentId = node.categoryId;
            subId = '';
          }
          return true;
        }
        if (node.subCategories && findCat(node.subCategories, targetId, node)) return true;
      }
      return false;
    };

    findCat(categoriesTree, p.categoryId);

    setEditId(p.productId);
    setForm({
      name: p.name,
      description: p.description,
      price: p.price,
      weight: p.weight,
      parentCategoryId: parentId,
      subCategoryId: subId,
      stock: p.stock
    });
    setExistingImages(p.images || []);
    setShowForm(true);
    setSelectedFiles([]);
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

  const selectedParentCategory = categoriesTree.find(c => c.categoryId === Number(form.parentCategoryId));
  const subCategories = selectedParentCategory?.subCategories || [];

  if (loading) return <div>Завантаження...</div>;
  if (pageLoading) return <div>Завантаження...</div>;
  if (error) return <div>{error}</div>;

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: 20 }}>
      <div style={{ marginBottom: 30 }}>
        <Link to="/admin" style={{ textDecoration: 'none', color: '#007bff', fontWeight: 600, display: 'block', marginBottom: 10 }}>
          ← Назад до дашборду
        </Link>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h1>Керування товарами</h1>
          <button 
            onClick={() => { 
              if (showForm) {
                resetForm();
              } else {
                setShowForm(true);
                setEditId(null);
              }
            }}
            style={{ padding: '10px 20px', background: showForm ? '#ffc107' : '#28a745', color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer', fontWeight: 600 }}
          >
            {showForm ? '← Скасувати' : '+ Додати товар'}
          </button>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: showForm ? '1fr 2fr' : '1fr', gap: 30 }}>
        {showForm && (
          <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', height: 'fit-content' }}>
            <h3 style={{ marginBottom: 20 }}>{editId ? 'Редагувати товар' : 'Новий товар'}</h3>
            <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 15 }}>
              <div className="form-group">
                <label style={labelStyle}>Назва *</label>
                <input name="name" value={form.name} onChange={handleChange} placeholder="Назва товару" style={inputStyle} required />
              </div>
              
              <div className="form-group">
                <label style={labelStyle}>Опис * (мін. 10 симв.)</label>
                <textarea name="description" value={form.description} onChange={handleChange} placeholder="Опис товару" rows={4} style={inputStyle} required />
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
                <select name="parentCategoryId" value={form.parentCategoryId} onChange={handleChange} style={inputStyle} required>
                  <option value="">Оберіть категорію</option>
                  {categoriesTree.map((c) => (
                    <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
                  ))}
                </select>
              </div>

              {form.parentCategoryId && subCategories.length > 0 && (
                <div className="form-group">
                  <label style={labelStyle}>Підкатегорія (необов'язково)</label>
                  <select name="subCategoryId" value={form.subCategoryId} onChange={handleChange} style={inputStyle}>
                    <option value="">Без підкатегорії</option>
                    {subCategories.map((c) => (
                      <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
                    ))}
                  </select>
                </div>
              )}

              <div className="form-group">
                <label style={labelStyle}>Залишок на складі</label>
                <input name="stock" type="number" value={form.stock} onChange={handleChange} style={inputStyle} />
              </div>

              {/* Управління існуючими зображеннями */}
              {existingImages.length > 0 && (
                <div className="form-group">
                  <label style={labelStyle}>Поточні зображення</label>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
                    {existingImages.map((img) => (
                      <div key={img.imageId} style={{ position: 'relative', width: 60, height: 60 }}>
                        <img src={buildImageUrl(img.imageURL)} alt="Product" style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: 4 }} />
                        <button 
                          type="button" 
                          onClick={() => removeExistingImage(img.imageId)}
                          style={removeBtnStyle}
                        >✕</button>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Управління новими зображеннями */}
              <div className="form-group">
                <label style={labelStyle}>Додати зображення</label>
                <input 
                  type="file" 
                  accept="image/*" 
                  multiple
                  onChange={handleFileChange} 
                  style={{ ...inputStyle, padding: '8px' }}
                />
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, marginTop: 10 }}>
                  {selectedFiles.map((file, index) => (
                    <div key={index} style={{ position: 'relative', width: 60, height: 60, border: '1px solid #ddd', borderRadius: 4, overflow: 'hidden' }}>
                      <img src={URL.createObjectURL(file)} alt="Preview" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                      <button 
                        type="button" 
                        onClick={() => removeSelectedFile(index)}
                        style={removeBtnStyle}
                      >✕</button>
                    </div>
                  ))}
                </div>
              </div>

              <button type="submit" style={{ padding: '12px', background: '#007bff', color: '#fff', border: 'none', borderRadius: 8, fontWeight: 600, cursor: 'pointer', marginTop: 10 }}>
                {editId ? 'Зберегти зміни' : 'Створити товар'}
              </button>
            </form>
          </div>
        )}

        <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)' }}>
          <h3 style={{ marginBottom: 20 }}>Список товарів ({products.length})</h3>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '2px solid #eee', textAlign: 'left' }}>
                  <th style={thStyle}>Товар</th>
                  <th style={thStyle}>Категорія</th>
                  <th style={thStyle}>Ціна</th>
                  <th style={thStyle}>Склад</th>
                  <th style={thStyle}>Дії</th>
                </tr>
              </thead>
              <tbody>
                {products.map((p) => (
                  <tr key={p.productId} style={{ borderBottom: '1px solid #f0f0f0' }}>
                    <td style={tdStyle}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                        {p.images && p.images.length > 0 && (
                          <img src={buildImageUrl(p.images[0].imageURL)} alt="" style={{ width: 40, height: 40, objectFit: 'cover', borderRadius: 4 }} />
                        )}
                        <div>
                          <div style={{ fontWeight: 500 }}>{p.name}</div>
                          <div style={{ fontSize: '0.8rem', color: '#888' }}>ID: {p.productId}</div>
                        </div>
                      </div>
                    </td>
                    <td style={tdStyle}>{p.category?.name || '—'}</td>
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

const removeBtnStyle = {
  position: 'absolute',
  top: -5,
  right: -5,
  background: '#dc3545',
  color: '#fff',
  border: 'none',
  borderRadius: '50%',
  width: 20,
  height: 20,
  fontSize: 12,
  cursor: 'pointer',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  boxShadow: '0 2px 4px rgba(0,0,0,0.2)'
};

export default ProductsAdmin;
