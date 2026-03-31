import React from 'react';
import { Link } from 'react-router-dom';

function PaymentCancelPage() {
  return (
    <div className="payment-result-page" style={{ maxWidth: 600, margin: '50px auto', textAlign: 'center', padding: 20 }}>
      <div style={{ fontSize: '64px', color: '#f5222d', marginBottom: '20px' }}>✕</div>
      <h1>Оплата скасована</h1>
      <p style={{ fontSize: '18px', color: '#666', marginBottom: '30px' }}>
        Ви скасували процес оплати. Ви можете повернутися до кошика та спробувати ще раз.
      </p>
      <div style={{ display: 'flex', gap: '15px', justifyContent: 'center' }}>
        <Link to="/cart" className="btn btn-primary" style={{ padding: '10px 20px', textDecoration: 'none', background: '#1890ff', color: '#fff', borderRadius: '4px' }}>
          До кошика
        </Link>
        <Link to="/" className="btn btn-secondary" style={{ padding: '10px 20px', textDecoration: 'none', border: '1px solid #d9d9d9', color: '#000', borderRadius: '4px' }}>
          На головну
        </Link>
      </div>
    </div>
  );
}

export default PaymentCancelPage;
