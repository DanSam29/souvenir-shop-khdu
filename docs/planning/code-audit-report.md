# Етап 1.1 — Звіт аудиту поточного коду

Пов'язаний пункт плану: [development-plan.md](./development-plan.md) → `1.1. Аудит поточного коду ✅`

**Метод аудиту:** прочитано всі 16 контролерів, 7 сервісів, `Program.cs`, `AppDbContext.cs`, `App.js`, `package.json`; виконано побудову solution.

**Базовий стан компіляції:** `dotnet build khdu-souvenir-shop.sln` → 0 errors, 0 warnings.

---

## A. RBAC / [Authorize] аудит контролерів

| # | Контролер | Атрибут (рівень) | Проблеми |
|---|-----------|------------------|----------|
| 1 | AdminCategoriesController | `[Authorize(Roles="Manager,Administrator")]` (class) | Немає SuperAdmin; немає `POST reorder` (п.3.3) |
| 2 | AdminProductsController | `[Authorize(Roles="Manager,Administrator")]` (class) | Немає SuperAdmin; немає endpoints керування зображеннями (reorder/primary) |
| 3 | AnalyticsController | `[Authorize(Roles="Administrator")]` (class) | Занадто вузько: план (11.2) вимагає Manager+ та SuperAdmin |
| 4 | CartController | `[Authorize]` (class) | OK; немає bulk add, авто-видалення при qty=0 |
| 5 | CategoriesController | public GET + `[Authorize(Roles="Admin,Manager")]` на записах | **BUG:** роль `"Admin"` не існує (правильно `"Administrator"`) → endpoints недоступні; дублює AdminCategoriesController |
| 6 | CompaniesController | `[Authorize(Roles="Administrator,Manager")]` (class) | Немає SuperAdmin; немає окремих deactivate/activate (п.8.3) |
| 7 | FeaturesController | без [Authorize] | OK (публічний статус фічефлагів) |
| 8 | IntegrationsController | `[Authorize(Roles="Administrator,Manager")]` (class) | Занадто широко: план (15.1) — лише Administrator/SuperAdmin (Manager ❌) |
| 9 | NovaPoshtaController | без [Authorize] | OK (публічний) |
| 10 | OrdersController | `[Authorize]` (class) + `Administrator,Manager` на admin-методах | Немає SuperAdmin; дубль `GetAllOrders` (`/admin` і `/all`); немає state-machine валідації переходів |
| 11 | ProductsController | public GET + `Administrator,Manager` на записах | Немає SuperAdmin; дублює AdminProductsController; немає reorder/primary зображень |
| 12 | PromotionsController | public/my + `[Authorize(Roles="Administrator")]` на CRUD | Занадто вузько: план (7.6) — Manager+ та SuperAdmin; немає validate/assign/statistics |
| 13 | UniversityController | `[Authorize(Roles="Admin,Manager")]` (class) | **BUG:** роль `"Admin"` не існує → контролер недоступний; за планом — Administrator/SuperAdmin |
| 14 | UsersController | метод-рівневий | `Administrator` без SuperAdmin; **`GET /api/users/{id}` без [Authorize] → витік ПД кустомерів**; немає refresh/logout/forgot/reset/addresses |
| 15 | WarehouseDocumentsController | `[Authorize(Roles="Administrator,Manager")]` (class) | Немає SuperAdmin; немає GET by id / PUT / DELETE; немає export та low-stock |
| 16 | WebhooksController | без [Authorize] (перевірка підпису) | OK; немає ідемпотентності за EventId (п.5.2) |

**Ключові RBAC-дефекти:**
- Роль `SuperAdmin` **не використовується жодного разу** в `[Authorize]` (лише згадка в коментарі [User.cs](../src/backend/Models/User.cs)), хоча фронтенд уже включає її в `ADMIN_ROLES`.
- Хибна назва ролі `"Admin"` у [CategoriesController.cs](../src/backend/Controllers/CategoriesController.cs) (3 методи) та [UniversityController.cs](../src/backend/Controllers/UniversityController.cs) — ці endpoints не спрацюють для жодної реальної ролі.
- Публічні адмін-операції гілки без контролю ролі: `GET /api/Users/{id}`.

## B. Async / await аудит сервісів

Базово всі DB-операції сервісів виконуються через `async/await` — OK. Виявлені відхилення від плану:

