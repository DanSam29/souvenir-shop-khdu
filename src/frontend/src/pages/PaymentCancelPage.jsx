import React from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

function PaymentCancelPage() {
  const { t } = useTranslation();
  return (
    <div className="payment-result-page" style={{ maxWidth: 600, margin: '50px auto', textAlign: 'center', padding: 20 }}>
      <div style={{ fontSize: '64px', color: '#f5222d', marginBottom: '20px' }}>✕</div>
      <h1>{t('checkout.cancel_title')}</h1>
      <p style={{ fontSize: '18px', color: '#666', marginBottom: '15px' }}>
        {t('checkout.cancel_message')}
      </p>
      <p style={{ fontSize: '15px', color: '#888', marginBottom: '30px' }}>
        {t('checkout.cancel_hint')}
      </p>
      <div style={{ display: 'flex', gap: '15px', justifyContent: 'center' }}>
        <Link to="/cart" className="btn btn-primary" style={{ padding: '10px 20px', textDecoration: 'none', background: '#1890ff', color: '#fff', borderRadius: '4px' }}>
          {t('checkout.back_to_cart')}
        </Link>
        <Link to="/profile" className="btn btn-secondary" style={{ padding: '10px 20px', textDecoration: 'none', border: '1px solid #d9d9d9', color: '#000', borderRadius: '4px' }}>
          {t('checkout.go_to_orders')}
        </Link>
      </div>
    </div>
  );
}

export default PaymentCancelPage;
