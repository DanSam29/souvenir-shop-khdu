# Backend API - Інтернет-магазин сувенірної продукції ХДУ

## 1. Вступ

### 1.1. Опис Backend
Backend реалізовано на платформі **.NET Core 8.0** з використанням **ASP.NET Core Web API**. Система надає RESTful API для управління товарами, користувачами, кошиком та замовленнями інтернет-магазину.

**Основні ендпоінти:**
- `/api/Products` — управління каталогом товарів (GET, POST, PUT, DELETE)
- `/api/Categories` — управління категоріями (GET, POST, PUT, DELETE)
- `/api/Users/register` — реєстрація користувачів (POST)
- `/api/Users/login` — авторизація користувачів (POST)
- `/api/Cart` — управління кошиком (GET, POST, PUT, DELETE)

**Архітектура:** Клієнт-серверна трирівнева модель з використанням Entity Framework Core для роботи з MySQL базою даних.

---

## 2. Налаштування середовища розробки

### 2.1. Встановлене програмне забезпечення
- **.NET SDK 8.0** — для розробки Web API
- **Visual Studio Code** — IDE для розробки
- **MySQL 8.0** — реляційна база даних
- **Postman / Thunder Client** — для тестування API

### 2.2. Встановлення NuGet пакетів

Для налаштування проекту виконайте наступні команди:

```bash
# Створення проекту
dotnet new webapi -n KhduSouvenirShop.API
cd KhduSouvenirShop.API

# Entity Framework Core для MySQL
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.0

# Serilog для логування
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.0
dotnet add package Serilog.Sinks.File --version 5.0.0

# Swagger для документації API
dotnet add package Swashbuckle.AspNetCore --version 6.5.0

# BCrypt для хешування паролів
dotnet add package BCrypt.Net-Next --version 4.0.3
```

### 2.3. Конфігурація підключення до БД

Файл `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=khdu_souvenir_shop;User=root;Password=YOUR_PASSWORD;CharSet=utf8mb4;"
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS",
    "Issuer": "KhduSouvenirShopAPI",
    "Audience": "KhduSouvenirShopUsers",
    "ExpireMinutes": 1440
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## 3. Код сервера

### 3.1. Program.cs (повний код)

```csharp
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Налаштування Serilog для логування
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Підключення до MySQL через Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Налаштування контролерів з обробкою циклічних посилань у JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = 
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Додавання Swagger для документації API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Налаштування CORS для frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Пояснення ключових компонентів:**
- **`AddDbContext<AppDbContext>`** — реєстрація контексту бази даних для DI
- **`AddControllers()`** — додавання підтримки MVC контролерів
- **`UseSerilog()`** — підключення логування через Serilog
- **`AddCors()`** — налаштування CORS для доступу з React frontend
- **`ReferenceHandler.IgnoreCycles`** — обробка циклічних посилань у JSON (User → Cart → User)

### 3.2. AppDbContext.cs (контекст бази даних)

```csharp
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Shipping> Shippings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Унікальний індекс на Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Один кошик на користувача
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            // Унікальний номер замовлення
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            // Каскадне видалення кошика при видаленні користувача
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Каскадне видалення товарів кошика
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```

