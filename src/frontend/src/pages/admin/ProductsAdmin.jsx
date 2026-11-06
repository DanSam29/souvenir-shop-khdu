import React, { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { productsAPI, categoriesAPI, adminProductsAPI, buildImageUrl } from '../../services/api';

const ADMIN_ROLES = ['Manager', 'Administrator', 'SuperAdmin'];

// Розгортання дерева категорій у плаский список (для фільтрів та вибору)
const flattenCategories = (nodes, level = 0, acc = []) => {
  nodes.forEach((node) => {
    acc.push({ categoryId: node.categoryId, name: node.name, level });
    if (node.subCategories && node.subCategories.length > 0) {
      flattenCategories(node.subCategories, level + 1, acc);
    }
  });
  return acc;
};

// Переміщення елемента масиву з позиції from на позицію to
const moveItem = (list, from, to) => {
  const arr = [...list];
  const [item] = arr.splice(from, 1);
  arr.splice(to, 0, item);
  return arr;
};

const WIZARD_STEPS = ['Основна інформація', 'Ціна та склад', 'Зображення'];

function ProductsAdmin() {
  const { user, isAuthenticated, loading } = useAuth();
  const navigate = useNavigate();
  const [products, setProducts] = useState([]);
  const [categoriesTree, setCategoriesTree] = useState([]);
  const [pageLoading, setPageLoading] = useState(true);
  const [error, setError] = useState(null);
  // Фільтри таблиці
  const [search, setSearch] = useState('');
  const [filterCategory, setFilterCategory] = useState('');
  const [filterStock, setFilterStock] = useState('all');
  // Масове виділення
  const [selectedIds, setSelectedIds] = useState([]);
  // Модалка підтвердження видалення
  const [confirm, setConfirm] = useState(null); // { ids: number[], bulk: boolean, name?: string }
  // Майстер створення/редагування товару
  const [wizardOpen, setWizardOpen] = useState(false);
  const [step, setStep] = useState(0);
  const [editId, setEditId] = useState(null);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({
    name: '',
    description: '',
    price: '',
    weight: '',
    parentCategoryId: '',
    subCategoryId: '',
    stock: 0
  });
  const [existingImages, setExistingImages] = useState([]);
  const [selectedFiles, setSelectedFiles] = useState([]);
  const [imgDrag, setImgDrag] = useState(null); // { list: 'existing'|'new', index }
  const [imgOver, setImgOver] = useState(null); // { list, index }

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
      const [prodRes, catRes] = await Promise.all([
        productsAPI.getAll({ pageSize: 100 }),
        categoriesAPI.getAll()
      ]);
      setProducts(prodRes.data.items || []);
      setCategoriesTree(catRes.data || []);
      setError(null);
    } catch (e) {
      setError('Помилка завантаження даних');
    } finally {
      setPageLoading(false);
    }
  };

  const flatCategories = useMemo(() => flattenCategories(categoriesTree), [categoriesTree]);

  const filteredProducts = useMemo(() => {
    const term = search.trim().toLowerCase();
    return products.filter((p) => {
      if (term && !p.name.toLowerCase().includes(term)) return false;
      if (filterCategory && p.categoryId !== Number(filterCategory)) return false;
      if (filterStock === 'in' && !(p.stock > 0)) return false;
      if (filterStock === 'out' && p.stock > 0) return false;
      return true;
    });
  }, [products, search, filterCategory, filterStock]);

  const allSelected = filteredProducts.length > 0
    && filteredProducts.every((p) => selectedIds.includes(p.productId));

  const toggleSelect = (id) => {
    setSelectedIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
  };

  const toggleSelectAll = () => {
    setSelectedIds(allSelected ? [] : filteredProducts.map((p) => p.productId));
  };

  // --- Майстер ---
  const resetWizard = () => {
    setStep(0);
    setEditId(null);
    setForm({ name: '', description: '', price: '', weight: '', parentCategoryId: '', subCategoryId: '', stock: 0 });
    setExistingImages([]);
    setSelectedFiles([]);
    setImgDrag(null);
    setImgOver(null);
  };

  const openCreate = () => {
    resetWizard();
    setWizardOpen(true);
  };

  const openEdit = (p) => {
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
    setStep(0);
    setForm({
      name: p.name,
      description: p.description || '',
      price: p.price,
      weight: p.weight,
      parentCategoryId: parentId,
      subCategoryId: subId,
      stock: p.stock
    });
    setExistingImages(p.images || []);
    setSelectedFiles([]);
    setWizardOpen(true);
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => {
      const updated = { ...prev, [name]: value };
      if (name === 'parentCategoryId') updated.subCategoryId = '';
      return updated;
    });
  };

  const validateStep = () => {
    if (step === 0) {
      if (!form.name.trim()) { alert('Введіть назву товару'); return false; }
      if (form.description.trim().length < 10) { alert('Опис має містити мінімум 10 символів'); return false; }
      if (!form.parentCategoryId) { alert('Оберіть категорію'); return false; }
    }
    if (step === 1) {
      if (!form.price || Number(form.price) <= 0) { alert('Введіть коректну ціну'); return false; }
      if (!form.weight || Number(form.weight) <= 0) { alert('Введіть коректну вагу'); return false; }
    }
    return true;
  };

  const nextStep = () => { if (validateStep()) setStep((s) => Math.min(WIZARD_STEPS.length - 1, s + 1)); };
  const prevStep = () => setStep((s) => Math.max(0, s - 1));

  const handleSave = async () => {
    const categoryId = form.subCategoryId || form.parentCategoryId;
    if (!categoryId) { alert('Будь ласка, оберіть категорію'); return; }
    setSaving(true);
    try {
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
      // Завантаження нових зображень у вибраному порядку
      for (const file of selectedFiles) {
        const formData = new FormData();
        formData.append('file', file);
        await adminProductsAPI.uploadImage(productId, formData);
      }
      await loadData();
      setWizardOpen(false);
      resetWizard();
    } catch (err) {
      const errorMsg = err.response?.data?.errors
        ? Object.values(err.response.data.errors).flat().join('\n')
        : err.response?.data?.message || 'Помилка збереження товару';
      alert(errorMsg);
    } finally {
      setSaving(false);
    }
  };

  // --- Зображення ---
  const handleFileChange = (e) => {
    const files = Array.from(e.target.files);
    setSelectedFiles((prev) => [...prev, ...files]);
    e.target.value = '';
  };

  const removeSelectedFile = (index) => {
    setSelectedFiles((prev) => prev.filter((_, i) => i !== index));
  };

  const removeExistingImage = async (imageId) => {
    if (!window.confirm('Видалити це зображення назавжди?')) return;
    try {
      await adminProductsAPI.deleteImage(editId, imageId);
      setExistingImages((prev) => prev.filter((img) => img.imageId !== imageId));
      loadData();
    } catch (err) {
      alert('Помилка при видаленні зображення');
    }
  };

  const setPrimary = async (imageId) => {
    if (!editId) return;
    try {
      await adminProductsAPI.setPrimaryImage(editId, imageId);
      setExistingImages((prev) => prev.map((img) => ({ ...img, isPrimary: img.imageId === imageId })));
    } catch (err) {
      alert('Помилка встановлення головного зображення');
    }
  };

  const handleImgDragStart = (list, index) => setImgDrag({ list, index });

  const handleImgDragOver = (e, list, index) => {
    e.preventDefault();
    if (!imgDrag || imgDrag.list !== list) return;
    setImgOver({ list, index });
  };

  const handleImgDrop = async (e, list, index) => {
    e.preventDefault();
    const drag = imgDrag;
    setImgDrag(null);
    setImgOver(null);
    if (!drag || drag.list !== list) return;
    if (drag.index === index) return;
    if (list === 'existing') {
      const next = moveItem(existingImages, drag.index, index);
      setExistingImages(next);
      if (editId) {
        try {
          await adminProductsAPI.reorderImages(
            editId,
            next.map((img, i) => ({ imageId: img.imageId, displayOrder: i }))
          );
        } catch (err) {
          alert('Не вдалося зберегти порядок зображень');
        }
      }
    } else {
      setSelectedFiles(moveItem(selectedFiles, drag.index, index));
    }
  };

  // --- Видалення ---
  const askDeleteSingle = (p) => setConfirm({ ids: [p.productId], bulk: false, name: p.name });
  const askDeleteBulk = () => setConfirm({ ids: selectedIds, bulk: true });

  const doDelete = async () => {
    if (!confirm) return;
    setSaving(true);
    try {
      if (confirm.bulk) {
        const res = await adminProductsAPI.bulkDelete(confirm.ids);
        await loadData();
        setSelectedIds([]);
        alert(res.message || 'Готово');
      } else {
        await adminProductsAPI.delete(confirm.ids[0]);
        await loadData();
      }
      setConfirm(null);
    } catch (err) {
      alert(err.response?.data?.message || err.response?.data?.error || 'Помилка видалення');
    } finally {
      setSaving(false);
    }
  };

  const selectedParentCategory = categoriesTree.find((c) => c.categoryId === Number(form.parentCategoryId));
  const subCategories = selectedParentCategory?.subCategories || [];

  if (loading || pageLoading) return <div style={{ padding: 20 }}>Завантаження...</div>;
  if (error) return <div style={{ padding: 20 }}>{error}</div>;

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: 20 }}>
      <div style={{ marginBottom: 24 }}>
        <Link to="/admin" style={{ textDecoration: 'none', color: '#007bff', fontWeight: 600, display: 'block', marginBottom: 10 }}>
          ← Назад до дашборду
        </Link>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h1>Керування товарами</h1>
          <button onClick={openCreate} style={primaryBtnStyle('#28a745')}>+ Додати товар</button>
        </div>
      </div>

      <div style={{ background: '#fff', padding: 24, borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)' }}>
        {/* Фільтри */}
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, marginBottom: 20 }}>
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Пошук за назвою..."
            style={{ ...inputStyle, maxWidth: 260 }}
          />
          <select value={filterCategory} onChange={(e) => setFilterCategory(e.target.value)} style={{ ...inputStyle, maxWidth: 220 }}>
            <option value="">Усі категорії</option>
            {flatCategories.map((c) => (
              <option key={c.categoryId} value={c.categoryId}>
                {'\u00A0'.repeat(c.level * 3)}{c.name}
              </option>
            ))}
          </select>
          <select value={filterStock} onChange={(e) => setFilterStock(e.target.value)} style={{ ...inputStyle, maxWidth: 180 }}>
            <option value="all">Уся наявність</option>
            <option value="in">Лише в наявності</option>
            <option value="out">Немає в наявності</option>
          </select>
          {selectedIds.length > 0 && (
            <button onClick={askDeleteBulk} style={primaryBtnStyle('#dc3545')}>
              Видалити вибрані ({selectedIds.length})
            </button>
          )}
        </div>

        <h3 style={{ marginBottom: 16 }}>Список товарів ({filteredProducts.length})</h3>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #eee', textAlign: 'left' }}>
                <th style={{ ...thStyle, width: 40 }}>
                  <input type="checkbox" checked={allSelected} onChange={toggleSelectAll} />
                </th>
                <th style={thStyle}>Товар</th>
                <th style={thStyle}>Категорія</th>
                <th style={thStyle}>Ціна</th>
                <th style={thStyle}>Склад</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Дії</th>
              </tr>
            </thead>
            <tbody>
              {filteredProducts.map((p) => (
                <tr key={p.productId} style={{ borderBottom: '1px solid #f0f0f0' }}>
                  <td style={tdStyle}>
                    <input
                      type="checkbox"
                      checked={selectedIds.includes(p.productId)}
                      onChange={() => toggleSelect(p.productId)}
                    />
                  </td>
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
                  <td style={tdStyle}>{Number(p.price).toFixed(2)} грн</td>
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
                    <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
                      <button onClick={() => openEdit(p)} style={actionBtnStyle('#ffc107')} title="Редагувати">✎</button>
                      <button onClick={() => askDeleteSingle(p)} style={actionBtnStyle('#dc3545')} title="Видалити">✕</button>
                    </div>
                  </td>
                </tr>
              ))}
              {filteredProducts.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ padding: 24, textAlign: 'center', color: '#888' }}>
                    Товарів не знайдено
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Модалка підтвердження видалення */}
      {confirm && (
        <Modal
          title={confirm.bulk ? 'Масове видалення' : 'Видалити товар?'}
          onClose={() => setConfirm(null)}
        >
          <p style={{ marginBottom: 20 }}>
            {confirm.bulk
              ? `Видалити вибрані товари (${confirm.ids.length})? Товари з активними замовленнями буде пропущено.`
              : `Товар «${confirm.name}» буде видалено.`}
          </p>
          <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
            <button type="button" onClick={() => setConfirm(null)} style={secondaryBtnStyle} disabled={saving}>Скасувати</button>
            <button type="button" onClick={doDelete} style={primaryBtnStyle('#dc3545')} disabled={saving}>
              {saving ? 'Видалення...' : 'Видалити'}
            </button>
          </div>
        </Modal>
      )}

      {/* Майстер створення/редагування */}
      {wizardOpen && (
        <Modal title={editId ? 'Редагування товару' : 'Новий товар'} onClose={() => setWizardOpen(false)} wide>
          <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
            {WIZARD_STEPS.map((label, i) => (
              <div key={label} style={{ flex: 1, textAlign: 'center' }}>
                <div style={{
                  height: 4,
                  borderRadius: 2,
                  background: i <= step ? '#007bff' : '#e9ecef',
                  marginBottom: 6
                }} />
                <span style={{ fontSize: '0.8rem', color: i <= step ? '#007bff' : '#aaa', fontWeight: 600 }}>{label}</span>
              </div>
            ))}
          </div>

          {step === 0 && (
            <div style={{ display: 'grid', gap: 15 }}>
              <div>
                <label style={labelStyle}>Назва *</label>
                <input name="name" value={form.name} onChange={handleChange} placeholder="Назва товару" style={inputStyle} />
              </div>
              <div>
                <label style={labelStyle}>Опис * (мін. 10 симв.)</label>
                <textarea name="description" value={form.description} onChange={handleChange} placeholder="Опис товару" rows={4} style={inputStyle} />
              </div>
              <div>
                <label style={labelStyle}>Категорія *</label>
                <select name="parentCategoryId" value={form.parentCategoryId} onChange={handleChange} style={inputStyle}>
                  <option value="">Оберіть категорію</option>
                  {categoriesTree.map((c) => (
                    <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
                  ))}
                </select>
              </div>
              {form.parentCategoryId && subCategories.length > 0 && (
                <div>
                  <label style={labelStyle}>Підкатегорія (необов'язково)</label>
                  <select name="subCategoryId" value={form.subCategoryId} onChange={handleChange} style={inputStyle}>
                    <option value="">Без підкатегорії</option>
                    {subCategories.map((c) => (
                      <option key={c.categoryId} value={c.categoryId}>{c.name}</option>
                    ))}
                  </select>
                </div>
              )}
            </div>
          )}

          {step === 1 && (
            <div style={{ display: 'grid', gap: 15 }}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 15 }}>
                <div>
                  <label style={labelStyle}>Ціна (грн) *</label>
                  <input name="price" type="number" step="0.01" value={form.price} onChange={handleChange} style={inputStyle} />
                </div>
                <div>
                  <label style={labelStyle}>Вага (кг) *</label>
                  <input name="weight" type="number" step="0.001" value={form.weight} onChange={handleChange} style={inputStyle} />
                </div>
              </div>
              <div>
                <label style={labelStyle}>Залишок на складі</label>
                <input name="stock" type="number" value={form.stock} onChange={handleChange} style={inputStyle} />
                <p style={{ fontSize: '0.8rem', color: '#888', marginTop: 6 }}>
                  Зменшення залишку відбувається через замовлення; тут можна скоригувати вручну.
                </p>
              </div>
            </div>
          )}

          {step === 2 && (
            <div style={{ display: 'grid', gap: 20 }}>
              {existingImages.length > 0 && (
                <div>
                  <label style={labelStyle}>Поточні зображення (перетягніть для зміни порядку, ★ - головне)</label>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
                    {existingImages.map((img, index) => (
                      <div
                        key={img.imageId}
                        draggable
                        onDragStart={() => handleImgDragStart('existing', index)}
                        onDragOver={(e) => handleImgDragOver(e, 'existing', index)}
                        onDrop={(e) => handleImgDrop(e, 'existing', index)}
                        style={{
                          position: 'relative',
                          width: 72,
                          height: 72,
                          cursor: 'grab',
                          opacity: imgDrag?.list === 'existing' && imgDrag.index === index ? 0.4 : 1,
                          outline: imgOver?.list === 'existing' && imgOver.index === index ? '2px solid #007bff' : 'none',
                          borderRadius: 6,
                          border: img.isPrimary ? '2px solid #28a745' : '1px solid #ddd',
                          overflow: 'hidden'
                        }}
                      >
                        <img src={buildImageUrl(img.imageURL)} alt="Product" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                        <button type="button" onClick={() => setPrimary(img.imageId)} title="Зробити головним" style={imgBadgeStyle('#28a745', 'left')}>★</button>
                        <button type="button" onClick={() => removeExistingImage(img.imageId)} title="Видалити" style={imgBadgeStyle('#dc3545', 'right')}>✕</button>
                        {img.isPrimary && (
                          <span style={{ position: 'absolute', bottom: 0, left: 0, right: 0, background: 'rgba(40,167,69,0.85)', color: '#fff', fontSize: 10, textAlign: 'center' }}>
                            Головне
                          </span>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div>
                <label style={labelStyle}>Додати зображення</label>
                <input type="file" accept="image/*" multiple onChange={handleFileChange} style={{ ...inputStyle, padding: '8px' }} />
                {selectedFiles.length > 0 && (
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, marginTop: 12 }}>
                    {selectedFiles.map((file, index) => (
                      <div
                        key={`${file.name}-${index}`}
                        draggable
                        onDragStart={() => handleImgDragStart('new', index)}
                        onDragOver={(e) => handleImgDragOver(e, 'new', index)}
                        onDrop={(e) => handleImgDrop(e, 'new', index)}
                        style={{
                          position: 'relative',
                          width: 72,
                          height: 72,
                          cursor: 'grab',
                          opacity: imgDrag?.list === 'new' && imgDrag.index === index ? 0.4 : 1,
                          outline: imgOver?.list === 'new' && imgOver.index === index ? '2px solid #007bff' : 'none',
                          border: '1px solid #ddd',
                          borderRadius: 6,
                          overflow: 'hidden'
                        }}
                      >
                        <img src={URL.createObjectURL(file)} alt="Preview" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                        <button type="button" onClick={() => removeSelectedFile(index)} title="Прибрати" style={imgBadgeStyle('#dc3545', 'right')}>✕</button>
                      </div>
                    ))}
                  </div>
                )}
                <p style={{ fontSize: '0.8rem', color: '#888', marginTop: 6 }}>
                  Порядок завантаження відповідає порядку мініатюр. Перше зображення нового товару стане головним.
                </p>
              </div>
            </div>
          )}

          <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', marginTop: 24 }}>
            {step > 0 && (
              <button type="button" onClick={prevStep} style={secondaryBtnStyle} disabled={saving}>← Назад</button>
            )}
            <button type="button" onClick={() => setWizardOpen(false)} style={secondaryBtnStyle} disabled={saving}>Скасувати</button>
            {step < WIZARD_STEPS.length - 1 ? (
              <button type="button" onClick={nextStep} style={primaryBtnStyle('#007bff')} disabled={saving}>Далі →</button>
            ) : (
              <button type="button" onClick={handleSave} style={primaryBtnStyle('#28a745')} disabled={saving}>
                {saving ? 'Збереження...' : editId ? 'Зберегти зміни' : 'Створити товар'}
              </button>
            )}
          </div>
        </Modal>
      )}
    </div>
  );
}

const Modal = ({ title, children, onClose, wide }) => (
  <div style={overlayStyle} onClick={onClose}>
    <div style={{ ...modalStyle, maxWidth: wide ? 680 : 480 }} onClick={(e) => e.stopPropagation()}>
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
const imgBadgeStyle = (color, side) => ({
  position: 'absolute',
  top: 2,
  [side]: 2,
  background: color,
  color: '#fff',
  border: 'none',
  borderRadius: '50%',
  width: 18,
  height: 18,
  fontSize: 11,
  cursor: 'pointer',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  boxShadow: '0 2px 4px rgba(0,0,0,0.2)'
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

export default ProductsAdmin;
