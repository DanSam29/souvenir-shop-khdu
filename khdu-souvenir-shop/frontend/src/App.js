import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import Header from './components/Header';
import HomePage from './pages/HomePage';
import ProductPage from './pages/ProductPage';
import RegisterPage from './pages/RegisterPage';
import LoginPage from './pages/LoginPage';
import ProfilePage from './pages/ProfilePage';
import CartPage from './pages/CartPage';
import logo from './assets/khdu-logo.png';
import './App.css';

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
              <Route path="/cart" element={<CartPage />} />
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