import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { usersAPI } from '../services/api';
import './RegisterForm.css';

function RegisterForm() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    phone: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

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
      const payload = {
        ...formData,
        language: i18n.language
      };
      const response = await usersAPI.register(payload);
      console.log('Реєстрація успішна:', response);
      
      // Перенаправлення на сторінку входу з повідомленням про успіх
      navigate('/login', { state: { registrationSuccess: true } });
    } catch (err) {
      console.error('Помилка реєстрації:', err);
      
      const responseData = err.response?.data;
      
      if (err.response?.status === 409) {
        setError(t('auth.email_exists'));
      } else if (responseData && responseData.errors && responseData.errors.length > 0) {
        // Відображаємо першу помилку з масиву помилок від backend (FluentValidation)
        setError(responseData.errors[0]);
      } else if (responseData && responseData.message) {
        // Відображаємо повідомлення від backend
        setError(responseData.message);
      } else {
        setError(t('auth.unknown_error'));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="register-form-container">
      <form onSubmit={handleSubmit} className="register-form">
        <h2>{t('auth.register_title')}</h2>

        {error && <div className="error-message">{error}</div>}

        <div className="form-group">
          <label htmlFor="firstName">{t('auth.first_name')} *</label>
          <input
            type="text"
            id="firstName"
            name="firstName"
            value={formData.firstName}
            onChange={handleChange}
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="lastName">{t('auth.last_name')} *</label>
          <input
            type="text"
            id="lastName"
            name="lastName"
            value={formData.lastName}
            onChange={handleChange}
            required
          />
        </div>

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
          />
          <small>{t('auth.password_hint')}</small>
        </div>

        <div className="form-group">
          <label htmlFor="phone">{t('auth.phone')}</label>
          <input
            type="tel"
            id="phone"
            name="phone"
            value={formData.phone}
            onChange={handleChange}
            placeholder="+380501234567"
          />
        </div>

        <button type="submit" disabled={loading} className="submit-btn">
          {loading ? t('auth.registering') : t('auth.register_btn')}
        </button>
      </form>
    </div>
  );
}

export default RegisterForm;
