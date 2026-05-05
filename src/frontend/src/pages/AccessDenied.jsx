import React from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import './AccessDenied.css';

const AccessDenied = () => {
  const { t } = useTranslation();

  return (
    <div className="access-denied-container">
      <div className="access-denied-content">
        <div className="error-icon">🚫</div>
        <h1>Доступ заборонено</h1>
        <p>На жаль, у вас немає прав для перегляду цієї сторінки.</p>
        <p>Якщо ви вважаєте, що це помилка, зверніться до адміністратора.</p>
        <Link to="/" className="back-home-btn">
          Повернутися на головну
        </Link>
      </div>
    </div>
  );
};

export default AccessDenied;
