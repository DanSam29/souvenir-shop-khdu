import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { rest } from 'msw';
import RegisterForm from '../RegisterForm';
import { server } from '../../test/server';
import { API_URL } from '../../test/handlers';

const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate
}));

const renderForm = () => {
  const utils = render(<MemoryRouter><RegisterForm /></MemoryRouter>);
  return { ...utils, form: utils.container.querySelector('form') };
};

describe('RegisterForm', () => {
  beforeEach(() => mockNavigate.mockClear());

  test('рендерить усі поля форми', () => {
    renderForm();
    expect(screen.getByLabelText(/Ім'я/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Прізвище/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Email/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Пароль/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Зареєструватися' })).toBeInTheDocument();
  });

  test('показує помилку для некоректного email', async () => {
    const { form } = renderForm();
    fireEvent.change(screen.getByLabelText(/Email/), { target: { name: 'email', value: 'invalid-email' } });
    fireEvent.change(screen.getByLabelText(/Пароль/), { target: { name: 'password', value: 'Password123!' } });
    fireEvent.submit(form);

    expect(await screen.findByText('Невірний формат email')).toBeInTheDocument();
  });

  test('показує помилку для закороткого пароля', async () => {
    const { form } = renderForm();
    fireEvent.change(screen.getByLabelText(/Email/), { target: { name: 'email', value: 'new@ksu.edu.ua' } });
    fireEvent.change(screen.getByLabelText(/Пароль/), { target: { name: 'password', value: '123' } });
    fireEvent.submit(form);

    expect(await screen.findByText('Пароль має містити мінімум 8 символів')).toBeInTheDocument();
  });

  test('успішна реєстрація → редирект на /login', async () => {
    server.use(
      rest.post(`${API_URL}/Users/register`, (req, res, ctx) =>
        res(ctx.status(200), ctx.json({ token: 'jwt', id: 1, email: 'new@ksu.edu.ua' }))
      )
    );
    const { form } = renderForm();
    fireEvent.change(screen.getByLabelText(/Email/), { target: { name: 'email', value: 'new@ksu.edu.ua' } });
    fireEvent.change(screen.getByLabelText(/Пароль/), { target: { name: 'password', value: 'Password123!' } });
    fireEvent.submit(form);

    await waitFor(() =>
      expect(mockNavigate).toHaveBeenCalledWith('/login', { state: { registrationSuccess: true } })
    );
  });

  test('409 від backend → повідомлення про існуючий email', async () => {
    server.use(
      rest.post(`${API_URL}/Users/register`, (req, res, ctx) =>
        res(ctx.status(409), ctx.json({ message: 'Email exists' }))
      )
    );
    const { form } = renderForm();
    fireEvent.change(screen.getByLabelText(/Email/), { target: { name: 'email', value: 'taken@ksu.edu.ua' } });
    fireEvent.change(screen.getByLabelText(/Пароль/), { target: { name: 'password', value: 'Password123!' } });
    fireEvent.submit(form);

    expect(await screen.findByText('Email вже зареєстрований')).toBeInTheDocument();
  });
});
