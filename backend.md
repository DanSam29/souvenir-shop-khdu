# Backend API для інтернет-магазину сувенірної продукції ХДУ

## Вступ

Backend для інтернет-магазину сувенірної продукції Херсонського державного університету розроблено на **ASP.NET Core 8.0 (C#)** з використанням **Entity Framework Core** для роботи з базою даних **MySQL**.

**Реалізовані базові ендпоінти:**
- `GET /api/Products` — отримання всіх товарів
- `GET /api/Products/{id}` — отримання товару за ID
- `GET /api/Products/search?query={query}` — пошук товарів
- `GET /api/Products/category/{categoryId}` — товари за категорією
- `GET /api/Categories` — отримання всіх категорій
- `GET /api/Categories/{id}` — категорія за ID
- `POST /api/Users/register` — реєстрація користувача
- `GET /api/Users/{id}` — отримання даних користувача

**Технологічний стек:**
- **Framework:** ASP.NET Core 8.0 Web API
- **ORM:** Entity Framework Core 8.0
- **База даних:** MySQL 8.0 (Pomelo.EntityFrameworkCore.MySql)
- **Логування:** Serilog
- **Документація API:** Swagger/OpenAPI
- **Тестування:** Thunder Client, Swagger UI

---

## Налаштування середовища

### 1. Встановлення .NET SDK
```bash
# Перевірка версії
dotnet --version
```

Завантажити: https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Створення проєкту
```bash
mkdir khdu-souvenir-shop
cd khdu-souvenir-shop
dotnet new webapi -n KhduSouvenirShop.API
cd KhduSouvenirShop.API
```

### 3. Встановлення NuGet пакетів
```bash
# Entity Framework Core для MySQL
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.0

# Serilog для логування
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.0
dotnet add package Serilog.Sinks.File --version 5.0.0

# Swagger для документації API
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

### 4. Налаштування підключення до БД

**Файл `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=khdu_souvenir_shop;User=root;Password=YOUR_PASSWORD;CharSet=utf8mb4;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

**Важливо:** Замініть `YOUR_PASSWORD` на ваш реальний пароль від MySQL!

---

## Структура проєкту

```
KhduSouvenirShop.API/
├── Controllers/           # API контролери
│   ├── ProductsController.cs
│   ├── CategoriesController.cs
│   └── UsersController.cs
├── Models/               # Моделі даних (Entity Framework)
│   ├── User.cs
│   ├── Product.cs
│   ├── Category.cs
│   ├── ProductImage.cs
│   ├── Cart.cs
│   ├── CartItem.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── Payment.cs
│   └── Shipping.cs
├── Data/                 # DbContext
│   └── AppDbContext.cs
├── DTOs/                 # Data Transfer Objects
│   └── RegisterDto.cs
├── logs/                 # Логи Serilog
│   └── log-20241214.txt
├── appsettings.json      # Конфігурація
└── Program.cs            # Точка входу
```

---

## Код сервера

### Program.cs (точка входу)

```csharp
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Data;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Налаштування Serilog для логування
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Додавання DbContext з MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Додавання контролерів з обробкою циклічних посилань
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Додавання Swagger для документації API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Khdu Souvenir Shop API",
        Version = "v1",
        Description = "API для інтернет-магазину сувенірної продукції ХДУ"
    });
});

// Додавання CORS (для frontend)
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

// Middleware для розробки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

Log.Information("Сервер запущено!");
app.Run();
```

**Пояснення ключових моментів:**

- **`builder.Services.AddDbContext<AppDbContext>`** — реєстрація контексту БД для Dependency Injection
- **`ReferenceHandler.IgnoreCycles`** — обробка циклічних посилань у JSON (Product → Category → Products → ...)
- **`UseSerilog()`** — підключення Serilog для логування запитів та помилок
- **`AddSwagger()`** — автоматична генерація документації API
- **`AddCors("AllowAll")`** — дозвіл на запити з frontend (React)

---

## Приклад контролера: ProductsController.cs

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
                .Include(p => p.Category)
                .Include(p => p.Images)
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            _logger.LogInformation("Запит на отримання товару з ID: {ProductId}", id);

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                _logger.LogWarning("Товар з ID {ProductId} не знайдено", id);
                return NotFound(new { error = "Товар не знайдено" });
            }

            return Ok(product);
        }

        // GET: api/Products/search?query=футболка
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string query)
        {
            _logger.LogInformation("Пошук товарів за запитом: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Пошуковий запит не може бути порожнім" });
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

