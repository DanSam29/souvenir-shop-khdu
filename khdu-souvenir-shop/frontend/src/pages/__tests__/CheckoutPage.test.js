import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import CheckoutPage from '../CheckoutPage';

const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate
}));

jest.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ isAuthenticated: true })
}));

const mockCart = {
  items: [
    { cartItemId: 1, productName: 'Футболка ХДУ', productPrice: 200, quantity: 1 },
    { cartItemId: 2, productName: 'Кружка ХДУ', productPrice: 150, quantity: 2 }
  ],
  totalAmount: 500
};

const mockUser = {
  firstName: 'Іван',
  lastName: 'Студент',
  phone: '+380961234567'
};

jest.mock('../../services/api', () => ({
  cartAPI: { getCart: jest.fn() },
  usersAPI: { getCurrentUser: jest.fn() },
  ordersAPI: { checkout: jest.fn() }
}));

const { cartAPI, usersAPI, ordersAPI } = require('../../services/api');

describe('CheckoutPage', () => {
  beforeEach(() => {
    cartAPI.getCart.mockResolvedValue({ data: mockCart });
    usersAPI.getCurrentUser.mockResolvedValue({ data: mockUser });
    ordersAPI.checkout.mockReset();
  });

  test('оформлення із застосованим промокодом показує рядок знижки', async () => {
    ordersAPI.checkout.mockResolvedValue({
      data: {
        orderNumber: 'ORD-12345',
        totalAmount: 450,
        discountTotal: 50
      }
    });

    render(<MemoryRouter><CheckoutPage /></MemoryRouter>);

    await waitFor(() => expect(screen.getByText('Оформлення замовлення')).toBeInTheDocument());

    fireEvent.change(screen.getByLabelText('Місто'), { target: { value: 'Київ' } });
    fireEvent.change(screen.getByLabelText('Номер відділення'), { target: { value: '12' } });
    fireEvent.change(screen.getByLabelText('Промокод'), { target: { value: 'KHDU10' } });

    fireEvent.click(screen.getByRole('button', { name: 'Підтвердити замовлення' }));

    await waitFor(() => expect(screen.getByText('Замовлення оформлено')).toBeInTheDocument());
    expect(screen.getByText(/Номер: ORD-12345/)).toBeInTheDocument();
    expect(screen.getByText(/Сума: 450\.00 грн/)).toBeInTheDocument();
    expect(screen.getByText(/Знижка застосована: −50\.00 грн/)).toBeInTheDocument();
  });

  test('оформлення без промокоду не показує рядок знижки', async () => {
    ordersAPI.checkout.mockResolvedValue({
      data: {
        orderNumber: 'ORD-67890',
        totalAmount: 500
      }
    });

    render(<MemoryRouter><CheckoutPage /></MemoryRouter>);

    await waitFor(() => expect(screen.getByText('Оформлення замовлення')).toBeInTheDocument());

    fireEvent.change(screen.getByLabelText('Місто'), { target: { value: 'Львів' } });
    fireEvent.change(screen.getByLabelText('Номер відділення'), { target: { value: '5' } });

    fireEvent.click(screen.getByRole('button', { name: 'Підтвердити замовлення' }));

    await waitFor(() => expect(screen.getByText('Замовлення оформлено')).toBeInTheDocument());
    expect(screen.getByText(/Номер: ORD-67890/)).toBeInTheDocument();
    expect(screen.getByText(/Сума: 500\.00 грн/)).toBeInTheDocument();
    expect(screen.queryByText(/Знижка застосована/)).toBeNull();
  });
});
