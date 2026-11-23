// @ts-check
const { defineConfig, devices } = require('@playwright/test');

// Окремий порт для dev-сервера E2E: на 3000 зазвичай працює docker-compose-збірка
// (nginx зі знімком старого коду), тому тести мусять піднімати актуальні джерела.
const PORT = process.env.E2E_PORT || '3210';
// Базова адреса фронтенду (E2E_BASE_URL має приоритет над локальним dev-сервером)
const BASE_URL = process.env.E2E_BASE_URL || `http://localhost:${PORT}`;
// Opt-in режим: запуск проти реального бекенду (docker-compose або dotnet run)
const REAL_MODE = process.env.E2E_REAL === '1';

module.exports = defineConfig({
  testDir: './e2e',
  outputDir: 'test-results',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 2 : undefined,
  timeout: 30_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: BASE_URL,
    locale: 'uk-UA',
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      // Основний режим: усі /api/** запити мокаються (реальний бекенд не потрібен)
      name: 'mocked',
      testIgnore: '**/real/**',
      use: { ...devices['Desktop Chrome'] },
    },
    // Повний стек — вмикається лише за E2E_REAL=1 і потребує запущеного бекенду
    ...(REAL_MODE
      ? [{
          name: 'full-stack',
          testMatch: '**/real/**/*.spec.js',
          use: { ...devices['Desktop Chrome'] },
        }]
      : []),
  ],
  webServer: {
    command: 'npm start',
    url: BASE_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 180_000,
    env: { BROWSER: 'none', PORT, CI: 'false' },
  },
});
