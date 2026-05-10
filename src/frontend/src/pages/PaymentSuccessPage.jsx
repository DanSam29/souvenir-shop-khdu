import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { ordersAPI } from '../services/api';

function PaymentSuccessPage() {
  const [searchParams] = useSearchParams();
  const sessionId = searchParams.get('session_id');
  const navigate = useNavigate();
  const [verifying, setVerifying] = useState(!!sessionId);

  useEffect(() => {
    if (!sessionId) {
      navigate('/');
      return;
    }

    const verify = async () => {
      try {
        await ordersAPI.verifyPayment(sessionId);
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
        <h1>Перевіряємо статус оплати...</h1>
        <p>Будь ласка, зачекайте кілька секунд.</p>
      </div>
    );
  }

  return (
    <div className="payment-result-page" style={{ maxWidth: 600, margin: '50px auto', textAlign: 'center', padding: 20 }}>
      <div style={{ fontSize: '64px', color: '#52c41a', marginBottom: '20px' }}>✓</div>
      <h1>Оплата успішна!</h1>
      <p style={{ fontSize: '18px', color: '#666', marginBottom: '30px' }}>
        Дякуємо за ваше замовлення. Ми вже почали його обробку.
      </p>
      <div style={{ display: 'flex', gap: '15px', justifyContent: 'center' }}>
        <Link to="/profile" className="btn btn-primary" style={{ padding: '10px 20px', textDecoration: 'none', background: '#1890ff', color: '#fff', borderRadius: '4px' }}>
          До моїх замовлень
        </Link>
        <Link to="/" className="btn btn-secondary" style={{ padding: '10px 20px', textDecoration: 'none', border: '1px solid #d9d9d9', color: '#000', borderRadius: '4px' }}>
          На головну
        </Link>
      </div>
    </div>
  );
}

export default PaymentSuccessPage;