**Пояснення:**

- **`[Route("api/[controller]")]`** — визначає базовий маршрут (`/api/Products`)
- **`[HttpGet]`** — атрибут для GET-запитів
- **`Include(p => p.Category)`** — завантаження пов'язаних даних (JOIN в SQL)
- **`_logger.LogInformation()`** — логування запитів через Serilog
- **`Ok(products)`** — повернення JSON з HTTP статусом 200
- **`NotFound(new { error = "..." })`** — повернення HTTP 404 з JSON-повідомленням

---

## Тестування API

### 1. Запуск сервера

```bash
dotnet run
```

**Очікуваний вивід:**
```
info: Сервер запущено!
info: Now listening on: http://localhost:5225
```

### 2. Тестування через Thunder Client (VS Code)

#### GET: Отримання всіх товарів

**Запит:**
```
GET http://localhost:5225/api/Products
```

**Очікувана відповідь (200 OK):**
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
    "createdAt": "2024-12-14T10:00:00Z",
    "category": {
      "categoryId": 1,
      "name": "Одяг",
      "displayOrder": 1
    },
    "images": []
  }
]
```

#### POST: Реєстрація користувача

**Запит:**
```
POST http://localhost:5225/api/Users/register
Content-Type: application/json

{
  "firstName": "Данило",
  "lastName": "Самородський",
  "email": "danylo@khdu.edu.ua",
  "password": "Password123!",
  "phone": "+380501234567"
}
```

**Очікувана відповідь (201 Created):**
```json
{
  "userId": 1,
  "firstName": "Данило",
  "lastName": "Самородський",
  "email": "danylo@khdu.edu.ua",
  "role": "Customer"
}
```

#### POST: Валідація — Email вже існує

**Запит:** Повторна реєстрація з тим самим email

**Очікувана відповідь (409 Conflict):**
```json
{
  "error": "Цей Email вже зареєстрований"
}
```

### 3. Тестування через cURL

```bash
# GET: Отримання товарів
curl -X GET http://localhost:5225/api/Products

# GET: Пошук товарів
curl -X GET "http://localhost:5225/api/Products/search?query=футболка"

# POST: Реєстрація користувача
curl -X POST http://localhost:5225/api/Users/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Іван",
    "lastName": "Петренко",
    "email": "ivan@khdu.edu.ua",
    "password": "SecurePass123!",
    "phone": "+380671234567"
  }'

