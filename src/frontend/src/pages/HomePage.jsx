import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import ProductList from '../components/ProductList';
import ProductCard from '../components/ProductCard';
import { productsAPI, categoriesAPI } from '../services/api';
import './HomePage.css';

function HomePage() {
  const { t, i18n } = useTranslation();
  const isEn = i18n.language === 'en';
  const [featured, setFeatured] = useState([]);
  const [newProducts, setNewProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  useEffect(() => {
    let active = true;
    const loadHome = async () => {
      try {
        const [featuredRes, newRes, categoriesRes] = await Promise.all([
          productsAPI.getFeatured(8),
          productsAPI.getNew(8),
          categoriesAPI.getAll()
        ]);
        if (!active) return;
        setFeatured(featuredRes.data || []);
        setNewProducts(newRes.data || []);
        setCategories(categoriesRes.data || []);
      } catch (err) {
        console.error('Помилка завантаження головної сторінки:', err);
      }
    };
    loadHome();
    return () => { active = false; };
  }, []);
  const categoryName = (c) => (isEn && c.nameEn) ? c.nameEn : c.name;
  return (
    <div className="home-page">
      {/* Рекомендовані товари */}
      {featured.length > 0 && (
        <section className="home-section">
          <div className="home-section-header">
            <h2>{t('home.featured')}</h2>
            <Link to="/#catalog" className="home-section-link">{t('common.view_all')}</Link>
          </div>
          <div className="product-grid">
            {featured.map(product => (
              <ProductCard key={product.productId} product={product} />
            ))}
          </div>
        </section>
      )}

      {/* Новинки */}
      {newProducts.length > 0 && (
        <section className="home-section">
          <div className="home-section-header">
            <h2>{t('home.new_arrivals')}</h2>
            <Link to="/#catalog" className="home-section-link">{t('common.view_all')}</Link>
          </div>
          <div className="product-grid">
            {newProducts.map(product => (
              <ProductCard key={product.productId} product={product} />
            ))}
          </div>
        </section>
      )}

      {/* Категорії */}
      {categories.length > 0 && (
        <section className="home-section">
          <div className="home-section-header">
            <h2>{t('home.categories')}</h2>
          </div>
          <div className="category-tiles">
            {categories.map(category => (
              <Link
                key={category.categoryId}
                to={`/?category=${category.categoryId}#catalog`}
                className="category-tile"
              >
                <span className="category-tile-name">{categoryName(category)}</span>
                {category.subCategories?.length > 0 && (
                  <span className="category-tile-sub">
                    {category.subCategories.map(categoryName).join(', ')}
                  </span>
                )}
              </Link>
            ))}
          </div>
        </section>
      )}

      {/* Повний каталог з фільтрами */}
      <div id="catalog">
        <ProductList />
      </div>
    </div>
  );
}

export default HomePage;
