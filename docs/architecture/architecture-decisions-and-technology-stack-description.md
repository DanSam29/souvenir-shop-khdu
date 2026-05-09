# Опис архітектурних рішень та технологічного стеку
## 1. Загальна архітектура системи
### 1.1. Архітектурний стиль
Інтернет-магазин сувенірної продукції ХДУ побудований за трирівневою клієнт-серверною архітектурою з використанням REST API для комунікації між рівнями.  
Рівні системи:  
1. Presentation Layer (Frontend)  
    - Web Application (React SPA)  
    - Admin Panel (React Admin)  
2. Business Logic Layer (Backend)  
    - API Gateway (.NET Core)  
    - Services (бізнес-логіка)  
    - Integrations (зовнішні сервіси)  
3. Data Layer  
    - MySQL Database  
    - In-Memory Cache  
    - MinIO Object Storage
### 1.2. Архітектурні патерни
1. Layered Architecture (Шарова архітектура)  
    - Controllers - обробка HTTP запитів  
    - Services - бізнес-логіка  
    - Repositories - доступ до даних  
    - Integrations - взаємодія з зовнішніми API  
2. Repository Pattern  
    - Абстракція доступу до бази даних  
    - Використання Entity Framework Core  
    - Централізована логіка роботи з даними  
3. Dependency Injection  
    - Вбудований DI контейнер ASP.NET Core  
    - Інверсія залежностей між компонентами  
    - Полегшення тестування та підтримки  
4. MVC Pattern (Model-View-Controller)  
    - Розділення відповідальностей у Frontend  
    - React компоненти як Views  
    - Services як Controllers  
    - State Management як Models  
5. Strategy Pattern (для системи знижок)  
    - Різні стратегії розрахунку знижок (відсоткова, фіксована, спеціальна ціна)  
    - Динамічний вибір стратегії на основі типу промоції  
    - Легке додавання нових типів знижок без зміни існуючого коду  
6. Priority Queue Pattern (для стакування знижок)  
    - Застосування знижок за пріоритетом  
    - Автоматичний вибір найвигіднішої комбінації знижок  
    - Підтримка складних правил стакування
## 2. Обґрунтування архітектурних рішень
### 2.1. Вибір REST API замість GraphQL
Рішення: REST API  
Обґрунтування:  
  - Простота реалізації для навчального проєкту  
  - Стандартизовані HTTP методи (GET, POST, PUT, DELETE)  
  - Легка інтеграція з React та .NET Core  
  - Краща підтримка кешування  
  - Зрозуміла структура endpoints для документації  
Недоліки GraphQL у нашому випадку:  
  - Надмірна складність для простого CRUD  
  - Більший overhead для малого проєкту  
  - Складніше налаштування безпеки
### 2.2. Вибір MySQL замість PostgreSQL
Рішення: MySQL 8.0  
Обґрунтування:  
  - Простіше налаштування та адміністрування для навчального проєкту  
  - Менші вимоги до ресурсів серверу (важливо для безкоштовного хостингу)  
  - Вища швидкість читання даних для операцій SELECT (перегляд каталогу, пошук)  
  - Відмінна підтримка в Entity Framework Core через Pomelo.EntityFrameworkCore.MySql  
  - Достатня функціональність: JSON поля, ACID транзакції, full-text search  
Альтернатива (PostgreSQL):  
  - Можна використати для складніших аналітичних систем  
  - Має переваги при дуже великих обсягах даних
### 2.3. Вибір In-Memory Cache замість Redis
Рішення: In-Memory Cache (ASP.NET Core)  
Обґрунтування:  
  - Вбудований у .NET Core, без додаткових залежностей  
  - Достатній для невеликого магазину  
  - Економія ресурсів на Render.com  
  - Простіше налаштування та підтримка  
Коли потрібен Redis:  
  - При горизонтальному масштабуванні (багато серверів)  
  - При потребі розподіленого кешу  
  - При дуже великому навантаженні (10000+ користувачів)
### 2.4. Вибір монолітної архітектури замість мікросервісів
Рішення: Монолітна архітектура з модульною структурою  
Обґрунтування:  
  - Простіше розробка та налагодження  
  - Менші накладні витрати на інфраструктуру  
  - Легше розгортання (один Docker контейнер)  
  - Достатньо для магазину з 500-1000 користувачів  
  - Модульна структура дозволяє легко виділити мікросервіси у майбутньому  
Коли потрібні мікросервіси:  
  - При масштабуванні окремих модулів незалежно  
  - При розподіленій команді розробки  
  - При дуже великому навантаженні (100000+ користувачів)
### 2.5. Вибір MinIO замість AWS S3
Рішення: MinIO (self-hosted)  
Обґрунтування:  
  - Безкоштовне зберігання зображень  
  - S3-сумісний API (легко мігрувати на AWS S3 у майбутньому)  
  - Повний контроль над даними  
  - Простіше налаштування для навчального проєкту  
