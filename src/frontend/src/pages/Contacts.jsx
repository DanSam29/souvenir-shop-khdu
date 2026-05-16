import React from 'react';
import { useTranslation } from 'react-i18next';

function Contacts() {
  const { t } = useTranslation();

  return (
    <div className="container" style={{ padding: '40px 20px', lineHeight: '1.6' }}>
      <h1>{t('legal.contacts')}</h1>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '40px', marginTop: '20px' }}>
        <div>
          <h3>Зв'яжіться з нами</h3>
          <p>📧 Email: office@ksu.ks.ua</p>
          <p>📞 Телефон: +380963102636</p>
          <p>🌐 Веб-сайт: <a href="https://www.kspu.edu" target="_blank" rel="noopener noreferrer">www.kspu.edu</a></p>
        </div>
        <div>
          <h3>Наші адреси</h3>
          <p><strong>Юридична адреса:</strong><br />вул. Університетська, 27, м. Херсон, 73003</p>
          <p><strong>Фактична адреса (офіс):</strong><br />вул. Шевченка, 14, м. Івано-Франківськ, 76018</p>
        </div>
      </div>
    </div>
  );
}

export default Contacts;
