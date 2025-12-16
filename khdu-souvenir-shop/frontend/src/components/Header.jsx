import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI } from '../services/api';
import logo from '../assets/khdu-logo.png';
import './Header.css';

function Header() {
  const { isAuthenticated } = useAuth();
  const [cartCount, setCartCount] = useState(0);

  // Завантаження кількості товарів у кошику (тільки для авторизованих)
  useEffect(() => {
    if (isAuthenticated) {
      loadCartCount();
    } else {
      setCartCount(0);
    }
  }, [isAuthenticated]);

  const loadCartCount = async () => {
    try {
      const response = await cartAPI.getCart();
      setCartCount(response.data.itemCount || 0);
    } catch (error) {
      console.error('Помилка завантаження кошика:', error);
    }
  };

  return (
    <header className="header">
      <div className="container">
        <Link to="/" className="logo">
          <img src={logo} alt="Герб ХДУ" className="logo-emblem" />
          <div className="logo-text">
            <h1>ХДУ Сувеніри</h1>
            <span className="logo-subtitle">Херсонський державний університет</span>
          </div>
        </Link>
        <nav className="nav">
          <Link to="/" className="nav-link">
            Каталог
          </Link>
          
          {/* Показуємо для неавторизованих */}
          {!isAuthenticated && (
            <>
              <Link to="/login" className="nav-link">
                Увійти
              </Link>
              <Link to="/register" className="nav-link nav-link-register">
                Реєстрація
              </Link>
            </>
          )}
          
          {/* Показуємо для авторизованих */}
          {isAuthenticated && (
            <Link to="/profile" className="nav-link nav-link-profile">
              <span className="nav-icon">👤</span>
              Особистий кабінет
            </Link>
          )}
          
          <Link to="/cart" className="nav-link nav-link-cart">
            <span className="nav-icon">🛒</span>
            Кошик
            {cartCount > 0 && <span className="cart-badge">{cartCount}</span>}
          </Link>
        </nav>
      </div>
    </header>
  );
}

export default Header;