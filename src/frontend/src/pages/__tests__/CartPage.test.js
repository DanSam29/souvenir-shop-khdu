import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { rest } from 'msw';
import CartPage from '../CartPage';
import { server } from '../../test/server';
import { API_URL } from '../../test/handlers';

jest.mock('../../contexts/AuthContext', () => ({ useAuth: jest.fn() }));
const { useAuth } = require('../../contexts/AuthContext');

const cart = {
  items: [
    {
      cartItemId: 11,
      productId: 1,
      productName: 'Футболка ХДУ',
      productPrice: 250,
      quantity: 2,
      subtotal: 500,
      productImage: '/uploads/tshirt.jpg'
    },
    {
      cartItemId: 12,
      productId: 2,
      productName: 'Кружка ХДУ',
      productPrice: 150,
      originalPrice: 200,
      quantity: 1,
      subtotal: 150,
      productImage: '/uploads/mug.jpg'
    }
  ],
  itemCount: 3,
  totalAmount: 650
};

const renderCart = () => render(<MemoryRouter><CartPage /></MemoryRouter>);

describe('CartPage', () => {
  beforeEach(() => {
    server.use(
      rest.get(`${API_URL}/Cart`, (req, res, ctx) => res(ctx.status(200), ctx.json(cart)))
    );
  });

  test('гість бачить запрошення авторизуватися', () => {
    useAuth.mockReturnValue({ isAuthenticated: false });
    renderCart();

    expect(
      screen.getByText('Для додавання товарів до кошика потрібно авторизуватися')
    ).toBeInTheDocument();
  });

  test('рендерить позиції кошика та підсумок', async () => {
    useAuth.mockReturnValue({ isAuthenticated: true });
    renderCart();

    expect(await screen.findByText('Ваш кошик')).toBeInTheDocument();
    expect(screen.getByText('Футболка ХДУ')).toBeInTheDocument();
    expect(screen.getByText('Кружка ХДУ')).toBeInTheDocument();
    expect(screen.getByText('500.00 грн')).toBeInTheDocument();
    expect(screen.getByText('150.00 грн')).toBeInTheDocument();
    expect(screen.getByText('650.00 грн')).toBeInTheDocument();
    expect(screen.getByText('Знижка:')).toBeInTheDocument();
    expect(screen.getByText('-50.00 грн')).toBeInTheDocument();
  });

  test('збільшення кількості надсилає PUT із новим значенням', async () => {
    useAuth.mockReturnValue({ isAuthenticated: true });
    const putSpy = jest.fn();
    server.use(
      rest.put(`${API_URL}/Cart/items/:id`, async (req, res, ctx) => {
        const body = await req.json();
        putSpy(req.params.id, body.quantity);
        return res(ctx.status(200), ctx.json({}));
      })
    );
    renderCart();
    await screen.findByText('Ваш кошик');

    // Другий товар має кількість 1 → клік "+" надсилає quantity=2
    fireEvent.click(screen.getAllByText('+')[1]);

    await waitFor(() => expect(putSpy).toHaveBeenCalledWith('12', 2));
  });

  test('видалення позиції надсилає DELETE', async () => {
    useAuth.mockReturnValue({ isAuthenticated: true });
    const deleteSpy = jest.fn();
    server.use(
      rest.delete(`${API_URL}/Cart/items/:id`, (req, res, ctx) => {
        deleteSpy(req.params.id);
        return res(ctx.status(200), ctx.json({}));
      })
    );
    renderCart();
    await screen.findByText('Ваш кошик');

    fireEvent.click(screen.getAllByText('✕')[0]);

    await waitFor(() => expect(deleteSpy).toHaveBeenCalledWith('11'));
  });

  test('очищення кошика після підтвердження надсилає DELETE /Cart', async () => {
    useAuth.mockReturnValue({ isAuthenticated: true });
    const clearSpy = jest.fn();
    server.use(
      rest.delete(`${API_URL}/Cart`, (req, res, ctx) => {
        clearSpy();
        return res(ctx.status(200), ctx.json({}));
      })
    );
    const confirmSpy = jest.spyOn(window, 'confirm').mockReturnValue(true);
    renderCart();
    await screen.findByText('Ваш кошик');

    fireEvent.click(screen.getByRole('button', { name: /Очистити кошик/ }));

    await waitFor(() => expect(clearSpy).toHaveBeenCalledTimes(1));
    confirmSpy.mockRestore();
  });
});
