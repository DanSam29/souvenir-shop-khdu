import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { promotionsAPI } from '../services/api';

// Публічна сторінка зі списком активних акцій
function PromotionsPage() {
  const { t, i18n } = useTranslation();
  const isEn = i18n.language === 'en';
  const [promotions, setPromotions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        const res = await promotionsAPI.getActive();
        setPromotions(res.data || []);
      } catch (err) {
        console.error('Не вдалося завантажити акції', err);
        setError(t('common.load_error'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [t]);

  // Формат відображення розміру знижки залежно від типу
  const formatValue = (p) => {
    if (p.type === 'PERCENTAGE') return `-${p.value}%`;
    return `-${Number(p.value).toFixed(2)} ${t('common.currency')}`;
  };

  if (loading) return <div className="loading">{t('common.loading')}</div>;

  return (
    <div className="promotions-page" style={{ maxWidth: 1000, margin: '40px auto', padding: '0 20px' }}>
      <h1 style={{ marginBottom: 10 }}>{t('promotions.title')}</h1>
      <p style={{ color: '#888', marginBottom: 30 }}>{t('promotions.subtitle')}</p>

      {error && <div className="error">{error}</div>}

      {!error && promotions.length === 0 && (
        <div style={{ background: '#fff', padding: 30, borderRadius: 12, textAlign: 'center', color: '#888', boxShadow: '0 2px 8px rgba(0,0,0,0.05)' }}>
          {t('promotions.empty')}
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: 20 }}>
        {promotions.map((p) => {
          const name = isEn && p.nameEn ? p.nameEn : p.name;
          const description = isEn && p.descriptionEn ? p.descriptionEn : p.description;
          return (
            <div
              key={p.promotionId}
              style={{
                background: '#fff',
                borderRadius: 12,
                padding: 20,
                borderLeft: '6px solid #28a745',
                boxShadow: '0 4px 12px rgba(0,0,0,0.06)',
                display: 'flex',
                flexDirection: 'column',
                gap: 10
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 10 }}>
                <h3 style={{ margin: 0, fontSize: '1.1rem' }}>{name}</h3>
                <span style={{ color: '#28a745', fontWeight: 700, fontSize: '1.2rem', whiteSpace: 'nowrap' }}>
                  {formatValue(p)}
                </span>
              </div>
              {description && <p style={{ margin: 0, color: '#555', fontSize: '0.95rem' }}>{description}</p>}
              <div style={{ marginTop: 'auto', display: 'flex', justifyContent: 'space-between', color: '#888', fontSize: '0.85rem' }}>
                <span>{t(`promotions.target_${p.targetType}`, { defaultValue: p.targetType })}</span>
                {p.endDate && (
                  <span>{t('promotions.valid_until')}: {new Date(p.endDate).toLocaleDateString()}</span>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default PromotionsPage;