Альтернатива (AWS S3):  
  - Платний сервіс (навіть Free Tier має обмеження)  
  - Можна використати для production версії
## 3. Технологічний стек
### 3.1. Frontend

| Технологія | Версія | Призначення |
|------------|--------|-------------|
| React | 18.x | Основний UI фреймворк для SPA |
| React Router | 6.x | Маршрутизація сторінок |
| Axios | 1.x | HTTP клієнт для REST API |
| React Admin | 4.x | Адміністративна панель |
| Tailwind CSS | 3.x | Utility-first CSS фреймворк |
| Vite | 5.x | Build tool та dev server |
Обґрунтування вибору React:  
  - Найпопулярніший фреймворк  
  - Компонентний підхід (переважне використання коду)  
  - Virtual DOM (висока продуктивність)  
  - Багато готових бібліотек та інструментів
### 3.2. Backend

| Технологія | Версія | Призначення |
|------------|--------|-------------|
| ASP.NET Core | 8.0 | Web API Framework |
| Entity Framework Core | 8.0 | ORM для роботи з БД |
| AutoMapper | 12.x | Маппінг між DTO та Entity |
| FluentValidation | 11.x | Валідація даних |
| Serilog | 3.x | Структуроване логування |
| Swagger/OpenAPI | 6.x | API документація |
| iTextSharp (iText7) | 5.x | Генерація PDF звітів з графіками та таблицями |
| EPPlus | 7.x | Генерація Excel (XLSX) звітів з форматуванням та графіками |
| CsvHelper | 30.x | Генерація CSV файлів для експорту табличних даних |
Обґрунтування вибору .NET Core:  
  - Кросплатформність (Windows, Linux, macOS)  
  - Висока продуктивність  
  - Вбудована підтримка Dependency Injection  
  - Відмінна документація та підтримка від Microsoft  
  - Велика кількість бібліотек для аналітики та звітності (iText7, EPPlus, CsvHelper, Chart.js integration)
### 3.3. Database

| Технологія | Версія | Призначення |
|------------|--------|-------------|
| MySQL | 8.0 | Основна реляційна БД |
| Pomelo.EntityFrameworkCore.MySql | 8.x | MySQL provider для EF Core |
### 3.4. Caching & Storage

| Технологія | Версія | Призначення |
|------------|--------|-------------|
| In-Memory Cache | Built-in | Кешування товарів та категорій |
| MinIO | Latest | Об'єктне сховище для зображень |
### 3.5. Authentication & Security

| Технологія | Версія | Призначення |
|------------|--------|-------------|
| JWT (JSON Web Tokens) | - | Токени автентифікації |
| ASP.NET Identity | 8.0 | Управління користувачами |
| BCrypt | - | Хешування паролів |
| HTTPS/TLS | 1.3 | Шифрування трафіку |
### 3.6. External Integrations

| Технологія | Версія | Призначення |
|------------|--------|-------------|
| Stripe | v1 | Обробка онлайн-платежів (Sandbox mode) |
| Nova Poshta API | v2.0 | Розрахунок доставки, відділення |
| SMTP (Gmail/SendGrid) | - | Відправка email-сповіщень |
| University API | - | Верифікація студентів, отримання академічних даних (studentStatus, GPA, scholarshipStatus) |
### 3.7. DevOps & Deployment

| Технологія | Версія | Призначення |
|------------|--------|-------------|
| Docker | 24.x | Контейнеризація додатків |
| Docker Compose | 2.x | Локальна розробка |
| GitHub Actions | - | CI/CD pipeline |
| Render.com | - | Cloud hosting (PaaS) |
| Nginx | 1.25 | Веб-сервер для Frontend |
### 3.8. Testing

| Технологія | Версія | Призначення |
|------------|--------|-------------|
| xUnit | 2.x | Unit тести для Backend |
| Moq | 4.x | Mocking для тестів |
| Jest | 29.x | Unit тести для Frontend |
| React Testing Library | 14.x | Тестування React компонентів |
| Cypress | 13.x | E2E тести |
## 4. Структура Backend модулів
### 4.1. Модульна організація
```
API/
├── Controllers/                  # HTTP endpoints
│ ├── PromotionController.cs
│ └── ...
├── Services/                     # Бізнес-логіка
│ ├── PromotionService.cs
│ └── ...
├── Repositories/                 # Доступ до БД
│ ├── PromotionRepository.cs
│ ├── UserPromotionRepository.cs
│ └── ...
├── Models/                       # Entity класи
│ ├── Promotion.cs
│ ├── UserPromotion.cs
│ └── ...
├── DTOs/                         # Data Transfer Objects
│ ├── PromotionDTO.cs
│ ├── CreatePromotionDTO.cs
│ ├── ApplyPromoCodeDTO.cs
│ └── ...
├── Integrations/                 # Зовнішні API
│ ├── UniversityIntegration.cs
│ └── ...
├── Middleware/                   # Custom middleware
└── Infrastructure/               # Допоміжні сервіси
```
### 4.2. Модулі системи

