import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { categoriesAPI, adminCategoriesAPI, productsAPI } from '../../services/api';

const ADMIN_ROLES = ['Manager', 'Administrator', 'SuperAdmin'];

// Пошук вузла дерева за ID
const findNode = (nodes, id) => {
  for (const node of nodes) {
    if (node.categoryId === id) return node;
    if (node.subCategories && node.subCategories.length > 0) {
      const found = findNode(node.subCategories, id);
      if (found) return found;
    }
  }
  return null;
};

// Заміна масиву дочірніх вузлів у дереві (parentId === null => корінь)
const updateChildren = (nodes, parentId, newChildren) => {
  if (parentId === null) return newChildren;
  return nodes.map((node) => {
    if (node.categoryId === parentId) {
      return { ...node, subCategories: newChildren };
    }
    if (node.subCategories && node.subCategories.length > 0) {
      return { ...node, subCategories: updateChildren(node.subCategories, parentId, newChildren) };
    }
    return node;
  });
};

function CategoriesAdmin() {
  const { user, isAuthenticated, loading } = useAuth();
  const navigate = useNavigate();
  const [categoriesTree, setCategoriesTree] = useState([]);
  const [products, setProducts] = useState([]);
  const [pageLoading, setPageLoading] = useState(true);
  const [error, setError] = useState(null);
  const [modal, setModal] = useState(null); // 'create' | 'edit' | 'delete'
  const [editId, setEditId] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({
    name: '',
    parentCategoryId: '',
    description: '',
    displayOrder: 0
  });
  const [dragId, setDragId] = useState(null);
  const [dragOverId, setDragOverId] = useState(null);

  useEffect(() => {
    if (loading) return;
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    if (!ADMIN_ROLES.includes(user?.role)) {
      navigate('/access-denied');
      return;
    }
    loadData();
  }, [loading, isAuthenticated, user, navigate]);

  const loadData = async () => {
    try {
      setPageLoading(true);
      const [catRes, prodRes] = await Promise.all([
        categoriesAPI.getAll(),
        productsAPI.getAll({ pageSize: 100 })
      ]);
      setCategoriesTree(catRes.data || []);
      setProducts(prodRes.data.items || []);
      setError(null);
    } catch (e) {
      setError('Помилка завантаження даних');
    } finally {
      setPageLoading(false);
    }
  };

  const getCategoryProductCount = (category) => {
    let count = products.filter(p => p.categoryId === category.categoryId).length;
    if (category.subCategories && category.subCategories.length > 0) {
      category.subCategories.forEach(sub => {
        count += getCategoryProductCount(sub);
      });
    }
    return count;
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const openCreate = () => {
    setForm({ name: '', parentCategoryId: '', description: '', displayOrder: 0 });
    setEditId(null);
    setModal('create');
  };

  const openEdit = (cat) => {
    setForm({
      name: cat.name,
      parentCategoryId: cat.parentCategoryId || '',
      description: cat.description || '',
      displayOrder: cat.displayOrder || 0
    });
    setEditId(cat.categoryId);
    setModal('edit');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.name.trim()) {
      alert('Введіть назву категорії');
      return;
    }
    setSaving(true);
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
      await loadData();
      setModal(null);
    } catch (err) {
      alert(err.response?.data?.error || 'Помилка збереження категорії');
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = (cat) => {
    setDeleteTarget(cat);
    setModal('delete');
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setSaving(true);
    try {
      await adminCategoriesAPI.delete(deleteTarget.categoryId);
      await loadData();
      setModal(null);
      setDeleteTarget(null);
    } catch (err) {
      alert(err.response?.data?.message || 'Помилка видалення категорії');
    } finally {
      setSaving(false);
    }
  };

  // Drag-and-drop зміна порядку в межах одному рівні дерева
  const handleDragStart = (cat) => setDragId(cat.categoryId);

  const handleDragOver = (e, cat) => {
    e.preventDefault();
    const source = findNode(categoriesTree, dragId);
    if (!source || source.categoryId === cat.categoryId) return;
    if ((source.parentCategoryId ?? null) !== (cat.parentCategoryId ?? null)) return;
    setDragOverId(cat.categoryId);
  };

  const handleDrop = async (e, cat) => {
    e.preventDefault();
    const source = findNode(categoriesTree, dragId);
    setDragOverId(null);
    setDragId(null);
    if (!source || source.categoryId === cat.categoryId) return;
    if ((source.parentCategoryId ?? null) !== (cat.parentCategoryId ?? null)) return;

    const parentId = source.parentCategoryId ?? null;
    const siblings = (parentId === null
      ? categoriesTree
      : findNode(categoriesTree, parentId).subCategories).slice();
    const fromIdx = siblings.findIndex(s => s.categoryId === source.categoryId);
    const toIdx = siblings.findIndex(s => s.categoryId === cat.categoryId);
    if (fromIdx === -1 || toIdx === -1) return;

    const [moved] = siblings.splice(fromIdx, 1);
    siblings.splice(toIdx, 0, moved);
    const reordered = siblings.map((s, idx) => ({ ...s, displayOrder: idx }));
    setCategoriesTree(updateChildren(categoriesTree, parentId, reordered));
    try {
      await adminCategoriesAPI.reorder(reordered.map((s, idx) => ({
        categoryId: s.categoryId,
        displayOrder: idx
      })));
    } catch (err) {
      alert(err.response?.data?.message || 'Не вдалося зберегти порядок');
      loadData();
    }
  };

  const CategoryRow = ({ category, level = 0 }) => {
    const productCount = level === 0
      ? getCategoryProductCount(category)
      : products.filter(p => p.categoryId === category.categoryId).length;
    return (
      <React.Fragment>
        <tr
          draggable
          onDragStart={() => handleDragStart(category)}
          onDragOver={(e) => handleDragOver(e, category)}
          onDrop={(e) => handleDrop(e, category)}
          style={{
            borderBottom: '1px solid #f0f0f0',
            background: level === 0 ? '#fafafa' : '#fff',
            opacity: dragId === category.categoryId ? 0.4 : 1,
            borderTop: dragOverId === category.categoryId ? '2px solid #007bff' : '2px solid transparent'
          }}
        >
          <td style={{ padding: '12px 8px', paddingLeft: level * 30 + 10, cursor: 'grab' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <span style={{ color: '#bbb' }}>⠿</span>
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
              <button onClick={() => openEdit(category)} style={actionBtnStyle('#ffc107')} title="Редагувати">✎</button>
              <button onClick={() => confirmDelete(category)} style={actionBtnStyle('#dc3545')} title="Видалити">✕</button>
            </div>
          </td>
        </tr>
        {category.subCategories && category.subCategories.map(sub => (
          <CategoryRow key={sub.categoryId} category={sub} level={level + 1} />
        ))}
      </React.Fragment>
    );
  };

  if (loading || pageLoading) return <div style={{ padding: 20 }}>Завантаження...</div>;
  if (error) return <div style={{ padding: 20 }}>{error}</div>;

  return (
    <div style={{ maxWidth: 1000, margin: '0 auto', padding: 20 }}>
      <div style={{ marginBottom: 30 }}>
        <Link to="/admin" style={{ textDecoration: 'none', color: '#007bff', fontWeight: 600, display: 'block', marginBottom: 10 }}>
          ← Назад до дашборду
        </Link>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h1>Керування категоріями</h1>
          <button onClick={openCreate} style={primaryBtnStyle('#28a745')}>+ Додати категорію</button>
        </div>
        <p style={{ color: '#888', fontSize: '0.85rem', marginTop: 8 }}>
          Перетягніть рядок, щоб змінити порядок відображення в межах одного рівня.
        </p>
      </div>

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

      {modal === 'create' || modal === 'edit' ? (
        <Modal title={editId ? 'Редагувати категорію' : 'Нова категорія'} onClose={() => setModal(null)}>
          <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 15 }}>
            <div>
              <label style={labelStyle}>Назва *</label>
              <input name="name" value={form.name} onChange={handleChange} style={inputStyle} required />
            </div>
            <div>
              <label style={labelStyle}>Батьківська категорія</label>
              <select name="parentCategoryId" value={form.parentCategoryId} onChange={handleChange} style={inputStyle}>
                <option value="">Немає (головна)</option>
                {categoriesTree
                  .filter(c => c.categoryId !== editId)
                  .map(c => (
                    <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
                  ))}
              </select>
            </div>
            <div>
              <label style={labelStyle}>Опис</label>
              <textarea name="description" value={form.description} onChange={handleChange} rows={3} style={inputStyle} />
            </div>
            <div>
              <label style={labelStyle}>Порядок відображення</label>
              <input name="displayOrder" type="number" value={form.displayOrder} onChange={handleChange} style={inputStyle} />
            </div>
            <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', marginTop: 10 }}>
              <button type="button" onClick={() => setModal(null)} style={secondaryBtnStyle} disabled={saving}>Скасувати</button>
              <button type="submit" style={primaryBtnStyle('#007bff')} disabled={saving}>
                {saving ? 'Збереження...' : editId ? 'Зберегти зміни' : 'Створити'}
              </button>
            </div>
          </form>
        </Modal>
      ) : null}

      {modal === 'delete' && deleteTarget ? (
        <Modal title="Видалити категорію?" onClose={() => { setModal(null); setDeleteTarget(null); }}>
          <p style={{ marginBottom: 20 }}>
            Категорію «<strong>{deleteTarget.name}</strong>» буде видалено. Категорія має бути порожньою
            (без товарів і підкатегорій).
          </p>
          <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
            <button type="button" onClick={() => { setModal(null); setDeleteTarget(null); }} style={secondaryBtnStyle} disabled={saving}>Скасувати</button>
            <button type="button" onClick={handleDelete} style={primaryBtnStyle('#dc3545')} disabled={saving}>
              {saving ? 'Видалення...' : 'Видалити'}
            </button>
          </div>
        </Modal>
      ) : null}
    </div>
  );
}

const Modal = ({ title, children, onClose }) => (
  <div style={overlayStyle} onClick={onClose}>
    <div style={modalStyle} onClick={(e) => e.stopPropagation()}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <h3 style={{ margin: 0 }}>{title}</h3>
        <button onClick={onClose} style={{ background: 'none', border: 'none', fontSize: '1.3rem', cursor: 'pointer', color: '#888' }}>✕</button>
      </div>
      {children}
    </div>
  </div>
);

const overlayStyle = {
  position: 'fixed',
  inset: 0,
  background: 'rgba(0,0,0,0.45)',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  zIndex: 2000,
  padding: 20
};
const modalStyle = {
  background: '#fff',
  borderRadius: 12,
  padding: 24,
  width: '100%',
  maxWidth: 480,
  maxHeight: '90vh',
  overflowY: 'auto',
  boxShadow: '0 12px 40px rgba(0,0,0,0.25)'
};
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
const primaryBtnStyle = (color) => ({
  padding: '10px 20px',
  background: color,
  color: '#fff',
  border: 'none',
  borderRadius: 8,
  cursor: 'pointer',
  fontWeight: 600
});
const secondaryBtnStyle = {
  padding: '10px 20px',
  background: '#f1f3f5',
  color: '#333',
  border: '1px solid #ddd',
  borderRadius: 8,
  cursor: 'pointer',
  fontWeight: 600
};

export default CategoriesAdmin;
