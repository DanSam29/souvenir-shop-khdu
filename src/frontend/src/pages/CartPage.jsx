import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, buildImageUrl } from '../services/api';
import './CartPage.css';

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
      <div className="cart-page">
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
      <div className="cart-page">
        <div className="loading">{t('common.loading')}</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="cart-page">
        <div className="error">
          <p>{error}</p>
          <button onClick={loadCart}>{t('common.try_again')}</button>
        </div>
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="cart-page">
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

  return (
    <div className="cart-page">
      <div className="cart-container">
        <div className="cart-header">
          <h1>{t('cart.title')}</h1>
          <button className="clear-cart-btn" onClick={handleClearCart}>
            🗑️ {t('cart.clear')}
          </button>
        </div>

        <div className="cart-content">
          <div className="cart-items">
            {cart.items.map(item => {
              const displayName = (isEn && item.productNameEn) ? item.productNameEn : item.productName;
              return (
                <div key={item.cartItemId} className="cart-item">
                  <div className="item-image">
                    <img 
                      src={buildImageUrl(item.productImage)} 
                      alt={displayName} 
                    />
                  </div>
                  <div className="item-info">
                    <Link to={`/product/${item.productId}?from=cart`}>
                      <h3>{displayName}</h3>
                    </Link>
                    <p className="item-price">{item.productPrice} {t('common.currency')}</p>
                  </div>
                  <div className="item-quantity">
                    <button className="quantity-btn" onClick={() => handleUpdateQuantity(item.cartItemId, item.quantity - 1)}>-</button>
                    <span className="quantity-value">{item.quantity}</span>
                    <button className="quantity-btn" onClick={() => handleUpdateQuantity(item.cartItemId, item.quantity + 1)}>+</button>
                  </div>
                  <div className="item-subtotal">
                    {(item.subtotal).toFixed(2)} {t('common.currency')}
                  </div>
                  <button 
                    className="remove-btn" 
                    onClick={() => handleRemoveItem(item.cartItemId)}
                    title={t('common.delete')}
                  >
                    ✕
                  </button>
                </div>
              );
            })}
          </div>

          <aside className="cart-summary">
            <h2>{t('cart.summary')}</h2>
            <div className="summary-row">
              <span>{t('cart.item_count')}:</span>
              <span>{cart.itemCount}</span>
            </div>
            <div className="summary-row summary-total">
              <span>{t('cart.total')}:</span>
              <span className="total-amount">{cart.totalAmount.toFixed(2)} {t('common.currency')}</span>
            </div>
            <Link to="/checkout" className="checkout-btn">
              {t('cart.checkout')}
            </Link>
          </aside>
        </div>
      </div>
    </div>
  );
}

export default CartPage;