### 3.3. ProductsController.cs (приклад контролера)

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            _logger.LogInformation("Запит на отримання всіх товарів");
            
            var products = await _context.Products
                .Include(p => p.Category)   // Завантажуємо категорію
                .Include(p => p.Images)     // Завантажуємо зображення
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound(new { error = "Товар не знайдено" });
            }

            return Ok(product);
        }

        // GET: api/Products/search?query=футболка
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Пошуковий запит порожній" });
            }

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.Name.Contains(query) || p.Description.Contains(query))
                .ToListAsync();

            return Ok(products);
        }
    }
}
```

**Пояснення методів контролера:**
- **`[Route("api/[controller]")]`** — визначає базовий URL роут (`/api/Products`)
- **`[HttpGet]`** — атрибут для GET-запитів (читання даних)
- **`Include()`** — eager loading для завантаження пов'язаних сутностей (Category, Images)
- **`async/await`** — асинхронне виконання для кращої продуктивності
- **`Ok()`, `NotFound()`, `BadRequest()`** — повернення HTTP статусів (200, 404, 400)

---

## 4. Тестування API

### 4.1. Інструменти тестування
Для тестування використовувався **Thunder Client** (розширення VS Code) та **Postman**.

### 4.2. Приклади запитів та відповідей

#### Тест 1: GET всі товари
**Запит:**
```http
GET http://localhost:5225/api/Products
```

**Відповідь (200 OK):**
```json
[
  {
    "productId": 1,
    "name": "Футболка ХДУ",
    "description": "Футболка з логотипом університету",
    "price": 350.00,
    "weight": 0.200,
    "categoryId": 1,
    "stock": 15,
    "createdAt": "2024-12-14T08:53:47",
    "category": {
      "categoryId": 1,
      "name": "Одяг",
      "displayOrder": 1
    },
    "images": []
  },
  {
    "productId": 2,
    "name": "Блокнот",
    "description": "Блокнот А5 з емблемою",
    "price": 120.00,
    "weight": 0.150,
    "categoryId": 2,
    "stock": 30,
    "createdAt": "2024-12-14T08:53:47",
    "category": {
      "categoryId": 2,
      "name": "Канцелярія",
      "displayOrder": 2
    },
    "images": []
  }
]
```

#### Тест 2: GET товар за ID
**Запит:**
```http
GET http://localhost:5225/api/Products/1
```

**Відповідь (200 OK):**
```json
{
  "productId": 1,
  "name": "Футболка ХДУ",
  "description": "Футболка з логотипом університету",
  "price": 350.00,
  "weight": 0.200,
  "categoryId": 1,
  "stock": 15,
  "createdAt": "2024-12-14T08:53:47",
  "category": {
    "categoryId": 1,
    "name": "Одяг",
    "displayOrder": 1
  },
  "images": []
}
```

#### Тест 3: Пошук товарів
**Запит:**
```http
GET http://localhost:5225/api/Products/search?query=футболка
```

**Відповідь (200 OK):**
```json
[
  {
    "productId": 1,
    "name": "Футболка ХДУ",
    "description": "Футболка з логотипом університету",
    "price": 350.00,
    "categoryId": 1,
    "stock": 15
  }
]
```

#### Тест 4: POST реєстрація користувача
**Запит:**
```http
POST http://localhost:5225/api/Users/register
Content-Type: application/json

{
  "firstName": "Данило",
  "lastName": "Морозов",
  "email": "danylo@khdu.edu.ua",
  "password": "Password123!",
  "phone": "+380501234567"
}
```

**Відповідь (201 Created):**
```json
{
  "userId": 3,
  "firstName": "Данило",
  "lastName": "Морозов",
  "email": "danylo@khdu.edu.ua",
  "role": "Customer"
}
```

#### Тест 5: Валідація - Email вже існує
**Запит:**
```http
POST http://localhost:5225/api/Users/register
Content-Type: application/json

{
  "firstName": "Тест",
  "lastName": "Тестович",
  "email": "danylo@khdu.edu.ua",
  "password": "Password123!"
}
```

**Відповідь (409 Conflict):**
```json
{
  "error": "Цей Email вже зареєстрований"
}
```

### 4.3. Приклади curl команд

```bash
# GET всі товари
curl -X GET http://localhost:5225/api/Products

# GET товар за ID
curl -X GET http://localhost:5225/api/Products/1

# Пошук товарів
curl -X GET "http://localhost:5225/api/Products/search?query=футболка"

# POST реєстрація користувача
curl -X POST http://localhost:5225/api/Users/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Іван",
    "lastName": "Петров",
    "email": "ivan@khdu.edu.ua",
    "password": "SecurePass123!",
    "phone": "+380671234567"
  }'

# GET категорії
curl -X GET http://localhost:5225/api/Categories

# GET товари за категорією
curl -X GET http://localhost:5225/api/Products/category/1
```

### 4.4. Swagger документація
API автоматично документується через Swagger UI за адресою:
```
http://localhost:5225/swagger
```

Swagger надає інтерактивний інтерфейс для тестування всіх ендпоінтів з можливістю відправки запитів безпосередньо з браузера.

---

## 5. Інтеграція з базою даних

### 5.1. Моделі Entity Framework Core

#### User.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Customer";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційні властивості
        public virtual Cart? Cart { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
```

