import React from 'react';
import { useTranslation } from 'react-i18next';

function PrivacyPolicy() {
  const { t } = useTranslation();
  return (
    <div className="container" style={{ padding: '40px 20px', lineHeight: '1.6' }}>
      <h1>{t('legal.privacy_policy')}</h1>
      <p>{t('legal.privacy_intro')}</p>
      <h3>{t('legal.privacy_s1_title')}</h3>
      <p>{t('legal.privacy_s1_text')}</p>
      <h3>{t('legal.privacy_s2_title')}</h3>
      <p>{t('legal.privacy_s2_text')}</p>
      <h3>{t('legal.privacy_s3_title')}</h3>
      <p>{t('legal.privacy_s3_text')}</p>
    </div>
  );
}

export default PrivacyPolicy;