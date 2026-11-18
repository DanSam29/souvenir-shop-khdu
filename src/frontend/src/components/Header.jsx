import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import { cartAPI, usersAPI } from '../services/api';
import logo from '../assets/khdu-logo.png';

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
    // Для авторизованих зберігаємо мову в профілі, щоб листи надходили цією мовою
    if (isAuthenticated) {
      usersAPI.updateLanguage(newLang).catch((error) => {
        console.error('Помилка збереження мови:', error);
      });
    }
  };
  return (
    <header className="text-white py-4 shadow-[0_4px_12px_rgba(0,86,179,0.3)] sticky top-0 z-[1000] bg-[linear-gradient(135deg,#0056b3_0%,#007bff_100%)]">
      <div className="container flex justify-between items-center gap-12 max-[480px]:flex-col max-[480px]:gap-4">
        <Link to="/" className="flex items-center gap-4 no-underline text-white transition-transform hover:scale-[1.02]">
          <img src={logo} alt="Герб ХДУ" className="w-[50px] h-[50px] bg-white rounded-full flex items-center justify-center shadow-[0_2px_8px_rgba(0,0,0,0.2)] p-[5px] object-contain" />
          <div className="flex flex-col">
            <h1 className="m-0 text-2xl font-bold">{t('common.app_name')}</h1>
            <span className="text-xs opacity-90 font-light hidden md:inline">{t('common.university_name')}</span>
          </div>
        </Link>
        <nav className="flex gap-8 items-center max-md:gap-4">
          <Link to="/" className="text-white no-underline font-medium flex items-center gap-2 px-4 py-2 rounded-lg transition-all relative whitespace-nowrap hover:-translate-y-0.5 hover:bg-white/15">
            {t('nav.home')}
          </Link>
          <Link to="/promotions" className="text-white no-underline font-medium flex items-center gap-2 px-4 py-2 rounded-lg transition-all relative whitespace-nowrap hover:-translate-y-0.5 hover:bg-white/15">
            {t('nav.promotions')}
          </Link>
          {isAdmin && (
            <Link to="/admin" className="text-white no-underline font-medium flex items-center gap-2 px-4 py-2 rounded-lg transition-all relative whitespace-nowrap hover:-translate-y-0.5 bg-white/20 border border-dashed border-white/50 hover:bg-white/30 hover:border-solid">
              {t('nav.admin')}
            </Link>
          )}
          <button onClick={toggleLanguage} className="bg-white/20 border border-white/30 text-white px-[0.6rem] py-[0.3rem] rounded cursor-pointer font-semibold text-xs transition-all hover:bg-white/30">
            {i18n.language === 'ua' ? 'EN' : 'UA'}
          </button>
          {/* Відображення для неавторизованих */}
          {!isAuthenticated && (
            <>
              <Link to="/login" className="text-white no-underline font-medium flex items-center gap-2 px-4 py-2 rounded-lg transition-all relative whitespace-nowrap hover:-translate-y-0.5 hover:bg-white/15">
                {t('nav.login')}
              </Link>
              <Link to="/register" className="text-white no-underline font-medium flex items-center gap-2 px-4 py-2 rounded-lg transition-all relative whitespace-nowrap hover:-translate-y-0.5 border-2 border-white hover:bg-white/15">
                {t('nav.register')}
              </Link>
            </>
          )}
          {/* Відображення для авторизованих */}
          {isAuthenticated && (
            <Link to="/profile" className="text-white no-underline font-medium flex items-center gap-2 px-4 py-2 rounded-lg transition-all relative whitespace-nowrap hover:-translate-y-0.5 hover:bg-white/15">
              <span className="text-[1.2rem] max-md:hidden">👤</span>
              {t('nav.profile')}
            </Link>
          )}
          <Link to="/cart" className="no-underline flex items-center gap-2 px-4 py-2 rounded-lg transition-all relative whitespace-nowrap hover:-translate-y-0.5 bg-gold text-[#333] font-semibold hover:bg-gold-dark hover:text-black">
            <span className="text-[1.2rem] max-md:hidden">🛒</span>
            {t('nav.cart')}
            {cartCount > 0 && <span className="bg-[#dc3545] text-white rounded-full w-5 h-5 flex items-center justify-center text-xs font-bold absolute -top-[5px] -right-[5px]">{cartCount}</span>}
          </Link>
        </nav>
      </div>
    </header>
  );
}

export default Header;
