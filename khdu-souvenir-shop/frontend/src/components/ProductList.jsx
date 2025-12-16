import React, { useState, useEffect, useCallback } from 'react';
import ProductCard from './ProductCard';
import { productsAPI, categoriesAPI } from '../services/api';
import './ProductList.css';

function ProductList() {
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
  
  // Застосування фільтрів
  const applyFilters = useCallback(() => {
    let filtered = [...allProducts];
    
    // Фільтр за ціною
    filtered = filtered.filter(p => 
      p.price >= priceRange[0] && p.price <= priceRange[1]
    );
    
    // Фільтр за категоріями
    if (selectedCategories.length > 0) {
      filtered = filtered.filter(p => 
        selectedCategories.includes(p.categoryId)
      );
    }
    
    setProducts(filtered);
  }, [allProducts, priceRange, selectedCategories]);

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
  if (loading && products.length === 0) {
    return <div className="loading">Завантаження товарів...</div>;
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
                placeholder="Пошук товарів..."
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
            <button type="submit" className="search-btn-sticky">🔍 Шукати</button>
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
            <h4>Категорії</h4>
            <div className="categories-list">
              {categories.map(category => (
                <label key={category.categoryId} className="category-checkbox">
                  <input
                    type="checkbox"
                    checked={selectedCategories.includes(category.categoryId)}
                    onChange={() => toggleCategory(category.categoryId)}
                  />
                  <span>{category.name}</span>
                </label>
              ))}
            </div>
          </div>
        </aside>

        {/* Список товарів */}
        <div className="products-content">
          <div className="products-header">
            <h2>Каталог товарів</h2>
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