// Сценарій менеджера: створення категорії, товару та прибуткової накладної,
// перевірка перерахунку поточних залишків на складі.

const { test, expect } = require('./fixtures/test');
const { loginAs } = require('./fixtures/actions');

const MANAGER = 'manager@ksu.edu.ua';
const CATEGORY_NAME = 'Тестова категорія E2E';
const PRODUCT_NAME = 'Тестовий товар E2E';

test.describe('Manager: довідники та склад', () => {
  test('створює категорію, товар і прибуткову накладну', async ({ page }) => {
    await loginAs(page, MANAGER);

    // 1. Нова категорія
    await page.goto('/admin/categories');
    await expect(page.getByRole('heading', { name: 'Керування категоріями' })).toBeVisible();
    await page.getByRole('button', { name: '+ Додати категорію' }).click();
    await page.fill('#category-name', CATEGORY_NAME);
    await page.getByRole('button', { name: 'Створити', exact: true }).click();
    await expect(page.getByText(CATEGORY_NAME)).toBeVisible();

    // 2. Новий товар у цій категорії (майстер із 3 кроків)
    await page.goto('/admin/products');
    await page.getByRole('button', { name: '+ Додати товар' }).click();
    await page.fill('#product-name', PRODUCT_NAME);
    await page.fill('#product-description', 'Опис тестового товару для E2E-перевірки');
    await page.selectOption('#product-category', { label: CATEGORY_NAME });
    await page.getByRole('button', { name: 'Далі →' }).click();
    await page.fill('#product-price', '99');
    await page.fill('#product-weight', '0.3');
    await page.fill('#product-stock', '2');
    await page.getByRole('button', { name: 'Далі →' }).click();
    await page.getByRole('button', { name: 'Створити товар' }).click();
    await expect(page.getByText(PRODUCT_NAME).first()).toBeVisible();

    // 3. Прибуткова накладна на 5 одиниць
    await page.goto('/admin/warehouse');
    await expect(page.getByRole('heading', { name: 'Склад' })).toBeVisible();
    await page.getByRole('button', { name: '+ Створити накладну' }).click();
    await page.selectOption('#incoming-product', { label: PRODUCT_NAME });
    await page.fill('#incoming-quantity', '5');
    await page.fill('#incoming-purchase-price', '10');
    await page.selectOption('#incoming-company', { label: 'ТОВ «Сувеніри Опт»' });
    await page.getByRole('button', { name: 'Створити', exact: true }).click();

    // 4. Поточні залишки: 2 (створення) + 5 (прибуткова) = 7
    await page.getByRole('button', { name: 'Поточні залишки' }).click();
    const row = page.locator('tr', { hasText: PRODUCT_NAME });
    await expect(row).toBeVisible();
    await expect(row.locator('td').nth(3)).toHaveText('7');
    await expect(row).toContainText('Достатньо');
  });
});
