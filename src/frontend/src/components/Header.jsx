import React, { useState, useEffect, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, productsAPI, buildImageUrl } from '../services/api';
import logo from '../assets/khdu-logo.png';
import './Header.css';

function Header() {
  const { t, i18n } = useTranslation();
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();
  const [cartCount, setCartCount] = useState(0);
  const [searchQuery, setSearchQuery] = useState('');
  const [suggestions, setSuggestions] = useState([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const searchRef = useRef(null);
  const isEn = i18n.language === 'en';
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
  // Закриття підказок при кліку поза полем пошуку
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (searchRef.current && !searchRef.current.contains(e.target)) {
        setShowSuggestions(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);
  // Автодоповнення з дебаунсом
  useEffect(() => {
    const term = searchQuery.trim();
    if (term.length < 2) {
      setSuggestions([]);
      setShowSuggestions(false);
      return;
    }
    const timer = setTimeout(async () => {
      try {
        const response = await productsAPI.search(term);
        setSuggestions((response.data || []).slice(0, 5));
        setShowSuggestions(true);
      } catch (err) {
        console.error('Помилка автодоповнення пошуку:', err);
        setSuggestions([]);
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [searchQuery]);
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
  const handleSearchSubmit = (e) => {
    e.preventDefault();
    const term = searchQuery.trim();
    setShowSuggestions(false);
    if (!term) return;
    navigate(`/?search=${encodeURIComponent(term)}#catalog`);
  };
  const handleSuggestionClick = (productId) => {
    setSearchQuery('');
    setSuggestions([]);
    setShowSuggestions(false);
    navigate(`/product/${productId}`);
  };
  return (
    <header className="header">
      <div className="container">
        <Link to="/" className="logo">
          <img src={logo} alt="Герб ХДУ" className="logo-emblem" />
          <div className="logo-text">
            <h1>{t('common.app_name')}</h1>
            <span className="logo-subtitle">{t('common.university_name')}</span>
          </div>
        </Link>
        <nav className="nav">
          <div className="header-search" ref={searchRef}>
            <form onSubmit={handleSearchSubmit} className="header-search-form">
              <input
                type="text"
                className="header-search-input"
                placeholder={t('header.search_placeholder')}
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onFocus={() => { if (suggestions.length > 0) setShowSuggestions(true); }}
              />
              <button type="submit" className="header-search-btn" aria-label={t('common.search')}>🔍</button>
            </form>
            {showSuggestions && suggestions.length > 0 && (
              <ul className="header-search-suggestions">
                {suggestions.map(item => {
                  const name = (isEn && item.nameEn) ? item.nameEn : item.name;
                  const primaryImage = item.images?.find(img => img.isPrimary)?.imageURL || item.images?.[0]?.imageURL;
                  return (
                    <li key={item.productId}>
                      <button type="button" onClick={() => handleSuggestionClick(item.productId)}>
                        {primaryImage && (
                          <img src={buildImageUrl(primaryImage)} alt={name} className="suggestion-image" />
                        )}
                        <span className="suggestion-name">{name}</span>
                        <span className="suggestion-price">{item.price.toFixed(2)} {t('common.currency')}</span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
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
          {/* Відображення для неавторизованих */}
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
          {/* Відображення для авторизованих */}
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