| Модуль | Відповідальність |
|--------|------------------|
| CatalogModule | Товари, категорії, зображення, акції |
| CartModule | Кошик покупок |
| OrderModule | Замовлення та їх обробка |
| PaymentModule | Платежі через Stripe |
| ShippingModule | Доставка через Nova Poshta |
| UserModule | Автентифікація, авторизація, профілі |
| WarehouseModule | Складський облік, накладні |
| AdminPanelModule | Адміністративні функції, звіти |
| NotificationModule | Email-сповіщення |
| PromotionModule | Акції, знижки, промокоди, стакування знижок, розрахунок фінальних цін, аналітика ефективності |
| UniversityModule | Інтеграція з University API, автоматична верифікація студентів, призначення студентських знижок |
| AnalyticsModule | Збір, обробка та візуалізація аналітичних даних: агрегація даних з Orders, OrderItems, Products, Categories, Promotions; розрахунок ключових метрик (KPI); генерація звітів у різних форматах; кешування розрахованих метрик; візуалізація даних (графіки, таблиці) |
## 5. Безпека системи
### 5.1. Механізми захисту
1. Автентифікація та авторизація:  
    - JWT токени з терміном дії (1 година)  
    - Refresh токени для оновлення сесії  
    - Role-Based Access Control (RBAC)  
    - Хешування паролів за допомогою BCrypt (salt rounds: 12)  
2. Захист від веб-загроз:  
    - XSS (Cross-Site Scripting): Санітизація вхідних даних, Content Security Policy  
    - CSRF (Cross-Site Request Forgery): CSRF токени, SameSite cookies  
    - SQL Injection: Використання Entity Framework Core (параметризовані запити)  
    - Brute Force: Rate Limiting (100 запитів/хвилину на IP)  
3. Захист даних:  
    - HTTPS/TLS 1.3 для всього трафіку  
    - Шифрування чутливих даних в БД  
    - API ключі зберігаються в Environment Variables (Stripe, Nova Poshta, University)  
    - Логи не містять особистих даних (GDPR compliant)  
    - Захист персональних даних студентів (studentStatus, GPA) згідно GDPR  
    - University API викликається тільки через backend (ніколи з frontend)  
4. Захист платежів:  
    - Токенізація карток через Stripe  
    - Система НЕ зберігає номери карток та CVV  
    - PCI DSS Compliance через Stripe
### 5.2. CORS політика
AllowedOrigins:  
  - https://khdu-eshop.onrender.com (Production Frontend)  
  - http://localhost:5173 (Development Frontend)  
AllowedMethods: GET, POST, PUT, DELETE  
AllowedHeaders: Authorization, Content-Type
## 6. Продуктивність та масштабованість
### 6.1. Оптимізація продуктивності
Frontend:  
  - Code splitting (React.lazy)  
  - Lazy loading зображень  
  - Кешування статичних ресурсів (1 рік)  
  - Compression (Gzip/Brotli)  
  - Minification та uglification  
Backend:  
  - In-Memory кешування (товари, категорії) - TTL:  
      + Товари та категорії - TTL: 5 хвилин  
      + Активні акції та знижки - TTL: 10 хвилин  
      + Студентські знижки за типом (REGULAR, SCHOLARSHIP, HIGH\_ACHIEVER) - TTL: 30 хвилин  
      + Персональні знижки користувача - TTL: 5 хвилин  
      + Аналітичні метрики (sales, products, categories) - TTL: 15 хвилин  
      + Фінансові звіти та ROI розрахунки - TTL: 30 хвилин  
      + Метрики конверсії - TTL: 1 година  
  - Database query optimization (indexed fields)  
  - Connection pooling (MySQL)  
  - Асинхронні операції (async/await)  
  - Pagination для списків (default: 20 items)  
Database:  
  - Індекси на часто використовуваних полях:  
      + Products.Name (full-text search)  
      + Orders.UserId, Orders.Status, Orders.CreatedAt (для аналітики по періодах)  
      + Users.Email (unique), Users.StudentStatus, Users.StudentExpiresAt  
      + Promotions.PromoCode (unique), Promotions.IsActive, Promotions.StartDate, Promotions.EndDate  
      + UserPromotions.UserId, UserPromotions.PromotionId  
      + OrderItems.AppliedPromotionId, OrderItems.ProductId (для аналітики товарів)  
      + OutgoingDocuments.AppliedPromotionId  
      + Products.CategoryId (для аналітики категорій)  
      + Композитний індекс (Orders.CreatedAt, Orders.Status) для швидких запитів аналітики  
  - Stored procedures для складних запитів
### 6.2. Стратегія масштабування
Вертикальне масштабування (Short-term):  
  - Збільшення RAM/CPU на Render.com  
  - Оптимізація SQL запитів  
  - Покращення алгоритмів  
