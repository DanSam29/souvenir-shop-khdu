// jest-dom adds custom jest matchers for asserting on DOM nodes.
// allows you to do things like:
// expect(element).toHaveTextContent(/react/i)
// learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom';
import { TextEncoder, TextDecoder } from 'util';
import i18n from './i18n';
import { server } from './test/server';

// msw у середовищі jsdom потребує TextEncoder/TextDecoder
if (typeof global.TextEncoder === 'undefined') global.TextEncoder = TextEncoder;
if (typeof global.TextDecoder === 'undefined') global.TextDecoder = TextDecoder;

// Перехоплення мережевих запитів axios через msw (Mock Service Worker)
beforeAll(() => server.listen({ onUnhandledRequest: 'bypass' }));
afterEach(() => {
  server.resetHandlers();
  localStorage.clear();
  sessionStorage.clear();
});
afterAll(() => server.close());

// Тести завжди виконуються українською, незалежно від мови браузера jsdom
beforeEach(async () => {
  if (i18n.language !== 'ua') {
    await i18n.changeLanguage('ua');
  }
});
