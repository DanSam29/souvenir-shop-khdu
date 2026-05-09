# Матриця відстеження вимог
## 1. Функціональні вимоги для Гостя
| ID Вимоги | Назва вимоги | Use Case | Модуль | Класи | Activity Diagram | Sequence Diagram | Пріоритет | Статус |
|-----------|--------------|----------|--------|-------|------------------|------------------|-----------|--------|
| FR-G-01 | Перегляд каталогу товарів | UC\_Catalog, UC\_Sort | CatalogModule | ProductController, ProductService, ProductRepository, Product, Category | - | - | Високий | Реалізовано |
| FR-G-02 | Пошук та фільтрація товарів | UC\_Search, UC\_Filter | CatalogModule | ProductController, ProductService, ProductRepository, Product | - | - | Високий | Реалізовано |
| FR-G-03 | Перегляд детальної інформації про товар | UC\_Details | CatalogModule | ProductController, ProductService, Product, ProductImage | - | - | Високий | Реалізовано |
| FR-G-04 | Реєстрація та авторизація | UC\_Register, UC\_Login | UserModule, UniversityModule | AuthController, AuthService, UserRepository, User, JWTService, UniversityIntegration, PromotionService | Activity Diagram Реєстрація користувача | Sequence Diagram Реєстрація користувача | Високий | Реалізовано |
## 2. Функціональні вимоги для Користувача
| ID Вимоги | Назва вимоги | Use Case | Модуль | Класи | Activity Diagram | Sequence Diagram | Пріоритет | Статус |
|-----------|--------------|----------|--------|-------|------------------|------------------|-----------|--------|
| FR-C-01 | Додавання товарів до кошика | UC\_AddToCart, UC\_ViewCart, UC\_ChangeQuantity, UC\_RemoveFromCart | CartModule | CartController, CartService, CartRepository, Cart, CartItem | - | - | Високий | Реалізовано |
| FR-C-02 | Оформлення замовлення (з видатковою накладною, застосуванням знижок) | UC\_PlaceOrder, UC\_ConfirmOrder, UC\_SelectPayment, UC\_SelectDelivery | OrderModule, PaymentModule, ShippingModule, WarehouseModule, PromotionModule | OrderController, OrderService, OrderRepository, Order, OrderItem, WarehouseService, OutgoingDocument, PromotionService, Promotion | Activity Diagram Оформлення замовлення, Activity Diagram Створення видаткової накладної | Sequence Diagram Оформлення замовлення,  Sequence Diagram Створення видаткової накладної | Високий | Реалізовано |
| FR-C-03 | Керування особистим кабінетом | UC\_EditContact, UC\_ChangePassword, UC\_ManageAddresses | UserModule | UserController, UserService, UserRepository, User | - | - | Середній | Реалізовано |
| FR-C-04 | Перегляд історії замовлень | UC\_ViewOrderHistory, UC\_ViewOrderDetails | OrderModule | OrderController, OrderService, OrderRepository, Order, OrderHistory | - | - | Середній | Реалізовано |
| FR-C-05 | Відстеження статусів замовлення | UC\_TrackOrder, UC\_EmailNotification | OrderModule, NotificationModule | OrderController, OrderService, Order, OrderHistory, NotificationService | - | - | Середній | Реалізовано |
| FR-C-06 | Збереження даних для доставки | UC\_ManageAddresses, UC\_AddAddress, UC\_EditAddress, UC\_DeleteAddress | ShippingModule | ShippingController, ShippingService, ShippingRepository, Shipping | - | - | Низький | Реалізовано |
| FR-C-07 | Отримання персональних пропозицій та автоматичних знижок | UC\_GetRecommendations | NotificationModule, Promotion Module, UserModule | NotificationService, PromotionService, User | - | - | Середній | Реалізовано |
| FR-C-08 | Перегляд доступних знижок | UC\_ViewAvailablePromotions | PromotionModule | PromotionController, PromotionService, PromotionRepository, Promotion, UserPromotion, User | - | - | Середній | Реалізовано |
| FR-C-09 | Верифікувати студентский статус | UC\_VerifyStudentStatus | UserModule, UniversityModule | UserController, AuthService, UniversityIntegration, PromotionService, User, UserRepository, Promotion, UserPromotion | Activity Diagram Реєстрація користувача | Sequence Diagram Реєстрація користувача | Високий | Реалізовано |
| FR-C-10 | Ввести промокод при оформленні | UC\_EnterPromocode | OrderModule, PromotionModule | OrderController, OrderService, PromotionService, PromotionRepository, Promotion, UserPromotion | Activity Diagram Оформлення замовлення | Sequence Diagram Оформлення замовлення | Середній | Реалізовано |
| FR-C-11 | Перегляд історії використаних знижок | UC\_ViewPromotionHistory | PromotionModule | PromotionController, PromotionService, UserPromotionRepository, UserPromotion, Promotion, User | - | - | Низький | Реалізовано |
## 3. Функціональні вимоги для Менеджера
| ID Вимоги | Назва вимоги | Use Case | Модуль | Класи | Activity Diagram | Sequence Diagram | Пріоритет | Статус |
|-----------|--------------|----------|--------|-------|------------------|------------------|-----------|--------|
| FR-M-01 | Керування каталогом товарів (Stock через накладні) | UC\_AddProduct, UC\_EditProduct, UC\_DeleteProduct, UC\_ViewProducts, UC\_ManageAttributes | CatalogModule, WarehouseModule | ProductController, ProductService, ProductRepository, Product, WarehouseService | Activity Diagram Додавання нового товару | Sequence Diagram Додавання нового товару | Високий | Реалізовано |
| FR-M-02 | Керування категоріями товарів | UC\_CreateCategory, UC\_EditCategory, UC\_DeleteCategory, UC\_SetupHierarchy, UC\_AssignProducts | CatalogModule | CategoryController, CategoryService, CategoryRepository, Category | - | - | Високий | Реалізовано |
| FR-M-03 | Завантаження зображень товарів | UC\_UploadImage, UC\_ValidateImage, UC\_SetMainImage, UC\_EditImage, UC\_DeleteImage | CatalogModule | ProductController, ProductService, ProductImage, MinIOIntegration | Activity Diagram Додавання нового товару (частина процесу) | Sequence Diagram Додавання нового товару (частина процесу) | Високий | Реалізовано |
| FR-M-04 | Керування акціями та знижками | UC\_CreatePromo, UC\_SelectDiscountType, UC\_SetAudience, UC\_SetConditions, UC\_SetDates, UC\_GeneratePromoCode, UC\_EditPromo, UC\_DeactivatePromo, UC\_DeletePromo, UC\_ApplyDiscountProduct, UC\_ApplyDiscountCategory, UC\_ApplyDiscountCart, UC\_AssignPersonalDiscount, UC\_ViewPromotionStats, UC\_ExportPromotionsReport, UC\_FilterPromotions, UC\_ViewActivePromotions | CatalogModule, Promotion | AdminController, PromotionController, PromotionService, PromotionRepository, Promotion, UserPromotion, User, ProductService, CategoryService | - | - | Високий | Реалізовано |
| FR-M-05 | Перегляд запасів на складі (розраховується з документів) | UC\_ViewStock, UC\_LowStockAlert, UC\_FilterStock, UC\_ExportReport | WarehouseModule | WarehouseController, WarehouseService, Product, IncomingDocument, OutgoingDocument | - | - | Середній | Реалізовано |
| FR-M-06 | Оформлення прибуткової накладної | UC\_CreateIncomingDoc, UC\_EnterIncomingData, UC\_ViewIncomingHistory, UC\_FilterIncoming, UC\_ExportIncoming | WarehouseModule | WarehouseController, WarehouseService, IncomingRepository, CompanyRepository, IncomingDocument, Company | Activity Diagram Оформлення прибуткової накладної | Sequence Diagram Оформлення прибуткової накладної | Високий | Реалізовано |
| FR-M-07 | Оформлення видаткової накладної | UC\_CreateOutgoingDoc, UC\_EnterOutgoingData, UC\_ViewOutgoingHistory, UC\_FilterIncoming, UC\_ExportIncoming | WarehouseModule | WarehouseController, WarehouseService, OutgoingRepository, CompanyRepository, OutgoingDocument, Company | Activity Diagram Створення видаткової накладної | Sequence Diagram Створення видаткової накладної | Високий | Реалізовано |
| FR-M-08 | Керування фірмами-постачальниками | UC\_ViewCompanies, UC\_AddCompany, UC\_EditCompany, UC\_DeactivateCompany, UC\_FilterCompanies, UC\_SearchCompany | WarehouseModule | WarehouseController, WarehouseService, CompanyRepository, Company | - | - | Середній | Реалізовано |
| FR-M-09 | Перегляд аналітики продажів | UC\_ViewSalesAnalytics, UC\_AnalyticsSelectPeriod, UC\_AnalyticsApplyFilters | AnalyticsModule | AnalyticsController, AnalyticsService, OrderRepository, ProductRepository, PromotionRepository | Activity Diagram Перегляд аналітики | Sequence Diagram Перегляд аналітики | Високий | Реалізовано |
| FR-M-10 | Перегляд популярності товарів | UC\_ViewProductPopularityManager, UC\_AnalyticsSelectPeriod | AnalyticsModule | AnalyticsController, AnalyticsService, ProductRepository, OrderRepository | - | - | Високий | Реалізовано |
| FR-M-11 | Перегляд аналітики категорій | UC\_ViewCategoryAnalytics, UC\_AnalyticsSelectPeriod | AnalyticsModule | AnalyticsController, AnalyticsService, CategoryRepository, ProductRepository, OrderRepository | - | - | Середній | Реалізовано |
| FR-M-12 | Експорт аналітичних звітів | UC\_ExportAnalyticsReport | AnalyticsModule | AnalyticsController, AnalyticsService, ReportService | - | - | Середній | Реалізовано |
## 4. Функціональні вимоги для Адміністратора
| ID Вимоги | Назва вимоги | Use Case | Модуль | Класи | Activity Diagram | Sequence Diagram | Пріоритет | Статус |
|-----------|--------------|----------|--------|-------|------------------|------------------|-----------|--------|
| FR-A-01 | Керування замовленнями | UC\_ViewAllOrders, UC\_ViewOrderDetails, UC\_ChangeOrderStatus, UC\_CancelOrder, UC\_FilterOrders, UC\_ExportOrders, UC\_ProcessReturn | AdminPanelModule, OrderModule | AdminController, OrderService, OrderRepository, Order, OrderHistory | Activity Diagram Зміна статусу замовлення | Sequence Diagram Зміна статусу замовлення | Високий | Реалізовано |
| FR-A-02 | Керування користувачами | UC\_ViewUsers, UC\_ViewUserProfile, UC\_ViewUserOrderHistory, UC\_FilterUsers, UC\_BlockUser, UC\_UnblockUser, UC\_ResetPassword | AdminPanelModule, UserModule | AdminController, UserService, UserRepository, User | - | - | Високий | Реалізовано |
| FR-A-03 | Перегляд фінансових звітів | UC\_ViewSalesReport, UC\_ViewIncomeReport, UC\_ViewProductPopularity, UC\_SelectPeriod, UC\_ViewCategoryStats, UC\_ViewConversion, UC\_ExportFinancialReport | AdminPanelModule | AdminController, ReportService, OrderRepository | - | - | Середній | Реалізовано |
| FR-A-04 | Керування налаштуваннями системи | UC\_SetupCurrency, UC\_SetupLanguage, UC\_ConfigureStoreSettings, UC\_ConfigureDelivery, UC\_SetupDeliveryZones, UC\_SetupDeliveryTariffs | AdminPanelModule | AdminController | - | - | Середній | Реалізовано |
| FR-A-05 | Доступ до всіх модулів адмін-панелі | UC\_AccessCatalog, UC\_AccessOrders, UC\_AccessUsers, UC\_AccessPayments, UC\_AccessDelivery, UC\_AccessReports | AdminPanelModule | AdminController | - | - | Високий | Реалізовано |
| FR-A-06 | Перегляд складських документів | UC\_ViewIncoming, UC\_ViewOutgoing, UC\_FilterWarehouseDocs, UC\_ViewDocDetails, UC\_ExportWarehouse, UC\_AnalyzeMovement | WarehouseModule, AdminPanelModule | WarehouseController, WarehouseService, IncomingRepository, OutgoingRepository | - | - | Середній | Реалізовано |
| FR-A-07 | Перегляд ефективності акцій та схвалення студентських верифікацій | UC\_ViewActivePromotions, UC\_ViewPromotionEffectiveness, UC\_ExportPromotionsReport, UC\_ApproveStudentVerification, UC\_ViewStudentsWithDiscounts, UC\_ViewVerificationQueue, UC\_RetryVerification | AdminPanelModule, PromotionModule | AdminController, PromotionService, PromotionRepository, UserRepository, UniversityIntegration, User, Promotion, UserPromotion, OrderRepository | - | - | Середній | Реалізовано |
| FR-A-08 | Перегляд повної фінансової аналітики | UC\_ViewFinancialAnalytics, UC\_SelectPeriod | AnalyticsModule, AdminPanelModule | AnalyticsController, AnalyticsService, OrderRepository, IncomingRepository, PromotionRepository | - | - | Високий | Реалізовано |
| FR-A-09 | Перегляд метрик конверсії | UC\_ViewConversionMetrics, UC\_SelectPeriod | AnalyticsModule, AdminPanelModule | AnalyticsController, AnalyticsService, OrderRepository, UserRepository, CartRepository | - | - | Високий | Реалізовано |
| FR-A-10 | Аналіз поведінки користувачів | UC\_AnalyzeUserBehavior, UC\_SelectPeriod | AnalyticsModule, AdminPanelModule | AnalyticsController, AnalyticsService, UserRepository, OrderRepository, ProductRepository | - | - | Середній | Реалізовано |
## 5. Функціональні вимоги для СуперАдміна
| ID Вимоги | Назва вимоги | Use Case | Модуль | Класи | Activity Diagram | Sequence Diagram | Пріоритет | Статус |
|-----------|--------------|----------|--------|-------|------------------|------------------|-----------|--------|
| FR-SA-01 | Керування ролями та дозволами | UC\_CreateRole, UC\_EditRole, UC\_DeleteRole, UC\_AssignRole, UC\_ViewAccessMatrix, UC\_ConfigureRolePermissions, UC\_DefineModulePermissions, UC\_DefineCRUDPermissions | AdminPanelModule | AdminController, UserService, User, Role | - | - | Високий | Реалізовано |
| FR-SA-02 | Доступ до системних налаштувань | UC\_ConfigureSecurity, UC\_ConfigureJWT, UC\_ConfigureCORS, UC\_ConfigureHTTPS, UC\_ConfigureBackup, UC\_ConfigureBackupStorage, UC\_SetBackupSchedule, UC\_ConfigureIntegrations | AdminPanelModule | AdminController | - | - | Високий | Реалізовано |
| FR-SA-03 | Перегляд системних логів | UC\_ViewSystemLogs, UC\_ViewErrorLogs, UC\_ViewUserActivityLogs, UC\_FilterLogs, UC\_ExportLogs, UC\_ClearOldLogs, UC\_ConfigureLogLevel | AdminPanelModule | AdminController | - | - | Середній | Реалізовано |
| FR-SA-04 | Керування інтеграціями | UC\_ConfigureStripe, UC\_SetStripeKeys, UC\_ConfigureStripeWebhook, UC\_TestPaymentIntegration, UC\_ConfigureNovaPoshta, UC\_SetNovaPoshtaKey, UC\_ConfigureDeliveryParams, UC\_TestDeliveryIntegration, UC\_ConfigureUniversity, UC\_SetUniversityKey, UC\_ConfigureStudentVerification, UC\_TestStudentVerification, UC\_ViewIntegrationStatus | PaymentModule, ShippingModule, UniversityModule | AdminController, StripeIntegration, NovaPoshtaIntegration, UniversityIntegration | Activity Diagram Налаштування інтеграції Stripe, Activity Diagram Реєстрація користувача | Sequence Diagram Налаштування інтеграції Stripe, Sequence Diagram Реєстрація користувача | Високий | Реалізовано |
| FR-SA-05 | Резервне копіювання та відновлення | UC\_ViewBackupList, UC\_InitiateManualBackup, UC\_RestoreFromBackup, UC\_SelectRestorePoint, UC\_DownloadBackup, UC\_DeleteOldBackup, UC\_VerifyBackupIntegrity | AdminPanelModule | AdminController | - | - | Високий | Реалізовано |
## 6. Нефункціональні вимоги - Продуктивність
| ID Вимоги | Назва виомги | Компоненти | Класи | Технології | Діаграми | Пріоритет | Статус |
|-----------|--------------|------------|-------|------------|----------|-----------|--------|
| NFR-P-01 | Обробка одночасних користувачів (500+) | CatalogModule, CartModule, OrderModule | CacheService, ProductService, InMemoryCache | In-Memory Cache, Load Balancer | C2 Container Diagram, C3 Component Diagram | Високий | Реалізовано |
| NFR-P-02 | Час відгуку системи (<3 сек для сторінок, <2 сек для API) | Всі модулі | CacheService, InMemoryCache, всі Controllers | Кешування, Nginx, оптимізовані SQL запити | C2 Container Diagram | Високий | Реалізовано |
| NFR-P-03 | Обробка транзакцій (50+ замовлень/хвилину) | OrderModule, PaymentModule, WarehouseModule | OrderService, PaymentService, WarehouseService, DbContext | Транзакції БД, асинхронна обробка | Sequence Diagram Оформлення замовлення | Високий | Реалізовано |
## 7. Нефункціональні вимоги - Надійність
| ID Вимоги | Назва виомги | Компоненти | Класи | Технології | Діаграми | Пріоритет | Статус |
|-----------|--------------|------------|-------|------------|----------|-----------|--------|
| NFR-R-01 | Доступність системи (99.5% uptime) | Всі модулі | Всі компоненти | Render.com, Health Check endpoint, моніторинг | Deployment Diagram | Високий | Реалізовано |
| NFR-R-02 | Резервне копіювання даних (щоденне) | AdminPanelModule | AdminController, DbContext | MySQL Automated Backups, Render.com | Deployment Diagram | Високий | Реалізовано |
| NFR-R-03 | Стійкість до помилок інтеграцій | PaymentModule, ShippingModule, NotificationModule | StripeIntegration, NovaPoshtaIntegration, EmailIntegration | Try-catch блоки, retry mechanism, fallback logic | Sequence Diagrams (всі), Activity Diagrams | Середній | Реалізовано |
## 8. Нефункціональні вимоги - Безпека
| ID Вимоги | Назва виомги | Компоненти | Класи | Технології | Діаграми | Пріоритет | Статус |
|-----------|--------------|------------|-------|------------|----------|-----------|--------|
| NFR-S-01 | Захищене з'єднання (HTTPS TLS 1.3) | Всі модулі | Всі Controllers | HTTPS, TLS 1.3, Render.com SSL | Deployment Diagram, C2 Container Diagram | Високий | Реалізовано |
| NFR-S-02 | Аутентифікація та авторизація (JWT + RBAC) | UserModule, AdminPanelModule | AuthService, JWTService, User, Role | JWT токени, ASP.NET Identity, RBAC | Class Diagram, Sequence Diagram Реєстрація | Високий | Реалізовано |
| NFR-S-03 | Захист платіжних даних (токенізація) | PaymentModule | PaymentService, StripeIntegration | Stripe токенізація, PCI DSS через Stripe | Sequence Diagram Оформлення замовлення | Високий | Реалізовано |
| NFR-S-04 | Захист від веб-загроз (XSS, CSRF, SQL Injection) | Всі модулі | ValidationService, всі Controllers | FluentValidation, EF Core параметризовані запити, CORS, CSRF tokens | C3 Component Diagram | Високий | Реалізовано |
## 9. Нефункціональні вимоги - Зручність використання
| ID Вимоги | Назва виомги | Компоненти | Класи | Технології | Діаграми | Пріоритет | Статус |
|-----------|--------------|------------|-------|------------|----------|-----------|--------|
| NFR-U-01 | Інтуїтивність інтерфейсу (≤5 кліків до замовлення) | CatalogModule, CartModule, OrderModule | React UI, Tailwind CSS | Use Case Diagrams (всі ролі) | Високий | Реалізовано |
| NFR-U-02 | Адаптивність дизайну (ПК, планшети, смартфони) | Всі модулі (Frontend) | React Responsive Design, Tailwind CSS | C2 Container Diagram | Високий | Реалізовано |
| NFR-U-03 | Локалізація (українська, англійська) | Всі модулі (Frontend) | React i18n | C2 Container Diagram | Середній | Реалізовано |
## 10. Нефункціональні вимоги - Масштабованість та Сумісність
| ID Вимоги | Назва виомги | Компоненти | Класи | Технології | Діаграми | Пріоритет | Статус |
|-----------|--------------|------------|-------|------------|----------|-----------|--------|
| NFR-SC-01 | Горизонтальне масштабування | Всі модулі | Docker, Render.com multiple instances, модульна архітектура | Deployment Diagram, Package Diagram | Високий | Реалізовано |
| NFR-CO-01 | Сумісність з браузерами (Chrome, Firefox, Safari, Edge) | Всі модулі (Frontend) | React, modern JavaScript (ES6+) | C2 Container Diagram | Високий | Реалізовано |
| NFR-CO-02 | Інтеграція з зовнішніми сервісами | PaymentModule, ShippingModule, NotificationModule | Stripe API, Nova Poshta API, SMTP | C1 System Context, C3 Component Diagram, Sequence Diagrams | Високий | Реалізовано |
| NFR-CO-03 | Мультивалютна підтримка платежів | PaymentModule | Stripe Adaptive Pricing (135+ валют) | Sequence Diagram Оформлення замовлення | Середній | Реалізовано |
## 11. Нефункціональні вимоги - Супроводжуваність
| ID Вимоги | Назва виомги | Компоненти | Класи | Технології | Діаграми | Пріоритет | Статус |
|-----------|--------------|------------|-------|------------|----------|-----------|--------|
| NFR-M-01 | Модульність та документація | Всі модулі | Модульна архітектура, Swagger API документація, коментарі в коді | Package Diagram, Component Diagram, Class Diagram | Середній | Реалізовано |
## 12. State Machine Diagrams Coverage
| Сутність | State Machine Diagram | Вимоги | Модуль | Статус |
|----------|-----------------------|--------|--------|--------|
| Order | State Machine Diagram Order | FR-C-02, FR-C-05, FR-A-01 | OrderModule | Реалізовано |
| Payment | State Machine Diagram Payment | FR-C-02, NFR-S-03 | PaymentModule | Реалізовано |
| OutgoingDocument | State Machine Diagram OutgoingDocument | FR-C-02, FR-M-07 | WarehouseModule | Реалізовано |
| IncomingDocument | State Machine Diagram IncomingDocument | FR-M-06 | WarehouseModule | Реалізовано |
## 13. Architecture Diagrams Coverage
| Діаграма | Призначення | Вимоги | Статус |
|----------|-------------|--------|--------|
| C1 System Context | Загальний контекст системи, актори, зовнішні системи | Всі FR, NFR-CO-02 | Реалізовано |
| C2 Container Diagram | Контейнери системи (Frontend, Backend, БД) | Всі FR, NFR-P-01, NFR-P-02, NFR-S-01 | Реалізовано |
| C3 Component Diagram | Внутрішня структура API Gateway | Всі FR, NFR-S-04, NFR-M-01 | Реалізовано |
| Deployment Diagram | Розгортання на Render.com | NFR-R-01, NFR-R-02, NFR-SC-01 | Реалізовано |
| Package Diagram | Структура модулів системи | NFR-M-01, всі FR | Реалізовано |
| Component Diagram | Детальна структура компонентів | NFR-M-01, всі FR | Реалізовано |
| Class Diagram | Структура класів системи | Всі FR, NFR-S-02, NFR-M-01 | Реалізовано |
| ER Diagram | Структура бази даних | Всі FR, NFR-R-02 | Реалізовано |
| Requirements Diagram | Зв'язки між вимогами та компонентами | Всі FR та NFR | Реалізовано |
## Підсумкова статистика покриття
Функціональні вимоги:  
  - Гість: 4/4 (100%)  
  - Користувач: 11/11 (100%)  
  - Менеджер: 12/12 (100%)  
  - Адміністратор: 10/10 (100%)  
  - СуперАдмін: 5/5 (100%)  
Загалом FR: 42/42 (100%)  
Нефункціональні вимоги:  
  - Продуктивність: 3/3 (100%)  
  - Надійність: 3/3 (100%)  
  - Безпека: 4/4 (100%)  
  - Зручність використання: 3/3 (100%)  
  - Масштабованість та Сумісність: 4/4 (100%)  
  - Супроводжуваність: 1/1 (100%)  
Загалом NFR: 18/18 (100%)  
Use Cases:  
  - Гість: 5 Use Cases  
  - Користувач: 19 Use Cases  
  - Менеджер: 56 Use Cases  
  - Адміністратор: 35 Use Cases  
  - СуперАдмін: 38 Use Cases  
Загалом Use Cases: 153 Use Cases  
Діаграми:  
  - Activity Diagrams: 8/8  
  - Sequence Diagrams: 8/8  
  - State Machine Diagrams: 4/4  
  - Architecture Diagrams: 9/9  
  - Use Case Diagrams: 5/5  
Загалом діаграм: 34 діаграми