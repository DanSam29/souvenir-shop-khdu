import React from 'react';
import ProductList from '../components/ProductList';

function HomePage() {
  return (
    <div className="home-page">
      <div id="catalog">
        <ProductList />
      </div>
    </div>
  );
}

export default HomePage;
