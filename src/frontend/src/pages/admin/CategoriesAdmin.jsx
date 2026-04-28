import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { categoriesAPI, adminCategoriesAPI } from '../../services/api';

function CategoriesAdmin() {
  const { user, isAuthenticated, loading } = useAuth();
  const navigate = useNavigate();

  const [categories, setCategories] = useState([]);
  const [pageLoading, setPageLoading] = useState(true);
  const [error, setError] = useState(null);

  const [form, setForm] = useState({
    name: '',
    parentCategoryId: '',
    description: '',
    displayOrder: 0
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
    loadCategories();
  }, [loading, isAuthenticated, user, navigate]);

  const loadCategories = async () => {
    try {
      setPageLoading(true);
      const res = await categoriesAPI.getAll();
      setCategories(res.data);
      setError(null);
    } catch (e) {
      setError('Помилка завантаження категорій');
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
      parentCategoryId: '',
      description: '',
      displayOrder: 0
    });
    setEditId(null);
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
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Видалити категорію?')) return;
    try {
      await adminCategoriesAPI.delete(id);
      await loadCategories();
    } catch (err) {
      alert(err.response?.data?.error || 'Помилка видалення категорії');
    }
  };

  if (loading) return <div>Завантаження...</div>;
  if (pageLoading) return <div>Завантаження...</div>;
  if (error) return <div>{error}</div>;

  return (
    <div style={{ maxWidth: 900, margin: '0 auto', padding: 20 }}>
      <h2>Адмін: Категорії</h2>

      <form onSubmit={handleSubmit} style={{ marginBottom: 20, display: 'grid', gap: 12 }}>
        <input
          name="name"
          value={form.name}
          onChange={handleChange}
          placeholder="Назва"
          required
        />
        <input
          name="parentCategoryId"
          value={form.parentCategoryId}
          onChange={handleChange}
          placeholder="ID батьківської категорії (необов'язково)"
        />
        <input
          name="description"
          value={form.description}
          onChange={handleChange}
          placeholder="Опис"
        />
        <input
          name="displayOrder"
          type="number"
          value={form.displayOrder}
          onChange={handleChange}
          placeholder="Порядок відображення"
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
            <th style={{ textAlign: 'left' }}>ParentID</th>
            <th style={{ textAlign: 'left' }}>Порядок</th>
            <th style={{ textAlign: 'left' }}>Дії</th>
          </tr>
        </thead>
        <tbody>
          {categories.map((c) => (
            <tr key={c.categoryId}>
              <td>{c.categoryId}</td>
              <td>{c.name}</td>
              <td>{c.parentCategoryId ?? '-'}</td>
              <td>{c.displayOrder ?? 0}</td>
              <td>
                <button onClick={() => startEdit(c)} style={{ marginRight: 8 }}>Редагувати</button>
                <button onClick={() => handleDelete(c.categoryId)}>Видалити</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default CategoriesAdmin;
