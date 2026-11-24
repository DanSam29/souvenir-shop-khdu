import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import LoginPage from '../LoginPage';
import i18n from '../../i18n';

const mockNavigate = jest.fn();
const mockLogin = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));
jest.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ login: mockLogin })
}));

const submitLogin = () => {
  fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'student@ksu.edu.ua' } });
  fireEvent.change(screen.getByLabelText(/Пароль|Password/i), { target: { value: 'Password123!' } });
  fireEvent.click(screen.getByRole('button', { name: /Увійти|Login/i }));
};

describe('LoginPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('відображення плейсхолдеру email example@ksu.edu.ua', () => {
    render(<MemoryRouter><LoginPage /></MemoryRouter>);
    const input = screen.getByLabelText(/Email/i);
    expect(input).toHaveAttribute('placeholder', 'example@ksu.edu.ua');
  });

  test('показує повідомлення про тимчасове блокування при 423/AccountLocked', async () => {
    mockLogin.mockRejectedValue({
      response: { status: 423, data: { errors: ['AccountLocked'], message: 'AccountLocked' } }
    });
    render(<MemoryRouter><LoginPage /></MemoryRouter>);

    submitLogin();

    expect(await screen.findByText(i18n.t('auth.account_locked'))).toBeInTheDocument();
  });

  test('показує повідомлення про невірні дані при 401', async () => {
    mockLogin.mockRejectedValue({ response: { status: 401, data: {} } });
    render(<MemoryRouter><LoginPage /></MemoryRouter>);

    submitLogin();

    expect(await screen.findByText(i18n.t('auth.invalid_credentials'))).toBeInTheDocument();
  });
});
