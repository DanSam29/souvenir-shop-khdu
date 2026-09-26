## Аналіз поточного стану

**Реалізовано:**
- ✅ Архітектурна документація (SRS, діаграми UML, вимоги) - 34 діаграми
- ✅ Бекенд: ASP.NET Core 8.0 + EF Core 8.0 + MySQL 8.0
- ✅ JWT автентифікація + RBAC (5 ролей: Guest, Customer, Manager, Administrator, SuperAdmin)
- ✅ Моделі БД (15 сутностей) + міграції + seed-дані (категорії, товари, зображення)
- ✅ Інфраструктурні компоненти: CORS, Rate Limiting, Serilog, Swagger/OpenAPI, In-Memory Cache, FluentValidation, Exception Middleware
- ✅ Soft Delete + Аудит (CreatedAt/UpdatedAt/DeletedAt + CreatedBy/UpdatedBy/DeletedBy)
- ✅ Каркас контролерів (16 контролерів) та базових сервісів (7 сервісів)
- ✅ Валідатори для сутностей (Order, Product, Promotion, User, Warehouse)
- ✅ Фронтенд: React 19 + React Router + i18n (UA/EN) + базові сторінки та роутинг
- ✅ AuthContext + ProtectedRoute з перевіркою ролей
- ✅ Docker Compose для локальної розробки + Dockerfile для API та Frontend
- ✅ BCrypt хешування паролів

**Що потребує повної реалізації:**
- ❌ Шар Repository (абстракція доступу до даних)
- ❌ DTO класи + AutoMapper маппінг
- ❌ Повна бізнес-логіка в контролерах та сервісах
- ❌ Транзакційність критичних операцій (створення замовлення, скасування)
- ❌ Повна реалізація інтеграцій (Stripe webhooks, Nova Poshta, University API, SMTP)
- ❌ Система знижок, промокодів та їх стакування з пріоритетами
- ❌ Складські операції (прибуткові/видаткові накладні, розрахунок залишків)
- ❌ Аналітичні звіти та експорт (PDF, Excel, CSV)
- ❌ Мініфікація/оптимізація фронтенду, Tailwind CSS, адаптивний дизайн
- ❌ Повна реалізація всіх UI сторінок (публічних та адмін)
- ❌ Юніт-тести, інтеграційні та E2E тести
- ❌ CI/CD пайплайн (GitHub Actions)
- ❌ Деплой на прод (Render.com) + бекапи + моніторинг

---

## Етап 1: Аудит, рефакторинг архітектури та завершення Data Access Layer
**Мета:** Привести кодову базу до єдиного стандарту, завершити архітектуру DAL та підготувати фундамент для швидкої реалізації бізнес-логіки.

1.1. Аудит поточного коду
- Перевірити всі контролери на наявність [Authorize] атрибутів та RBAC
- Перевірити сервіси на наявність асинхронних операцій (async/await)
- Визначити порожні/частково реалізовані методи
- Сформувати backlog технічного боргу

1.2. Реалізація Repository Layer
- Створити загальний інтерфейс `IRepository<T>` з базовими CRUD операціями
- Створити `Repository<T>` generic реалізацію на основі DbSet
- Створити спеціалізовані репозиторії:
  - IUserRepository / UserRepository
  - IProductRepository / ProductRepository
  - ICategoryRepository / CategoryRepository
  - ICartRepository / CartRepository
  - IOrderRepository / OrderRepository
  - IPaymentRepository / PaymentRepository
  - IShippingRepository / ShippingRepository
  - IPromotionRepository / PromotionRepository
  - IWarehouseRepository / WarehouseRepository
  - ICompanyRepository / CompanyRepository
  - IAnalyticsRepository / AnalyticsRepository
- Зареєструвати репозиторії в DI контейнер (Scoped)

1.3. DTO Layer та AutoMapper
- Встановити NuGet пакет AutoMapper та AutoMapper.Extensions.Microsoft.DependencyInjection
- Створити папку DTOs з підпапками:
  - Requests (вхідні DTO)
  - Responses (вихідні DTO)
  - Admin (адміністративні DTO)
- Створити MappingProfile з усіма конфігураціями маппінгу
- Зареєструвати AutoMapper в Program.cs

1.4. Додаткові NuGet пакети
- Встановити:
  - EPPlus (Excel export)
  - iTextSharp / iText7 (PDF export)
  - CsvHelper (CSV export)
  - Minio.Client (MinIO S3 storage)
  - AutoMapper
  - xUnit, Moq, FluentAssertions (тести)

1.5. Seed-додаткові дані
- Ролі користувачів (окрема таблиця AspNetRoles або вбудована в User)
- Тестові користувачі: superadmin@ksu.edu.ua, admin@ksu.edu.ua, manager@ksu.edu.ua, customer@ksu.edu.ua, student@ksu.edu.ua
- Тестові промоакції та промокоди
- Тестові компанії-постачальники
- Тестові складські документи

**Критерій готовності:** Всі запити до БД йдуть через Repository Layer; усі DTO визначені; AutoMapper працює; seed-дані створюють корисне тестове середовище; компіляція без warnings.

---

## Етап 2: User Module - Повна реалізація користувачів та автентифікації
**Мета:** Завершити систему реєстрації, авторизації, профілів та керування користувачами.

2.1. Автентифікація та регістрація
- UsersController:
  - POST /api/users/register - реєстрація з валідацією (FluentValidation), BCrypt хешування
  - POST /api/users/login - видача JWT + refresh token
  - POST /api/users/refresh - оновлення access токену
  - POST /api/users/logout - відкликання refresh токену
  - POST /api/users/forgot-password - запит на скидання паролю
  - POST /api/users/reset-password - скидання паролю
- Валідація: унікальність email, формат email, складність паролю, порівняння ConfirmPassword
- Автоматичне створення кошика для нового користувача
- Автоматична перевірка email на домен @ksu.edu.ua / @student.ksu.edu.ua та запуск верифікації (Етап 9)

2.2. Особистий кабінет (Customer)
- GET /api/users/me - поточний профіль
- PUT /api/users/me - оновлення профілю (FirstName, LastName, Phone)
- POST /api/users/change-password - зміна паролю (зі старого паролю)
- GET /api/users/me/orders - історія замовлень (пагінація, фільтри)
- GET /api/users/me/orders/{id} - деталі замовлення з історією
- GET /api/users/me/addresses - збережені адреси доставки
- POST /api/users/me/addresses - додати адресу
- PUT /api/users/me/addresses/{id} - редагувати адресу
- DELETE /api/users/me/addresses/{id} - видалити адресу
- POST /api/users/me/addresses/{id}/default - встановити за замовчуванням
- GET /api/users/me/promotions - доступні персональні/студентські знижки
- GET /api/users/me/promotions/history - історія використаних знижок

