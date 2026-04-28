import React from 'react';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import LoginPage from '../LoginPage';

const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

jest.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ login: jest.fn() })
}));

describe('LoginPage', () => {
  test('показує плейсхолдер email example@ksu.edu.ua', () => {
    render(<MemoryRouter><LoginPage /></MemoryRouter>);
    const input = screen.getByLabelText(/Email/i);
    expect(input).toHaveAttribute('placeholder', 'example@ksu.edu.ua');
  });
});
