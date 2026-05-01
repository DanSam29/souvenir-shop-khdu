import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, buildImageUrl } from '../services/api';
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
        <div className="loading">Завантаження...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="cart-page">
        <div className="error">
          <p>{error}</p>
          <button onClick={loadCart}>Спробувати знову</button>
        </div>
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="cart-page">
        <div className="empty-cart">
          <div className="cart-icon">🛒</div>
          <h2>Кошик порожній</h2>
          <p>Ваш кошик ще не заповнений сувенірами</p>
          <Link to="/" className="btn-primary">
            Перейти до каталогу
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="cart-page">
      <div className="cart-container">
        <div className="cart-header">
          <h1>Кошик</h1>
          <button className="clear-cart-btn" onClick={handleClearCart}>
            🗑️ Очистити кошик
          </button>
        </div>

        <div className="cart-content">
          <div className="cart-items">
            {cart.items.map(item => (
              <div key={item.cartItemId} className="cart-item">
                <div className="item-image">
                  <img 
                    src={buildImageUrl(item.productImage)} 
                    alt={item.productName} 
                  />
                </div>
                <div className="item-info">
                  <Link to={`/product/${item.productId}?from=cart`}>
                    <h3>{item.productName}</h3>
                  </Link>
                  <p className="item-price">{item.productPrice} грн</p>
                </div>
                <div className="item-quantity">
                  <button className="quantity-btn" onClick={() => handleUpdateQuantity(item.cartItemId, item.quantity - 1)}>-</button>
                  <span className="quantity-value">{item.quantity}</span>
                  <button className="quantity-btn" onClick={() => handleUpdateQuantity(item.cartItemId, item.quantity + 1)}>+</button>
                </div>
                <div className="item-subtotal">
                  {(item.subtotal).toFixed(2)} грн
                </div>
                <button 
                  className="remove-btn" 
                  onClick={() => handleRemoveItem(item.cartItemId)}
                  title="Видалити"
                >
                  ✕
                </button>
              </div>
            ))}
          </div>

          <aside className="cart-summary">
            <h2>Підсумок</h2>
            <div className="summary-row">
              <span>Товарів:</span>
              <span>{cart.itemCount}</span>
            </div>
            <div className="summary-row summary-total">
              <span>Разом:</span>
              <span className="total-amount">{cart.totalAmount.toFixed(2)} грн</span>
            </div>
            <Link to="/checkout" className="checkout-btn">
              Оформити замовлення
            </Link>
          </aside>
        </div>
      </div>
    </div>
  );
}

export default CartPage;
