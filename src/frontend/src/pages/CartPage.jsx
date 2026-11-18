import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, buildImageUrl } from '../services/api';

function CartPage() {
  const { t, i18n } = useTranslation();
  const { isAuthenticated } = useAuth();
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const isEn = i18n.language === 'en';
  const loadCart = useCallback(async () => {
    try {
      setLoading(true);
      const response = await cartAPI.getCart();
      setCart(response.data);
    } catch (err) {
      console.error('Помилка завантаження кошика:', err);
      setError(t('common.load_error') || 'Не вдалося завантажити кошик');
    } finally {
      setLoading(false);
    }
  }, [t]);
  useEffect(() => {
    if (isAuthenticated) {
      loadCart();
    } else {
      setLoading(false);
    }
  }, [isAuthenticated, loadCart]);
  const handleUpdateQuantity = async (cartItemId, newQuantity) => {
    if (newQuantity < 1) return;
    try {
      await cartAPI.updateQuantity(cartItemId, newQuantity);
      await loadCart(); // Перезавантажуємо кошик
    } catch (err) {
      console.error('Помилка оновлення кількості:', err);
      alert(err.response?.data?.error || t('common.update_error') || 'Помилка оновлення кількості');
    }
  };
  const handleRemoveItem = async (cartItemId) => {
    try {
      await cartAPI.removeFromCart(cartItemId);
      await loadCart();
    } catch (err) {
      console.error('Помилка видалення товару:', err);
      alert(t('common.remove_error') || 'Помилка видалення товару');
    }
  };
  const handleClearCart = async () => {
    if (!window.confirm(t('cart.clear_confirm') || 'Ви впевнені, що хочете очистити кошик?')) {
      return;
    }
    try {
      await cartAPI.clearCart();
      await loadCart();
    } catch (err) {
      console.error('Помилка очищення кошика:', err);
      alert(t('common.clear_error') || 'Помилка очищення кошика');
    }
  };
  // Якщо не авторизований
  if (!isAuthenticated) {
    return (
      <div className="min-h-[80vh] p-8 bg-[#f5f5f5]">
        <div className="guest-message">
          <div className="cart-icon">🛒</div>
          <h2>{t('common.auth_required_cart')}</h2>
          <p>{t('cart.login_prompt')}</p>
          <div className="guest-actions">
            <Link to="/login" className="btn-primary">
              {t('nav.login')}
            </Link>
            <Link to="/register" className="btn-secondary">
              {t('nav.register')}
            </Link>
          </div>
        </div>
      </div>
    );
  }
  if (loading) {
    return (
      <div className="min-h-[80vh] p-8 bg-[#f5f5f5]">
        <div className="loading">{t('common.loading')}</div>
      </div>
    );
  }
  if (error) {
    return (
      <div className="min-h-[80vh] p-8 bg-[#f5f5f5]">
        <div className="error">
          <p>{error}</p>
          <button onClick={loadCart}>{t('common.try_again')}</button>
        </div>
      </div>
    );
  }
  if (!cart || cart.items.length === 0) {
    return (
      <div className="min-h-[80vh] p-8 bg-[#f5f5f5]">
        <div className="empty-cart">
          <div className="cart-icon">🛒</div>
          <h2>{t('cart.empty')}</h2>
          <p>{t('cart.empty_desc')}</p>
          <Link to="/" className="btn-primary">
            {t('product.back_to_catalog')}
          </Link>
        </div>
      </div>
    );
  }
  const totalDiscount = cart.items.reduce((sum, item) => {
    const original = item.originalPrice || item.productPrice || 0;
    return sum + Math.max(0, original - item.productPrice) * item.quantity;
  }, 0);
  return (
    <div className="min-h-[80vh] p-8 bg-[#f5f5f5]">
      <div className="max-w-[1200px] mx-auto bg-white rounded-xl shadow-[0_4px_20px_rgba(0,0,0,0.1)] p-8">
        <div className="flex justify-between items-center mb-8 pb-4 border-b-2 border-[#f0f0f0]">
          <h1 className="m-0 text-[2rem] text-[#333]">{t('cart.title')}</h1>
          <button className="px-6 py-[0.6rem] bg-[#dc3545] text-white border-none rounded-lg font-semibold cursor-pointer transition-colors hover:bg-[#c82333]" onClick={handleClearCart}>
            🗑️ {t('cart.clear')}
          </button>
        </div>
        <div className="grid grid-cols-[2fr_1fr] gap-8">
          <div className="mb-8">
            {cart.items.map(item => {
              const displayName = (isEn && item.productNameEn) ? item.productNameEn : item.productName;
              const hasDiscount = item.originalPrice && item.originalPrice > item.productPrice;
              return (
                <div key={item.cartItemId} className="relative flex items-center gap-6 p-6 bg-[#f9f9f9] rounded-lg mb-4 transition-shadow flex-wrap md:flex-nowrap hover:shadow-[0_4px_12px_rgba(0,0,0,0.1)]">
                  <div className="w-[100px] h-[100px] flex-shrink-0 max-md:w-20 max-md:h-20">
                    <img 
                      src={buildImageUrl(item.productImage)} 
                      alt={displayName} 
                      className="w-full h-full object-cover rounded-lg"
                    />
                  </div>
                  <div className="flex-1">
                    <Link to={`/product/${item.productId}?from=cart`}>
                      <h3 className="m-0 mb-2 text-[1.2rem] text-[#333]">{displayName}</h3>
                    </Link>
                    <p className="m-0 text-[#666] text-base flex items-center gap-2">
                      {hasDiscount ? (
                        <>
                          <span className="text-[#595959] line-through text-[0.9rem]">{item.originalPrice.toFixed(2)} {t('common.currency')}</span>
                          <span className="text-[#dc3545] font-semibold">{item.productPrice} {t('common.currency')}</span>
                        </>
                      ) : (
                        <span>{item.productPrice} {t('common.currency')}</span>
                      )}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <button className="w-9 h-9 border-2 border-[#667eea] bg-white text-[#667eea] rounded-md text-[1.2rem] cursor-pointer transition-all [&:enabled:hover]:bg-[#667eea] [&:enabled:hover]:text-white disabled:opacity-40 disabled:cursor-not-allowed" onClick={() => handleUpdateQuantity(item.cartItemId, item.quantity - 1)}>-</button>
                    <span className="text-[1.2rem] font-semibold min-w-[40px] text-center">{item.quantity}</span>
                    <button className="w-9 h-9 border-2 border-[#667eea] bg-white text-[#667eea] rounded-md text-[1.2rem] cursor-pointer transition-all [&:enabled:hover]:bg-[#667eea] [&:enabled:hover]:text-white disabled:opacity-40 disabled:cursor-not-allowed" onClick={() => handleUpdateQuantity(item.cartItemId, item.quantity + 1)}>+</button>
                  </div>
                  <div className="text-[1.3rem] font-bold text-[#333] min-w-[120px] text-right max-md:w-full max-md:text-left max-md:mt-4">
                    {(item.subtotal).toFixed(2)} {t('common.currency')}
                  </div>
                  <button 
                    className="w-9 h-9 bg-[#dc3545] text-white border-none rounded-md text-[1.2rem] cursor-pointer transition-colors flex-shrink-0 max-md:absolute max-md:top-4 max-md:right-4 hover:bg-[#c82333]" 
                    onClick={() => handleRemoveItem(item.cartItemId)}
                    title={t('common.delete')}
                  >
                    ✕
                  </button>
                </div>
              );
            })}
          </div>
          <aside className="bg-[#f9f9f9] p-8 rounded-lg border-2 border-[#e0e0e0]">
            <h2>{t('cart.summary')}</h2>
            <div className="flex justify-between mb-4 text-[1.1rem] text-[#666]">
              <span>{t('cart.item_count')}:</span>
              <span>{cart.itemCount}</span>
            </div>
            {totalDiscount > 0 && (
              <div className="flex justify-between mb-4 text-[1.1rem] text-[#dc3545]">
                <span>{t('cart.discount')}:</span>
                <span>-{totalDiscount.toFixed(2)} {t('common.currency')}</span>
              </div>
            )}
            <div className="flex justify-between mb-4 border-t-2 border-[#e0e0e0] pt-4 mt-4 text-[1.4rem] font-bold text-[#333]">
              <span>{t('cart.total')}:</span>
              <span className="text-[#667eea]">{cart.totalAmount.toFixed(2)} {t('common.currency')}</span>
            </div>
            <Link to="/checkout" className="block w-full p-[1.2rem] text-white no-underline text-center border-none rounded-lg text-[1.2rem] font-semibold cursor-pointer mt-6 transition-all hover:-translate-y-0.5 hover:shadow-[0_5px_20px_rgba(102,126,234,0.4)] bg-[linear-gradient(135deg,#667eea_0%,#764ba2_100%)]">
              {t('cart.checkout')}
            </Link>
          </aside>
        </div>
      </div>
    </div>
  );
}

export default CartPage;