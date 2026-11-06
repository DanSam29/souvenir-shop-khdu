import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ordersAPI } from '../services/api';

function PaymentSuccessPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const sessionId = searchParams.get('session_id');
  const navigate = useNavigate();
  const [verifying, setVerifying] = useState(!!sessionId);
  const [order, setOrder] = useState(null);
  useEffect(() => {
    if (!sessionId) {
      navigate('/');
      return;
    }
    const verify = async () => {
      try {
        const res = await ordersAPI.verifyPayment(sessionId);
        if (res?.data) {
          setOrder(res.data);
        }
      } catch (err) {
        console.error('Помилка верифікації платежу:', err);
      } finally {
        setVerifying(false);
      }
    };
    verify();
  }, [sessionId, navigate]);
  if (verifying) {
    return (
      <div className="payment-result-page" style={{ maxWidth: 600, margin: '50px auto', textAlign: 'center', padding: 20 }}>
        <div className="spinner" style={{ fontSize: '48px', marginBottom: '20px' }}>⌛</div>
        <h1>{t('checkout.verifying_title')}</h1>
        <p>{t('checkout.verifying_message')}</p>
      </div>
    );
  }
  return (
    <div className="payment-result-page" style={{ maxWidth: 600, margin: '50px auto', textAlign: 'center', padding: 20 }}>
      <div style={{ fontSize: '64px', color: '#52c41a', marginBottom: '20px' }}>✓</div>
      <h1>{t('checkout.success_title')}</h1>
      <p style={{ fontSize: '18px', color: '#666', marginBottom: '20px' }}>
        {t('checkout.success_message')}
      </p>
      {order?.orderNumber && (
        <p style={{ fontSize: '18px', marginBottom: '30px' }}>
          {t('checkout.order_number')}: <strong>{order.orderNumber}</strong>
        </p>
      )}
      <div style={{ display: 'flex', gap: '15px', justifyContent: 'center' }}>
        {order?.orderId ? (
          <Link
            to={`/order/${order.orderId}`}
            className="btn btn-primary"
            style={{ padding: '10px 20px', textDecoration: 'none', background: '#1890ff', color: '#fff', borderRadius: '4px' }}
          >
            {t('checkout.view_order_details')}
          </Link>
        ) : (
          <Link to="/profile" className="btn btn-primary" style={{ padding: '10px 20px', textDecoration: 'none', background: '#1890ff', color: '#fff', borderRadius: '4px' }}>
            {t('checkout.go_to_orders')}
          </Link>
        )}
        <Link to="/" className="btn btn-secondary" style={{ padding: '10px 20px', textDecoration: 'none', border: '1px solid #d9d9d9', color: '#000', borderRadius: '4px' }}>
          {t('checkout.back_home')}
        </Link>
      </div>
    </div>
  );
}

export default PaymentSuccessPage;
