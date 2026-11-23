import { setupServer } from 'msw/node';
import { handlers } from './handlers';

// Єдиний msw-сервер для всіх тестів (перехоплює axios-запити у середовищі Node)
export const server = setupServer(...handlers);
