import React from 'react';
import { useTranslation } from 'react-i18next';

function Contacts() {
  const { t } = useTranslation();
  return (
    <div className="container" style={{ padding: '40px 20px', lineHeight: '1.6' }}>
      <h1>{t('legal.contacts')}</h1>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '40px', marginTop: '20px' }}>
        <div>
          <h3>{t('legal.contact_us')}</h3>
          <p>📧 Email: office@ksu.ks.ua</p>
          <p>📞 {t('legal.phone_label')}: +380963102636</p>
          <p>🌐 {t('legal.website_label')}: <a href="https://www.kspu.edu" target="_blank" rel="noopener noreferrer">www.kspu.edu</a></p>
        </div>
        <div>
          <h3>{t('legal.our_addresses')}</h3>
          <p><strong>{t('legal.legal_address')}:</strong><br />{t('legal.legal_address_value')}</p>
          <p><strong>{t('legal.office_address')}:</strong><br />{t('legal.office_address_value')}</p>
        </div>
      </div>
    </div>
  );
}

export default Contacts;