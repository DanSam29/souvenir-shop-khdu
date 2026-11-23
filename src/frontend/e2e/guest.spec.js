// Гостьовий сценарій: перегляд каталогу → пошук → сторінка товару.
// Реальний backend не потрібен — усі /api/** запити обслуговує mock (див. fixtures/mockApi.js).

const { test, expect } = require('./fixtures/test');

test.describe('Guest: каталог → пошук → товар', () => {
  test('перегляд каталогу, пошук за назвою та відкриття товару', async ({ page }) => {
    await page.goto('/');

    // Каталог наповнюється товарами з mock API
    await expect(page.getByText('Студентська чашка ХДУ')).toBeVisible();
    await expect(page.getByText('Футболка ХДУ')).toBeVisible();

    // Пошук за частиною назви
    await page.locator('input[aria-label="Пошук..."]').fill('Блокнот');
    await page.getByRole('button', { name: '🔍 Пошук...' }).click();

    await expect(page.getByText('Блокнот ХДУ')).toBeVisible();
    await expect(page.getByText('Студентська чашка ХДУ')).toHaveCount(0);

    // Перехід на сторінку товару зі списку
    await page.getByRole('link', { name: /Блокнот ХДУ/ }).first().click();
    await page.waitForURL('**/product/2');

    await expect(page.getByRole('heading', { name: 'Блокнот ХДУ' })).toBeVisible();
    await expect(page.locator('.add-to-cart-btn')).toBeVisible();
  });
});
