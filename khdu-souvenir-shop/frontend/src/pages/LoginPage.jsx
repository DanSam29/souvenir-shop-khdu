import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import './LoginPage.css';

function LoginPage() {
  const [formData, setFormData] = useState({
    email: '',
    password: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    // Базова валідація
    if (!formData.email.includes('@')) {
      setError('Невірний формат email');
      setLoading(false);
      return;
    }

    if (formData.password.length < 8) {
      setError('Пароль має містити мінімум 8 символів');
      setLoading(false);
      return;
    }

    try {
      await login(formData.email, formData.password);
      console.log('Авторизація успішна!');
      
      // Перенаправлення на головну сторінку
      navigate('/');
    } catch (err) {
      console.error('Помилка авторизації:', err);
      if (err.response?.status === 401) {
        setError('Невірний email або пароль');
      } else {
        setError('Помилка авторизації. Спробуйте пізніше.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <form onSubmit={handleSubmit} className="login-form">
          <h2>Вхід в систему</h2>

          {error && <div className="error-message">{error}</div>}

          <div className="form-group">
            <label htmlFor="email">Email *</label>
            <input
              type="email"
              id="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              required
              placeholder="example@ksu.edu.ua"
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Пароль *</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              required
              minLength="8"
              placeholder="Мінімум 8 символів"
            />
          </div>

          <button type="submit" disabled={loading} className="submit-btn">
            {loading ? 'Вхід...' : 'Увійти'}
          </button>

          <div className="form-footer">
            <p>
              Ще не маєте облікового запису?{' '}
              <Link to="/register" className="link">
                Зареєструватися
              </Link>
            </p>
          </div>
        </form>
      </div>
    </div>
  );
}

export default LoginPage;
