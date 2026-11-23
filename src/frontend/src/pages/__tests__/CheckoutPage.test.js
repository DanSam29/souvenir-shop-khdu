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

jest.mock('../../services/api', () => ({
  cartAPI: { getCart: jest.fn() },
  usersAPI: { getCurrentUser: jest.fn(), getAddresses: jest.fn() },
  ordersAPI: { checkout: jest.fn(), calculate: jest.fn() },
  featuresAPI: { getStatus: jest.fn() },
  promotionsAPI: { validatePromoCode: jest.fn() },
  novaPoshtaAPI: { getCities: jest.fn(), getWarehouses: jest.fn(), calculate: jest.fn() }
}));
const { cartAPI, usersAPI, ordersAPI, featuresAPI, promotionsAPI } = require('../../services/api');

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

// Проходження майстра: контакти → доставка → оплата
const fillWizard = async ({ promoCode } = {}) => {
  render(<MemoryRouter><CheckoutPage /></MemoryRouter>);
  await waitFor(() => expect(screen.getByText('Оформлення замовлення')).toBeInTheDocument());

  // Крок 1 — контактні дані (вже заповнені з профілю)
  fireEvent.click(screen.getByRole('button', { name: 'Далі' }));

  // Крок 2 — доставка (ручний ввід, бо Nova Poshta вимкнено фічефлагом)
  fireEvent.change(screen.getByLabelText('Місто'), { target: { name: 'city', value: 'Київ' } });
  fireEvent.change(screen.getByLabelText('Відділення'), { target: { name: 'warehouseNumber', value: '12' } });
  fireEvent.click(screen.getByRole('button', { name: 'Далі' }));

  // Крок 3 — оплата та промокод
  if (promoCode) {
    fireEvent.change(screen.getByLabelText('Промокод'), { target: { name: 'promoCode', value: promoCode } });
    fireEvent.click(screen.getByRole('button', { name: 'Застосувати' }));
    await waitFor(() => expect(ordersAPI.calculate).toHaveBeenCalled());
  }
  fireEvent.click(screen.getByRole('button', { name: 'Далі' }));

  // Крок 4 — підтвердження (кнопка сабміту з'являється лише тут)
  await waitFor(() =>
    expect(screen.getByRole('button', { name: 'Оформити замовлення' })).toBeInTheDocument()
  );

  return document.querySelector('form');
};

describe('CheckoutPage', () => {
  beforeEach(() => {
    cartAPI.getCart.mockResolvedValue({ data: mockCart });
    usersAPI.getCurrentUser.mockResolvedValue({ data: mockUser });
    usersAPI.getAddresses.mockResolvedValue({ data: [] });
    featuresAPI.getStatus.mockResolvedValue({ data: { novaPoshtaEnabled: false, stripeEnabled: false } });
    ordersAPI.checkout.mockReset();
    ordersAPI.calculate.mockReset();
    promotionsAPI.validatePromoCode.mockReset();
  });

  test('оформлення із застосованим промокодом показує рядок знижки', async () => {
    promotionsAPI.validatePromoCode.mockResolvedValue({
      data: { valid: true, promotion: { name: 'KHDU10' } }
    });
    // Знижка 500 - 450 = 50 грн
    ordersAPI.calculate.mockResolvedValue({ data: { totalAmount: 450 } });
    ordersAPI.checkout.mockResolvedValue({
      data: { orderNumber: 'ORD-12345', totalAmount: 450 }
    });

    const form = await fillWizard({ promoCode: 'KHDU10' });

    // Рядок знижки дублюється у формі підтвердження та в підсумку замовлення
    expect(screen.getAllByText('Знижка').length).toBeGreaterThan(0);
    expect(screen.getAllByText('-50.00 грн').length).toBeGreaterThan(0);

    fireEvent.submit(form);

    await waitFor(() =>
      expect(screen.getByText(/Замовлення успішно оформлено!/)).toBeInTheDocument()
    );
    expect(screen.getByText(/ORD-12345/)).toBeInTheDocument();
    expect(screen.getByText(/450\.00 грн/)).toBeInTheDocument();
    expect(ordersAPI.checkout.mock.calls[0][0].promoCode).toBe('KHDU10');
  });

  test('оформлення без промокоду не показує рядок знижки', async () => {
    ordersAPI.checkout.mockResolvedValue({
      data: { orderNumber: 'ORD-67890', totalAmount: 500 }
    });

    const form = await fillWizard();

    expect(screen.queryByText('Знижка')).toBeNull();

    fireEvent.submit(form);

    await waitFor(() =>
      expect(screen.getByText(/Замовлення успішно оформлено!/)).toBeInTheDocument()
    );
    expect(screen.getByText(/ORD-67890/)).toBeInTheDocument();
    expect(ordersAPI.checkout.mock.calls[0][0].promoCode).toBeNull();
  });
});
