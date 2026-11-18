import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { usersAPI } from '../services/api';

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
        // Відображення першої помилки з масиву помилок від backend (FluentValidation)
        setError(responseData.errors[0]);
      } else if (responseData && responseData.message) {
        // Відображення повідомлення від backend
        setError(responseData.message);
      } else {
        setError(t('auth.unknown_error'));
      }
    } finally {
      setLoading(false);
    }
  };
  return (
    <div className="max-w-[500px] mx-auto my-8 px-5">
      <form onSubmit={handleSubmit} className="bg-white p-8 rounded-lg shadow-[0_2px_8px_rgba(0,0,0,0.1)]">
        <h2 className="m-0 mb-6 text-[#333]">{t('auth.register_title')}</h2>
        {error && <div className="p-3 mb-4 bg-[#f8d7da] text-[#dc3545] rounded border border-[#f5c6cb]">{error}</div>}
        <div className="mb-6">
          <label htmlFor="firstName" className="block mb-2 font-medium text-[#333]">{t('auth.first_name')} *</label>
          <input
            type="text"
            id="firstName"
            name="firstName"
            value={formData.firstName}
            onChange={handleChange}
            required
            className="w-full p-3 border border-[#ddd] rounded text-base box-border focus:outline-none focus:border-[#007bff]"
          />
        </div>
        <div className="mb-6">
          <label htmlFor="lastName" className="block mb-2 font-medium text-[#333]">{t('auth.last_name')} *</label>
          <input
            type="text"
            id="lastName"
            name="lastName"
            value={formData.lastName}
            onChange={handleChange}
            required
            className="w-full p-3 border border-[#ddd] rounded text-base box-border focus:outline-none focus:border-[#007bff]"
          />
        </div>
        <div className="mb-6">
          <label htmlFor="email" className="block mb-2 font-medium text-[#333]">{t('auth.email')} *</label>
          <input
            type="email"
            id="email"
            name="email"
            value={formData.email}
            onChange={handleChange}
            required
            placeholder="example@ksu.edu.ua"
            className="w-full p-3 border border-[#ddd] rounded text-base box-border focus:outline-none focus:border-[#007bff]"
          />
        </div>
        <div className="mb-6">
          <label htmlFor="password" className="block mb-2 font-medium text-[#333]">{t('auth.password')} *</label>
          <input
            type="password"
            id="password"
            name="password"
            value={formData.password}
            onChange={handleChange}
            required
            minLength="8"
            className="w-full p-3 border border-[#ddd] rounded text-base box-border focus:outline-none focus:border-[#007bff]"
          />
          <small className="block mt-1 text-[#666] text-[0.85rem]">{t('auth.password_hint')}</small>
        </div>
        <div className="mb-6">
          <label htmlFor="phone" className="block mb-2 font-medium text-[#333]">{t('auth.phone')}</label>
          <input
            type="tel"
            id="phone"
            name="phone"
            value={formData.phone}
            onChange={handleChange}
            placeholder="+380501234567"
            className="w-full p-3 border border-[#ddd] rounded text-base box-border focus:outline-none focus:border-[#007bff]"
          />
        </div>
        <button type="submit" disabled={loading} className="w-full p-3 bg-[#007bff] text-white border-none rounded text-base cursor-pointer transition-colors [&:enabled:hover]:bg-[#0056b3] disabled:bg-[#6c757d] disabled:cursor-not-allowed">
          {loading ? t('auth.registering') : t('auth.register_btn')}
        </button>
      </form>
    </div>
  );
}

export default RegisterForm;