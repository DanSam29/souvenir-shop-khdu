import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, buildImageUrl } from '../services/api';

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
    e.preventDefault(); // Запобігнення переходу по Link
    if (!isAuthenticated) {
      alert(t('common.auth_required_cart') || 'Для додавання товарів до кошика потрібно авторизуватися');
      navigate('/login');
      return;
    }
    try {
      setAdding(true);
      await cartAPI.addToCart(product.productId, 1);
      alert(t('common.added_to_cart') || 'Товар додано до кошика!');
      // Оновлення Header через перезавантаження
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
    <div className="flex flex-col bg-white rounded-lg overflow-hidden shadow-[0_2px_8px_rgba(0,0,0,0.1)] transition hover:-translate-y-1 hover:shadow-[0_4px_12px_rgba(0,0,0,0.15)]">
      <Link to={`/product/${product.productId}`} className="flex-1 flex flex-col no-underline text-inherit">
        <div className="w-full h-[250px] overflow-hidden bg-[#f4f4f4]">
          {primaryImageUrl ? (
            <img src={primaryImageUrl} alt={displayName} width={400} height={400} loading="lazy" decoding="async" className="w-full h-full object-cover" />
          ) : (
            <div className="no-image">{t('product.no_image')}</div>
          )}
        </div>
        <div className="p-4">
          <h3 className="m-0 mb-2 text-[1.1rem] text-[#333]">{displayName}</h3>
          {displayCategory && (
            <p className="m-0 mb-3 text-[#666] text-[0.9rem]">{displayCategory}</p>
          )}
          <div className="flex gap-2 items-center flex-wrap">
            {product.originalPrice && product.originalPrice !== product.price ? (
              <>
                <span className="text-base text-[#595959] line-through">{product.originalPrice.toFixed(2)} {t('common.currency')}</span>
                <span className="text-[1.25rem] font-bold text-[#007bff]">{product.price.toFixed(2)} {t('common.currency')}</span>
              </>
            ) : (
              <span className="text-[1.25rem] font-bold text-[#007bff]">{product.price.toFixed(2)} {t('common.currency')}</span>
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