#### Product.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,3)")]
        public decimal Weight { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int Stock { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційні властивості
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
```

### 5.2. Приклади SQL-запитів

#### Вибірка всіх користувачів
```sql
SELECT * FROM Users;
```

#### Вибірка товарів з категоріями (еквівалент Include())
```sql
SELECT 
    p.ProductId, 
    p.Name, 
    p.Price, 
    p.Stock,
    c.Name AS CategoryName
FROM Products p
INNER JOIN Categories c ON p.CategoryId = c.CategoryId;
```

#### Пошук товарів за назвою
```sql
SELECT * FROM Products 
WHERE Name LIKE '%футболка%' OR Description LIKE '%футболка%';
```

#### Перевірка унікальності email при реєстрації
```sql
SELECT COUNT(*) FROM Users WHERE Email = 'test@khdu.edu.ua';
```

#### Отримання кошика користувача з товарами
```sql
SELECT 
    ci.CartItemId,
    p.Name AS ProductName,
    p.Price,
    ci.Quantity,
    (p.Price * ci.Quantity) AS Subtotal
FROM CartItems ci
INNER JOIN Products p ON ci.ProductId = p.ProductId
INNER JOIN Carts c ON ci.CartId = c.CartId
WHERE c.UserId = 1;
```

### 5.3. Зв'язок моделей з таблицями БД

| C# Model         | MySQL Table          | Тип зв'язку                     |
|------------------|----------------------|---------------------------------|
| User             | Users                | 1:1 з Cart, 1:N з Orders        |
| Product          | Products             | 1:N з ProductImage, CartItem    |
| Category         | Categories           | 1:N з Product (самозв'язок)     |
| Cart             | Carts                | 1:1 з User, 1:N з CartItem      |
| CartItem         | CartItems            | N:1 з Cart, Product             |
| Order            | Orders               | N:1 з User, 1:N з OrderItem     |
| OrderItem        | OrderItems           | N:1 з Order, Product            |
| Payment          | Payments             | 1:1 з Order                     |
| Shipping         | Shippings            | 1:1 з Order                     |

---

## 6. Висновки

### 6.1. Досягнуті результати
1. **Налаштовано середовище розробки:** Встановлено .NET SDK 8.0, налаштовано підключення до MySQL через Entity Framework Core
2. **Створено 13 моделей даних:** User, Product, Category, Cart, Order та інші, що відповідають UML-діаграмі класів
3. **Реалізовано 4 базові API контролери:**
   - `ProductsController` — 4 ендпоінти (GET всі, за ID, пошук, за категорією)
   - `CategoriesController` — 3 ендпоінти (GET всі, за ID, товари категорії)
   - `UsersController` — 2 ендпоінти (POST реєстрація, GET за ID)
   - `CartController` — 5 ендпоінтів (GET, POST, PUT, DELETE)
4. **Налаштовано інфраструктуру:**
   - Swagger для автоматичної документації API
   - Serilog для логування запитів та помилок
   - CORS для інтеграції з React frontend
   - Обробка циклічних посилань у JSON серіалізації
5. **Протестовано всі ендпоінти:** Через Thunder Client та Swagger UI, всі тести пройдені успішно (статуси 200, 201, 404, 409)

### 6.2. Відповідність User Stories
Backend API повністю відповідає функціональним вимогам з практичної роботи №2:

- **FR-G-01:** Перегляд каталогу — `GET /api/Products`
- **FR-G-02:** Пошук та фільтрація — `GET /api/Products/search?query=...`
- **FR-G-03:** Детальна інформація про товар — `GET /api/Products/{id}`
- **FR-G-04:** Реєстрація користувача — `POST /api/Users/register`
- **FR-C-01:** Додавання товарів до кошика — `POST /api/Cart/items`

### 6.3. Готовність до інтеграції з Frontend
Backend API повністю готовий до інтеграції з React frontend:
- Всі ендпоінти повертають коректний JSON
- CORS налаштований для доступу з `http://localhost:3000`
- Документація доступна через Swagger UI
- Логування дозволяє відстежувати помилки на продакшені

### 6.4. Наступні кроки
1. Реалізація JWT аутентифікації для захищених ендпоінтів
2. Додавання ендпоінтів для оформлення замовлення з інтеграцією Stripe
3. Реалізація складського обліку через прибуткові/видаткові накладні
4. Створення адміністративних ендпоінтів для менеджерів
5. Написання unit-тестів для контролерів та сервісів
6. Інтеграція з Nova Poshta API для розрахунку доставки

---

## 7. Список використаних технологій

| Технологія                       | Версія | Призначення                          |
|----------------------------------|--------|--------------------------------------|
| .NET Core SDK                    | 8.0    | Платформа розробки Web API           |
| ASP.NET Core Web API             | 8.0    | Фреймворк для створення REST API     |
| Entity Framework Core            | 8.0    | ORM для роботи з MySQL               |
| Pomelo.EntityFrameworkCore.MySql | 8.0.0  | Провайдер MySQL для EF Core          |
| Serilog                          | 8.0.0  | Структуроване логування              |
| Swashbuckle (Swagger)            | 6.5.0  | Автоматична документація API         |
| BCrypt.Net                       | 4.0.3  | Хешування паролів                    |
| MySQL                            | 8.0    | Реляційна база даних                 |

---

## 8. Корисні посилання

- [Офіційна документація .NET](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core/)
- [ASP.NET Core Web API Tutorial](https://docs.microsoft.com/aspnet/core/tutorials/first-web-api)
- [Swagger Documentation](https://swagger.io/docs/)
- [Репозиторій проекту на GitHub](https://github.com/DanSam29/souvenir-shop-khdu)