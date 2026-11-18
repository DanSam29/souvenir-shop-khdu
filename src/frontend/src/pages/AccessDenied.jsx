import React from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const AccessDenied = () => {
  const { t } = useTranslation();
  return (
    <div className="flex justify-center items-center min-h-[calc(100vh-200px)] p-5 bg-[#f8f9fa]">
      <div className="text-center bg-white p-10 rounded-xl shadow-[0_4px_20px_rgba(0,0,0,0.08)] max-w-[500px] w-full">
        <div className="text-[64px] mb-5">🚫</div>
        <h1 className="text-[#dc3545] mb-[15px] text-[28px]">{t('access_denied.title')}</h1>
        <p className="text-[#6c757d] mb-[10px] leading-relaxed">{t('access_denied.message_1')}</p>
        <p className="text-[#6c757d] mb-[10px] leading-relaxed">{t('access_denied.message_2')}</p>
        <Link to="/" className="inline-block mt-[25px] py-3 px-6 bg-[#004a99] text-white no-underline rounded-md font-semibold transition-colors hover:bg-[#003366]">
          {t('access_denied.back_home')}
        </Link>
      </div>
    </div>
  );
};

export default AccessDenied;