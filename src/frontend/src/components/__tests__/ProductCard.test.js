import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { rest } from 'msw';
import ProductCard from '../ProductCard';
import { server } from '../../test/server';
import { API_URL } from '../../test/handlers';

const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate
}));
jest.mock('../../contexts/AuthContext', () => ({ useAuth: jest.fn() }));
const { useAuth } = require('../../contexts/AuthContext');

const baseProduct = {
  productId: 1,
  name: 'Футболка ХДУ',
  nameEn: 'KSU T-Shirt',
  price: 250,
  originalPrice: 300,
  stock: 10,
  category: { name: 'Одяг' },
  images: [{ imageURL: '/uploads/tshirt.jpg', isPrimary: true }]
};

const renderCard = (product = baseProduct) =>
  render(<MemoryRouter><ProductCard product={product} /></MemoryRouter>);

// jsdom робить window.location.reload доступним лише для читання,
// тому підміняємо весь об'єкт location перед запуском тестів.
const originalLocation = window.location;
const reloadSpy = jest.fn();

beforeAll(() => {
  Object.defineProperty(window, 'location', {
    configurable: true,
    writable: true,
    value: { ...originalLocation, reload: reloadSpy }
  });
});

afterAll(() => {
  Object.defineProperty(window, 'location', {
    configurable: true,
    writable: true,
    value: originalLocation
  });
});

describe('ProductCard', () => {
  let alertSpy;

  beforeEach(() => {
    mockNavigate.mockClear();
    reloadSpy.mockClear();
    alertSpy = jest.spyOn(window, 'alert').mockImplementation(() => {});
  });

  afterEach(() => {
    alertSpy.mockRestore();
  });

  test('рендерить назву, категорію, ціну та кнопку', () => {
    useAuth.mockReturnValue({ isAuthenticated: false });
    renderCard();

    expect(screen.getByText('Футболка ХДУ')).toBeInTheDocument();
    expect(screen.getByText('Одяг')).toBeInTheDocument();
    expect(screen.getByText(/250\.00/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Додати у кошик' })).toBeInTheDocument();
  });

  test('показує перекреслену стару ціну при знижці', () => {
    useAuth.mockReturnValue({ isAuthenticated: false });
    renderCard();

    expect(screen.getByText(/300\.00/)).toBeInTheDocument();
  });

  test('для товару без залишку кнопка заблокована', () => {
    useAuth.mockReturnValue({ isAuthenticated: false });
    renderCard({ ...baseProduct, stock: 0 });

    const button = screen.getByRole('button', { name: 'Немає в наявності' });
    expect(button).toBeDisabled();
  });

  test('гість при додаванні отримує попередження та редирект на /login', () => {
    useAuth.mockReturnValue({ isAuthenticated: false });
    renderCard();

    fireEvent.click(screen.getByRole('button', { name: 'Додати у кошик' }));

    expect(alertSpy).toHaveBeenCalled();
    expect(mockNavigate).toHaveBeenCalledWith('/login');
  });

  test('авторизований користувач додає товар у кошик', async () => {
    useAuth.mockReturnValue({ isAuthenticated: true });
    server.use(
      rest.post(`${API_URL}/Cart/items`, (req, res, ctx) =>
        res(ctx.status(200), ctx.json({ itemCount: 1, totalAmount: 250 }))
      )
    );
    renderCard();

    fireEvent.click(screen.getByRole('button', { name: 'Додати у кошик' }));

    await waitFor(() => expect(alertSpy).toHaveBeenCalledWith('Товар додано до кошика!'));
    expect(mockNavigate).not.toHaveBeenCalled();
  });
});
