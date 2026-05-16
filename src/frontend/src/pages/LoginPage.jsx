import React, { useState } from 'react';
import { useNavigate, Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';
import './LoginPage.css';

function LoginPage() {
  const { t } = useTranslation();
  const location = useLocation();
  const registrationSuccess = location.state?.registrationSuccess;
  
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
      setError(t('auth.email_invalid'));
      setLoading(false);
      return;
    }

    if (formData.password.length < 8) {
      setError(t('auth.password_too_short'));
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
      
      const apiResponse = err.response?.data;
      const errors = apiResponse?.errors || [];
      const message = apiResponse?.message;
      
      // Перевіряємо, чи є код AccountBlocked у списку помилок або в повідомленні
      if (errors.includes('AccountBlocked') || message === 'AccountBlocked') {
        setError(t('auth.account_blocked'));
      } else if (err.response?.status === 401) {
        setError(t('auth.invalid_credentials'));
      } else if (message) {
        setError(message);
      } else if (errors.length > 0) {
        setError(errors[0]);
      } else {
        setError(t('auth.unknown_error'));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <form onSubmit={handleSubmit} className="login-form">
          <h2>{t('auth.login_title')}</h2>

          {registrationSuccess && (
            <div className="success-message" style={{ 
              backgroundColor: '#e6ffed', 
              color: '#28a745', 
              padding: '10px', 
              borderRadius: '4px', 
              marginBottom: '15px',
              border: '1px solid #b7eb8f',
              textAlign: 'center'
            }}>
              {t('auth.registration_success')}
            </div>
          )}

          {error && <div className="error-message">{error}</div>}

          <div className="form-group">
            <label htmlFor="email">{t('auth.email')} *</label>
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
            <label htmlFor="password">{t('auth.password')} *</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              required
              minLength="8"
              placeholder={t('auth.password')}
            />
          </div>

          <button type="submit" disabled={loading} className="submit-btn">
            {loading ? t('auth.logging_in') : t('auth.login_btn')}
          </button>

          <div className="form-footer">
            <p>
              {t('auth.register_prompt')}{' '}
              <Link to="/register" className="link">
                {t('auth.register_link')}
              </Link>
            </p>
          </div>
        </form>
      </div>
    </div>
  );
}

export default LoginPage;
