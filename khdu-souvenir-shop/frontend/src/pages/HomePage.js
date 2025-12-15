import React from 'react';
import ProductList from '../components/ProductList';
import logo from '../assets/khdu-logo.png';
import './HomePage.css';

function HomePage() {
  return (
    <div>
      <section className="hero-banner">
        <div className="hero-content">
          <img src={logo} alt="Герб ХДУ" className="hero-emblem" />
          <h2 className="hero-title">Сувенірна продукція ХДУ</h2>
          <p className="hero-subtitle">
            Офіційні товари з символікою Херсонського державного університету
          </p>
          <div className="hero-features">
            <div className="feature-badge">
              <span className="feature-icon">✓</span>
              Офіційна продукція
            </div>
            <div className="feature-badge">
              <span className="feature-icon">🚚</span>
              Доставка Nova Poshta
            </div>
            <div className="feature-badge">
              <span className="feature-icon">💳</span>
              Онлайн оплата
            </div>
          </div>
        </div>
      </section>
      <ProductList />
    </div>
  );
}

export default HomePage;