| Сервіс | Проблема |
|--------|----------|
| NovaPoshtaService | `SendRawRequestAsync` без `CancellationToken`, без timeout, без Polly retry/circuit-breaker (п.6.1, 14.4) |
| UniversityService | **Стаб**, а не інтеграція: `Task.Delay(500)` + хардкод GPA 4.5 (п.9.1); немає черги/ретраїв |
| EmailService | Застарілий `SmtpClient`; немає retry, HTML-шаблонів, ідемпотентності (SentEmails), локалізації (п.10) |
| ImageService | `DeleteImageAsync` — синхронний `File.Delete`, загорнутий у `Task` (псевдо-async) |
| PaymentService | `new PromotionService(_context)` замість DI (service-locator) у `CreateCheckoutSessionForCartAsync` та `CreateOrderFromSuccessfulSession` — ламає тестованість |
| OrdersController | Виклик зовнішнього HTTP (Nova Poshta calculate) виконується **всередині відкритої DB-транзакції** |
| PaymentService | `HandleWebhookAsync` без збереження EventId → немає ідемпотентності webhook |

## C. Порожні / частково реалізовані частини

- **Repository Layer** — відсутній повністю (0 файлів).
- **DTO / AutoMapper** — відсутні; скрізь анонімні об'єкти у відповідях.
- **Відсутні сутності:** `VerificationQueue`, `SentEmail`, `RefreshToken`, `UserAddress`.
- **Background services (IHostedService):** відсутні (черга верифікацій, черга листів, expiry студентського статусу).
- **Аналітика:** лише `summary` + CSV експорт продажів (немає financial/promotions/products/categories, PDF/Excel).
- **Нотифікації:** 3 простих методи, без шаблонів/локалізації/ідемпотентності.
- **Користувачі:** немає refresh/logout/forgot-password/reset-password та адрес доставки.
- **Акції:** немає двигуна стакування за пріоритетами, `validate`, `assign personal`, статистики (частковий `PromotionService`).
- **Склад:** немає GET by id / PUT / DELETE документів, експорту (CSV/XLSX/PDF), low-stock індикаторів, `RecalculateStockAsync`.
- **Джерело істини Stock суперечливе:** `Product.Stock` змінюється в checkout (decrement), incoming (increment), outgoing (decrement) і напряму в admin product update — розбіжність із п.8.4 (Stock має випливати з накладних).
- **Тести:** існує лише `PromotionServiceTests.cs`; покриття мінімальне.

## D. Backlog технічного боргу (TD)

| ID | Пріоритет | Опис | Пов'язаний етап |
|----|-----------|------|-----------------|
| TD-01 | Critical | Виправити хибну роль `"Admin"` → `"Administrator"` (CategoriesController, UniversityController) | 1 / 14 |
| TD-02 | Critical | Закрити публічний `GET /api/Users/{id}` (додати `[Authorize(Roles="Administrator,SuperAdmin")]`) | 2 / 14 |
| TD-03 | High | Додати `SuperAdmin` до всіх адмін-контролерів згідно матриці доступів (15.1) | 15 |
| TD-04 | High | Привести матрицю RBAC контролерів у відповідність до плану (Analytics→Manager+, Integrations→Admin+, Promotions→Manager+) | 15 |
| TD-05 | High | Створити Repository Layer та перевести запити з контролерів (антипатерн "товсті контролери") | 1.2 |
| TD-06 | High | Ввести DTO Layer + AutoMapper замість анонімних об'єктів | 1.3 |
| TD-07 | High | Усунути дублювання: `GetAllOrders` (admin/all), дубль CRUD Products/Categories у двох контролерах | 3 / 4 |
| TD-08 | High | Прибрати `new PromotionService(_context)`, використати DI | 1 / 7 |
| TD-09 | Medium | Винести HTTP-виклики (Nova Poshta) з-за меж DB-транзакції в checkout | 4 |
| TD-10 | Medium | Додати timeout + CancellationToken + Polly retry/circuit-breaker для зовнішніх API | 14 |
| TD-11 | Medium | Додати ідемпотентність Stripe webhook за EventId | 5 |
| TD-12 | Medium | Уніфікувати генерацію `OrderNumber` в одному місці | 4 |
| TD-13 | Medium | Додати валідацію переходів статусів замовлення (state machine) | 4 |
| TD-14 | Medium | Узгодити джерело істини `Product.Stock` з накладними | 8 |
| TD-15 | Low | Замінити застарілий `SmtpClient`; перевести `ImageService.DeleteImageAsync` на коректний async | 10 / 3 |