2.3. Керування користувачами (Admin/SuperAdmin)
- GET /api/admin/users - список користувачів (пагінація, фільтри по ролі/статусу/пошук)
- GET /api/admin/users/{id} - деталі профілю
- GET /api/admin/users/{id}/orders - замовлення користувача
- PUT /api/admin/users/{id}/role - зміна ролі (SuperAdmin -> Admin -> Manager -> Customer)
- PUT /api/admin/users/{id}/block - блокування (IsLocked = true)
- PUT /api/admin/users/{id}/unblock - розблокування
- POST /api/admin/users/{id}/reset-password - скидання паролю адміністратором

2.4. Frontend - користувацькі сторінки
- LoginPage - форма входу, перемикання UA/EN, перехід до реєстрації, Remember Me
- RegisterPage - форма реєстрації з валідацією, перемикання UA/EN
- ProfilePage - вкладки:
  - Профіль (редагування особистих даних)
  - Безпека (зміна паролю)
  - Замовлення (історія з прогрес-баром статусів)
  - Адреси (керування адресами)
  - Знижки (доступні + історія)
  - Студентський статус (покази статусу, кнопка оновлення)
- OrderDetailsPage - деталі замовлення, трекінг, позиції, знижки

**Критерій готовності:** Весь життєвий цикл користувача працює; ролі розмежовані; refresh токени працюють; профіль редагується; валідація на всіх формах.

---

## Етап 3: Catalog Module - Каталог, товари, категорії, зображення, пошук
**Мета:** Повна реалізація публічного каталогу та адмін-керування асортиментом.

3.1. Категорії (публічні)
- CategoriesController:
  - GET /api/categories - дерево категорій (з підкатегоріями)
  - GET /api/categories/{id} - деталі категорії
  - GET /api/categories/{id}/products - товари категорії (пагінація, сортування, фільтри)

