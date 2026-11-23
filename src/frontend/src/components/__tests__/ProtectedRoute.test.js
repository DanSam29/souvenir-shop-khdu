import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import ProtectedRoute from '../ProtectedRoute';

jest.mock('../../contexts/AuthContext', () => ({ useAuth: jest.fn() }));
const { useAuth } = require('../../contexts/AuthContext');

function renderAt(path, allowedRoles) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route
          path="/admin"
          element={
            <ProtectedRoute allowedRoles={allowedRoles}>
              <div>Захищений вміст</div>
            </ProtectedRoute>
          }
        />
        <Route path="/login" element={<div>Сторінка входу</div>} />
        <Route path="/access-denied" element={<div>Доступ заборонено</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('ProtectedRoute', () => {
  test('показує індикатор завантаження під час перевірки сесії', () => {
    useAuth.mockReturnValue({ user: null, loading: true, isAuthenticated: false });
    renderAt('/admin', ['Administrator']);
    expect(screen.getByText('Завантаження...')).toBeInTheDocument();
  });

  test('редирект на /login для неавторизованого користувача', () => {
    useAuth.mockReturnValue({ user: null, loading: false, isAuthenticated: false });
    renderAt('/admin', ['Administrator']);
    expect(screen.getByText('Сторінка входу')).toBeInTheDocument();
    expect(screen.queryByText('Захищений вміст')).not.toBeInTheDocument();
  });

  test('редирект на /access-denied при невідповідній ролі', () => {
    useAuth.mockReturnValue({ user: { role: 'Customer' }, loading: false, isAuthenticated: true });
    renderAt('/admin', ['Administrator']);
    expect(screen.getByText('Доступ заборонено')).toBeInTheDocument();
    expect(screen.queryByText('Захищений вміст')).not.toBeInTheDocument();
  });

  test('рендерить вміст для дозволеної ролі', () => {
    useAuth.mockReturnValue({ user: { role: 'Manager' }, loading: false, isAuthenticated: true });
    renderAt('/admin', ['Administrator', 'Manager']);
    expect(screen.getByText('Захищений вміст')).toBeInTheDocument();
  });
});
