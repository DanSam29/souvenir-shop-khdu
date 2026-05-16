import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AuthProvider } from './contexts/AuthContext';
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
import AccessDenied from './pages/AccessDenied';
import PrivacyPolicy from './pages/PrivacyPolicy';
import Returns from './pages/Returns';
import Contacts from './pages/Contacts';
import logo from './assets/khdu-logo.png';
import './App.css';

const ADMIN_ROLES = ['Manager', 'Administrator', 'SuperAdmin'];

function App() {
  const { t } = useTranslation();

  return (
    <AuthProvider>
      <Router>
        <div className="App">
          <Header />
          <main>
            <Routes>
              <Route path="/" element={<HomePage />} />
              <Route path="/product/:id" element={<ProductPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/login" element={<LoginPage />} />
              <Route path="/profile" element={<ProfilePage />} />
              <Route path="/order/:id" element={<OrderDetailsPage />} />
              <Route path="/cart" element={<CartPage />} />
              <Route path="/checkout" element={<CheckoutPage />} />
              <Route path="/access-denied" element={<AccessDenied />} />
              
              {/* Адмінські маршрути з захистом */}
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

              <Route path="/checkout/success" element={<PaymentSuccessPage />} />
              <Route path="/checkout/cancel" element={<PaymentCancelPage />} />
              
              <Route path="/privacy-policy" element={<PrivacyPolicy />} />
              <Route path="/returns" element={<Returns />} />
              <Route path="/contacts" element={<Contacts />} />
              
              {/* Перенаправлення для неіснуючих сторінок */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>
          <footer className="footer">
            <div className="footer-content">
              <div className="footer-logo">
                <img src={logo} alt="Герб ХДУ" className="footer-emblem" />
                <div>
                  <h3>{t('footer.app_name_full')}</h3>
                  <p>{t('footer.university_full')}</p>
                </div>
              </div>
              <div className="footer-info">
                <div className="footer-section">
                  <h4>{t('checkout.phone')} / {t('profile.email')}</h4>
                  <p>📧 office@ksu.ks.ua</p>
                  <p>📞 +380963102636</p>
                  <p>🌐 <a href="https://www.kspu.edu/default.aspx?lang=uk" target="_blank" rel="noopener noreferrer">www.kspu.edu</a></p>
                </div>
                <div className="footer-section">
                  <h4>{t('common.addresses')}</h4>
                  <p><strong>{t('order.legal_address')}:</strong><br />{t('order.legal_street')},<br />{t('checkout.city_prefix')} {t('order.legal_city')}, 73003</p>
                  <p><strong>{t('order.actual_address')}:</strong><br />{t('order.actual_street')},<br />{t('checkout.city_prefix')} {t('order.actual_city')}, 76018</p>
                </div>
                <div className="footer-section">
                  <h4>{t('footer.info_links') || 'Інформація'}</h4>
                  <p><Link to="/privacy-policy">{t('legal.privacy_policy')}</Link></p>
                  <p><Link to="/returns">{t('legal.returns')}</Link></p>
                  <p><Link to="/contacts">{t('legal.contacts')}</Link></p>
                </div>
              </div>
            </div>
            <div className="footer-bottom">
              <p>© 2026 {t('footer.university_full')}. {t('footer.copyright')}</p>
            </div>
          </footer>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
