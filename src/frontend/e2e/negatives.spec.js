// Негативні сценарії: невірні облікові дані, недійсний промокод, нестача товару.

const { test, expect } = require('./fixtures/test');
const { loginAs, addProductToCart, openCheckoutFromCart, fillCheckoutDelivery } = require('./fixtures/actions');

const CUSTOMER = 'customer@ksu.edu.ua';

test.describe('Негативні сценарії', () => {
  test('вхід із невірним паролем', async ({ page }) => {
    await page.goto('/login');
    await page.fill('#email', CUSTOMER);
    await page.fill('#password', 'WrongPassword1');
    await page.getByRole('button', { name: 'Увійти', exact: true }).click();

    await expect(page.getByText('Невірний email або пароль')).toBeVisible();
  });

  test('недійсний промокод під час оформлення', async ({ page }) => {
    await loginAs(page, CUSTOMER);
    await addProductToCart(page, 1);
    await openCheckoutFromCart(page);
    await fillCheckoutDelivery(page);

    await page.fill('#promoCode', 'NOPE123');
    await page.getByRole('button', { name: 'Застосувати' }).click();

    await expect(page.getByText('Промокод недійсний')).toBeVisible();
  });

  test('товар без залишку та перевищення кількості у кошику', async ({ page, dialogs }) => {
    await loginAs(page, CUSTOMER);

    // Ручка з логотипом (id 4) має stock = 0 → кнопка додавання недоступна
    await page.goto('/product/4');
    await expect(page.locator('.add-to-cart-btn')).toBeDisabled();
    await expect(page.getByText('Немає в наявності').first()).toBeVisible();

    // Футболка ХДУ (id 3) має stock = 3; спроба збільшити кількість до 4 дає помилку
    await addProductToCart(page, 3);
    await page.goto('/cart');

    const plusButton = page.getByRole('button', { name: '+', exact: true });
    const quantityUpdated = () => page.waitForResponse(
      (r) => r.request().method() === 'PUT' && /\/Cart\/items\/\d+$/.test(new URL(r.url()).pathname)
    );
    // Кожен клік «+» чекаємо на завершення свого PUT, інакше запити випереджають оновлення стану
    await Promise.all([quantityUpdated(), plusButton.click()]); // 2
    await Promise.all([quantityUpdated(), plusButton.click()]); // 3
    await Promise.all([quantityUpdated(), plusButton.click()]); // 4 → перевищення залишку

    await expect.poll(() => dialogs.map((d) => d.message)).toContain('Недостатньо товару на складі');
  });
});