# GET: Отримання користувача за ID
curl -X GET http://localhost:5225/api/Users/1
```

### 4. Тестування через Swagger UI

Відкрийте в браузері:
```
http://localhost:5225/swagger
```

Swagger автоматично генерує інтерактивну документацію для всіх ендпоінтів з можливістю тестування прямо в браузері.

---

## Інтеграція з базою даних

### Модель User (Entity Framework)

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

### AppDbContext (контекст бази даних)

```csharp
using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        // ... інші DbSet

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Унікальний індекс для Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Каскадне видалення Cart при видаленні User
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```

### Приклади SQL-запитів

**Перевірка створених користувачів:**
```sql
SELECT * FROM Users;
```

**Результат:**
```
+--------+-----------+---------------+-------------------------+----------------------------+
| UserId | FirstName | LastName      | Email                   | Role                       |
+--------+-----------+---------------+-------------------------+----------------------------+
| 1      | Данило    | Самородський  | danylo@khdu.edu.ua      | Customer                   |
| 2      | Іван      | Петренко      | ivan@khdu.edu.ua        | Customer                   |
+--------+-----------+---------------+-------------------------+----------------------------+
```

**Перевірка товарів з категоріями (JOIN):**
```sql
SELECT p.ProductId, p.Name, p.Price, c.Name AS CategoryName
FROM Products p
JOIN Categories c ON p.CategoryId = c.CategoryId;
```

**Підрахунок товарів за категоріями:**
```sql
SELECT c.Name, COUNT(p.ProductId) AS TotalProducts
FROM Categories c
LEFT JOIN Products p ON c.CategoryId = p.CategoryId
GROUP BY c.CategoryId;
```

---

## Відповідність вимогам

### Реалізовані User Stories (з практичної №2)

| ID | User Story | Статус | Ендпоінт |
|----|------------|--------|----------|
| FR-G-01 | Як гість, я хочу переглядати каталог товарів | Виконано | `GET /api/Products` |
| FR-G-02 | Як гість, я хочу шукати товари за назвою | Виконано | `GET /api/Products/search` |
| FR-G-03 | Як гість, я хочу переглядати детальну інформацію про товар | Виконано | `GET /api/Products/{id}` |
| FR-G-04 | Як гість, я хочу зареєструватися в системі | Виконано | `POST /api/Users/register` |

### Відповідність UML діаграмам

- Класи з діаграми класів реалізовані як моделі EF Core
- Навігаційні властивості відповідають зв'язкам на UML
- Методи контролерів відповідають операціям з діаграми послідовностей

### Відповідність ER-діаграмі

- Таблиці БД відповідають моделям EF Core
- Foreign Keys працюють коректно
- Каскадне видалення налаштовано через `OnModelCreating()`

---

## Вирішені проблеми

### Проблема 1: Циклічні посилання (Circular Reference)

**Симптом:**
```
System.Text.Json.JsonException: A possible object cycle was detected
```

**Причина:** EF завантажував `Product → Category → Products → Category → ...`

**Рішення:**
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = 
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
```

### Проблема 2: HTTPS сертифікат

**Симптом:** Помилка `NET::ERR_CERT_AUTHORITY_INVALID` при `https://localhost`

**Рішення для розробки:**
```bash
# Використовувати HTTP замість HTTPS
http://localhost:5225/api/Products

# Або довірити dev-сертифікату:
dotnet dev-certs https --trust
```

---

## Висновки

1. **Backend повністю функціональний:** Створено робочий RESTful API з 8 ендпоінтами для базових операцій (перегляд товарів, пошук, реєстрація).

2. **API відповідає User Stories:** Реалізовані вимоги FR-G-01 (перегляд каталогу), FR-G-02 (пошук), FR-G-03 (деталі товару), FR-G-04 (реєстрація) з практичної роботи №2.

3. **Архітектура готова до розширення:** Модульна структура (Controllers, Models, Data) дозволяє легко додавати нові ендпоінти для кошика, замовлень, платежів.

4. **Інтеграція з БД працює:** Entity Framework Core коректно генерує SQL-запити, навігаційні властивості забезпечують зручну роботу з пов'язаними даними.

5. **Логування та документація:** Serilog пише логи в консоль та файл, Swagger автоматично генерує інтерактивну документацію API.

6. **Готовність до інтеграції з Frontend:** CORS налаштовано, JSON відповіді мають коректну структуру для споживання React додатком.

**Наступні кроки:**
- Додати JWT аутентифікацію для захищених ендпоінтів
- Реалізувати ендпоінти для кошика (`POST /api/Cart/items`)
- Реалізувати ендпоінти для замовлень (`POST /api/Orders`)
- Інтеграція з Stripe API для платежів
- Інтеграція з Nova Poshta API для доставки

---

## Ресурси

- [Офіційна документація ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core документація](https://docs.microsoft.com/ef/core)
- [Swagger/OpenAPI специфікація](https://swagger.io/specification/)
- [REST API Best Practices](https://restfulapi.net/)
- [HTTP статус коди](https://httpstatuses.com/)