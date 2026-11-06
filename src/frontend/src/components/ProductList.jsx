import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import ProductCard from './ProductCard';
import { productsAPI, categoriesAPI } from '../services/api';
import './ProductList.css';

const PAGE_SIZE = 12;

function ProductList() {
  const { t, i18n } = useTranslation();
  const [searchParams] = useSearchParams();
  const [products, setProducts] = useState([]);
  const [allProducts, setAllProducts] = useState([]);
  const [originalProducts, setOriginalProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchQuery, setSearchQuery] = useState('');
  const isEn = i18n.language === 'en';
  // Фільтри
  const [priceRange, setPriceRange] = useState([0, 10000]);
  const [maxPrice, setMaxPrice] = useState(10000);
  const [selectedCategories, setSelectedCategories] = useState([]);
  const [inStockOnly, setInStockOnly] = useState(false);
  const [sortBy, setSortBy] = useState('newest');
  const [page, setPage] = useState(1);
  // Функція для отримання всіх ID дочірніх категорій
  const getAllCategoryIds = useCallback((categoryIds) => {
    const allIds = new Set();
    const addIds = (id) => {
      if (allIds.has(id)) return;
      allIds.add(id);
      // Пошук категорії в дереві (рекурсивно)
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
  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      const [productsRes, categoriesRes] = await Promise.all([
        productsAPI.getAll({ pageSize: 100 }),
        categoriesAPI.getAll()
      ]);
      const productItems = productsRes.data.items || [];
      setOriginalProducts(productItems);
      setAllProducts(productItems);
      setCategories(categoriesRes.data);
      // Пошук максимальної ціни
      const prices = productItems.map(p => p.price);
      const max = Math.max(...prices, 1000);
      setMaxPrice(max);
      setPriceRange([0, max]);
      setError(null);
    } catch (err) {
      console.error('Помилка завантаження:', err);
      setError(t('common.load_error') || 'Не вдалося завантажити товари');
    } finally {
      setLoading(false);
    }
  }, [t]);
  // Завантаження товарів та категорій при монтуванні
  useEffect(() => {
    loadData();
  }, [loadData]);
  // Ініціалізація фільтрів з URL-параметрів (?category=, ?search=)
  useEffect(() => {
    if (categories.length === 0) return;
    const categoryParam = searchParams.get('category');
    if (categoryParam) {
      const catId = Number(categoryParam);
      if (!Number.isNaN(catId)) {
        setSelectedCategories([catId]);
      }
    }
    const searchParam = searchParams.get('search');
    if (searchParam) {
      setSearchQuery(searchParam);
    }
  }, [categories, searchParams]);
  // Результат фільтрації (ціна, категорії, наявність)
  const filteredProducts = useMemo(() => {
    let filtered = [...allProducts];
    filtered = filtered.filter(p => p.price >= priceRange[0] && p.price <= priceRange[1]);
    if (selectedCategories.length > 0) {
      const targetCategoryIds = getAllCategoryIds(selectedCategories);
      filtered = filtered.filter(p => targetCategoryIds.includes(p.categoryId));
    }
    if (inStockOnly) {
      filtered = filtered.filter(p => p.stock > 0);
    }
    return filtered;
  }, [allProducts, priceRange, selectedCategories, inStockOnly, getAllCategoryIds]);
  // Сортування
  const sortedProducts = useMemo(() => {
    const sorted = [...filteredProducts];
    switch (sortBy) {
      case 'price_asc':
        sorted.sort((a, b) => a.price - b.price);
        break;
      case 'price_desc':
        sorted.sort((a, b) => b.price - a.price);
        break;
      case 'name_asc':
        sorted.sort((a, b) => a.name.localeCompare(b.name));
        break;
      case 'name_desc':
        sorted.sort((a, b) => b.name.localeCompare(a.name));
        break;
      default:
        sorted.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
    }
    return sorted;
  }, [filteredProducts, sortBy]);
  // Пагінація
  const totalPages = Math.max(1, Math.ceil(sortedProducts.length / PAGE_SIZE));
  const paginatedProducts = useMemo(
    () => sortedProducts.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [sortedProducts, page]
  );
  // Скидання сторінки при зміні фільтрів
  useEffect(() => {
    setPage(1);
  }, [priceRange, selectedCategories, inStockOnly, sortBy, allProducts]);
  // Підтримка актуальної кількості товарів
  useEffect(() => {
    setProducts(sortedProducts);
  }, [sortedProducts]);
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
      setError(t('common.search_error') || 'Помилка пошуку товарів');
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
    setInStockOnly(false);
    setSortBy('newest');
    setSearchQuery('');
    setAllProducts(originalProducts);
  };
  // Очищення пошуку
  const handleClearSearch = () => {
    setSearchQuery('');
    setAllProducts(originalProducts);
  };
  // Рекурсивний рендеринг дерева категорій
  const renderCategoryTree = (nodes, level = 0) => {
    return nodes.map(category => {
      const displayName = (isEn && category.nameEn) ? category.nameEn : category.name;
      return (
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
              {displayName}
            </span>
          </label>
          {category.subCategories && category.subCategories.length > 0 &&
            renderCategoryTree(category.subCategories, level + 1)
          }
        </React.Fragment>
      );
    });
  };
  if (loading && products.length === 0) {
    return <div className="loading">{t('common.loading')}</div>;
  }
  if (error) {
    return (
      <div className="error">
        <p>{error}</p>
        <button onClick={loadData}>{t('common.try_again')}</button>
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
            <h3>{t('common.filters')}</h3>
            <button onClick={resetFilters} className="reset-btn">{t('common.reset')}</button>
          </div>
          {/* Фільтр за ціною */}
          <div className="filter-section">
            <h4>{t('common.price')}</h4>
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
                <span>{t('common.to')}: </span>
                <span>{priceRange[1]} {t('common.currency')}</span>
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
          {/* Фільтр наявності */}
          <div className="filter-section">
            <h4>{t('common.availability')}</h4>
            <label className="category-checkbox">
              <input
                type="checkbox"
                checked={inStockOnly}
                onChange={(e) => setInStockOnly(e.target.checked)}
              />
              <span>{t('common.in_stock_only')}</span>
            </label>
          </div>
        </aside>
        {/* Список товарів */}
        <div className="products-content">
          <div className="products-header">
            <h2>{t('home.all_products')}</h2>
            <div className="products-toolbar">
              <span className="products-count">{t('common.found')}: {sortedProducts.length}</span>
              <label className="sort-control">
                <span>{t('common.sort')}:</span>
                <select value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
                  <option value="newest">{t('common.sort_newest')}</option>
                  <option value="price_asc">{t('common.sort_price_asc')}</option>
                  <option value="price_desc">{t('common.sort_price_desc')}</option>
                  <option value="name_asc">{t('common.sort_name_asc')}</option>
                  <option value="name_desc">{t('common.sort_name_desc')}</option>
                </select>
              </label>
            </div>
          </div>
          {paginatedProducts.length === 0 ? (
            <p className="no-products">{t('common.no_results')}</p>
          ) : (
            <div className="product-grid">
              {paginatedProducts.map(product => (
                <ProductCard key={product.productId} product={product} />
              ))}
            </div>
          )}
          {totalPages > 1 && (
            <div className="pagination">
              <button
                className="page-btn"
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
              >
                ← {t('common.prev')}
              </button>
              <span className="page-info">
                {t('common.page')} {page} / {totalPages}
              </span>
              <button
                className="page-btn"
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
              >
                {t('common.next')} →
              </button>
            </div>
          )}
        </div>
      </div>
    </>
  );
}

export default ProductList;
