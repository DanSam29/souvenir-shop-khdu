import { rest } from 'msw';

// Базова URL backend API (та сама, що використовує services/api.js)
export const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5225/api';

// Базові хендлери msw: за замовчуванням кошик порожній.
// Конкретні тести перекривають їх через server.use(...).
export const handlers = [
  rest.get(`${API_URL}/Cart`, (req, res, ctx) =>
    res(ctx.status(200), ctx.json({ items: [], itemCount: 0, totalAmount: 0 }))
  ),
];
