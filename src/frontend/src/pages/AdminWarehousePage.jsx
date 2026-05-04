import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { warehouseAPI } from '../services/api';

function AdminWarehousePage() {
  const [stock, setStock] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadStock();
  }, []);

  const loadStock = async () => {
    try {
      setLoading(true);
      const res = await warehouseAPI.getStock();
      setStock(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="loading">Завантаження...</div>;

  const getStockColor = (current) => {
    if (current <= 0) return '#dc3545';
    if (current <= 5) return '#ffc107';
    return '#28a745';
  };

  return (
    <div className="admin-warehouse" style={{ padding: 20, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 20 }}>
        <Link to="/admin" className="back-link">
          ← Назад до дашборду
        </Link>
        <h1>Моніторинг залишків</h1>
      </div>
      <div style={{ background: '#fff', borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)', overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead style={{ background: '#f8f9fa' }}>
            <tr>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Товар</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Поточний залишок</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Загальний прихід</th>
              <th style={{ padding: '16px', textAlign: 'left', borderBottom: '1px solid #eee' }}>Загальний розхід</th>
            </tr>
          </thead>
          <tbody>
            {stock.map(item => (
              <tr key={item.productId} style={{ borderBottom: '1px solid #f0f0f0' }}>
                <td style={{ padding: '16px', fontWeight: 500 }}>{item.name}</td>
                <td style={{ padding: '16px' }}>
                  <span style={{ 
                    fontWeight: 'bold', 
                    color: getStockColor(item.currentStock) 
                  }}>
                    {item.currentStock}
                  </span>
                </td>
                <td style={{ padding: '16px' }}>{item.totalIncoming}</td>
                <td style={{ padding: '16px' }}>{item.totalOutgoing}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {stock.length === 0 && (
          <div style={{ padding: 40, textAlign: 'center', color: '#666' }}>
            Даних по складу ще немає
          </div>
        )}
      </div>
    </div>
  );
}

export default AdminWarehousePage;
