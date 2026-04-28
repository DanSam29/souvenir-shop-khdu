import React from 'react';
import { render, screen } from '@testing-library/react';
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

describe('ProfilePage', () => {
  test('відображає бейдж Студент при studentStatus', () => {
    render(<ProfilePage />);
    const all = screen.getAllByText('Студент');
    expect(all.length).toBeGreaterThan(0);
  });
});
