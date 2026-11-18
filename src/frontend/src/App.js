import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AuthProvider } from './contexts/AuthContext';
import { ToastProvider } from './components/Toast';
import Header from './components/Header';
import ProtectedRoute from './components/ProtectedRoute';
import HomePage from './pages/HomePage';
import ProductPage from './pages/ProductPage';
import RegisterPage from './pages/RegisterPage';
import LoginPage from './pages/LoginPage';
import ProfilePage from './pages/ProfilePage';
import OrderDetailsPage from './pages/OrderDetailsPage';
import AdminDashboard from './pages/AdminDashboard';
import AdminOrdersPage from './pages/AdminOrdersPage';
import AdminAnalyticsPage from './pages/AdminAnalyticsPage';
import AdminUsersPage from './pages/AdminUsersPage';
import AdminWarehousePage from './pages/AdminWarehousePage';
import CartPage from './pages/CartPage';
import CheckoutPage from './pages/CheckoutPage';
import PaymentSuccessPage from './pages/PaymentSuccessPage';
import PaymentCancelPage from './pages/PaymentCancelPage';
import CategoriesAdmin from './pages/admin/CategoriesAdmin';
import ProductsAdmin from './pages/admin/ProductsAdmin';
import CompaniesAdmin from './pages/admin/CompaniesAdmin';
import PromotionsAdmin from './pages/admin/PromotionsAdmin';
import PromotionsPage from './pages/PromotionsPage';
import AccessDenied from './pages/AccessDenied';
import PrivacyPolicy from './pages/PrivacyPolicy';
import Returns from './pages/Returns';
import Contacts from './pages/Contacts';
import logo from './assets/khdu-logo.png';

