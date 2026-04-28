import React, { createContext, useState, useContext, useEffect, useCallback } from 'react';
import { usersAPI } from '../services/api';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // Вихід (потрібен для loadUserData, тому визначаємо його першим)
  const logout = useCallback(() => {
    localStorage.removeItem('token');
    setUser(null);
  }, []);

  // Завантаження даних користувача (Обертаємо в useCallback для стабілізації)
  const loadUserData = useCallback(async () => {
    try {
      const response = await usersAPI.getCurrentUser();
      setUser(response.data);
    } catch (error) {
      console.error('Помилка завантаження даних користувача:', error);
      logout(); // Якщо токен невалідний - виходимо
    } finally {
      setLoading(false);
    }
  }, [logout]); // Залежність: logout, оскільки функція loadUserData її використовує.

  // Перевірка токену при завантаженні додатку
  useEffect(() => {
    const token = localStorage.getItem('token');
    if (token) {
      loadUserData();
    } else {
      setLoading(false);
    }
  }, [loadUserData]); // Залежність: loadUserData

  // Авторизація
  const login = async (email, password) => {
    const response = await usersAPI.login({ email, password });
    const { token, ...userData } = response.data;
    
    localStorage.setItem('token', token);
    setUser(userData);
    
    return response.data;
  };

  // Реєстрація
  const register = async (userData) => {
    const response = await usersAPI.register(userData);
    const { token, ...userInfo } = response.data;
    
    localStorage.setItem('token', token);
    setUser(userInfo);
    
    return response.data;
  };

  const value = {
    user,
    loading,
    login,
    register,
    logout,
    isAuthenticated: !!user
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

// Хук для використання контексту
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth має використовуватись всередині AuthProvider');
  }
  return context;
};

export default AuthContext;