import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

jest.mock('react-router-dom', () => {
  return {
    BrowserRouter: ({ children }) => <div>{children}</div>,
    Routes: () => <div />,
    Route: () => null,
    Link: ({ children }) => <a>{children}</a>,
    useParams: () => ({})
  };
});

test('відображає заголовок ХДУ Сувеніри', () => {
  render(<App />);
  const titles = screen.getAllByText(/ХДУ Сувеніри/i);
  expect(titles.length).toBeGreaterThan(0);
});
