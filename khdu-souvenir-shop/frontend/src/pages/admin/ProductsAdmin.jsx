import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
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
    <div style={{ maxWidth: 900, margin: '0 auto', padding: 20 }}>
      <h2>Адмін: Товари</h2>

      <form onSubmit={handleSubmit} style={{ marginBottom: 20, display: 'grid', gap: 12 }}>
        <input
          name="name"
          value={form.name}
          onChange={handleChange}
          placeholder="Назва"
          required
        />
        <textarea
          name="description"
          value={form.description}
          onChange={handleChange}
          placeholder="Опис"
          rows={3}
        />
        <input
          name="price"
          type="number"
          step="0.01"
          value={form.price}
          onChange={handleChange}
          placeholder="Ціна"
          required
        />
        <input
          name="weight"
          type="number"
          step="0.001"
          value={form.weight}
          onChange={handleChange}
          placeholder="Вага"
          required
        />
        <select
          name="categoryId"
          value={form.categoryId}
          onChange={handleChange}
          required
        >
          <option value="">Оберіть категорію</option>
          {categories.map((c) => (
            <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
          ))}
        </select>
        <input
          name="stock"
          type="number"
          value={form.stock}
          onChange={handleChange}
          placeholder="Кількість на складі"
        />
        <div style={{ display: 'flex', gap: 8 }}>
          <button type="submit">{editId ? 'Оновити' : 'Створити'}</button>
          {editId && <button type="button" onClick={resetForm}>Скасувати</button>}
        </div>
      </form>

      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr>
            <th style={{ textAlign: 'left' }}>ID</th>
            <th style={{ textAlign: 'left' }}>Назва</th>
            <th style={{ textAlign: 'left' }}>Ціна</th>
            <th style={{ textAlign: 'left' }}>Категорія</th>
            <th style={{ textAlign: 'left' }}>Склад</th>
            <th style={{ textAlign: 'left' }}>Дії</th>
          </tr>
        </thead>
        <tbody>
          {products.map((p) => (
            <tr key={p.productId}>
              <td>{p.productId}</td>
              <td>{p.name}</td>
              <td>{p.price}</td>
              <td>{p.category?.name || p.categoryId}</td>
              <td>{p.stock}</td>
              <td>
                <button onClick={() => startEdit(p)} style={{ marginRight: 8 }}>Редагувати</button>
                <button onClick={() => handleDelete(p.productId)}>Видалити</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default ProductsAdmin;
