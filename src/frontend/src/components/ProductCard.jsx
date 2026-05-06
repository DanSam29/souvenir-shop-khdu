import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, buildImageUrl } from '../services/api';
import './ProductCard.css';

function ProductCard({ product }) {
  const { t, i18n } = useTranslation();
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [adding, setAdding] = useState(false);

  const isEn = i18n.language === 'en';
  const displayName = (isEn && product.nameEn) ? product.nameEn : product.name;
  const displayCategory = (isEn && product.category?.nameEn) ? product.category.nameEn : product.category?.name;

  const primaryImage = product.images?.find(img => img.isPrimary)?.imageURL 
                      || product.images?.[0]?.imageURL;
  const primaryImageUrl = buildImageUrl(primaryImage);

  const handleAddToCart = async (e) => {
    e.preventDefault(); // Запобігаємо переходу по Link

    if (!isAuthenticated) {
      alert(t('common.auth_required_cart') || 'Для додавання товарів до кошика потрібно авторизуватися');
      navigate('/login');
      return;
    }

    try {
      setAdding(true);
      await cartAPI.addToCart(product.productId, 1);
      alert(t('common.added_to_cart') || 'Товар додано до кошика!');
      
      // Оновлюємо Header через перезавантаження (можна покращити через контекст)
      window.location.reload();
    } catch (err) {
      console.error('Помилка додавання до кошика:', err);
      if (err.response?.status === 401) {
        alert(t('common.session_expired') || 'Сесія закінчилася. Будь ласка, увійдіть знову');
        navigate('/login');
      } else {
        alert(err.response?.data?.error || t('common.add_to_cart_error') || 'Помилка додавання до кошика');
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
            <img src={primaryImageUrl} alt={displayName} />
          ) : (
            <div className="no-image">{t('product.no_image')}</div>
          )}
        </div>
        <div className="product-info">
          <h3 className="product-name">{displayName}</h3>
          {displayCategory && (
            <p className="product-category">{displayCategory}</p>
          )}
          <div className="product-price-container">
            {product.originalPrice && product.originalPrice !== product.price ? (
              <>
                <span className="product-price-original">{product.originalPrice.toFixed(2)} {t('common.currency')}</span>
                <span className="product-price">{product.price.toFixed(2)} {t('common.currency')}</span>
              </>
            ) : (
              <span className="product-price">{product.price.toFixed(2)} {t('common.currency')}</span>
            )}
          </div>
        </div>
      </Link>
      
      <button 
        onClick={handleAddToCart} 
        className="add-to-cart-btn"
        disabled={adding || product.stock === 0}
      >
        {adding ? t('common.adding') : product.stock === 0 ? t('product.out_of_stock') : t('product.add_to_cart')}
      </button>
    </div>
  );
}

export default ProductCard;