Горизонтальне масштабування (Long-term):  
  - Multiple backend instances за Load Balancer  
  - Перехід на Redis для розподіленого кешу  
  - Database Read Replicas (MySQL)  
  - Виділення окремих мікросервісів (Payment, Notification)
## 7. Моніторинг та логування
### 7.1. Система логування
Serilog структуроване логування:  
Levels:  
  - Debug: Детальна інформація для розробки  
  - Information: Основні події системи  
  - Warning: Потенційні проблеми  
  - Error: Помилки з stack trace  
  - Critical: Критичні збої системи  
Що логується:  
  - Всі API запити (endpoint, method, status, duration)  
  - Помилки з повним stack trace  
  - Зміни статусів замовлень  
  - Створення/видалення складських документів  
  - Спроби несанкціонованого доступу  
  - Створення, редагування, деактивація акцій та знижок  
  - Застосування знижок при оформленні замовлення (originalPrice, appliedPromotionId, discountAmount, finalPrice)  
  - Використання промокодів (код, користувач, результат валідації)  
  - Верифікація студентів через University API (email, результат, studentStatus, GPA)  
  - Призначення студентських знижок  
  - Помилки при взаємодії з University API  
  - Запити до аналітики (тип звіту, параметри, час виконання, користувач)  
  - Розрахунок аналітичних метрик (час розрахунку, кількість записів, cache hit/miss)  
  - Експорт звітів (формат, розмір файлу, користувач)  
  - Помилки при генерації звітів (формат, причина помилки)
### 7.2. Моніторинг
Метрики для моніторингу:  
  - Response time API endpoints (цільове: < 200ms)  
  - Database query duration (цільове: < 100ms)  
  - Cache hit ratio (цільове: > 80%)  
  - Error rate (цільове: < 1%)  
  - Active users онлайн  
  - Кількість активних акцій  
  - Кількість застосувань знижок за день  
  - Середній розмір знижки  
  - Відсоток замовлень зі знижками  
  - University API response time (цільове: < 500ms)  
  - University API availability (цільове: > 99%)  
  - Кількість успішних/невдалих верифікацій студентів  
  - Кількість запитів до аналітики за день/годину  
  - Середній час розрахунку аналітичних метрик (цільове: < 2 секунди)  
  - Cache hit ratio для аналітики (цільове: > 70%)  
  - Кількість експортованих звітів за день  
  - Розмір згенерованих звітів (моніторинг memory usage)  
  - Кількість одночасних запитів до аналітики  
Інструменти:  
  - Render.com Dashboard (CPU, RAM, Network)  
  - Serilog logs (Elasticsearch у production)  
  - Custom health check endpoint (/api/health)
## 8. Розгортання на Render.com
### 8.1. Інфраструктура
Services на Render.com:  
1. Frontend Web Service  
    - Type: Static Site  
    - Build Command: npm run build  
    - Publish Directory: dist  
    - Auto-deploy: GitHub main branch  
2. Admin Panel Web Service  
    - Type: Static Site  
    - Build Command: npm run build  
    - Publish Directory: dist  
    - Auto-deploy: GitHub main branch  
3. Backend Web Service  
    - Type: Web Service (Docker)  
    - Dockerfile: Dockerfile  
    - Health Check: /api/health  
    - Auto-deploy: GitHub main branch  
4. MySQL Database  
    - Type: MySQL (Managed)  
    - Version: 8.0  
    - Automatic backups: Daily  
5. MinIO Private Service  
    - Type: Private Service (Docker)  
    - Internal URL only
### 8.2. Environment Variables
Backend:  
  - `DATABASE\_URL=mysql://\...`  
  - `JWT\_SECRET=\...`  
  - `STRIPE\_SECRET\_KEY=\...`  
  - `STRIPE\_PUBLISHABLE\_KEY=\...`  
  - `STRIPE\_WEBHOOK\_SECRET=\...`  
  - `NOVAPOSHTA\_API\_KEY=\...`  
  - `UNIVERSITY\_API\_KEY=\...`  
  - `UNIVERSITY\_API\_BASE\_URL=https://api.ksu.edu.ua`  
  - `UNIVERSITY\_API\_TIMEOUT=5000`  
  - `STUDENT\_STATUS\_EXPIRY\_MONTHS=4`  
  - `HIGH\_ACHIEVER\_MIN\_GPA=4.5`  
  - `SMTP\_HOST=\...`  
  - `SMTP\_PORT=\...`  
  - `SMTP\_USERNAME=\...`  
  - `SMTP\_PASSWORD=\...`  
  - `MINIO\_ENDPOINT=\...`  
  - `MINIO\_ACCESS\_KEY=\...`  
  - `MINIO\_SECRET\_KEY=\...`  
