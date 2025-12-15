import React, { useState } from 'react';
import { usersAPI } from '../services/api';
import './RegisterForm.css';

function RegisterForm() {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    phone: ''
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(false);

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
      const response = await usersAPI.register(formData);
      console.log('Реєстрація успішна:', response.data);
      setSuccess(true);
      // Очистка форми
      setFormData({
        firstName: '',
        lastName: '',
        email: '',
        password: '',
        phone: ''
      });
    } catch (err) {
      console.error('Помилка реєстрації:', err);
      if (err.response?.status === 409) {
        setError('Email вже зареєстрований');
      } else {
        setError('Помилка реєстрації. Спробуйте пізніше.');
      }
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="success-message">
        <h2>✓ Реєстрація успішна!</h2>
        <p>Ви успішно зареєструвалися в системі.</p>
        <button onClick={() => setSuccess(false)}>Зареєструвати ще</button>
      </div>
    );
  }

  return (
    <div className="register-form-container">
      <form onSubmit={handleSubmit} className="register-form">
        <h2>Реєстрація</h2>

        {error && <div className="error-message">{error}</div>}

        <div className="form-group">
          <label htmlFor="firstName">Ім'я *</label>
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
          <label htmlFor="lastName">Прізвище *</label>
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
          <label htmlFor="email">Email *</label>
          <input
            type="email"
            id="email"
            name="email"
            value={formData.email}
            onChange={handleChange}
            required
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
          />
          <small>Мінімум 8 символів</small>
        </div>

        <div className="form-group">
          <label htmlFor="phone">Телефон</label>
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
          {loading ? 'Реєстрація...' : 'Зареєструватися'}
        </button>
      </form>
    </div>
  );
}

export default RegisterForm;