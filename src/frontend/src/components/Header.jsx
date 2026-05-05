import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI } from '../services/api';
import logo from '../assets/khdu-logo.png';
import './Header.css';

function Header() {
  const { t, i18n } = useTranslation();
  const { isAuthenticated, user } = useAuth();
  const [cartCount, setCartCount] = useState(0);

  // Перевірка чи є користувач адміністратором або менеджером
  const isAdmin = user && ['Manager', 'Administrator', 'SuperAdmin'].includes(user.role);

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

  const toggleLanguage = () => {
    const newLang = i18n.language === 'ua' ? 'en' : 'ua';
    i18n.changeLanguage(newLang);
  };

  return (
    <header className="header">
      <div className="container">
        <Link to="/" className="logo">
          <img src={logo} alt="Герб ХДУ" className="logo-emblem" />
          <div className="logo-text">
            <h1>{t('common.app_name')}</h1>
            <span className="logo-subtitle">Херсонський державний університет</span>
          </div>
        </Link>
        <nav className="nav">
          <Link to="/" className="nav-link">
            {t('nav.home')}
          </Link>

          {isAdmin && (
            <Link to="/admin" className="nav-link nav-link-admin">
              {t('nav.admin')}
            </Link>
          )}
          
          <button onClick={toggleLanguage} className="lang-switcher">
            {i18n.language === 'ua' ? 'EN' : 'UA'}
          </button>
          
          {/* Показуємо для неавторизованих */}
          {!isAuthenticated && (
            <>
              <Link to="/login" className="nav-link">
                {t('nav.login')}
              </Link>
              <Link to="/register" className="nav-link nav-link-register">
                {t('nav.register')}
              </Link>
            </>
          )}
          
          {/* Показуємо для авторизованих */}
          {isAuthenticated && (
            <Link to="/profile" className="nav-link nav-link-profile">
              <span className="nav-icon">👤</span>
              {t('nav.profile')}
            </Link>
          )}
          
          <Link to="/cart" className="nav-link nav-link-cart">
            <span className="nav-icon">🛒</span>
            {t('nav.cart')}
            {cartCount > 0 && <span className="cart-badge">{cartCount}</span>}
          </Link>
        </nav>
      </div>
    </header>
  );
}

export default Header;