import React, { useState } from 'react';
import { useNavigate, Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../contexts/AuthContext';

function LoginPage() {
  const { t } = useTranslation();
  const location = useLocation();
  const registrationSuccess = location.state?.registrationSuccess;
  const [formData, setFormData] = useState({
    email: '',
    password: ''
  });
  const [remember, setRemember] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [fieldErrors, setFieldErrors] = useState({});
  const { login } = useAuth();
  const navigate = useNavigate();
  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: value
    });
    setFieldErrors((prev) => {
      if (!prev[name]) return prev;
      const next = { ...prev };
      delete next[name];
      return next;
    });
  };
  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    // Базова валідація
    const errors = {};
    if (!formData.email.includes('@')) errors.email = t('auth.email_invalid');
    if (formData.password.length < 8) errors.password = t('auth.password_too_short');
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      setLoading(false);
      return;
    }
    try {
      await login(formData.email, formData.password, remember);
      console.log('Авторизація успішна!');
      // Перенаправлення на головну сторінку
      navigate('/');
    } catch (err) {
      console.error('Помилка авторизації:', err);
      const apiResponse = err.response?.data;
      const errors = apiResponse?.errors || [];
      const message = apiResponse?.message;
      // Перевірка, чи є код AccountBlocked у списку помилок або в повідомленні
      if (errors.includes('AccountBlocked') || message === 'AccountBlocked') {
        setError(t('auth.account_blocked'));
      } else if (errors.includes('AccountLocked') || message === 'AccountLocked' || err.response?.status === 423) {
        // Тимчасове блокування після серії невдалих спроб входу (вимога 14.1)
        setError(t('auth.account_locked'));
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
    <div className="min-h-[80vh] flex items-center justify-center p-8 bg-[linear-gradient(135deg,#667eea_0%,#764ba2_100%)]">
      <div className="w-full max-w-[450px]">
        <form onSubmit={handleSubmit} className="bg-white p-10 rounded-xl shadow-[0_10px_40px_rgba(0,0,0,0.15)]">
          <h2 className="m-0 mb-6 text-[2rem] text-[#333] text-center">{t('auth.login_title')}</h2>
          {registrationSuccess && (
            <div className="max-w-[500px] mx-auto mt-8 shadow-[0_2px_8px_rgba(0,0,0,0.1)]" style={{ 
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
          {error && <div className="bg-[#fee] text-[#c33] p-[0.85rem] rounded-lg mb-6 border-l-4 border-[#c33] text-[0.95rem]">{error}</div>}
          <div className="mb-6">
            <label htmlFor="email" className="block mb-2 font-semibold text-[#555] text-[0.95rem]">{t('auth.email')} *</label>
            <input
              type="email"
              id="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              required
              placeholder="example@ksu.edu.ua"
              aria-invalid={fieldErrors.email ? true : undefined}
              aria-describedby={fieldErrors.email ? 'email-error' : undefined}
              className="w-full p-[0.85rem] border-2 border-[#e0e0e0] rounded-lg text-base transition-colors focus:outline-none focus:border-[#667eea]"
            />
            {fieldErrors.email && (
              <span id="email-error" className="block mt-1 text-[0.85rem] text-[#c33]">{fieldErrors.email}</span>
            )}
          </div>
          <div className="mb-6">
            <label htmlFor="password" className="block mb-2 font-semibold text-[#555] text-[0.95rem]">{t('auth.password')} *</label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              required
              minLength="8"
              placeholder={t('auth.password')}
              aria-invalid={fieldErrors.password ? true : undefined}
              aria-describedby={fieldErrors.password ? 'password-error' : undefined}
              className="w-full p-[0.85rem] border-2 border-[#e0e0e0] rounded-lg text-base transition-colors focus:outline-none focus:border-[#667eea]"
            />
            {fieldErrors.password && (
              <span id="password-error" className="block mt-1 text-[0.85rem] text-[#c33]">{fieldErrors.password}</span>
            )}
          </div>
          <div className="mb-5">
            <label htmlFor="remember" className="flex items-center gap-2 m-0 font-medium cursor-pointer">
              <input
                type="checkbox"
                id="remember"
                name="remember"
                checked={remember}
                onChange={(e) => setRemember(e.target.checked)}
                className="w-auto m-0"
              />
              <span>{t('auth.remember_me')}</span>
            </label>
          </div>
          <button type="submit" disabled={loading} className="w-full p-4 text-white border-none rounded-lg text-[1.1rem] font-semibold cursor-pointer transition-all [&:enabled:hover]:-translate-y-0.5 [&:enabled:hover]:shadow-[0_5px_20px_rgba(102,126,234,0.4)] disabled:opacity-60 disabled:cursor-not-allowed bg-[linear-gradient(135deg,#667eea_0%,#764ba2_100%)]">
            {loading ? t('auth.logging_in') : t('auth.login_btn')}
          </button>
          <div className="mt-6 text-center text-[#666]">
            <p>
              {t('auth.register_prompt')}{' '}
              <Link to="/register" className="text-[#667eea] no-underline font-semibold hover:underline">
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