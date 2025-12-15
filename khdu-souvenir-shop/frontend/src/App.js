import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Header from './components/Header';
import HomePage from './pages/HomePage';
import ProductPage from './pages/ProductPage';
import RegisterPage from './pages/RegisterPage';
import logo from './assets/khdu-logo.png';
import './App.css';

function App() {
  return (
    <Router>
      <div className="App">
        <Header />
        <main>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/product/:id" element={<ProductPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/cart" element={
              <div style={{ padding: '2rem', textAlign: 'center' }}>
                <h2>🛒 Кошик (в розробці)</h2>
                <p>Функціонал кошика буде реалізовано в наступній практичній роботі</p>
              </div>
            } />
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
                <p>📧 info@kspu.edu</p>
                <p>📞 +38 (0552) 32-64-60</p>
              </div>
              <div className="footer-section">
                <h4>Навігація</h4>
                <p><a href="/">Каталог</a></p>
                <p><a href="/register">Реєстрація</a></p>
              </div>
            </div>
          </div>
          <div className="footer-bottom">
            <p>© 2025 Херсонський державний університет. Всі права захищено.</p>
          </div>
        </footer>
      </div>
    </Router>
  );
}

export default App;