Frontend:  
  - `VITE\_API\_URL=https://api.khdu-eshop.onrender.com`  
  - `VITE\_STRIPE\_PUBLISHABLE\_KEY=...`
### 8.3. CI/CD Pipeline (GitHub Actions)
Workflow:  
1. Push to GitHub main branch  
2. Run tests (xUnit, Jest)  
3. Build Docker image  
4. Push to Render.com  
5. Automatic deployment  
6. Run smoke tests  
7. Send notification (success/failure)
## 9. Переваги та обмеження обраної архітектури
### 9.1. Переваги
- Простота розробки: Монолітна архітектура легша у розробці та налагодженні  
- Низька вартість: Використання безкоштовного Render.com Free Tier  
- Швидкий старт: Можна запустити локально за 5 хвилин через Docker Compose  
- Модульність: Легко виділити модулі в окремі мікросервіси у майбутньому  
- Типобезпека: C# та TypeScript забезпечують надійність коду  
- Документація: Swagger автоматично генерує API документацію  
- Безпека: JWT + RBAC + HTTPS забезпечують високий рівень безпеки
### 9.2. Обмеження
- In-Memory Cache не підходить для горизонтального масштабування  
- Монолітна архітектура ускладнюється при зростанні функціоналу  
- Відсутність real-time нотифікацій (WebSocket)
### 9.3. Шляхи покращення
- Перехід на Redis для розподіленого кешу (критично для знижок при горизонтальному масштабуванні та для кешування аналітичних метрик)  
- Впровадження Message Queue (RabbitMQ) для асинхронної обробки (студентські верифікації, email про знижки, фонові розрахунки складних звітів)  
- Додавання WebSocket для real-time оновлень статусів (включаючи застосування знижок в реальному часі та live-оновлення дашбордів аналітики)  
- Виділення Payment, Notification, Promotion та Analytics у окремі мікросервіси  
- Впровадження Kubernetes для оркестрації контейнерів  
- Впровадження A/B тестування для акцій (оптимізація розмірів знижок)  
- Додавання Machine Learning для персоналізованих пропозицій знижок  
- Впровадження окремої аналітичної бази даних (OLAP) для складних звітів (наприклад, ClickHouse або TimescaleDB)  
- Додавання BI інструментів (Power BI, Tableau) для глибшого аналізу даних  
- Впровадження Data Warehouse для історичної аналітики та прогнозування  
- Додавання ML моделей для прогнозування попиту та оптимізації складських запасів
## 10. Детальна архітектура системи знижок
### 10.1. Структура даних
Promotion (Акція/Знижка):  
  - Базова інформація: id, name, description  
  - Тип та значення: type (PERCENTAGE/FIXED\_AMOUNT/SPECIAL\_PRICE), value  
  - Область застосування: targetType (PRODUCT/CATEGORY/CART/SHIPPING), targetId  
  - Цільова аудиторія: audienceType (ALL/STUDENTS/STAFF/ALUMNI/CUSTOM)  
  - Часові рамки: startDate, endDate  
  - Промокод: promoCode (unique, nullable)  
  - Умови: minOrderAmount, minQuantity  
  - Пріоритет та ліміти: priority (0-100), usageLimit, currentUsage  
  - Статус: isActive  
  - Метадані: createdBy, createdAt, updatedAt  
UserPromotion (Персональна знижка):  
  - Зв'язок: userId, promotionId  
  - Метрики: assignedAt, usedCount  
User (розширення):  
  - Студентський статус: studentStatus (NONE/REGULAR/SCHOLARSHIP/HIGH\_ACHIEVER)  
  - Академічні дані: gpa (decimal 0.00-5.00)  
  - Верифікація: studentVerifiedAt, studentExpiresAt  
OrderItem (розширення):  
  - Ціни: originalPrice, finalPrice  
  - Знижка: appliedPromotionId, discountAmount  
OutgoingDocument (розширення):  
  - Ціни: originalPrice, finalPrice (для Reason=ORDER)  
  - Знижка: appliedPromotionId, discountAmount (для Reason=ORDER)
### 10.2. Бізнес-логіка застосування знижок
**Крок 1: Збір доступних знижок**  
GetApplicablePromotions(userId, cartItems):  
  - Отримати дані User (studentStatus, gpa, studentExpiresAt)  
  - Перевірити чи активний studentStatus (studentExpiresAt > now)  
  - Якщо активний → вибрати студентські знижки за studentStatus  
  - Вибрати персональні знижки (UserPromotions для userId)  
  - Вибрати товарні/категорійні знижки (targetType=PRODUCT/CATEGORY)  
  - Вибрати знижки на кошик (targetType=CART)  
  - Відфільтрувати за:  
      + isActive = true  
      + startDate <= now <= endDate (якщо вказано)  
      + currentUsage < usageLimit (якщо вказано)  
  - Повернути список знижок  
