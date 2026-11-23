// Спільні дії для E2E-сценаріїв (логін, кошик, оформлення замовлення).
// Використовуються разом із фікстурами з ./test (mock, dialogs).

const { expect } = require('@playwright/test');

const DEFAULT_PASSWORD = 'Password123!';

/**
 * Вхід у систему через UI. Після успішного логіну застосунок робить navigate('/').
 * @param {import('@playwright/test').Page} page
 * @param {string} email
 * @param {string} [password]
 */
async function loginAs(page, email, password = DEFAULT_PASSWORD) {
  await page.goto('/login');
  await page.fill('#email', email);
  await page.fill('#password', password);
  await page.getByRole('button', { name: 'Увійти', exact: true }).click();
  // Логін успішний -> SPA переходить на головну сторінку
  await page.waitForURL((url) => url.pathname === '/', { timeout: 15000 });
}

/**
 * Додає товар до кошика через сторінку товару (кількість за замовчуванням 1).
 * Успішне додавання показує нативний alert і перезавантажує сторінку.
 * @param {import('@playwright/test').Page} page
 * @param {number} productId
 */
async function addProductToCart(page, productId) {
  await page.goto(`/product/${productId}`);
  // ProductPage після alert(...) викликає window.location.reload(), тому дочікуємось
  // завершення перезавантаження — інакше наступна навігація обривається (net::ERR_ABORTED).
  await Promise.all([
    page.waitForEvent('load'),
    page.locator('.add-to-cart-btn').click(),
  ]);
}

/**
 * Перехід до оформлення замовлення зі сторінки кошика.
 * @param {import('@playwright/test').Page} page
 */
async function openCheckoutFromCart(page) {
  await page.goto('/cart');
  await page.getByRole('link', { name: 'Оформити замовлення' }).click();
  await page.waitForURL('**/checkout');
}

/**
 * Проходить кроки 1–2 оформлення замовлення: контакти та адреса.
 * У mocked-режимі Новa Пошта вимкнена, тому адреса вводиться вручну.
 * @param {import('@playwright/test').Page} page
 * @param {{ city?: string, warehouseNumber?: string }} [options]
 */
async function fillCheckoutDelivery(page, { city = 'Київ', warehouseNumber = '12' } = {}) {
  // Крок 1: контактні дані підставляються з профілю користувача
  await expect(page.locator('#recipientName')).not.toHaveValue('', { timeout: 10000 });
  await page.getByRole('button', { name: 'Далі', exact: true }).click();
  // Крок 2: ручний ввід адреси доставки
  await page.fill('#city', city);
  await page.fill('#warehouseNumber', warehouseNumber);
  await page.getByRole('button', { name: 'Далі', exact: true }).click();
}

module.exports = {
  loginAs,
  addProductToCart,
  openCheckoutFromCart,
  fillCheckoutDelivery,
  DEFAULT_PASSWORD,
};
