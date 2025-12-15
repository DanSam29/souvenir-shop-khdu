import React from 'react';
import { Link } from 'react-router-dom';
import logo from '../assets/khdu-logo.png';
import './Header.css';

function Header() {
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
          <Link to="/login" className="nav-link">
            Увійти
          </Link>
          <Link to="/register" className="nav-link nav-link-register">
            Реєстрація
          </Link>
          <Link to="/cart" className="nav-link nav-link-cart">
            <span className="nav-icon">🛒</span>
            Кошик
            <span className="cart-badge">0</span>
          </Link>
        </nav>
      </div>
    </header>
  );
}

export default Header;