**Крок 2: Валідація промокоду (якщо введено)**  
ValidatePromoCode(code, userId, cartItems):  
  - Знайти Promotion WHERE promoCode = code  
  - Якщо не знайдено → return error "Промокод не знайдено"  
  - Перевірити isActive = true  
  - Перевірити startDate <= now <= endDate  
  - Перевірити currentUsage < usageLimit  
  - Перевірити minOrderAmount <= cartTotal  
  - Перевірити minQuantity <= totalQuantity  
  - Якщо всі перевірки пройдені → return valid  
  - Інакше → return error з причиною  
**Крок 3: Розрахунок фінальних цін зі стакуванням**  
CalculateFinalPrices(cartItems, promotions, promoCode): 
FOR EACH item IN cartItems:  
1. originalPrice = item.Product.Price  
2. Відфільтрувати promotions які застосовуються до цього item:  
    - targetType = PRODUCT AND targetId = item.ProductId
    - targetType = CATEGORY AND targetId = item.Product.CategoryId
    - targetType = CART
    - promoCode (якщо введено)
3. Відсортувати за пріоритетом:  
    - Priority 1: Персональні (audienceType = CUSTOM)  
    - Priority 2: Студентські (audienceType = STUDENTS)  
    - Priority 3: Товарні/Категорійні  
    - Priority 4: Загальні (audienceType = ALL)  
    - Priority 5: Промокод  
   Якщо однаковий тип → сортувати за полем priority (DESC)
4. Застосувати знижку з найвищим пріоритетом:
   IF promotion.type = PERCENTAGE:
       discount = originalPrice \* (promotion.value / 100)
   ELSE IF promotion.type = FIXED\_AMOUNT:
       discount = promotion.value
   ELSE IF promotion.type = SPECIAL\_PRICE:
       discount = originalPrice - promotion.value
5. finalPrice = originalPrice - discount  
6. appliedPromotionId = promotion.id  
7. Якщо введено промокод і він дає більшу знижку:  
    - Перерахувати discount з промокодом  
    - Оновити finalPrice, appliedPromotionId  
8. Зберегти: originalPrice, appliedPromotionId, discountAmount, finalPrice  
RETURN items з розрахованими цінами  
**Крок 4: Збереження інформації про знижки**  
При створенні замовлення:  
1. OrderItems зберігають: originalPrice, appliedPromotionId, discountAmount, finalPrice  
2. Promotions.currentUsage++  
3. UserPromotions.usedCount++ (якщо персональна знижка)  
4. OutgoingDocuments копіюють дані зі знижками з OrderItems
### 10.3. Інтеграція з University API
Процес верифікації студента:  
VerifyStudent(email):  
1. Перевірити email.endsWith("@ksu.edu.ua") OR email.endsWith("@student.ksu.edu.ua")  
2. Якщо НІ → return {found: false}  
3. TRY:  
    - response = UniversityAPI.GET("/students/{email}")  
    - HEADERS: Authorization: Bearer {UNIVERSITY\_API\_KEY}  
    - TIMEOUT: 5 seconds  
    - CATCH TimeoutException:  
    - LogError("University API timeout")  
    - CreateRetryTask(userId, retryAfter: 1 hour)  
    - return {apiError: true}  
    - CATCH Exception:  
    - LogError("University API error")  
    - CreateRetryTask(userId, retryAfter: 1 hour)  
    - return {apiError: true}  
4. IF response.found = false:  
    - LogWarning("Student not found in University API")  
    - return {found: false}  
5. Мапити studentStatus:  
   IF response.gpa >= 4.5 AND response.scholarshipStatus = ACTIVE:  
       studentStatus = HIGH\_ACHIEVER  
   ELSE IF response.scholarshipStatus = ACTIVE:  
       studentStatus = SCHOLARSHIP  
   ELSE:  
       studentStatus = REGULAR  
6. UPDATE Users SET:  
    - studentStatus = calculated\_status  
    - gpa = response.gpa  
    - studentVerifiedAt = NOW()  
    - studentExpiresAt = NOW() + 4 MONTHS  
7. AssignStudentPromotions(userId, studentStatus)  
8. SendEmail("Вітаємо! Ви верифіковані як студент. Доступні знижки: ...")  
9. return {found: true, studentStatus, gpa}
### 10.4. Аналітика ефективності акцій
Метрики для кожної акції:  
  - Кількість використань (Promotions.currentUsage)  
  - Загальна сума знижок (SUM(OrderItems.discountAmount) WHERE appliedPromotionId = ?)  
  - Кількість унікальних користувачів (COUNT(DISTINCT Orders.UserId))  
  - Середній чек з знижкою (AVG(Orders.TotalAmount))  
  - Конверсія (кількість замовлень зі знижкою / загальна кількість переглядів акції)  
  - ROI = (Revenue з акції - Сума знижок) / Сума знижок \* 100%  
