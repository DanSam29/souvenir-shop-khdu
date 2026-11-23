// Сценарій адміністратора: повний життєвий цикл COD-замовлення
// Processing → Shipped (з ТТН) → Delivered, після чого оплата стає Completed.

const { test, expect } = require('./fixtures/test');
const { loginAs } = require('./fixtures/actions');

const ADMIN = 'admin@ksu.edu.ua';
const ORDER_NUMBER = 'ORD-2026-0001';
const TTN = '20450123456789';

test.describe('Admin: життєвий цикл замовлення', () => {
  test('змінює статус Processing → Shipped → Delivered і закриває оплату COD', async ({ page, dialogs }) => {
    await loginAs(page, ADMIN);
    await page.goto('/admin/orders');
    await expect(page.getByRole('heading', { name: 'Керування замовленнями' })).toBeVisible();

    const row = page.locator('tr', { hasText: ORDER_NUMBER });
    await expect(row).toBeVisible();
    await expect(row).toContainText('В обробці');

    // Processing → Shipped: обов'язкове введення ТТН у модалці
    await row.locator('select').selectOption('Shipped');
    await expect(page.locator('#order-ttn')).toBeVisible();
    await page.fill('#order-ttn', TTN);
    await page.getByRole('button', { name: 'Підтвердити' }).click();

    await expect.poll(() => dialogs.map((d) => d.message)).toContain('Статус оновлено');
    await expect(row).toContainText('Відправлено');
    await expect(row).toContainText(TTN);

    // Shipped → Delivered: для післяплати закриває оплату
    await row.locator('select').selectOption('Delivered');
    await expect(row).toContainText('Доставлено');

    // Сторінка деталей: статус оплати Completed
    await row.getByRole('button', { name: 'Деталі' }).click();
    await page.waitForURL('**/order/5001');
    await expect(page.getByText('Статус оплати')).toBeVisible();
    await expect(page.getByText('Completed')).toBeVisible();
  });
});
