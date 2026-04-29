import React, { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import ProductCard from './ProductCard';
import { productsAPI, categoriesAPI } from '../services/api';
import './ProductList.css';

function ProductList() {
  const { t } = useTranslation();
  const [products, setProducts] = useState([]);
  const [allProducts, setAllProducts] = useState([]);
  const [originalProducts, setOriginalProducts] = useState([]); 
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchQuery, setSearchQuery] = useState('');
  
  // Фільтри
  const [priceRange, setPriceRange] = useState([0, 10000]);
  const [maxPrice, setMaxPrice] = useState(10000);
  const [selectedCategories, setSelectedCategories] = useState([]);
  
  // Функція для отримання всіх ID дочірніх категорій
  const getAllCategoryIds = useCallback((categoryIds) => {
    const allIds = new Set();
    
    const addIds = (id) => {
      if (allIds.has(id)) return;
      allIds.add(id);
      
      // Знаходимо категорію в дереві (рекурсивно)
      const findAndAddSub = (list) => {
        for (const cat of list) {
          if (cat.categoryId === id) {
            cat.subCategories?.forEach(sub => addIds(sub.categoryId));
            return true;
          }
          if (cat.subCategories?.length > 0) {
            if (findAndAddSub(cat.subCategories)) return true;
          }
        }
        return false;
      };
      
      findAndAddSub(categories);
    };
    
    categoryIds.forEach(id => addIds(id));
    return Array.from(allIds);
  }, [categories]);

  // Застосування фільтрів
  const applyFilters = useCallback(() => {
    let filtered = [...allProducts];
    
    // Фільтр за ціною
    filtered = filtered.filter(p => 
      p.price >= priceRange[0] && p.price <= priceRange[1]
    );
    
    // Фільтр за категоріями
    if (selectedCategories.length > 0) {
      const targetCategoryIds = getAllCategoryIds(selectedCategories);
      filtered = filtered.filter(p => 
        targetCategoryIds.includes(p.categoryId)
      );
    }
    
    setProducts(filtered);
  }, [allProducts, priceRange, selectedCategories, getAllCategoryIds]);

  // Завантаження товарів та категорій при монтуванні
  useEffect(() => {
    loadData();
  }, []);

  // Застосування фільтрів при їх зміні
  useEffect(() => {
    applyFilters();
  }, [priceRange, selectedCategories, allProducts, applyFilters]);

  const loadData = async () => {
    try {
      setLoading(true);
      const [productsRes, categoriesRes] = await Promise.all([
        productsAPI.getAll(),
        categoriesAPI.getAll()
      ]);
      
      setOriginalProducts(productsRes.data); 
      setAllProducts(productsRes.data);
      setProducts(productsRes.data);
      setCategories(categoriesRes.data);
      
      // Знаходимо максимальну ціну
      const prices = productsRes.data.map(p => p.price);
      const max = Math.max(...prices, 1000);
      setMaxPrice(max);
      setPriceRange([0, max]);
      
      setError(null);
    } catch (err) {
      console.error('Помилка завантаження:', err);
      setError('Не вдалося завантажити товари');
    } finally {
      setLoading(false);
    }
  };

  // Пошук товарів
  const handleSearch = async (e) => {
    e.preventDefault();
    if (!searchQuery.trim()) {
      setAllProducts(originalProducts);
      return;
    }

    try {
      setLoading(true);
      const response = await productsAPI.search(searchQuery);
      setAllProducts(response.data);
      setError(null);
    } catch (err) {
      console.error('Помилка пошуку:', err);
      setError('Помилка пошуку товарів');
    } finally {
      setLoading(false);
    }
  };

  // Зміна категорій
  const toggleCategory = (categoryId) => {
    setSelectedCategories(prev => 
      prev.includes(categoryId)
        ? prev.filter(id => id !== categoryId)
        : [...prev, categoryId]
    );
  };

  // Скидання фільтрів
  const resetFilters = () => {
    setPriceRange([0, maxPrice]);
    setSelectedCategories([]);
    setSearchQuery('');
    setAllProducts(originalProducts);
    setProducts (originalProducts);
  };

  // Очищення пошуку
  const handleClearSearch = () => {
    setSearchQuery('');
    setAllProducts(originalProducts);
  }
  // Рекурсивне рендеринг дерева категорій
  const renderCategoryTree = (nodes, level = 0) => {
    return nodes.map(category => (
      <React.Fragment key={category.categoryId}>
        <label 
          className="category-checkbox" 
          style={{ marginLeft: `${level * 20}px` }}
        >
          <input
            type="checkbox"
            checked={selectedCategories.includes(category.categoryId)}
            onChange={() => toggleCategory(category.categoryId)}
          />
          <span className={level === 0 ? 'parent-category' : 'child-category'}>
            {category.name}
          </span>
        </label>
        {category.subCategories && category.subCategories.length > 0 && 
          renderCategoryTree(category.subCategories, level + 1)
        }
      </React.Fragment>
    ));
  };

  if (loading && products.length === 0) {
    return <div className="loading">{t('common.loading')}</div>;
  }

  if (error) {
    return (
      <div className="error">
        <p>{error}</p>
        <button onClick={loadData}>Спробувати знову</button>
      </div>
    );
  }

  return (
    <>
      {/* Закріплений пошук */}
      <div className="search-bar-sticky">
        <div className="search-container">
          <form onSubmit={handleSearch} className="search-form-sticky">
            <div className="search-input-wrapper">
              <input
                type="text"
                placeholder={t('common.search')}
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="search-input-sticky"
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={handleClearSearch}
                  className="clear-search-btn"
                >
                  &times; 
                </button>
              )}
            </div>
            <button type="submit" className="search-btn-sticky">🔍 {t('common.search')}</button>
          </form>
        </div>
      </div>

      <div className="catalog-layout">
        {/* Фільтри зліва */}
        <aside className="filters-sidebar">
          <div className="filters-header">
            <h3>Фільтри</h3>
            <button onClick={resetFilters} className="reset-btn">Скинути</button>
          </div>

          {/* Фільтр за ціною */}
          <div className="filter-section">
            <h4>Ціна</h4>
            <div className="price-range">
              <input
                type="range"
                min="0"
                max={maxPrice}
                value={priceRange[1]}
                onChange={(e) => setPriceRange([0, +e.target.value])}
                className="price-slider"
              />
              <div className="price-labels">
                <span>До: </span>
                <span>{priceRange[1]} грн</span>
              </div>
            </div>
          </div>

          {/* Фільтр за категоріями */}
          <div className="filter-section">
            <h4>{t('home.categories')}</h4>
            <div className="categories-list">
              {renderCategoryTree(categories)}
            </div>
          </div>
        </aside>

        {/* Список товарів */}
        <div className="products-content">
          <div className="products-header">
            <h2>{t('home.all_products')}</h2>
            <span className="products-count">Знайдено: {products.length}</span>
          </div>

          {products.length === 0 ? (
            <p className="no-products">Товарів не знайдено</p>
          ) : (
            <div className="product-grid">
              {products.map(product => (
                <ProductCard key={product.productId} product={product} />
              ))}
            </div>
          )}
        </div>
      </div>
    </>
  );
}

export default ProductList;