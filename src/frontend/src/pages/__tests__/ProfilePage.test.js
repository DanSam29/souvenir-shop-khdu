import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import ProfilePage from '../ProfilePage';

const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate
}));

jest.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({
    isAuthenticated: true,
    user: {
      firstName: 'Іван',
      lastName: 'Студент',
      email: 'student@ksu.edu.ua',
      role: 'Customer',
      studentStatus: 'STUDENT'
    },
    logout: jest.fn()
  })
}));

// CRA має resetMocks: true, тому імплементації задаємо у beforeEach
jest.mock('../../services/api', () => ({
  usersAPI: {
    getCurrentUser: jest.fn(),
    getAddresses: jest.fn(),
    updateProfile: jest.fn(),
    changePassword: jest.fn(),
    updateLanguage: jest.fn()
  },
  ordersAPI: { getOrders: jest.fn() },
  promotionsAPI: { getMyPromotions: jest.fn(), getMyPromotionsHistory: jest.fn() },
  novaPoshtaAPI: { getCities: jest.fn(), getWarehouses: jest.fn() }
}));
const { usersAPI, ordersAPI, promotionsAPI } = require('../../services/api');

beforeEach(() => {
  usersAPI.getCurrentUser.mockResolvedValue({
    data: {
      userId: 1,
      firstName: 'Іван',
      lastName: 'Студент',
      email: 'student@ksu.edu.ua',
      phone: '+380961234567',
      role: 'Customer',
      studentStatus: 'STUDENT'
    }
  });
  usersAPI.getAddresses.mockResolvedValue({ data: [] });
  ordersAPI.getOrders.mockResolvedValue({ data: [] });
  promotionsAPI.getMyPromotions.mockResolvedValue({ data: [] });
  promotionsAPI.getMyPromotionsHistory.mockResolvedValue({ data: [] });
});

describe('ProfilePage', () => {
  test('відображення бейджу Студент при studentStatus', async () => {
    render(<MemoryRouter><ProfilePage /></MemoryRouter>);

    expect((await screen.findAllByText('Студент')).length).toBeGreaterThan(0);
  });
});
