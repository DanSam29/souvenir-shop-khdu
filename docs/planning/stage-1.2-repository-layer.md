# Етап 1.2 — Звіт: Реалізація Repository Layer

Пов'язаний пункт плану: [development-plan.md](./development-plan.md) → `1.2. Реалізація Repository Layer ✅`

## Що виконано

Створено новий шар доступу до даних у папці `src/backend/Repositories/`.

### Базовий (узагальнений) репозиторій
- `IRepository.cs` — інтерфейс `IRepository<T>` з базовими CRUD-операціями (GetById, GetAll, Find, FirstOrDefault, Exists, Count, Query, Add/AddRange, Update, Remove/RemoveRange, SaveChanges).
- `Repository.cs` — узагальнена реалізація `Repository<T>` на основі `DbSet`.

### Спеціалізовані репозиторії
| Модуль | Файли | Додаткові методи |
|--------|-------|------------------|
| User | `IUserRepository.cs`, `UserRepository.cs` | GetByEmailAsync, EmailExistsAsync, GetByRoleAsync, GetWithCartAsync |
| Product | `IProductRepository.cs`, `ProductRepository.cs` | GetWithDetailsAsync, GetByCategoryAsync, GetLowStockAsync |
| Category | `ICategoryRepository.cs`, `CategoryRepository.cs` | GetRootCategoriesAsync, GetWithSubCategoriesAsync, HasProductsAsync |
| Cart | `ICartRepository.cs`, `CartRepository.cs` | GetByUserIdAsync, GetWithItemsAsync |
| Order | `IOrderRepository.cs`, `OrderRepository.cs` | GetByNumberAsync, GetWithDetailsAsync, GetByUserIdAsync |
| Payment | `IPaymentRepository.cs`, `PaymentRepository.cs` | GetByOrderIdAsync, GetByStripeSessionIdAsync, GetByStripePaymentIntentIdAsync |
| Shipping | `IShippingRepository.cs`, `ShippingRepository.cs` | GetByOrderIdAsync |
| Promotion | `IPromotionRepository.cs`, `PromotionRepository.cs` | GetByCodeAsync, GetActiveAsync |
| Warehouse | `IWarehouseRepository.cs`, `WarehouseRepository.cs` | Робота з IncomingDocument/OutgoingDocument (Get...ById, Get...List, Add, Update, Remove) |
| Company | `ICompanyRepository.cs`, `CompanyRepository.cs` | GetActiveAsync, NameExistsAsync, EmailExistsAsync |
| Analytics | `IAnalyticsRepository.cs`, `AnalyticsRepository.cs` | GetTotalRevenueAsync, GetOrdersCountAsync, GetAverageOrderValueAsync, GetTopSellingProductsAsync (+ record `ProductSalesRecord`) |

### Реєстрація в DI (`Program.cs`)
- Відкритий generic: `IRepository<>` → `Repository<>` (Scoped).
- Усі 11 спеціалізованих репозиторіїв зареєстровано як Scoped.

## Результат
- `dotnet build khdu-souvenir-shop.sln` → 0 errors, 0 warnings.

## Примітка
- Репозиторії створено й підключено; переведення запитів контролерів/сервісів на новий шар виконуватиметься під час подальшого рефакторингу (TD-05).
