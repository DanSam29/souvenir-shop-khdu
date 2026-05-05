import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
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
import logo from './assets/khdu-logo.png';
import './App.css';

const ADMIN_ROLES = ['Manager', 'Administrator', 'SuperAdmin'];

function App() {
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
              
              {/* Перенаправлення для неіснуючих сторінок */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </main>
          <footer className="footer">
            <div className="footer-content">
              <div className="footer-logo">
                <img src={logo} alt="Герб ХДУ" className="footer-emblem" />
                <div>
                  <h3>ХДУ Сувеніри</h3>
                  <p>Херсонський державний університет</p>
                </div>
              </div>
              <div className="footer-info">
                <div className="footer-section">
                  <h4>Контакти</h4>
                  <p>📧 office@ksu.ks.ua</p>
                  <p>📞 +380963102636</p>
                  <p>🌐 <a href="https://www.kspu.edu/default.aspx?lang=uk" target="_blank" rel="noopener noreferrer">www.kspu.edu</a></p>
                </div>
                <div className="footer-section">
                  <h4>Адреси</h4>
                  <p><strong>Юридична:</strong><br />вул. Університетська, 27,<br />м. Херсон, 73003</p>
                  <p><strong>Фактична:</strong><br />вул. Шевченка, 14,<br />м. Івано-Франківськ, 76018</p>
                </div>
              </div>
            </div>
            <div className="footer-bottom">
              <p>© 2025 Херсонський державний університет. Всі права захищено.</p>
            </div>
          </footer>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
