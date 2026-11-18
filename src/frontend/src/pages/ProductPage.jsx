import React, { useState, useEffect, useCallback } from 'react';
import { useParams, Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { productsAPI, buildImageUrl, cartAPI } from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { Skeleton, SkeletonText } from '../components/ui';

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
    return (
      <div role="status" aria-live="polite" className="max-w-[1200px] mx-auto py-8 px-5">
        <span className="sr-only">{t('common.loading')}</span>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 bg-white p-8 rounded-lg">
          <Skeleton className="h-80 w-full" />
          <div className="flex flex-col gap-4">
            <SkeletonText lines={5} />
            <Skeleton className="h-10 w-40" />
          </div>
        </div>
      </div>
    );
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
    <div className="max-w-[1200px] mx-auto py-8 px-5">
      <Link to={from === 'cart' ? '/cart' : '/'} className="back-link">
        ← {from === 'cart' ? t('product.back_to_cart') : t('product.back_to_catalog')}
      </Link>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-12 bg-white p-8 rounded-lg shadow-[0_2px_8px_rgba(0,0,0,0.1)]">
        <div className="product-images">
          <img src={imageUrl} alt={displayName} width={600} height={600} loading="eager" decoding="async" className="w-full rounded-lg object-cover" />
          {images.length > 1 && (
            <div className="flex gap-3 mt-4 flex-wrap">
              {images.map((img, index) => (
                <button
                  key={img.imageId}
                  type="button"
                  className={`w-[72px] h-[72px] p-0 border-2 rounded-lg bg-none cursor-pointer overflow-hidden transition-colors hover:border-[#007bff] ${index === selectedImage ? 'border-[#007bff] shadow-[0_0_0_2px_rgba(0,123,255,0.2)]' : 'border-[#e9ecef]'}`}
                  onClick={() => setSelectedImage(index)}
                >
                  <img src={buildImageUrl(img.imageURL)} alt={`${displayName} ${index + 1}`} width={72} height={72} loading="lazy" decoding="async" className="w-full h-full object-cover rounded-none" />
                </button>
              ))}
            </div>
          )}
        </div>
        <div className="product-info-detailed">
          <h1 className="mt-0 mb-4 text-[#333]">{displayName}</h1>
          <p className="inline-block px-3 py-1 bg-[#e9ecef] text-[#495057] rounded text-[0.9rem] mb-6">{displayCategory}</p>
          <div className="flex justify-between items-center mb-8 pb-6 border-b border-[#e9ecef] gap-4 flex-wrap">
            {product.originalPrice && product.originalPrice !== product.price ? (
              <>
                <span className="text-[1.25rem] text-[#595959] line-through">{product.originalPrice.toFixed(2)} {t('common.currency')}</span>
                <span className="text-[2rem] font-bold text-[#007bff]">{product.price.toFixed(2)} {t('common.currency')}</span>
              </>
            ) : (
              <span className="text-[2rem] font-bold text-[#007bff]">{product.price.toFixed(2)} {t('common.currency')}</span>
            )}
            <span className={`px-4 py-2 rounded text-[0.9rem] ${product.stock > 0 ? 'bg-[#d4edda] text-[#155724]' : 'bg-[#f8d7da] text-[#721c24]'}`}>
              {product.stock > 0 ? `✓ ${t('product.in_stock')} (${product.stock} ${t('product.pcs')})` : `✗ ${t('product.out_of_stock')}`}
            </span>
          </div>
          <div className="mb-8">
            <h3 className="mt-0 mb-4 text-[#333]">{t('product.description')}</h3>
            <p className="text-[#666] leading-[1.6]">{displayDescription}</p>
          </div>
          <div className="product-specs">
            <h3 className="mt-0 mb-4 text-[#333]">{t('product.specs')}</h3>
            <table className="w-full mb-8">
              <tbody>
                <tr>
                  <td className="py-2 border-b border-[#e9ecef] font-medium text-[#495057] w-[40%]">{t('product.weight')}:</td>
                  <td className="py-2 border-b border-[#e9ecef]">{product.weight} {t('product.kg')}</td>
                </tr>
                <tr>
                  <td className="py-2 border-b border-[#e9ecef] font-medium text-[#495057] w-[40%]">{t('product.date_added')}:</td>
                  <td className="py-2 border-b border-[#e9ecef]">{new Date(product.createdAt).toLocaleDateString(i18n.language === 'en' ? 'en-US' : 'uk-UA')}</td>
                </tr>
              </tbody>
            </table>
          </div>
          {product.stock > 0 && (
            <div className="flex items-center gap-4 mb-5">
              <span className="font-semibold text-[#495057]">{t('product.quantity')}:</span>
              <div className="flex items-center border border-[#dee2e6] rounded-md overflow-hidden">
                <button type="button" onClick={decreaseQuantity} disabled={quantity <= 1} className="w-10 h-10 border-none bg-[#f8f9fa] text-[#1a1a1a] text-[1.25rem] cursor-pointer transition-colors enabled:hover:bg-[#e9ecef] disabled:text-[#adb5bd] disabled:cursor-not-allowed">−</button>
                <span className="min-w-[50px] text-center font-semibold text-[#1a1a1a]">{quantity}</span>
                <button type="button" onClick={increaseQuantity} disabled={quantity >= product.stock} className="w-10 h-10 border-none bg-[#f8f9fa] text-[#1a1a1a] text-[1.25rem] cursor-pointer transition-colors enabled:hover:bg-[#e9ecef] disabled:text-[#adb5bd] disabled:cursor-not-allowed">+</button>
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
        <section className="mt-12">
          <h2 className="mt-0 mb-6 text-[#1a1a1a] text-[1.6rem]">{t('product.related')}</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {product.relatedProducts.map(rel => {
              const relName = (isEn && rel.nameEn) ? rel.nameEn : rel.name;
              return (
                <Link key={rel.productId} to={`/product/${rel.productId}`} className="flex flex-col bg-white border border-[#e9ecef] rounded-xl overflow-hidden no-underline transition-all shadow-[0_2px_8px_rgba(0,0,0,0.05)] hover:-translate-y-1 hover:shadow-[0_6px_16px_rgba(0,86,179,0.15)]">
                  <div className="aspect-square overflow-hidden bg-[#f8f9fa]">
                    {rel.primaryImage ? (
                      <img src={buildImageUrl(rel.primaryImage)} alt={relName} width={300} height={300} loading="lazy" decoding="async" className="w-full h-full object-cover" />
                    ) : (
                      <div className="no-image">{t('product.no_image')}</div>
                    )}
                  </div>
                  <div className="px-4 pt-[0.9rem] pb-[1.1rem]">
                    <h3 className="mt-0 mb-2 text-base text-[#1a1a1a]">{relName}</h3>
                    <div className="flex items-baseline gap-2">
                      {rel.originalPrice && rel.originalPrice !== rel.price ? (
                        <>
                          <span className="text-[1.25rem] text-[#595959] line-through">{rel.originalPrice.toFixed(2)} {t('common.currency')}</span>
                          <span className="text-[2rem] font-bold text-[#007bff]">{rel.price.toFixed(2)} {t('common.currency')}</span>
                        </>
                      ) : (
                        <span className="text-[2rem] font-bold text-[#007bff]">{rel.price.toFixed(2)} {t('common.currency')}</span>
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
