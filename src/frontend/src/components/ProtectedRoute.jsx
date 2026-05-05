import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const ProtectedRoute = ({ children, allowedRoles }) => {
  const { user, loading, isAuthenticated } = useAuth();

  if (loading) {
    return <div className="loading-container">Завантаження...</div>;
  }

  if (!isAuthenticated) {
    // Якщо не авторизований - на сторінку входу
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    // Якщо роль не підходить - на сторінку "Доступ заборонено"
    return <Navigate to="/access-denied" replace />;
  }

  return children;
};

export default ProtectedRoute;
