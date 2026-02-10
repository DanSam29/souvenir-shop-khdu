import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

function ProfilePage() {
  const { user, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  // Перенаправлення на логін, якщо не авторизований
  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
    }
  }, [isAuthenticated, navigate]);

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  if (!user) {
    return (
      <div className="profile-page">
        <div className="loading">Завантаження...</div>
      </div>
    );
  }

  return (
    <div className="profile-page">
      <div className="profile-container">
        <div className="profile-header">
          <div className="profile-avatar">
            <span className="avatar-icon">👤</span>
          </div>
          <h2>Особистий кабінет</h2>
        </div>

        <div className="profile-info">
          <div className="info-group">
            <label>Ім'я:</label>
            <p>{user.firstName}</p>
          </div>

          <div className="info-group">
            <label>Прізвище:</label>
            <p>{user.lastName}</p>
          </div>

          <div className="info-group">
            <label>Email:</label>
            <p>{user.email}</p>
          </div>

          {user.studentStatus && user.studentStatus !== 'NONE' && (
            <div className="info-group">
              <label>Статус студента:</label>
              <p className="role-badge">Студент</p>
            </div>
          )}

          {user.phone && (
            <div className="info-group">
              <label>Телефон:</label>
              <p>{user.phone}</p>
            </div>
          )}

          <div className="info-group">
            <label>Роль:</label>
            <p className="role-badge">{user.role}</p>
          </div>
        </div>

        <div className="profile-actions">
          <button onClick={handleLogout} className="logout-btn">
            Вийти з облікового запису
          </button>
        </div>
      </div>
    </div>
  );
}

export default ProfilePage;