3.2. Товари (публічні)
- ProductsController:
  - GET /api/products - список товарів (пагінація pageSize=20, сортування price/name/newest/popular, фільтри minPrice/maxPrice/categoryId/inStock)
  - GET /api/products/{id} - деталі товару (з зображеннями, характеристиками, пов'язаними)
  - GET /api/products/search - full-text пошук по назві та опису (EF.Functions.FreeText)
  - GET /api/products/featured - рекомендовані/популярні товари
  - GET /api/products/new - новинки (останні додані)
  - Валідація: існування товару, обробка soft-deleted

3.3. Admin: Categories
- AdminCategoriesController (Authorize(Roles = "Manager,Administrator,SuperAdmin")):
  - CRUD для категорій
  - POST /api/admin/categories - створити (з перевіркою ParentCategoryId існування)
  - PUT /api/admin/categories/{id} - редагувати
  - DELETE /api/admin/categories/{id} - видалити (тільки якщо порожня; інакше помилка Conflict)
  - POST /api/admin/categories/reorder - зміна порядку відображення

3.4. Admin: Products
- AdminProductsController:
  - CRUD для товарів
  - POST /api/admin/products - створити
  - PUT /api/admin/products/{id} - редагувати
  - DELETE /api/admin/products/{id} - soft delete (тільки якщо немає в активних замовленнях)
  - POST /api/admin/products/{id}/images - завантажити зображення (IFormFile, валідація розміру/формату jpg/png/webp ≤5MB)
  - PUT /api/admin/products/{id}/images/reorder - змінити порядок
  - PUT /api/admin/products/{id}/images/{imageId}/primary - встановити головне
  - DELETE /api/admin/products/{id}/images/{imageId} - видалити зображення (і з диска/MinIO)
  - Інтеграція з ImageService (збереження до wwwroot або MinIO)
  - Інвалідація кешу каталогу при зміні товарів/категорій

3.5. ImageService - управління зображеннями
- Валідація MIME типу та розміру файлу
- Зміна розміру (опціонально) через System.Drawing або SixLabors.ImageSharp
- Генерація унікального імені файлу (GUID)
- Збереження в wwwroot/images/products/YYYY/MM/
- Повернення публічного URL
- Видалення фізичного файлу при видаленні запису

3.6. Кешування каталогу
- Всі публічні GET запити кешувати через IMemoryCache з TTL:
  - Список категорій - 5 хв
  - Список товарів - 3 хв
  - Деталі товару - 2 хв
  - Пошук - 1 хв
- Інвалідація при зміні через Admin контролери

3.7. Frontend - Публічний каталог
- HomePage: hero секція, featured товари, новинки, категорії
- ProductList: фільтри (ціна, категорія, наявність), сортування, пагінація, сітка товарів
- ProductPage: галерея зображень, опис, ціна, наявність, характеристики, пов'язані товари, кнопка "До кошика", селектор кількості
- Пошукова форма у Header з автодоповненням
- Адаптивний дизайн: grid 4/3/2/1 колонка для desktop/tablet/mobile

3.8. Frontend - Адмін каталог
- CategoriesAdmin: дерево категорій, drag-and-drop reorder, create/edit/delete модалки
- ProductsAdmin: таблиця товарів, фільтри, bulk delete, create/edit product wizard, керування зображеннями (завантаження, drag-to-sort, призначення головного)
- Перевірка ролей перед показом функціоналу

**Критерій готовності:** Каталог відображається; пошук/фільтри працюють; адмін може керувати категоріями та товарами; зображення завантажуються; кешування працює; зменшення на складі резервується через замовлення (не через ручне редагування).

---

## Етап 4: Cart Module та Order Module - Кошик, замовлення, транзакції
**Мета:** Реалізувати повний цикл від кошика до успішного оформлення замовлення з атомарними транзакціями.

4.1. Кошик (CartController)
- GET /api/cart - отримати кошик поточного користувача (зі знижками на позиціях)
- POST /api/cart/items - додати товар (productId, quantity)
- PUT /api/cart/items/{id} - змінити кількість
- DELETE /api/cart/items/{id} - видалити позицію
- DELETE /api/cart - очистити кошик
- POST /api/cart/items/bulk - масове додавання
- Автоматичне видалення позиції при quantity=0
- Валідація: quantity > 0, товар існує, stock достатній (warehouse available)

4.2. Оформлення замовлення (OrdersController)
- POST /api/orders - транзакційне створення замовлення:
  1. Валідувати кошик (не порожній, stock доступний, промокод валідний)
  2. Почати транзакцію (DbContext.Database.BeginTransactionAsync)
  3. Перевірити всі знижки (промо, персональні, студентські) - виклик PromotionService
  4. Розрахувати OrderItems: OriginalPrice, AppliedPromotionId, DiscountAmount, FinalPrice
  5. Створити Order (orderNumber = KSU-{yyyyMMdd}-{000000}, status = Processing)
  6. Створити Shipping (адреса, місто, відділення NP, trackingNumber = null)
  7. Створити Payment (method = Card/COD, status = Pending, amount)
  8. Створити OrderHistory (status = Processing, comment = "Замовлення створено")
  9. Створити OutgoingDocument(s) reason=ORDER для кожної позиції зі знижками
  10. Зменшити Products.Stock
  11. Збільшити Promotion.CurrentUsage, UserPromotion.UsedCount
  12. Очистити кошик
  13. Якщо Card → виклик PaymentService.CreateCheckoutSession, повернути paymentUrl
  14. Якщо COD → Payment.Status = Pending, Order.Status = Processing
  15. Зафіксувати транзакцію (CommitAsync)
  16. Відправити email підтвердження (background / queue)
- Повернути: orderId, orderNumber, paymentUrl (якщо Card), totalAmount
- Валідації:
  - Суми співпадають (розрахована сервером, не довіряємо клієнту)
  - Доставка: існування міста та відділення
  - Телефон: формат +380XXXXXXXXX

4.3. Скасування замовлення
- POST /api/orders/{id}/cancel - доступно Customer (тільки Processing) та Admin (будь-який до Delivered)
- Транзакція:
  1. Перевірити поточний статус
  2. Якщо Payment.Status = Completed (оплачено) → виклик Stripe Refund
  3. Видалити OutgoingDocument(s) reason=ORDER
  4. Повернути Products.Stock
  5. Order.Status = Cancelled
  6. Додати OrderHistory (Cancelled, причина скасування)
  7. Payment.Status = Refunded / Failed
  8. Відправити email про скасування

4.4. Адмін: Зміна статусів замовлення
- PUT /api/admin/orders/{id}/status (Authorize: Manager, Administrator, SuperAdmin):
  - Processing → Shipped: обов'язкове поле trackingNumber (ТТН Nova Poshta), запис у Shipping.trackingNumber, OrderHistory.comment = ТТН
  - Shipped → Delivered: якщо Payment.Method=COD → Payment.Status=Completed, додати OrderHistory
  - Будь-який → Cancelled: див. 4.3
  - Valid transitions: Processing → Shipped → Delivered, Processing → Cancelled, Shipped → Cancelled
- GET /api/admin/orders - список (пагінація, фільтр status/date/paymentMethod/search по orderNumber/email)
- GET /api/admin/orders/{id} - деталі з історією

4.5. Order State Machine (валідатор переходів)
- Створити enum OrderStatus: Pending, Processing, Shipped, Delivered, Cancelled, Refunded
- Перевірка дозволених переходів в OrderService:
  - Pending → Processing, Cancelled
  - Processing → Shipped, Cancelled
  - Shipped → Delivered, Cancelled
  - Delivered → (термінальний, тільки Return process)

4.6. Frontend
- CartPage: список позицій, зміна кількості, видалення, проміжні суми, знижки, total, кнопка "До оформлення"
- CheckoutPage:
  - Крок 1: Контактні дані (автозаповнена з профілю)
  - Крок 2: Доставка (автокомплит міста → відділення NP; збережені адреси; вартість доставки; орієнтовні дні)
  - Крок 3: Оплата (Card / COD; дані картки через Stripe Elements для Card)
  - Крок 4: Підтвердження (деталізація: товари, знижки, доставка, total; кнопка "Оформити")
  - Валідація всіх кроків
  - Перенаправлення на Stripe Checkout для Card
- PaymentSuccessPage: повідомлення про успіх, номер замовлення, посилання на деталі
- PaymentCancelPage: повідомлення про скасування, кнопка повернення до кошика
- OrderDetailsPage (Customer): прогрес-бар Processing → Shipped → Delivered, ТТН, деталі, товари

**Критерій готовності:** Кошик додає/змінює/видаляє позиції; оформлення замовлення атомарне; транзакції не залишають неконсистентних даних; скасування повертає stock; статуси змінюються тільки за дозволеними переходами; email надходить.

---

## Етап 5: Payment Module - Stripe, Webhooks, Refund, COD
**Мета:** Інтегрувати повноцінну оплату через Stripe з підтримкою всіх сценаріїв.

5.1. Налаштування Stripe
- Установка Stripe.net (вже є в csproj)
- Конфігурація в appsettings.json: SecretKey, PublishableKey, WebhookSecret, SuccessUrl, CancelUrl
- PaymentService реалізація:
  - CreateCheckoutSessionAsync(orderId, orderNumber, totalAmount, currency=UAH, customerEmail)
  - CreateRefundAsync(paymentIntentId, amount) - частковий/повний рефанд
  - ConstructEventAsync(payload, signatureHeader) - перевірка підпису webhook

5.2. Webhook (WebhooksController)
- POST /api/webhooks/stripe:
  1. Перевірити підпис через StripeEventUtility.ConstructEvent (WebhookSecret)
  2. Ідемпотентність: перевірити, чи вже оброблено цей EventId
  3. Обробити події:
     - payment_intent.succeeded: Payment.Status = Completed, Order.Status = Processing (якщо ще не), додати OrderHistory, надіслати email "Оплата пройшла успішно"
     - payment_intent.payment_failed: Payment.Status = Failed, Order.Status = Cancelled, повернути stock, видалити OutgoingDocument, надіслати email "Оплата не вдалася"
     - charge.refunded: Payment.Status = Refunded, додати OrderHistory
  4. Повернути 200 OK якнайшвидше; обробку в фоні якщо потрібно
- Обробка помилок webhook: логування, але повернення 200, щоб Stripe не ретрайв

5.3. Payment States
- enum PaymentStatus: Pending, RequiresAction, Succeeded, Failed, Cancelled, Refunded
- enum PaymentMethod: Card, CashOnDelivery
- Валідація: сума Payment.Amount == Order.TotalAmount + DeliveryCost

5.4. Cash-on-Delivery (COD)
- При зміні Order.Status → Delivered (адміном):
  - Якщо Payment.Method == COD і Payment.Status != Completed
  - Payment.Status = Completed
  - Payment.CompletedAt = DateTime.UtcNow
  - OrderHistory.comment = "Оплата при отриманні прийнята"

5.5. Тестування Stripe
- Sandbox ключі для локального розробки
- Stripe CLI для ретрансляції webhook: `stripe listen --forward-to localhost:5000/api/webhooks/stripe`
- Тестові сценарії: успішний платіж, відхилений платіж, відміна на сторінці Stripe, refund

5.6. Frontend
- CheckoutPage: радиокнопки Card vs COD
- Якщо Card → показати Stripe Elements або редірект на Hosted Checkout Page
- Success/Cancel сторінки очікують колбек, потім відображають статус
- Індикація сповіщень toast

**Критерій готовності:** Успішна оплата карткою через Stripe Sandbox змінює статуси; відхилений платіж скасовує замовлення; refund працює; webhooks обробляються з ідемпотентністю; COD коректно завершується при Delivered; всі сценарії працюють через Stripe CLI.

---

## Етап 6: Shipping Module - Nova Poshta API, адреси, розрахунок вартості
**Мета:** Інтегрувати Nova Poshta для вибору відділень та розрахунку доставки.

6.1. NovaPoshtaService
- Методи:
  - GetCitiesAsync(string? search) - список міст (пошук по назві)
  - GetWarehousesAsync(string cityRef, string? search) - список відділень у місті
  - CalculateDeliveryCostAsync(string cityRef, string warehouseRef, decimal totalWeight) - розрахунок вартості
  - GetTrackingStatusAsync(string trackingNumber) - відстеження ТТН
- Кешування:
  - Cities → 24 години (дуже рідко змінюються)
  - Warehouses(per city) → 6 годин
  - Розрахунок → не кешувати (динамічний)
- Rate Limiting: 50 запитів/хвилину (обмеження API NP)
- Retry + Polly policy (3 рази з експоненційним відкатом)
- Graceful fallback: якщо API недоступний → дозволити ручний ввод міста та відділення

6.2. NovaPoshtaController (public)
- GET /api/np/cities?search=Херсон → JSON список
- GET /api/np/warehouses?cityRef=...&search=2 → JSON список
- POST /api/np/calculate → {cityRef, warehouseRef, weight} → {cost, estimatedDays}

6.3. Shipping Model completion
- Shipping entity: cityRef, cityName, warehouseRef, warehouseNumber, warehouseAddress, deliveryCost, estimatedDays, trackingNumber, deliveryMethod = NovaPoshta
- Валідація: всі поля для NP заповнені, deliveryCost ≥ 0

6.4. Адмін: поле ТТН
- Під час зміни статусу на Shipped обов'язкове поле trackingNumber
- Збереження в Shipping.trackingNumber
- Коментар в OrderHistory: "Відправлено Новою Поштою. ТТН: {trackingNumber}"

6.5. Frontend - Checkout delivery step
- Autocomplete input "Місто" (debounce 300ms, виклик GET /api/np/cities)
- Після вибору міста → dropdown "Відділення" (GET /api/np/warehouses, пошук по номеру/адресі)
- Відображення вартості доставки та орієнтовних днів негайно після вибору
- Fallback: ручний ввод, якщо повертається помилка API
- У профілі: збережені адреси містяться з cityRef + warehouseRef

6.6. OrderDetails / AdminOrders - Tracking
- Посилання на відстеження Nova Poshta за ТТН
- (Опціонально) Виклик трекінгу API та відображення статусу

**Критерій готовності:** Список міст/відділень підвантажується; вартість розраховується з вагою; кеш працює; fallback працює; ТТН зберігається при Shipped; трекінг відображається.

---

## Етап 7: Promotion Module - Знижки, промокоди, стакування, пріоритети
**Мета:** Реалізувати повноцінний двигун застосування знижок за усіма правилами з документації.

7.1. Promotion Model (розширити якщо потрібно)
- Fields: PromotionId, Name, NameEn, Description, DescriptionEn, Type (Percent/FixedAmount/SpecialPrice), Value, TargetType (Product/Category/Cart/Shipping), TargetId (nullable), AudienceType (All/Students/Staff/Alumni/Custom/RegularScholarship/HighAchiever), StartDate (nullable), EndDate (nullable), PromoCode (unique, nullable), MinOrderAmount, MinQuantity, Priority (0-100, default 50), UsageLimit (nullable), CurrentUsage, IsActive, CreatedAt, CreatedBy

7.2. PromotionService - Двигун застосування
- Методи:
  - GetApplicablePromotionsAsync(userId, cartItems) - збирає всі можливі знижки
  - ValidatePromoCodeAsync(code, userId, cartItems) → {valid, error, promotion}
  - CalculateFinalPricesAsync(userId, cartItems, promoCode) → результат з OriginalPrice/AppliedPromotionId/DiscountAmount/FinalPrice для кожної позиції + обща знижка + subtotal+total
  - AssignPersonalPromotionAsync(userId, promotionId) - призначення персональної
  - AssignStudentPromotionsAsync(userId, studentStatus) - REGULAR/SCHOLARSHIP/HIGH_ACHIEVER
  - GetPromotionStatisticsAsync(promotionId) - статистика використання

7.3. Логіка приоритету при стакуванні:
1. Персональні (Audience=Custom)
2. Студентські (Audience=Students, RegularScholarship, HighAchiever)
3. Товарні (Target=Product) та Категорійні (Target=Category)
4. Загальні (Audience=All)
5. Промокоди (Apply promoCode overlay)
- При однаковому пріоритеті → вибрати найбільшу знижку
- Обмеження: FinalPrice ≥ 0

7.4. Промокоди
- Валідації (у такій послідовності):
  1. Існування (Promotions.Any(p => p.PromoCode == code))
  2. IsActive == true
  3. StartDate ≤ Now ≤ EndDate
  4. CurrentUsage < UsageLimit (якщо задано)
  5. cartTotal ≥ MinOrderAmount
  6. totalQuantity ≥ MinQuantity
- Помилки локалізовані: "Промокод не знайдено", "Термін дії закінчився", "Промокод вичерпано", "Мінімальна сума: 500 грн"
- Після успішного оформлення замовлення → CurrentUsage++

7.5. Persistence аудиту
- OrderItems зберігають: OriginalPrice, AppliedPromotionId, DiscountAmount, FinalPrice
- OutgoingDocument(reason=ORDER) копіює ці ж поля для фінансової звітності

7.6. PromotionsController + Admin
- Публічні:
  - GET /api/promotions/active - активні загальні акції (для сторінки "Акції")
  - POST /api/promotions/validate - валідація промокоду при checkout
- Admin (Manager+):
  - CRUD Promotion
  - POST /api/admin/promotions/{id}/assign/{userId} - персональне призначення
  - GET /api/admin/promotions/{id}/statistics - статистика ROI
  - Експорт статистики у CSV/PDF

7.7. Frontend
- CheckoutPage: поле "Промокод" з кнопкою "Застосувати" → виклик validate → оновлення тоталів
- Сторінка "Акції" - список активних промо
- Admin Promotions page (додати в адмін меню): CRUD, статистика, призначення персональних
- У деталях замовлення - список застосованих знижок + промокод

**Критерій готовності:** Усі типи знижок застосовуються за пріоритетом; промокоди валідуються за 6+ правилами; стакування дає коректну фінальну ціну; CurrentUsage не перевищує ліміти; статистика рахується.

---

## Етап 8: Warehouse Module - Склад, накладні, компанії, залишки
**Мета:** Автоматизувати складський облік: прибуткові/видаткові документи, компанії, залишки, low-stock алерти.

8.1. Сутності
- IncomingDocument: IncomingId, ProductId (single, або перейти на line-items?), Quantity, PurchasePrice (decimal), Reason (INCOMING/RETURN), CompanyId (nullable, null для RETURN), CreatedBy, CreatedAt, Notes
- OutgoingDocument: OutgoingId, ProductId, Quantity, OriginalPrice, AppliedPromotionId (nullable), DiscountAmount, FinalPrice, Reason (ORDER/WRITE_OFF/INVENTORY), OrderId (nullable), CompanyId (nullable), CreatedBy, CreatedAt, Notes
- Company: CompanyId, Name, NameEn, Email, Phone, Address, ContactPerson, IsActive, CreatedAt, DeletedAt/DeletedBy/IsDeleted (soft delete)

8.2. WarehouseDocumentsController (Manager+)
- Incoming:
  - GET /api/admin/incoming - список (фільтри date/product/company, пагінація)
  - GET /api/admin/incoming/{id} - деталі
  - POST /api/admin/incoming - створити (companyId обов'язковий якщо Reason != RETURN; деактивована компанія заборонена)
  - PUT /api/admin/incoming/{id} - редагувати
  - DELETE /api/admin/incoming/{id} - видалити (повертати stock назад)
- Outgoing:
  - GET /api/admin/outgoing - список
  - GET /api/admin/outgoing/{id} - деталі
  - POST /api/admin/outgoing - створити (вручну, reason=WRITE_OFF/INVENTORY; ORDER - тільки автоматично при оформленні)
  - PUT /api/admin/outgoing/{id}
  - DELETE /api/admin/outgoing/{id} - повернути stock

8.3. CompaniesController (Manager+)
- CRUD компанії
- Додатково:
  - POST /api/admin/companies/{id}/deactivate - м'яка деактивація
  - POST /api/admin/companies/{id}/activate
- Валідація: унікальність Name, Email
- Заборона: в новому IncomingDocument не можна вибрати деактивовану компанію

8.4. Розрахунок залишків (не дублювати в Product.Stock як джерело істини, але Product.Stock для швидкого відображення)
- Метод WarehouseService.RecalculateStockAsync(productId) = SUM(Incoming.Qty) - SUM(Outgoing.Qty)
- При кожному документі - автоматичне оновлення Product.Stock через тригер/метод
- Low stock alert: Products.Stock <= 5 (налаштовується) → відмітка в адмін-панелі

8.5. Поточні залишки / Export
- GET /api/admin/stock/current - агреговані залишки по всіх товарах: productId, productName, totalIncoming, totalOutgoing, currentStock, lowStock(bool)
- GET /api/admin/stock/export?format=csv|xlsx|pdf - експорт

8.6. Frontend - AdminWarehousePage
- Таби: Прибуткові / Видаткові / Поточні залишки / Компанії
- Прибуткові: create/edit/delete модалка, вибір компанії, товару, кількості, закупівельної ціни, причина
- Видаткові: create/edit/delete, причина (WRITE_OFF/INVENTORY), товар, кількість
- Залишки: таблиця з low-stock індикаторами, фільтр lowStock only, кнопки Export CSV/XLSX/PDF
- Компанії: CRUD, activate/deactivate toggle

**Критерій готовності:** Документи створюються і видаляються; залишки автоматично перераховуються; компанії деактивуються; low-stock помічений; експорт трьох форматів працює.

---

## Етап 9: University Module - Верифікація студентів, черга, ручні дії
**Мета:** Автоматична верифікація студентів через University API з retry чергою та ручним керуванням.

9.1. UniversityService
- VerifyStudentByEmailAsync(email, userId) - виклик зовнішнього API
  - Перевіряє домен email
  - HTTP GET з Authorization Bearer UNIVERSITY_API_KEY
  - Timeout 5 сек
  - Маппинг response.studentStatus + GPA → наш enum
  - Призначення знижок: REGULAR (10%), SCHOLARSHIP (15%), HIGH_ACHIEVER (GPA≥4.5) (20%) (приклади)
  - StudentExpiresAt = Now + 4 місяці
  - Надсилання email про присвоєння знижок
- Обробка недоступності:
  - Якщо API повертає помилку/timeout → створити запис VerificationQueue з retryAfter=1 година
  - Background service (IHostedService) що раз на 5 хвилин перевіряє чергу і ретраїть

9.2. VerificationQueue entity (додати в БД міграцією)
- QueueId, UserId, Email, Attempts, NextAttemptAt, LastError, Status (Pending/Processing/Completed/Failed)
- Створити міграцію #2

9.3. UniversityController
- User:
  - POST /api/users/me/student-status/refresh - ручне оновлення (кнопка у профілі)
- Admin (SuperAdmin/Administrator):
  - GET /api/admin/university/verifications - черга верифікацій + історія
  - POST /api/admin/university/verifications/{id}/approve - ручна верифікація
  - POST /api/admin/university/verifications/{id}/reject - відхилення
  - POST /api/admin/university/verifications/{id}/retry - запустити повторно
  - POST /api/admin/users/{id}/student-extend - продовжити термін ще на 4 місяці
- Integrations status dashboard:
  - GET /api/integrations/status → { Stripe: "ok", NovaPoshta: "ok", University: "degraded", Smtp: "ok" }

9.4. Студентський статус expiry
- Background service що раз на день перевіряє User.StudentExpiresAt < Today
- Для протермінованих → StudentStatus = NONE
- Надсилання email нагадування за тиждень до expiry
- В профілі показувати "Закінчується через N днів"

9.5. Stub / Mock University API для тестування
- У Development режимі, якщо UNIVERSITY_API_KEY не заданий → використовувати stub, що повертає випадкові дані або по сутності email:
  - student.regular@ → REGULAR
  - student.scholarship@ → SCHOLARSHIP
  - student.high@ → HIGH_ACHIEVER
  - notfound@ → Not Found

**Критерій готовності:** Реєстрація з @ksu.edu.ua запускає верифікацію; retry черга працює; ручні approve/reject працюють; expiry деактивує статус; Stub API дозволяє протестувати всі сценарії локально без реального API.

---

## Етап 10: Notification Module (Email) - Шаблони, відправка, логування
**Мета:** Надсилання email-сповіщень у всіх ключових точках життєвого циклу.

10.1. EmailService
- Імплементація через SmtpClient (Gmail SMTP / SendGrid / Brevo)
- Конфігурація: Smtp__Host, Smtp__Port, Smtp__Username, Smtp__Password, Smtp__EnableSsl, Smtp__FromAddress
- Метод SendEmailAsync(to, subject, htmlBody, cc?, bcc?)
- Retry policy: 3 рази
- Захист від дублювань: лог кожного відправлення (SentEmails таблиця з recipient+templateType+referenceId)
- Валідація email формату

10.2. SentEmails таблиця (мініграція #3)
- SentEmailId, Recipient, Subject, TemplateType, ReferenceId, SentAt, Status, ErrorMessage
- Унікальний індекс (TemplateType, ReferenceId) для ідемпотентності

10.3. HTML Шаблони (папка EmailTemplates)
- Base layout (header з логотипом ХДУ, footer, кольори бренду)
- Шаблони:
  - RegisterConfirm - вітаємо в системі
  - StudentVerified - студентський статус підтверджено, доступні знижки X%
  - OrderConfirmation - замовлення #{orderNumber} оформлено (деталізація: позиції, ціни, знижки, доставка, total, лінк на замовлення)
  - OrderPaymentSuccess - оплата успішна
  - OrderPaymentFailed - оплата не вдалася
  - OrderShipped - відправлено NP, ТТН
  - OrderDelivered - доставлено
  - OrderCancelled - скасовано (причина, якщо оплачено - рефанд)
  - StudentStatusExpiringSoon - закінчується через тиждень
  - StudentStatusExpired - закінчився, натисніть оновити
  - UserPromotionAssigned - призначено персональну знижку
  - PasswordReset - посилання на скидання

10.4. Локалізація шаблонів - UA та EN
- Шаблони в 2 мовних варіантах або з токенами
- Вибір мови за налаштуваннями користувача

10.5. Логування
- Serilog: Information для успішних відправлень, Error для помилок зі stack trace
- Адмін панель - інтеграційний дашборд: к-сть відправлених за останню годину/добу

10.6. (Опціонально) Фонова обробка
- Background service, що зчитує з черги повідомлень (ConcurrentQueue або MediatR) і відправляє асинхронно, щоб не блокувати API response

**Критерій готовності:** У всіх ключових подіях надходять листи з коректними даними; дублювання не надходять; SMTP credentials працюють; шаблони з UA/EN локалізацією.

---

## Етап 11: Analytics Module - Дашборди, метрики, експорт звітів
**Мета:** Реалізувати аналітику продажів, фінанси, ефективність акцій.

11.1. Метрики (з DTO)
- SalesMetrics: totalOrders, totalRevenue, averageOrderValue, ordersCountByStatus
- TimeSeries: salesByDay (date → revenue), salesByMonth
- TopProducts: productId, name, quantitySold, revenue
- TopCategories: categoryId, name, revenue
- PaymentStats: cardCount, codCount, cardPercent
- PromotionStats: promotionId, name, uses, totalDiscountAmount, revenue, ROI = (revenue_with - revenue_without) / totalDiscount
- Financial: totalRevenue, totalPurchaseCost (з Incoming), grossProfit, margin = profit/revenue

11.2. AnalyticsController (Manager+)
- GET /api/analytics/sales?from=2026-09-01&to=2026-09-30&groupBy=day|month
- GET /api/analytics/products?from=...
- GET /api/analytics/categories?from=...
- GET /api/analytics/promotions?from=...
- GET /api/analytics/financial?from=...
- GET /api/analytics/overview?from=... - короткий summary для головного дашборду

11.3. Оптимізації
- Всі складні агрегати кешувати з TTL 15-30 хвилин
- Інвалідація кешу при створенні/зміні замовлень/документів
- Індекси в БД: Orders(CreatedAt, Status), OrderItems(ProductId, AppliedPromotionId)

11.4. ReportExportService
- Експорт у 3 формати:
  - CSV (CsvHelper)
  - Excel XLSX (EPPlus) - з форматуванням, заголовками, сумами, графіками
  - PDF (iText7) - таблиці, заголовки, графік (згенерований як image через Chart.js serverside або ZXing)
- Endpoint: GET /api/analytics/export?type=sales|products|promotions|financial&format=csv|xlsx|pdf&from=...&to=...

11.5. Frontend AdminAnalyticsPage
- Overview дашборд: KPI картки (Total Revenue, Orders, AOV, Margin), графік продажів за період, фільтр періоду (Today, Yesterday, Last 7 Days, Last 30 Days, Custom Range)
- Таби: Продажі / Товари / Категорії / Акції / Фінанси
- Графіки: Line chart for sales by date, Bar chart for top products/categories, Pie chart for payment methods
- Таблиця метрик з пагінацією та сортуванням
- Кнопки Export: CSV / Excel / PDF

**Критерій готовності:** Дащборд показує коректні цифри; фільтр періоду працює; експорт 3 форматів генерує валідні файли; метрики кешовані.

---

## Етап 12: Frontend UX, Tailwind CSS, адаптивність, локалізація
**Мета:** Привести інтерфейс до сучасного стану, забезпечити зручність, поліглотність та адаптив.

12.1. Tailwind CSS Integration
- Встановити tailwindcss, postcss, autoprefixer: npm install -D tailwindcss@3 postcss autoprefixer
- Налаштувати tailwind.config.js, postcss.config.js
- Додати directives в index.css: @tailwind base; @tailwind components; @tailwind utilities;
- Кольори бренду: хересонський блакитний (#165DFF), золото, білий, темно-сірий
- Налаштувати теми (світла/темна опціонально)
- Переписати наявні CSS класи (.css файли) на Tailwind utility класи; видалити застарілі CSS

12.2. UI Components Library
- Використати готові компоненти або створити:
  - Button (primary, secondary, danger, outlined, disabled, loading states)
  - Input, Textarea, Select, Checkbox, Radio
  - Toast / Notifications (React Hot Toast або власний)
  - Modal / Dialog
  - DataTable (пагінація, сортування, фільтри)
  - Dropdown / Menu
  - Card, Badge, Progress bar, Spinner
  - Tabs, Accordion
- Окремий файл components/ui/

12.3. Адаптивність
- Mobile-first design за допомогою Tailwind breakpoints: sm (640px), md (768px), lg (1024px), xl (1280px)
- Гаманець меню на мобільних
- Сітки продуктів: 1→2→3→4 колонки
- Таблиці на мобільних: горизонтальний скрол або перетворення на картки

12.4. Доступність (ARIA)
- Лейбли на всіх інпутах
- Клавіатурна навігація
- Альт тексти на всіх зображеннях
- Контраст кольорів на рівні AA (WCAG)

12.5. UX поліпшення
- Кнопки із disabled/loading станом при відправці форм
- Індикація помилок у формах (під полем)
- Запам'ятовування мови (i18next localstorage)
- Skeleton loaders при завантаженні даних
- Empty state ілюстрації для порожніх сторінок
- Зручний checkout ≤5 кліків

12.6. Локалізація (повна)
- Дописати переклади у locales/ua.json та en.json для усіх нових сторінок
- Перекласти:
  - Всі кнопки, заголовки, повідомлення помилок
  - Адмін панель, меню
  - Email шаблони (Етап 10)
  - Категорії та товари (NameEn/DescriptionEn використовувати при language=en)

12.7. Оптимізація продуктивності Frontend
- React.lazy + Suspense для lazy loading сторінок
- Картинки з lazy loading + width/height атрибутами для CLS
- Мініфікація JS/CSS при build
- Compression gzip/brotli на сервері (Nginx або ASP.NET Response Compression)

**Критерій готовності:** Tailwind підключено і класи скрізь; сайтом зручно користуватись на телефоні, планшеті, десктопі; всі тексти обома мовами; UX не дратує.

---

## Етап 13: Тестування - Юніт, Інтеграційні, E2E
**Мета:** Покрити критичну бізнес-логіку тестами, забезпечити стабільність.

13.1. Unit Tests - Backend (xUnit + Moq + FluentAssertions)
- Створити окремий проект KhduSouvenirShop.Tests
- Протестувати сервіси:
  - PromotionServiceTests: пошук застосовних, пріоритет, валідація промокоду (усі 6+ правил), стакування, крайові випадки (від'ємна ціна, ліміти)
  - OrderServiceTests: CreateOrder транзакція (валідний кейс, нестача stock, невалідний промо), CancelOrder (повернення stock, refund), статус переходи (валідні та невалідні)
  - PaymentServiceTests: CreateCheckout, Refund, Webhook обробка (success/failed/refund)
  - WarehouseServiceTests: Додавання Incoming/Outgoing змінює Stock правильно, розрахунок залишків
  - UniversityServiceTests: Верифікація студентів, retry логіка, expiry, маппінг статусів
  - Валідатори: всі FluentValidation тести для CreateOrderDTO, CreatePromotionDTO, RegisterDTO тощо
  - RBAC тести: контролери відхиляють доступ без ролі

- Мета покриття: критичні бізнес-сервіси ≥80%

13.2. Integration Tests - Backend
- TestContainers для MySQL (запуск тимчасової БД в Docker)
- Тести:
  - E2E API endpoints: реєстрація → логін → додати в кошик → оформити замовлення (Stripe mock) → оплата success webhook → змінити статус Shipped → Delivered
  - Warehouse CRUD + stock consistency
  - Nova Poshta mock (WireMock)
  - Stripe webhook replay

13.3. Unit Tests - Frontend (Jest + React Testing Library)
- Тести компонентів:
  - Header (рендер, лінки, пошук)
  - ProductCard (рендер, onClick)
  - CartPage (додати, змінити кількість, видалити, тотал)
  - Forms: RegisterForm, LoginForm (валідація полів, помилки, submit)
  - ProtectedRoute (редирект якщо не авторизований)
  - AuthContext (логін, логаут, стан токена)
- msw (Mock Service Worker) для мокання axios запитів

13.4. E2E Tests (Playwright / Cypress)
- Установка: npm install -D playwright
- Сценарії:
  - Guest: перегляд каталогу → пошук → перегляд товару
  - Customer: регістрація → логін → додати до кошика 3 товари → застосувати промокод → оформити (Card) → оплатити Stripe Test Card → побачити success → перевірити в профілі замовлення
  - Manager: логін → додати продукт → додати категорію → створити incoming документ → перевірити stock
  - Admin: логін → змінити статус замовлення Processing→Shipped (ввести ТТН) → Delivered → перевірити payment COD completed
  - Admin: скасувати оплачене замовлення → перевірити refund
  - Negatives: невалідний промокод, нестача stock, некоректний логін
- Паралельний запуск тестів

13.5. Навантажувальні тести (опціонально для диплома)
- k6 / NBomber: 500 одночасних користувачів, 50 замовлень/хвилину
- Перевірити: API response time < 2s, DB connections не вичерпано, cache hit > 70%

13.6. Безпекові тести
- Перевірити:
  - JWT token expire after 1 год
  - Без ролі Manager не можна викликати admin endpoints → 401/403
  - SQL Injection (пошук з " OR 1=1 --)
  - XSS (введення <script>alert()</script> в пошук / коментарі)
  - Rate limiting на login (5 спроб → 429)
  - Паролі в БД - лише хеші BCrypt

**Критерій готовності:** Юніт-тести на критичні сервіси ≥80%; інтеграційні сценарії проходять; E2E сценарії всіх ролей проходять; безпекові перевірки green.

---

## Етап 14: Безпека, NFR, CSRF, валідація, аудит
**Мета:** Забезпечити виконання всіх нефункціональних вимог до безпеки.

14.1. Перевірити та додати:
- Response Compression (Brotli/Gzip) в Program.cs
- HTTPS Redirection + HSTS (вже є для non-Development)
- CORS строга: DefaultPolicy тільки з дозволених доменів
- CSRF: Antiforgery tokens для всіх не-GET запитів (особливо web forms, якщо рендеряться сервером; для SPA з JWT Bearer в заголовках - CSRF не так критичний, але краще додати X-CSRF токен для додаткового захисту)
- Authentication:
  - JWT expire = 1 година, Refresh token = 30 днів, зберігати в HttpOnly cookies або клієнтський в localStorage з SameSite=Lax
  - BCrypt.WorkFactor = 12+
  - Lockout on failed login attempts (5 невдалих → lock 5 хвилин)

14.2. Валідація всіх DTO
- Переконатися що ВСІ контролери використовують [ApiController] + FluentValidation auto validation
- Немає жодного прямого ModelState.IsValid = false manual якщо можна через валідатор
- Усі вхідні поля: MaxLength, регулярні вирази для телефона/пошти/промокодів

14.3. Логування та аудит
- Serilog WriteTo.Console + File ( вже є)
- Додати (опціонально) Seq sink для зручного пошуку логів локально
- Усі критичні дії логуються з UserId: зміна статусу, refund, скасування, зміни ролей, login fail
- Не логувати: паролі, токени, номери карток, CVV, Stripe credentials
- Логістику зв'язати з OrderHistory / Верифікації з чергою

14.4. Інтеграційна стійкість
- Усі HTTP клієнти (Nova Poshta, University, Stripe) з:
  - Polly Retry + Circuit Breaker
  - Timeout 5-10 сек
- Stripe ідемпотентність: Idempotency-Key header при створенні checkout session
- Payments: webhook eventId + check якщо вже оброблено

14.5. Секрети
- Переконатися що appsettings.json не містить реальних ключів
- Всі ключі в Environment Variables або User Secrets (локально)
- .gitignore містить appsettings.*.json якщо потрібно

**Критерій готовності:** Секрети не в комміті; валідація скрізь; 5 невдалих логінів → lock; HTTPS/HSTS; CORS строгий; лог структурований; зовнішні API з retry.

---

## Етап 15: Admin Panel, RBAC доступи, Integrations Dashboard
**Мета:** Завершити адмін-панель, розмежувати ролі чітко, додати інтеграційний дашборд.

15.1. Рівні доступу (меню + endpoints перевірити)
| Функція | Manager | Administrator | SuperAdmin |
|---------|---------|---------------|------------|
| Каталог (Products/Categories) | ✅ | ✅ | ✅ |
| Склад (Документи, Компанії, Залишки) | ✅ | ✅ | ✅ |
| Акції / Промокоди (CRUD) | ✅ | ✅ | ✅ |
| Персональні знижки користувачам | ✅ | ✅ | ✅ |
| Замовлення (перегляд, зміна статусів Shipped/Delivered) | ✅ | ✅ | ✅ |
| Замовлення (скасування + refund) | ❌ | ✅ | ✅ |
| Користувачі (перегляд) | ❌ | ✅ | ✅ |
| Користувачі (блокування, скидання паролю, зміна ролі) | ❌ | ✅ (крім SuperAdmin) | ✅ |
| Верифікації студентів ( approve/reject ) | ❌ | ✅ | ✅ |
| Аналітика / Звіти / Експорт | ✅ | ✅ | ✅ |
| Інтеграції статус-дашборд | ❌ | ✅ | ✅ |
| Системні налаштування, ключі інтеграцій, ролі та права | ❌ | ❌ | ✅ |
| Системні логи | ❌ | ❌ | ✅ |
| Backup / Restore | ❌ | ❌ | ✅ |

15.2. Admin сторінки (закінчити)
- AdminDashboard (home page) - кільцеві KPI: Total orders today, Revenue today, Low stock alerts, Pending processing orders count
- AdminOrdersPage: фільтри, сортування, bulk export, зміна статусів (кнопка зі статус-машиною), модалка з ТТН
- AdminUsersPage: таблиця, пошук, фільтр по ролі/активності, модалки блокування/скинути пароль/змінити роль, перехід на історію замовлень
- AdminWarehousePage: таби з Етапу 8
- AdminPromotionsPage: CRUD, статистика ROI, призначення персональних (додати до адмін меню - зараз відсутній роут)
- AdminIntegrationsPage (SuperAdmin/Admin):
  - Статус-карточки з кольорові (Green / Yellow / Red):
    - Stripe: останній webhook, payment success rate
    - Nova Poshta: API latency, запитів за годину
    - University: queue size, last retry, approved count
    - SMTP: відправлено за годину, помилки
    - Database: uptime, queries per sec
  - Кнопки "Тестувати інтеграцію" для кожної
- AdminSettingsPage (SuperAdmin): зміна значень налаштувань через UI (не обов'язково; можна env vars)

15.3. Features Controller (опціонально)
- Feature Flags для поступового вмикання: Nova Poshta, Stripe, University, Analytics export
- Admin може включати/вимикати без деплою (кешується)

**Критерій готовності:** Кожна роль бачить тільки свої пункти меню; неможливо викликати endpoint іншої ролі; інтеграційний дашборд відображає статуси.

---

## Етап 16: DevOps, Docker Compose, CI/CD GitHub Actions, Деплой на Render
**Мета:** Автоматизувати збірку, тестування і розгортання; підготувати прод-середовище.

16.1. Додати Solutions проекти
- KhduSouvenirShop.sln вже має API; додати .Tests проект

16.2. Docker Compose локальний - допрацювати
- Сервіси:
  - db: mysql:8.0, volume, port 3306
  - minio: minio/minio (для об'єктного сховища), консоль порт 9001
  - api: build ./src/backend, port 5000, залежить від db/minio
  - frontend: build ./src/frontend, port 3000/5173, залежить від api
  - (Опціонально) seq: datalust/seq для логів, port 5341
- docker-compose up -d запускає все однією командою

16.3. Dockerfile оптимізації
- Backend: multi-stage build (restore → build → publish → run on aspnet image)
- Frontend: multi-stage (node build → nginx alpine для serve static)
- .dockerignore вдосконалити

16.4. GitHub Actions - CI/CD Workflow (.github/workflows/ci-cd.yml)
- Тригери: push на main / pull request
- Jobs:
  1. **Build & Test Backend**:
     - Checkout
     - Setup .NET 8.0 SDK
     - Restore
     - Build
     - Run unit tests
     - Publish artifacts
  2. **Build & Test Frontend**:
     - Checkout
     - Setup Node.js 20.x
     - npm ci
     - npm test -- --watchAll=false
     - npm run build
     - Upload build artifact
  3. **Docker Build & Push**:
     - Якщо push на main:
       - Login to Docker Hub / Render Container Registry
       - Build images: api + frontend
       - Push tagged images
  4. **Deploy to Render**:
     - Deploy API Web Service (Render Deploy Hook)
     - Deploy Frontend Static Site
     - Run smoke tests: curl health check API, curl frontend HTML

16.5. Render.com налаштування (документувати в плані як задача)
- **Services:**
  1. **MySQL** - Aiven Cloud (Managed) або Render Managed MySQL, Automatic backups daily
  2. **MinIO** - Private Service, Persistent Disk 10GB, Internal only
  3. **Backend API** - Web Service from Docker, Health Check Path /api/health, Environment Variables: ConnectionStrings, Jwt settings, Stripe, Nova, University, SMTP, AllowedOrigins
  4. **Frontend** - Static Site, Build Command npm run build, Publish Directory build
- **Custom domains:** shop.ksu.edu.ua, api.shop.ksu.edu.ua
- **SSL/TLS auto** через Render Let's Encrypt

16.6. Міграції та Seed на проді
- Program.cs на старті: context.Database.Migrate() (або окремий job)
- Seed даних виконується тільки один раз (перевіряє по UserId / CategoryId)

16.7. Моніторинг та алерти
- Render metrics: CPU, RAM, Response time, Error rate
- (Опціонально) UptimeRobot для перевірки доступності кожні 5 хвилин → алерт на email
- Serilog logs → Render logs dashboard

**Критерій готовності:** Docker Compose піднімає локальне середовище з усіма залежностями; GitHub Actions успішно збирає та запускає тести; PR не проходить якщо тести впали; деплой на Render виконується автоматично при push на main; сайт доступний по HTTPS з власним доменом.