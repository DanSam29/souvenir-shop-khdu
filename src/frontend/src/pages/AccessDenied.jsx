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
        <h1>{t('access_denied.title')}</h1>
        <p>{t('access_denied.message_1')}</p>
        <p>{t('access_denied.message_2')}</p>
        <Link to="/" className="back-home-btn">
          {t('access_denied.back_home')}
        </Link>
      </div>
    </div>
  );
};

export default AccessDenied;
