import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI } from '../services/api';
import './CartPage.css';

function CartPage() {
  const { isAuthenticated } = useAuth();
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (isAuthenticated) {
      loadCart();
    } else {
      setLoading(false);
    }
  }, [isAuthenticated]);

  const loadCart = async () => {
    try {
      setLoading(true);
      const response = await cartAPI.getCart();
      setCart(response.data);
    } catch (err) {
      console.error('Помилка завантаження кошика:', err);
      setError('Не вдалося завантажити кошик');
    } finally {
      setLoading(false);
    }
  };

  const handleUpdateQuantity = async (cartItemId, newQuantity) => {
    if (newQuantity < 1) return;

    try {
      await cartAPI.updateQuantity(cartItemId, newQuantity);
      await loadCart(); // Перезавантажуємо кошик
    } catch (err) {
      console.error('Помилка оновлення кількості:', err);
      alert(err.response?.data?.error || 'Помилка оновлення кількості');
    }
  };

  const handleRemoveItem = async (cartItemId) => {
    try {
      await cartAPI.removeFromCart(cartItemId);
      await loadCart();
    } catch (err) {
      console.error('Помилка видалення товару:', err);
      alert('Помилка видалення товару');
    }
  };

  const handleClearCart = async () => {
    if (!window.confirm('Ви впевнені, що хочете очистити кошик?')) {
      return;
    }

    try {
      await cartAPI.clearCart();
      await loadCart();
    } catch (err) {
      console.error('Помилка очищення кошика:', err);
      alert('Помилка очищення кошика');
    }
  };

  // Якщо не авторизований
  if (!isAuthenticated) {
    return (
      <div className="cart-page">
        <div className="guest-message">
          <div className="cart-icon">🛒</div>
          <h2>Для додавання товарів до кошика потрібно авторизуватися</h2>
          <p>Увійдіть в систему або зареєструйтеся, щоб почати покупки</p>
          <div className="guest-actions">
            <Link to="/login" className="btn-primary">
              Увійти
            </Link>
            <Link to="/register" className="btn-secondary">
              Зареєструватися
            </Link>
          </div>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="cart-page">
        <div className="loading">Завантаження кошика...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="cart-page">
        <div className="error-message">{error}</div>
      </div>
    );
  }

  // Якщо кошик порожній
  if (!cart || cart.items.length === 0) {
    return (
      <div className="cart-page">
        <div className="empty-cart">
          <div className="cart-icon">🛒</div>
          <h2>Ваш кошик порожній</h2>
          <p>Додайте товари до кошика, щоб продовжити покупки</p>
          <Link to="/" className="back-to-catalog-btn">
            Перейти до каталогу
          </Link>
        </div>
      </div>
    );
  }

  // Якщо у кошику є товари
  return (
    <div className="cart-page">
      <div className="cart-container">
        <div className="cart-header">
          <h1>Кошик</h1>
          <button onClick={handleClearCart} className="clear-cart-btn">
            Очистити кошик
          </button>
        </div>

        <div className="cart-items">
          {cart.items.map((item) => (
            <div key={item.cartItemId} className="cart-item">
              <div className="item-image">
                {item.productImage ? (
                  <img src={item.productImage} alt={item.productName} />
                ) : (
                  <div className="no-image">Без фото</div>
                )}
              </div>

              <div className="item-info">
                <h3>{item.productName}</h3>
                <p className="item-price">{item.productPrice.toFixed(2)} грн</p>
              </div>

              <div className="item-quantity">
                <button
                  onClick={() => handleUpdateQuantity(item.cartItemId, item.quantity - 1)}
                  className="quantity-btn"
                  disabled={item.quantity <= 1}
                >
                  −
                </button>
                <span className="quantity-value">{item.quantity}</span>
                <button
                  onClick={() => handleUpdateQuantity(item.cartItemId, item.quantity + 1)}
                  className="quantity-btn"
                >
                  +
                </button>
              </div>

              <div className="item-subtotal">
                <p>{item.subtotal.toFixed(2)} грн</p>
              </div>

              <button
                onClick={() => handleRemoveItem(item.cartItemId)}
                className="remove-btn"
                title="Видалити товар"
              >
                ✕
              </button>
            </div>
          ))}
        </div>

        <div className="cart-summary">
          <div className="summary-row">
            <span>Кількість товарів:</span>
            <span>{cart.itemCount}</span>
          </div>
          <div className="summary-row summary-total">
            <span>Загальна сума:</span>
            <span className="total-amount">{cart.totalAmount.toFixed(2)} грн</span>
          </div>
          <button className="checkout-btn">
            Оформити замовлення
          </button>
        </div>
      </div>
    </div>
  );
}

export default CartPage;