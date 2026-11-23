// Сценарії зареєстрованого клієнта: реєстрація, логін, кошик, промокод,
// оформлення замовлення (накладений платіж та картка через мокований Stripe).
// Реальний backend не потрібен — усі /api/** запити обслуговує mock (див. fixtures/mockApi.js).

const { test, expect } = require('./fixtures/test');
const { loginAs, addProductToCart, openCheckoutFromCart, fillCheckoutDelivery } = require('./fixtures/actions');

const CUSTOMER = 'customer@ksu.edu.ua';

test.describe('Customer: реєстрація', () => {
  test('успішна реєстрація нового користувача та відмова для наявного email', async ({ page }) => {
    // Нова реєстрація
    await page.goto('/register');
    await page.fill('#firstName', 'Тест');
    await page.fill('#lastName', 'Тестовий');
    await page.fill('#email', 'new.user.e2e@ksu.edu.ua');
    await page.fill('#password', 'Password123!');
    await page.fill('#phone', '+380671234567');
    await page.getByRole('button', { name: 'Зареєструватися' }).click();

    // Успіх → редірект на логін із повідомленням
    await page.waitForURL('**/login');
    await expect(page.getByText('Реєстрація успішна! Тепер ви можете увійти.')).toBeVisible();

    // Повторна реєстрація з уже наявним email
    await page.goto('/register');
    await page.fill('#firstName', 'Олена');
    await page.fill('#lastName', 'Ковальчук');
    await page.fill('#email', CUSTOMER);
    await page.fill('#password', 'Password123!');
    await page.getByRole('button', { name: 'Зареєструватися' }).click();

    await expect(page.getByText('Email вже зареєстрований')).toBeVisible();
  });
});

test.describe('Customer: оформлення замовлення (COD + промокод)', () => {
  test('додає 3 товари, застосовує промокод і створює замовлення', async ({ page }) => {
    await loginAs(page, CUSTOMER);

    // Три різні товари у кошик (id 1, 2, 3)
    await addProductToCart(page, 1);
    await addProductToCart(page, 2);
    await addProductToCart(page, 3);

    // Кошик → оформлення замовлення
    await openCheckoutFromCart(page);
    await fillCheckoutDelivery(page);

    // Крок 3: застосування промокоду STUDENT10 (10%)
    await page.fill('#promoCode', 'STUDENT10');
    await page.getByRole('button', { name: 'Застосувати' }).click();
    await expect(page.getByText('Промокод застосовано').first()).toBeVisible();

    // У mocked-режимі Stripe вимкнено → доступний лише накладений платіж
    await expect(page.locator('#payment-CashOnDelivery')).toBeChecked();
    await page.getByRole('button', { name: 'Далі', exact: true }).click();

    // Крок 4: підтвердження замовлення
    await page.getByRole('button', { name: 'Оформити замовлення' }).click();
    await expect(page.getByRole('heading', { name: /Замовлення успішно оформлено/ })).toBeVisible();

    // 250 + 120 + 450 = 820 грн, знижка 10% → 738.00 грн
    await expect(page.getByText('738.00 грн')).toBeVisible();

    // Номер створеного замовлення
    const numberText = await page.locator('p', { hasText: 'Номер замовлення' }).innerText();
    const orderNumber = numberText.split(':').pop().trim();

    // Перехід у профіль → вкладка замовлень
    await page.getByRole('button', { name: 'Перейти в мої замовлення' }).click();
    await page.waitForURL('**/profile');
    await page.getByRole('button', { name: 'Замовлення', exact: true }).click();

    await expect(page.getByText(orderNumber)).toBeVisible();
    await expect(page.getByText('738.00 грн')).toBeVisible();
  });
});

test.describe('Customer: оплата карткою (мокований Stripe)', () => {
  test.use({ mockOptions: { stripeEnabled: true } });

  test('після оформлення переходить на сторінку оплати', async ({ page }) => {
    await loginAs(page, CUSTOMER);
    await addProductToCart(page, 1);
    await openCheckoutFromCart(page);
    await fillCheckoutDelivery(page);

    // Оплата карткою доступна, бо увімкнено фічефлаг Stripe
    await page.check('#payment-Card');
    await page.getByRole('button', { name: 'Далі', exact: true }).click();
    await page.getByRole('button', { name: 'Оформити замовлення' }).click();

    // Mock Stripe повертає paymentUrl '/checkout/cancel'
    await page.waitForURL('**/checkout/cancel');
    await expect(page.getByRole('heading', { name: 'Оплата скасована' })).toBeVisible();
  });
});
