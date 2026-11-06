import React, { useState, useEffect, useCallback } from 'react';
import { useParams, Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { productsAPI, buildImageUrl, cartAPI } from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import './ProductPage.css';

function ProductPage() {
  const { id } = useParams();
  const { t, i18n } = useTranslation();
  const [searchParams] = useSearchParams();
  const from = searchParams.get('from');
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [adding, setAdding] = useState(false);
  const [selectedImage, setSelectedImage] = useState(0);
  const [quantity, setQuantity] = useState(1);
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const isEn = i18n.language === 'en';
  const displayName = (isEn && product?.nameEn) ? product.nameEn : product?.name;
  const displayDescription = (isEn && product?.descriptionEn) ? product.descriptionEn : product?.description;
  const displayCategory = (isEn && product?.category?.nameEn) ? product.category.nameEn : product?.category?.name;
  const loadProduct = useCallback(async () => {
    try {
      setLoading(true);
      const response = await productsAPI.getById(id);
      setProduct(response.data);
      setSelectedImage(0);
      setQuantity(1);
      setError(null);
    } catch (err) {
      console.error('Помилка завантаження товару:', err);
      setError(t('product.not_found') || 'Товар не знайдено');
    } finally {
      setLoading(false);
    }
  }, [id, t]); // Залежність: id, оскільки API-запит залежить від нього.
  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      alert(t('common.auth_required_cart') || 'Для додавання товарів до кошика потрібно авторизуватися');
      navigate('/login');
      return;
    }
    try {
      setAdding(true);
      await cartAPI.addToCart(product.productId, quantity);
      alert(t('common.added_to_cart') || 'Товар додано до кошика!');
      window.location.reload();
    } catch (err) {
      console.error('Помилка додавання до кошика:', err);
      if (err.response?.status === 401) {
        alert(t('common.session_expired') || 'Сесія закінчилася. Будь ласка, увійдіть знову');
        navigate('/login');
      } else {
        alert(err.response?.data?.error || t('common.add_to_cart_error') || 'Помилка додавання до кошика');
      }
    } finally {
      setAdding(false);
    }
  };
  useEffect(() => {
    loadProduct();
  }, [loadProduct]); // Залежність: стабільна функція loadProduct (з useCallback)
  if (loading) {
    return <div className="loading">{t('common.loading')}</div>;
  }
  if (error || !product) {
    return (
      <div className="error">
        <p>{error}</p>
        <Link to={from === 'cart' ? '/cart' : '/'}>
          {from === 'cart' ? t('product.back_to_cart') : t('product.back_to_catalog')}
        </Link>
      </div>
    );
  }
  const images = product.images || [];
  const activeImage = images[selectedImage];
  const imageUrl = activeImage
    ? buildImageUrl(activeImage.imageURL)
    : 'https://via.placeholder.com/500x500?text=No+Image';
  const decreaseQuantity = () => setQuantity(q => Math.max(1, q - 1));
  const increaseQuantity = () => setQuantity(q => Math.min(product.stock || 1, q + 1));
  return (
    <div className="product-page">
      <Link to={from === 'cart' ? '/cart' : '/'} className="back-link">
        ← {from === 'cart' ? t('product.back_to_cart') : t('product.back_to_catalog')}
      </Link>
      <div className="product-details">
        <div className="product-images">
          <img src={imageUrl} alt={displayName} className="main-image" />
          {images.length > 1 && (
            <div className="image-thumbnails">
              {images.map((img, index) => (
                <button
                  key={img.imageId}
                  type="button"
                  className={`thumbnail ${index === selectedImage ? 'active' : ''}`}
                  onClick={() => setSelectedImage(index)}
                >
                  <img src={buildImageUrl(img.imageURL)} alt={`${displayName} ${index + 1}`} />
                </button>
              ))}
            </div>
          )}
        </div>
        <div className="product-info-detailed">
          <h1>{displayName}</h1>
          <p className="category-badge">{displayCategory}</p>
          <div className="price-section">
            {product.originalPrice && product.originalPrice !== product.price ? (
              <>
                <span className="price-original">{product.originalPrice.toFixed(2)} {t('common.currency')}</span>
                <span className="price">{product.price.toFixed(2)} {t('common.currency')}</span>
              </>
            ) : (
              <span className="price">{product.price.toFixed(2)} {t('common.currency')}</span>
            )}
            <span className={`stock-badge ${product.stock > 0 ? 'in-stock' : 'out-of-stock'}`}>
              {product.stock > 0 ? `✓ ${t('product.in_stock')} (${product.stock} ${t('product.pcs')})` : `✗ ${t('product.out_of_stock')}`}
            </span>
          </div>
          <div className="description">
            <h3>{t('product.description')}</h3>
            <p>{displayDescription}</p>
          </div>
          <div className="product-specs">
            <h3>{t('product.specs')}</h3>
            <table>
              <tbody>
                <tr>
                  <td>{t('product.weight')}:</td>
                  <td>{product.weight} {t('product.kg')}</td>
                </tr>
                <tr>
                  <td>{t('product.date_added')}:</td>
                  <td>{new Date(product.createdAt).toLocaleDateString(i18n.language === 'en' ? 'en-US' : 'uk-UA')}</td>
                </tr>
              </tbody>
            </table>
          </div>
          {product.stock > 0 && (
            <div className="quantity-selector">
              <span className="quantity-label">{t('product.quantity')}:</span>
              <div className="quantity-controls">
                <button type="button" onClick={decreaseQuantity} disabled={quantity <= 1}>−</button>
                <span className="quantity-value">{quantity}</span>
                <button type="button" onClick={increaseQuantity} disabled={quantity >= product.stock}>+</button>
              </div>
            </div>
          )}
          <button
            className="add-to-cart-btn"
            disabled={product.stock === 0 || adding}
            onClick={handleAddToCart}
          >
            {adding ? t('common.adding') : product.stock > 0 ? t('product.add_to_cart') : t('product.out_of_stock')}
          </button>
        </div>
      </div>

      {/* Схожі товари */}
      {product.relatedProducts && product.relatedProducts.length > 0 && (
        <section className="related-products">
          <h2>{t('product.related')}</h2>
          <div className="product-grid">
            {product.relatedProducts.map(rel => {
              const relName = (isEn && rel.nameEn) ? rel.nameEn : rel.name;
              return (
                <Link key={rel.productId} to={`/product/${rel.productId}`} className="related-card">
                  <div className="related-image">
                    {rel.primaryImage ? (
                      <img src={buildImageUrl(rel.primaryImage)} alt={relName} />
                    ) : (
                      <div className="no-image">{t('product.no_image')}</div>
                    )}
                  </div>
                  <div className="related-info">
                    <h3>{relName}</h3>
                    <div className="related-price">
                      {rel.originalPrice && rel.originalPrice !== rel.price ? (
                        <>
                          <span className="price-original">{rel.originalPrice.toFixed(2)} {t('common.currency')}</span>
                          <span className="price">{rel.price.toFixed(2)} {t('common.currency')}</span>
                        </>
                      ) : (
                        <span className="price">{rel.price.toFixed(2)} {t('common.currency')}</span>
                      )}
                    </div>
                  </div>
                </Link>
              );
            })}
          </div>
        </section>
      )}
    </div>
  );
}

export default ProductPage;
