import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { rest } from 'msw';
import Header from '../Header';
import { server } from '../../test/server';
import { API_URL } from '../../test/handlers';

jest.mock('../../contexts/AuthContext', () => ({ useAuth: jest.fn() }));
const { useAuth } = require('../../contexts/AuthContext');

const renderHeader = () => render(<MemoryRouter><Header /></MemoryRouter>);

describe('Header', () => {
  test('для гостя показує базові лінки та не показує профіль/адмін-панель', () => {
    useAuth.mockReturnValue({ isAuthenticated: false, user: null });
    renderHeader();

    expect(screen.getByText('Головна')).toBeInTheDocument();
    expect(screen.getByText('Акції')).toBeInTheDocument();
    expect(screen.getByText('Кошик')).toBeInTheDocument();
    expect(screen.getByText('Увійти')).toBeInTheDocument();
    expect(screen.getByText('Реєстрація')).toBeInTheDocument();
    expect(screen.queryByText('Профіль')).not.toBeInTheDocument();
    expect(screen.queryByText('Адмін-панель')).not.toBeInTheDocument();
  });

  test('для адміністратора показує профіль та адмін-панель', () => {
    useAuth.mockReturnValue({ isAuthenticated: true, user: { role: 'Administrator' } });
    renderHeader();

    expect(screen.getByText('Профіль')).toBeInTheDocument();
    expect(screen.getByText('Адмін-панель')).toBeInTheDocument();
    expect(screen.queryByText('Увійти')).not.toBeInTheDocument();
    expect(screen.queryByText('Реєстрація')).not.toBeInTheDocument();
  });

  test('показує кількість товарів у кошику', async () => {
    useAuth.mockReturnValue({ isAuthenticated: true, user: { role: 'Customer' } });
    server.use(
      rest.get(`${API_URL}/Cart`, (req, res, ctx) =>
        res(ctx.status(200), ctx.json({ items: [], itemCount: 5, totalAmount: 0 }))
      )
    );
    renderHeader();

    expect(await screen.findByText('5')).toBeInTheDocument();
  });

  test('перемикач мови змінює мову інтерфейсу', async () => {
    useAuth.mockReturnValue({ isAuthenticated: false, user: null });
    renderHeader();

    expect(screen.getByText('EN')).toBeInTheDocument();
    fireEvent.click(screen.getByText('EN'));

    expect(await screen.findByText('UA')).toBeInTheDocument();
    expect(await screen.findByText('Home')).toBeInTheDocument();
  });

  test('меню-гамбургер на мобільному розгортає навігацію', () => {
    useAuth.mockReturnValue({ isAuthenticated: false, user: null });
    renderHeader();

    const burger = screen.getByRole('button', { name: 'Відкрити меню' });
    fireEvent.click(burger);

    expect(screen.getByRole('button', { name: 'Закрити меню' })).toBeInTheDocument();
  });
});
