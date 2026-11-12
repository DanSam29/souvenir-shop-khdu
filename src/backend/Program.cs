using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Middleware;
using KhduSouvenirShop.API.Models.Common;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using KhduSouvenirShop.API.DTOs.Mappings;
using Polly;

var builder = WebApplication.CreateBuilder(args);
// Захист від занадто частого звернення до сервера
builder.Services.AddRateLimiter(options =>
{
    // Загальне обмеження для всіх користувачів: 100 запитів на хвилину від одного користувача
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    // Окремий захист для входу в систему: лише 5 спроб на 5 хвилин
    options.AddPolicy("AuthPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(5)
            }));
    // Обмеження для Nova Poshta: 50 запитів на хвилину (ліміт API NP)
    options.AddPolicy("NovaPoshtaPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 50,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(ApiResponse<object>.FailureResult("Занадто багато запитів. Спробуйте пізніше.", "TooManyRequests"), token);
    };
});
// Дозвіл фронтенду звертатися до нашого API
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
// Налаштування запису логів (в консоль, в папку logs)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();
// Підключення БД MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 8))));
// Налаштування безпеки (JWT токенів)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key не налаштовано в appsettings.json");
var key = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer не налаштовано"),
        ValidAudience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience не налаштовано"),
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
// Пришвидшення роботи каталогу товарів через кешування в пам'яті
builder.Services.AddMemoryCache();
// Реєстрація внутрішніх сервісів (бізнес-логіку)
// Рендерер HTML-шаблонів листів (singleton із кешем вмісту шаблонів)
builder.Services.AddSingleton<KhduSouvenirShop.API.Services.IEmailTemplateRenderer, KhduSouvenirShop.API.Services.EmailTemplateRenderer>();
// Черга листів у пам'яті (singleton, спільна для API та фонового сервісу)
builder.Services.AddSingleton<KhduSouvenirShop.API.Services.IEmailQueue, KhduSouvenirShop.API.Services.EmailQueue>();
builder.Services.AddScoped<KhduSouvenirShop.API.Services.PromotionService>();
builder.Services.AddScoped<KhduSouvenirShop.API.Services.WarehouseService>();
builder.Services.AddScoped<KhduSouvenirShop.API.Services.IPaymentService, KhduSouvenirShop.API.Services.PaymentService>();
builder.Services.AddScoped<KhduSouvenirShop.API.Services.IEmailService, KhduSouvenirShop.API.Services.EmailService>();
builder.Services.AddScoped<KhduSouvenirShop.API.Services.IImageService, KhduSouvenirShop.API.Services.ImageService>();
builder.Services.AddScoped<KhduSouvenirShop.API.Services.IAuthService, KhduSouvenirShop.API.Services.AuthService>();
builder.Services.AddScoped<KhduSouvenirShop.API.Services.IAnalyticsService, KhduSouvenirShop.API.Services.AnalyticsService>();
builder.Services.AddScoped<KhduSouvenirShop.API.Services.IReportExportService, KhduSouvenirShop.API.Services.ReportExportService>();
builder.Services.AddHttpClient<KhduSouvenirShop.API.Services.INovaPoshtaService, KhduSouvenirShop.API.Services.NovaPoshtaService>()
    // Retry для тимчасових збоїв NP API: 3 спроби з експоненційним відкатом (2с, 4с, 8с)
    .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(
        3,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
builder.Services.AddHttpClient<KhduSouvenirShop.API.Services.IUniversityService, KhduSouvenirShop.API.Services.UniversityService>();
// Фоновий сервіс, що раз на 5 хвилин повторює невдалі верифікації студентів з черги
builder.Services.AddHostedService<KhduSouvenirShop.API.Services.VerificationQueueBackgroundService>();
// Фоновий сервіс, що раз на добу деактивує протерміновані студентські статуси та надсилає нагадування
builder.Services.AddHostedService<KhduSouvenirShop.API.Services.StudentStatusExpiryBackgroundService>();
// Фоновий сервіс, що надсилає листи з черги, не блокуючи відповідь API
builder.Services.AddHostedService<KhduSouvenirShop.API.Services.EmailQueueBackgroundService>();
// Реєстрація репозиторіїв (шар доступу до даних)
builder.Services.AddScoped(typeof(KhduSouvenirShop.API.Repositories.IRepository<>), typeof(KhduSouvenirShop.API.Repositories.Repository<>));
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.IUserRepository, KhduSouvenirShop.API.Repositories.UserRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.IProductRepository, KhduSouvenirShop.API.Repositories.ProductRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.ICategoryRepository, KhduSouvenirShop.API.Repositories.CategoryRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.ICartRepository, KhduSouvenirShop.API.Repositories.CartRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.IOrderRepository, KhduSouvenirShop.API.Repositories.OrderRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.IPaymentRepository, KhduSouvenirShop.API.Repositories.PaymentRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.IShippingRepository, KhduSouvenirShop.API.Repositories.ShippingRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.IPromotionRepository, KhduSouvenirShop.API.Repositories.PromotionRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.IWarehouseRepository, KhduSouvenirShop.API.Repositories.WarehouseRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.ICompanyRepository, KhduSouvenirShop.API.Repositories.CompanyRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.IAnalyticsRepository, KhduSouvenirShop.API.Repositories.AnalyticsRepository>();
builder.Services.AddScoped<KhduSouvenirShop.API.Repositories.IUnitOfWork, KhduSouvenirShop.API.Repositories.UnitOfWork>();
// Реєстрація AutoMapper (шар перетворення сутностей у DTO)
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
// Автоматична перевірка правильності введених даних (валідація)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
// Налаштування контролерів та формату обміну даними (JSON)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            var response = ApiResponse<object>.FailureResult(errors, "Validation Error");
            return new BadRequestObjectResult(response);
        };
    });
// Створення сторінки документації API (Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "KhduSouvenirShop API",
        Version = "v1",
        Description = "API для інтернет-магазину сувенірної продукції ХДУ"
    });
    // Додавання можливості ключ безпеки прямо у Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введіть 'Bearer' [пробіл] і ваш токен у текстове поле нижче.\r\n\r\nПриклад: \"Bearer 12345abcdef\""
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
var app = builder.Build();
// Початкове наповнення БД корисними тестовими даними (виконується один раз, повторні запуски нічого не змінюють)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<KhduSouvenirShop.API.Data.AppDbContext>();
        await KhduSouvenirShop.API.Data.DbSeeder.SeedAsync(dbContext, app.Logger);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Не вдалося виконати початкове наповнення БД");
    }
}
// Глобальний обробник помилок для зручного відображення помилок
app.UseMiddleware<ExceptionMiddleware>();
// Налаштування увімкнення Swagger в режимі розробки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}
app.UseHttpsRedirection();
// Додавання заголовків безпеки, щоб захистити сайт від хакерських атак
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self'; frame-ancestors 'none';");
    await next();
});
app.UseCors("DefaultPolicy");
app.UseRateLimiter();
app.UseStaticFiles(); // Дозвіл на перегляд картинок товарів через браузер
app.UseAuthentication();  // Автентифікація користувача
app.UseAuthorization();   // Перевірка прав користувача
app.MapControllers();
Log.Information("Сервер запущено!");
app.Run();