Dashboard для менеджера:  
  - Топ-10 акцій за кількістю використань  
  - Топ-10 акцій за сумою знижок  
  - Графік використань по датах  
  - Порівняння ефективності різних типів знижок  
  - Аналіз по цільових аудиторіях
### 10.5. Обробка edge cases
Промокод використано повністю:  
  - Перевірка: currentUsage < usageLimit  
  - Повідомлення: "Промокод вичерпано"  
Закінчився термін studentStatus:  
  - Автоматична деактивація знижок при studentExpiresAt < NOW()  
  - Email-нагадування за тиждень до закінчення  
  - Кнопка "Оновити статус" в особистому кабінеті  
University API недоступний:  
  - Створення задачі в StudentVerificationQueue  
  - Повторні спроби: 1 год, 6 год, 24 год  
  - Якщо не вдалося → переведення в ручну верифікацію  
Стакування знижок призводить до від'ємної ціни:  
  - Обмеження: finalPrice >= 0  
  - Якщо finalPrice < 0 → finalPrice = 0, discountAmount = originalPrice  
Промокод не відповідає умовам:  
  - Перевірка minOrderAmount, minQuantity  
  - Чіткі повідомлення: "Для цього промокоду потрібна мінімальна сума замовлення 500 грн"
## 11. Архітектура аналітичного модуля
### 11.1. Принципи роботи
AnalyticsModule побудований за принципами:  
1. Read-Only доступ до даних - модуль тільки читає дані з основних таблиць, ніколи не модифікує їх  
2. Агресивне кешування - всі розраховані метрики кешуються з TTL 15-60 хвилин  
3. Lazy calculation - метрики розраховуються тільки за запитом, а не постійно в фоні  
4. Асинхронна обробка складних звітів - експорт великих звітів виконується асинхронно  
5. Інвалідація кешу - кеш автоматично інвалідується при створенні нових замовлень або зміні даних
### 11.2. Процес розрахунку аналітики
Крок 1: Отримання запиту від користувача  
  - AnalyticsController отримує параметри (startDate, endDate, filters, groupBy)  
  - Валідує параметри (період не більше 2 років, дати валідні)  
  - Перевіряє права доступу (MANAGER або ADMIN)  
Крок 2: Перевірка кешу  
  - AnalyticsService формує ключ кешу на основі параметрів  
  - Перевіряє наявність даних у In-Memory Cache  
  - Якщо дані в кеші та актуальні (TTL не вийшов) → повертає з кешу  
  - Якщо кеш порожній або застарілий → переходить до розрахунку  
Крок 3: Збір даних з репозиторіїв  
  - OrderRepository.getOrdersByPeriod(startDate, endDate, filters)  
  - ProductRepository.getProductsByIds(productIds)  
  - IncomingRepository.getIncomingByProducts(productIds, period) -- для розрахунку ROI  
  - PromotionRepository.getPromotionsByPeriod(period) -- для аналізу знижок  
