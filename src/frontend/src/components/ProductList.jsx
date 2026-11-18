import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import ProductCard from './ProductCard';
import { productsAPI, categoriesAPI } from '../services/api';

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
            className="flex items-center gap-2 cursor-pointer p-2 rounded-md transition-colors hover:bg-[#f8f9fa]"
            style={{ marginLeft: `${level * 20}px` }}
          >
            <input
              type="checkbox"
              className="w-[18px] h-[18px] cursor-pointer"
              checked={selectedCategories.includes(category.categoryId)}
              onChange={() => toggleCategory(category.categoryId)}
            />
            <span className={`text-[#495057] text-[0.95rem] ${level === 0 ? 'font-bold text-[#1a1a1a]' : 'font-normal text-[#6c757d]'}`}>
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
      <div className="sticky top-[65px] bg-white z-[999] shadow-[0_2px_8px_rgba(0,0,0,0.1)] py-4">
        <div className="max-w-[1200px] mx-auto px-5">
          <form onSubmit={handleSearch} className="flex gap-4">
            <div className="flex-1 relative flex">
              <input
                type="text"
                aria-label={t('common.search')}
                placeholder={t('common.search')}
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="flex-1 p-[0.85rem] pr-10 border-2 border-[#e1e8ed] rounded-lg text-base transition-all focus:outline-none focus:border-[#007bff] focus:shadow-[0_0_0_3px_rgba(0,123,255,0.1)]"
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={handleClearSearch}
                  className="absolute right-2 top-1/2 -translate-y-1/2 bg-none border-none text-[#595959] cursor-pointer text-2xl p-[0.2rem] leading-none transition-colors z-[2] hover:text-[#333]"
                >
                  &times;
                </button>
              )}
            </div>
            <button type="submit" className="text-white border-none rounded-lg cursor-pointer text-base font-semibold transition-all hover:-translate-y-0.5 hover:shadow-[0_4px_12px_rgba(0,86,179,0.3)] bg-[linear-gradient(135deg,#007bff,#0056b3)] px-8 py-[0.85rem]">🔍 {t('common.search')}</button>
          </form>
        </div>
      </div>
      <div className="max-w-[1200px] mx-auto px-5 py-8 grid grid-cols-1 lg:grid-cols-[280px_1fr] gap-8">
        {/* Фільтри зліва */}
        <aside className="bg-white p-6 rounded-xl shadow-[0_2px_8px_rgba(0,0,0,0.08)] h-fit lg:sticky lg:top-[150px]">
          <div className="flex justify-between items-center mb-6 pb-4 border-b-2 border-[#e9ecef]">
            <h3 className="m-0 text-[#1a1a1a] text-[1.3rem]">{t('common.filters')}</h3>
            <button onClick={resetFilters} className="px-4 py-2 bg-[#f8f9fa] border border-[#dee2e6] rounded-md cursor-pointer text-[0.9rem] text-[#495057] transition-all hover:bg-[#e9ecef]">{t('common.reset')}</button>
          </div>
          {/* Фільтр за ціною */}
          <div className="mb-8">
            <h4 className="m-0 mb-4 text-[#495057] text-base font-semibold">{t('common.price')}</h4>
            <div className="flex flex-col gap-2">
              <input
                type="range"
                aria-label={t('common.price')}
                min="0"
                max={maxPrice}
                value={priceRange[1]}
                onChange={(e) => setPriceRange([0, +e.target.value])}
                className="w-full cursor-pointer"
              />
              <div className="flex justify-between text-[#007bff] font-semibold text-[0.95rem]">
                <span>{t('common.to')}: </span>
                <span>{priceRange[1]} {t('common.currency')}</span>
              </div>
            </div>
          </div>
          {/* Фільтр за категоріями */}
          <div className="mb-8">
            <h4 className="m-0 mb-4 text-[#495057] text-base font-semibold">{t('home.categories')}</h4>
            <div className="flex flex-col gap-3">
              {renderCategoryTree(categories)}
            </div>
          </div>
          {/* Фільтр наявності */}
          <div className="mb-8">
            <h4 className="m-0 mb-4 text-[#495057] text-base font-semibold">{t('common.availability')}</h4>
            <label className="flex items-center gap-2 cursor-pointer p-2 rounded-md transition-colors hover:bg-[#f8f9fa]">
              <input
                type="checkbox"
                className="w-[18px] h-[18px] cursor-pointer"
                checked={inStockOnly}
                onChange={(e) => setInStockOnly(e.target.checked)}
              />
              <span className="text-[#495057] text-[0.95rem]">{t('common.in_stock_only')}</span>
            </label>
          </div>
        </aside>
        {/* Список товарів */}
        <div className="min-h-[400px]">
          <div className="flex justify-between items-center mb-6 gap-4 flex-wrap">
            <div className="flex items-center gap-5 flex-wrap">
              <span className="text-[#666] text-base">{t('common.found')}: {sortedProducts.length}</span>
              <label className="flex items-center gap-2 text-[#495057] text-[0.95rem]">
                <span>{t('common.sort')}:</span>
                <select value={sortBy} onChange={(e) => setSortBy(e.target.value)} className="px-3 py-2 border border-[#dee2e6] rounded-md bg-white text-[#1a1a1a] text-[0.95rem] cursor-pointer focus:outline-none focus:border-[#007bff] focus:shadow-[0_0_0_3px_rgba(0,123,255,0.1)]">
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
            <p className="text-center p-12 text-[#666] text-[1.1rem]">{t('common.no_results')}</p>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
              {paginatedProducts.map(product => (
                <ProductCard key={product.productId} product={product} />
              ))}
            </div>
          )}
          {totalPages > 1 && (
            <div className="flex justify-center items-center gap-6 mt-10">
              <button
                className="px-5 py-[0.6rem] bg-white border border-[#007bff] text-[#007bff] rounded-md cursor-pointer font-semibold transition-all [&:enabled:hover]:bg-[#007bff] [&:enabled:hover]:text-white disabled:border-[#dee2e6] disabled:text-[#adb5bd] disabled:cursor-not-allowed"
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
              >
                ← {t('common.prev')}
              </button>
              <span className="text-[#495057] text-[0.95rem] font-semibold">
                {t('common.page')} {page} / {totalPages}
              </span>
              <button
                className="px-5 py-[0.6rem] bg-white border border-[#007bff] text-[#007bff] rounded-md cursor-pointer font-semibold transition-all [&:enabled:hover]:bg-[#007bff] [&:enabled:hover]:text-white disabled:border-[#dee2e6] disabled:text-[#adb5bd] disabled:cursor-not-allowed"
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