const ADMIN_ROLES = ['Manager', 'Administrator', 'SuperAdmin'];
function App() {
  const { t } = useTranslation();
  return (
    <AuthProvider>
      <ToastProvider>
        <Router>
          <div className="App min-h-screen flex flex-col">
          <Header />
          <main className="flex-1">
            <Routes>
              <Route path="/" element={<HomePage />} />
              <Route path="/product/:id" element={<ProductPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/login" element={<LoginPage />} />
              <Route path="/profile" element={<ProfilePage />} />
              <Route path="/order/:id" element={<OrderDetailsPage />} />
              <Route path="/cart" element={<CartPage />} />
              <Route path="/checkout" element={<CheckoutPage />} />
              <Route path="/promotions" element={<PromotionsPage />} />
              <Route path="/access-denied" element={<AccessDenied />} />
              {/* Маршрути адміністратора з захистом */}
              <Route path="/admin" element={
                <ProtectedRoute allowedRoles={ADMIN_ROLES}>
                  <AdminDashboard />
                </ProtectedRoute>
              } />
              <Route path="/admin/orders" element={
                <ProtectedRoute allowedRoles={ADMIN_ROLES}>
                  <AdminOrdersPage />
                </ProtectedRoute>
              } />
              <Route path="/admin/analytics" element={
                <ProtectedRoute allowedRoles={ADMIN_ROLES}>
                  <AdminAnalyticsPage />
                </ProtectedRoute>
              } />
              <Route path="/admin/users" element={
                <ProtectedRoute allowedRoles={['Administrator', 'SuperAdmin']}>
                  <AdminUsersPage />
                </ProtectedRoute>
              } />
              <Route path="/admin/warehouse" element={
                <ProtectedRoute allowedRoles={ADMIN_ROLES}>
                  <AdminWarehousePage />
                </ProtectedRoute>
              } />
              <Route path="/admin/categories" element={
                <ProtectedRoute allowedRoles={ADMIN_ROLES}>
                  <CategoriesAdmin />
                </ProtectedRoute>
              } />
              <Route path="/admin/products" element={
                <ProtectedRoute allowedRoles={ADMIN_ROLES}>
                  <ProductsAdmin />
                </ProtectedRoute>
              } />
              <Route path="/admin/companies" element={
                <ProtectedRoute allowedRoles={ADMIN_ROLES}>
                  <CompaniesAdmin />
                </ProtectedRoute>
              } />
              <Route path="/admin/promotions" element={
                <ProtectedRoute allowedRoles={ADMIN_ROLES}>
                  <PromotionsAdmin />
                </ProtectedRoute>
              } />
              <Route path="/checkout/success" element={<PaymentSuccessPage />} />
              <Route path="/checkout/cancel" element={<PaymentCancelPage />} />
              <Route path="/privacy-policy" element={<PrivacyPolicy />} />
              <Route path="/returns" element={<Returns />} />
              <Route path="/contacts" element={<Contacts />} />
              {/* Перенаправлення для неіснуючих сторінок */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>
          <footer className="text-white mt-12 pt-14 px-8 pb-6 bg-[linear-gradient(135deg,#1a1a1a_0%,#2d2d2d_100%)]">
            <div className="max-w-[1600px] mx-auto grid grid-cols-1 md:grid-cols-2 gap-12 pb-8 border-b border-white/10">
              <div className="flex items-center gap-4 justify-center md:justify-start">
                <img src={logo} alt="Герб ХДУ" className="w-[60px] h-[60px] bg-white rounded-full flex items-center justify-center object-contain shadow-[0_2px_8px_rgba(0,0,0,0.3)] p-2" />
                <div>
                  <h3 className="m-0 text-2xl">{t('footer.app_name_full')}</h3>
                  <p className="mt-1 text-[0.95rem] opacity-70">{t('footer.university_full')}</p>
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-10">
                <div>
                  <h4 className="mb-4 text-gold text-[1.15rem]">{t('checkout.phone')} / {t('profile.email')}</h4>
                  <p className="my-2 text-base leading-normal opacity-80">📧 office@ksu.ks.ua</p>
                  <p className="my-2 text-base leading-normal opacity-80">📞 +380963102636</p>
                  <p className="my-2 text-base leading-normal opacity-80">🌐 <a href="https://www.kspu.edu/default.aspx?lang=uk" target="_blank" rel="noopener noreferrer" className="text-white no-underline transition-colors hover:text-gold">www.kspu.edu</a></p>
                </div>
                <div>
                  <h4 className="mb-4 text-gold text-[1.15rem]">{t('common.addresses')}</h4>
                  <p className="my-2 text-base leading-normal opacity-80"><strong>{t('order.legal_address')}:</strong><br />{t('order.legal_street')},<br />{t('checkout.city_prefix')} {t('order.legal_city')}, 73003</p>
                  <p className="my-2 text-base leading-normal opacity-80"><strong>{t('order.actual_address')}:</strong><br />{t('order.actual_street')},<br />{t('checkout.city_prefix')} {t('order.actual_city')}, 76018</p>
                </div>
                <div>
                  <h4 className="mb-4 text-gold text-[1.15rem]">{t('footer.info_links') || 'Інформація'}</h4>
                  <p className="my-2 text-base leading-normal opacity-80"><Link to="/privacy-policy" className="text-white no-underline transition-colors hover:text-gold">{t('legal.privacy_policy')}</Link></p>
                  <p className="my-2 text-base leading-normal opacity-80"><Link to="/returns" className="text-white no-underline transition-colors hover:text-gold">{t('legal.returns')}</Link></p>
                  <p className="my-2 text-base leading-normal opacity-80"><Link to="/contacts" className="text-white no-underline transition-colors hover:text-gold">{t('legal.contacts')}</Link></p>
                </div>
              </div>
            </div>
            <div className="max-w-[1600px] mx-auto pt-8 text-center opacity-60 text-[0.95rem]">
              <p>© 2026 {t('footer.university_full')}. {t('footer.copyright')}</p>
            </div>
          </footer>
        </div>
        </Router>
      </ToastProvider>
    </AuthProvider>
  );
}

export default App;