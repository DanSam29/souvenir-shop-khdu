const base = require('@playwright/test');
const { installMockApi } = require('./mockApi');

/**
 * Розширений test зі спільними фікстурами E2E:
 *  - mockOptions — параметри мокованого API (задаються через test.use)
 *  - mock        — встановлений мокований API (стан ізольований на тест)
 *  - dialogs     — зібрані alert/confirm (UI використовує нативні діалоги)
 */
const test = base.test.extend({
  mockOptions: [{}, { option: true }],

  // auto: true — мок-API вмикається для кожного тесту, навіть якщо фікстуру не запитано явно
  mock: [async ({ page, mockOptions }, use) => {
    // Фіксуємо українську мову інтерфейсу, щоб текстові очікування були детермінованими
    await page.addInitScript(() => {
      try { window.localStorage.setItem('i18nextLng', 'ua'); } catch { /* ignore */ }
    });
    const state = await installMockApi(page, mockOptions);
    await use(state);
  }, { auto: true }],

  dialogs: async ({ page }, use) => {
    const messages = [];
    page.on('dialog', (dialog) => {
      messages.push({ type: dialog.type(), message: dialog.message() });
      dialog.accept().catch(() => {});
    });
    await use(messages);
  },
});

const expect = base.expect;

module.exports = { test, expect };
