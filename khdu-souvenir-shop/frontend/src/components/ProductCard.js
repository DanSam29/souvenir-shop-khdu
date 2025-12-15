import React from 'react';
import { Link } from 'react-router-dom';
import './ProductCard.css';

function ProductCard({ product }) {
  // Placeholder зображення якщо немає
  const imageUrl = product.images && product.images.length > 0 
    ? product.images[0].imageURL 
    : 'https://via.placeholder.com/300x300?text=No+Image';

  return (
    <div className="product-card">
      <Link to={`/product/${product.productId}`} className="product-link">
        <div className="product-image">
          <img src={imageUrl} alt={product.name} />
        </div>
        <div className="product-info">
          <h3 className="product-name">{product.name}</h3>
          <p className="product-category">{product.category?.name}</p>
          <div className="product-footer">
            <span className="product-price">{product.price} грн</span>
            <span className={`product-stock ${product.stock > 0 ? 'in-stock' : 'out-of-stock'}`}>
              {product.stock > 0 ? '✓ В наявності' : '✗ Немає'}
            </span>
          </div>
        </div>
      </Link>
    </div>
  );
}

export default ProductCard;