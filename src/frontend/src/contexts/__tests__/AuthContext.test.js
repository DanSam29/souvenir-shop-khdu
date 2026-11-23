import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { AuthProvider, useAuth } from '../AuthContext';

// Юніт-тест логіки контексту: мережевий шар мокається, щоб перевірити
// роботу зі сховищами (localStorage/sessionStorage) та станом користувача.
jest.mock('../../services/api', () => ({
  usersAPI: {
    login: jest.fn(),
    register: jest.fn(),
    getCurrentUser: jest.fn(),
    updateLanguage: jest.fn()
  }
}));

const { usersAPI } = require('../../services/api');

function Probe() {
  const { isAuthenticated, user, loading, login, logout } = useAuth();
  return (
    <div>
      <span data-testid="loading">{String(loading)}</span>
      <span data-testid="auth">{String(isAuthenticated)}</span>
      <span data-testid="email">{user?.email || 'none'}</span>
      <button onClick={() => login('student@ksu.edu.ua', 'Password123!', true)}>login-remember</button>
      <button onClick={() => login('student@ksu.edu.ua', 'Password123!', false)}>login-session</button>
      <button onClick={logout}>logout</button>
    </div>
  );
}

const setup = () => render(<AuthProvider><Probe /></AuthProvider>);

describe('AuthContext', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    sessionStorage.clear();
  });

  test('початковий стан — неавторизований, loading=false', async () => {
    setup();
    expect(screen.getByTestId('auth')).toHaveTextContent('false');
    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('false'));
  });

  test('login з remember=true зберігає токен у localStorage', async () => {
    usersAPI.login.mockResolvedValue({
      data: { token: 'jwt-local', id: 1, email: 'student@ksu.edu.ua' }
    });
    setup();
    fireEvent.click(screen.getByText('login-remember'));
    await waitFor(() => expect(screen.getByTestId('auth')).toHaveTextContent('true'));

    expect(localStorage.getItem('token')).toBe('jwt-local');
    expect(sessionStorage.getItem('token')).toBeNull();
    expect(screen.getByTestId('email')).toHaveTextContent('student@ksu.edu.ua');
    expect(usersAPI.login).toHaveBeenCalledWith({
      email: 'student@ksu.edu.ua',
      password: 'Password123!'
    });
  });

  test('login з remember=false зберігає токен у sessionStorage', async () => {
    usersAPI.login.mockResolvedValue({
      data: { token: 'jwt-session', id: 2, email: 'student@ksu.edu.ua' }
    });
    setup();
    fireEvent.click(screen.getByText('login-session'));
    await waitFor(() => expect(screen.getByTestId('auth')).toHaveTextContent('true'));

    expect(sessionStorage.getItem('token')).toBe('jwt-session');
    expect(localStorage.getItem('token')).toBeNull();
  });

  test('logout очищає токен і скидає користувача', async () => {
    usersAPI.login.mockResolvedValue({ data: { token: 'jwt', id: 1, email: 'a@ksu.edu.ua' } });
    setup();
    fireEvent.click(screen.getByText('login-remember'));
    await waitFor(() => expect(screen.getByTestId('auth')).toHaveTextContent('true'));

    fireEvent.click(screen.getByText('logout'));
    await waitFor(() => expect(screen.getByTestId('auth')).toHaveTextContent('false'));
    expect(localStorage.getItem('token')).toBeNull();
  });

  test('відновлює сесію за наявним токеном', async () => {
    localStorage.setItem('token', 'existing-token');
    usersAPI.getCurrentUser.mockResolvedValue({
      data: { id: 3, email: 'restored@ksu.edu.ua', preferredLanguage: 'ua' }
    });
    setup();

    await waitFor(() => expect(screen.getByTestId('email')).toHaveTextContent('restored@ksu.edu.ua'));
    expect(screen.getByTestId('auth')).toHaveTextContent('true');
    expect(usersAPI.getCurrentUser).toHaveBeenCalledTimes(1);
  });

  test('невалідний токен при відновленні сесії → logout', async () => {
    localStorage.setItem('token', 'bad-token');
    usersAPI.getCurrentUser.mockRejectedValue(new Error('401 Unauthorized'));
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});

    setup();

    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('false'));
    expect(screen.getByTestId('auth')).toHaveTextContent('false');
    expect(localStorage.getItem('token')).toBeNull();
    consoleSpy.mockRestore();
  });
});
