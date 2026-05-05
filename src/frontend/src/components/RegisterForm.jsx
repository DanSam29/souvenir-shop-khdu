import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { usersAPI } from '../services/api';
import './RegisterForm.css';

function RegisterForm() {
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
      console.log('Реєстрація успішна:', response);
      
      // Перенаправлення на сторінку входу з повідомленням про успіх
      navigate('/login', { state: { registrationSuccess: true } });
    } catch (err) {
      console.error('Помилка реєстрації:', err);
      
      const responseData = err.response?.data;
      
      if (err.response?.status === 409) {
        setError('Email вже зареєстрований');
      } else if (responseData && responseData.errors && responseData.errors.length > 0) {
        // Відображаємо першу помилку з масиву помилок від backend (FluentValidation)
        setError(responseData.errors[0]);
      } else if (responseData && responseData.message) {
        // Відображаємо повідомлення від backend
        setError(responseData.message);
      } else {
        setError('Помилка реєстрації. Спробуйте пізніше.');
      }
    } finally {
      setLoading(false);
    }
  };

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
