import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import './LoginPage.css';

function LoginPage() {
  const [formData, setFormData] = useState({
    email: '',
    password: ''
  });
  const [error, setError] = useState(null);

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    // Тут буде логіка авторизації в наступних практичних
    console.log('Login attempt:', formData);
    setError('Функціонал авторизації буде реалізовано в наступній практичній роботі');
  };

  return (
    <div className="login-page">
      <div className="login-form-container">
        <form onSubmit={handleSubmit} className="login-form">
          <h2>Вхід</h2>

          {error && <div className="info-message">{error}</div>}

          <div className="form-group">
            <label htmlFor="email">Email</label>
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
            <label htmlFor="password">Пароль</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              required
            />
          </div>

          <button type="submit" className="submit-btn">
            Увійти
          </button>

          <div className="form-footer">
            <p>Немає облікового запису? <Link to="/register">Зареєструватися</Link></p>
          </div>
        </form>
      </div>
    </div>
  );
}

export default LoginPage;