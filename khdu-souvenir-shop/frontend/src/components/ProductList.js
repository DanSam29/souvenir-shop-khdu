import React, { useState, useEffect } from 'react';
import ProductCard from './ProductCard';
import { productsAPI } from '../services/api';
import './ProductList.css';

function ProductList() {
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchQuery, setSearchQuery] = useState('');

  // Завантаження товарів при монтуванні компонента
  useEffect(() => {
    loadProducts();
  }, []);

  const loadProducts = async () => {
    try {
      setLoading(true);
      const response = await productsAPI.getAll();
      setProducts(response.data);
      setError(null);
    } catch (err) {
      console.error('Помилка завантаження товарів:', err);
      setError('Не вдалося завантажити товари. Перевірте з\'єднання з backend.');
    } finally {
      setLoading(false);
    }
  };

  // Пошук товарів
  const handleSearch = async (e) => {
    e.preventDefault();
    if (!searchQuery.trim()) {
      loadProducts();
      return;
    }

    try {
      setLoading(true);
      const response = await productsAPI.search(searchQuery);
      setProducts(response.data);
      setError(null);
    } catch (err) {
      console.error('Помилка пошуку:', err);
      setError('Помилка пошуку товарів');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="loading">Завантаження товарів...</div>;
  }

  if (error) {
    return (
      <div className="error">
        <p>{error}</p>
        <button onClick={loadProducts}>Спробувати знову</button>
      </div>
    );
  }

  return (
    <div className="product-list-container">
      {/* Пошук */}
      <form onSubmit={handleSearch} className="search-form">
        <input
          type="text"
          placeholder="Пошук товарів..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="search-input"
        />
        <button type="submit" className="search-btn">Шукати</button>
      </form>

      {/* Список товарів */}
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
  );
}

export default ProductList;