Крок 4: Розрахунок метрик  
  - Паралельні розрахунки (використання Task.WhenAll в C#)  
  - Aggregate metrics: totalOrders, totalRevenue, averageOrderValue  
  - Time series: salesByDay/Week/Month (GroupBy + Sum)  
  - Distributions: salesByPaymentMethod, salesByStatus (GroupBy + Count/Sum)  
  - Top lists: topProducts, topCategories (OrderByDescending + Take)  
  - Comparisons: changeVsPreviousPeriod (розрахунок % змін)  
Крок 5: Формування результату  
  - AnalyticsService формує DTO (SalesAnalyticsDTO, ProductAnalyticsDTO тощо)  
  - Додає метадані (generatedAt, calculationTime, cached: false)  
Крок 6: Збереження в кеш  
  - Cache.set(cacheKey, result, TTL=15min)  
  - Логування події розрахунку (Serilog)  
Крок 7: Повернення результату  
  - AnalyticsController повертає HTTP 200 OK з даними  
  - Frontend отримує дані та будує візуалізації
### 11.3. Оптимізації для продуктивності
1. Композитні індекси для частих запитів:  
    - (Orders.CreatedAt, Orders.Status)  
    - (OrderItems.ProductId, OrderItems.OrderId)  
    - (Products.CategoryId, Products.Id)  
2. Materialized Views (опціонально, для MySQL 8.0):  
    - daily\_sales\_summary (pre-aggregated по днях)  
    - product\_performance (pre-calculated метрики товарів)  
    - Автоматичне оновлення через triggers або scheduled jobs  
3. Pagination для великих датасетів:  
    - При експорті великих звітів використовується cursor-based pagination  
    - Обмеження на розмір одного запиту (max 10000 records)  
4. Connection Pooling:  
    - Використання пулу з'єднань до БД (MySQL Connection Pool)  
    - Max pool size: 50 connections  
5. Query Optimization:  
    - EXPLAIN ANALYZE для всіх складних запитів  
    - Використання покриваючих індексів (covering indexes)  
    - Уникнення N+1 запитів через EAGER loading
### 11.4. Експорт звітів
Формати експорту:  
  - PDF:  
      + Використання iText7 для генерації PDF  
      + Включення графіків як зображень (Chart.js → Canvas → PNG → PDF)  
      + Форматування таблиць з Borders, Padding, Colors  
      + Титульна сторінка з параметрами звіту та логотипом  
      + Footer з датою генерації та page numbers  
  - Excel (XLSX):  
      + Використання EPPlus для генерації XLSX  
      + Декілька листів (sheets):  
          * "Summary" - загальні метрики  
          * "Sales Dynamics" - часові ряди  
          * "Top Products" - таблиця топ товарів  
          * "Charts" - вбудовані Excel charts  
      + Форматування комірок (numbers, dates, currency)  
      + Автоматична ширина колонок  
      + Freeze Panes для заголовків  
  - CSV:  
      + Використання CsvHelper для генерації CSV  
      + UTF-8 encoding з BOM (для Excel)  
      + Comma separator (або semicolon для європейських локалей)  
      + Тільки табличні дані, без графіків  
      + Ідеально для подальшої обробки в Excel/Power BI  
Асинхронна обробка великих звітів:  
  - Якщо звіт містить > 5000 записів → асинхронна генерація  
  - Користувач отримує job ID  
  - Перевірка статусу через polling (GET /api/analytics/export/{jobId}/status)  
  - Після завершення - download link на згенерований файл  
  - Файли зберігаються 24 години, потім автоматично видаляються
### 11.5. Безпека аналітичних даних
1. Роl-Based Access Control:  
    - MANAGER - доступ до базової аналітики (продажі, товари, категорії, акції)  
    - ADMIN - доступ до повної аналітики (включаючи фінансову та поведінку користувачів)  
    - SUPERADMIN - доступ до всієї аналітики + сирих даних (raw data exports)  
2. Data Masking для чутливих даних:  
    - PII (personally identifiable information) не включається в звіти  
    - Email та телефони користувачів не експортуються (тільки UserID або анонімізовані дані)  
    - Фінансові дані доступні тільки ADMIN/SUPERADMIN  
3. Rate Limiting:  
    - Обмеження на кількість запитів до аналітики: 10 запитів/хвилину на користувача  
    - Обмеження на кількість експортів: 5 експортів/годину на користувача  
4. Audit Logging:  
    - Логування всіх запитів до аналітики (хто, коли, які дані)  
    - Логування всіх експортів звітів (формат, розмір файлу)  
    - Retention period: 90 днів
### 11.6. Інтеграція з іншими модулями
AnalyticsModule взаємодіє з:  
  - OrderModule - читання замовлень, OrderItems для розрахунку продажів  
  - ProductModule - читання товарів, категорій для аналізу популярності  
  - PromotionModule - читання акцій для аналізу ефективності знижок  
  - WarehouseModule - читання IncomingDocuments для розрахунку витрат та ROI  
  - UserModule - читання користувачів для аналізу поведінки  
  - ReportService - генерація PDF/Excel/CSV файлів  
Напрямок залежностей: AnalyticsModule залежить від інших модулів, але інші модулі не залежать від AnalyticsModule (односторонні залежності).
### 11.7. Майбутні покращення
- Real-time analytics через WebSocket (live dashboard updates)  
- Machine Learning для прогнозування продажів  
- Predictive analytics для оптимізації складських запасів  
- Anomaly detection для виявлення аномальних паттернів продажів  
- Integration з BI tools (Power BI, Tableau) через REST API  
- Data Warehouse для довгострокового зберігання історичних даних
## 12. Висновок
Обрана архітектура та технологічний стек забезпечують баланс між простотою розробки, продуктивністю та вартістю для навчального проєкту інтернет-магазину.  
Ключові рішення:  
  - Трирівнева архітектура з REST API  
  - .NET Core + React для надійності та продуктивності  
  - MySQL для безкоштовного managed database  
  - Docker для контейнеризації та портативності  
  - Render.com для простого та безкоштовного деплою  
  - Повнофункціональна система знижок з підтримкою стакування, промокодів та автоматичної верифікації студентів  
  - Інтеграція з University API для автоматизації студентських знижок  
  - Комплексна система аналітики для управління бізнес-процесами (продажі, товари, категорії, акції, фінанси, конверсія, поведінка користувачів)  
  - Детальна аналітика ефективності акцій для прийняття бізнес-рішень  
  - Експорт звітів у форматах PDF/Excel/CSV для подальшого аналізу  
Система здатна обслуговувати 500-1000 одночасних користувачів та має потенціал для масштабування до 10000+ користувачів з мінімальними архітектурними змінами. Архітектура системи знижок та аналітики побудована з урахуванням гнучкості та можливості розширення функціоналу (A/B тестування, ML-рекомендації, програми лояльності, predictive analytics, Data Warehouse integration).