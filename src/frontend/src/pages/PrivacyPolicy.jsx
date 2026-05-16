import React from 'react';
import { useTranslation } from 'react-i18next';

function PrivacyPolicy() {
  const { t } = useTranslation();

  return (
    <div className="container" style={{ padding: '40px 20px', lineHeight: '1.6' }}>
      <h1>{t('legal.privacy_policy')}</h1>
      <p>Ми поважаємо вашу конфіденційність і зобов'язуємося захищати ваші персональні дані.</p>
      
      <h3>1. Які дані ми збираємо</h3>
      <p>Ми збираємо ваше ім'я, email, номер телефону та адресу доставки для обробки замовлень.</p>
      
      <h3>2. Як ми використовуємо ваші дані</h3>
      <p>Ваші дані використовуються виключно для виконання замовлень та покращення сервісу нашого магазину.</p>
      
      <h3>3. Захист даних</h3>
      <p>Ми використовуємо сучасні методи шифрування для захисту вашої особистої інформації.</p>
    </div>
  );
}

export default PrivacyPolicy;
