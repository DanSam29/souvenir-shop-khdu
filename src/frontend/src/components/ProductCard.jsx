import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, buildImageUrl } from '../services/api';
import './ProductCard.css';

function ProductCard({ product }) {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [adding, setAdding] = useState(false);

  const primaryImage = product.images?.find(img => img.isPrimary)?.imageURL 
                      || product.images?.[0]?.imageURL;
  const primaryImageUrl = buildImageUrl(primaryImage);

  const handleAddToCart = async (e) => {
    e.preventDefault(); // Запобігаємо переходу по Link

    if (!isAuthenticated) {
      alert('Для додавання товарів до кошика потрібно авторизуватися');
      navigate('/login');
      return;
    }

    try {
      setAdding(true);
      await cartAPI.addToCart(product.productId, 1);
      alert('Товар додано до кошика!');
      
      // Оновлюємо Header через перезавантаження (можна покращити через контекст)
      window.location.reload();
    } catch (err) {
      console.error('Помилка додавання до кошика:', err);
      if (err.response?.status === 401) {
        alert('Сесія закінчилася. Будь ласка, увійдіть знову');
        navigate('/login');
      } else {
        alert(err.response?.data?.error || 'Помилка додавання до кошика');
      }
    } finally {
      setAdding(false);
    }
  };

  return (
    <div className="product-card">
      <Link to={`/product/${product.productId}`} className="product-link">
        <div className="product-image">
          {primaryImageUrl ? (
            <img src={primaryImageUrl} alt={product.name} />
          ) : (
            <div className="no-image">Без фото</div>
          )}
        </div>
        <div className="product-info">
          <h3 className="product-name">{product.name}</h3>
          {product.category && (
            <p className="product-category">{product.category.name}</p>
          )}
          <p className="product-price">{product.price.toFixed(2)} грн</p>
        </div>
      </Link>
      
      <button 
        onClick={handleAddToCart} 
        className="add-to-cart-btn"
        disabled={adding || product.stock === 0}
      >
        {adding ? 'Додавання...' : product.stock === 0 ? 'Немає в наявності' : 'Додати до кошика'}
      </button>
    </div>
  );
}

export default ProductCard;