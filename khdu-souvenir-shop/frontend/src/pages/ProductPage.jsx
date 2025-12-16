import React, { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { productsAPI } from '../services/api';
import './ProductPage.css';

function ProductPage() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const loadProduct = useCallback(async () => {
    try {
      setLoading(true);
      const response = await productsAPI.getById(id);
      setProduct(response.data);
      setError(null);
    } catch (err) {
      console.error('Помилка завантаження товару:', err);
      setError('Товар не знайдено');
    } finally {
      setLoading(false);
    }
  }, [id]); // Залежність: id, оскільки API-запит залежить від нього.

  useEffect(() => {
    loadProduct();
  }, [loadProduct]); // Залежність: стабільна функція loadProduct (з useCallback)

  if (loading) {
    return <div className="loading">Завантаження...</div>;
  }

  if (error || !product) {
    return (
      <div className="error">
        <p>{error}</p>
        <Link to="/">Повернутися до каталогу</Link>
      </div>
    );
  }

  const imageUrl = product.images && product.images.length > 0 
    ? product.images[0].imageURL 
    : 'https://via.placeholder.com/500x500?text=No+Image';

  return (
    <div className="product-page">
      <Link to="/" className="back-link">← Назад до каталогу</Link>
      
      <div className="product-details">
        <div className="product-images">
          <img src={imageUrl} alt={product.name} />
        </div>

        <div className="product-info-detailed">
          <h1>{product.name}</h1>
          <p className="category-badge">{product.category?.name}</p>
          
          <div className="price-section">
            <span className="price">{product.price} грн</span>
            <span className={`stock-badge ${product.stock > 0 ? 'in-stock' : 'out-of-stock'}`}>
              {product.stock > 0 ? `✓ В наявності (${product.stock} шт)` : '✗ Немає в наявності'}
            </span>
          </div>

          <div className="description">
            <h3>Опис товару</h3>
            <p>{product.description}</p>
          </div>

          <div className="product-specs">
            <h3>Характеристики</h3>
            <table>
              <tbody>
                <tr>
                  <td>Вага:</td>
                  <td>{product.weight} кг</td>
                </tr>
                <tr>
                  <td>Дата додавання:</td>
                  <td>{new Date(product.createdAt).toLocaleDateString('uk-UA')}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <button 
            className="add-to-cart-btn" 
            disabled={product.stock === 0}
          >
            {product.stock > 0 ? 'Додати до кошика' : 'Немає в наявності'}
          </button>
        </div>
      </div>
    </div>
  );
}

export default ProductPage;