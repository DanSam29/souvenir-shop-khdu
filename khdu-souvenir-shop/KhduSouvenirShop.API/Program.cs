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

var builder = WebApplication.CreateBuilder(args);

// 1. Налаштування Rate Limiting (Обмеження частоти запитів)
builder.Services.AddRateLimiter(options =>
{
    // Глобальний лімітер для всіх запитів
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100, // Максимум 100 запитів
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1) // за 1 хвилину
            }));

    // Спеціальний лімітер для авторизації (захист від brute-force)
    options.AddPolicy("AuthPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5, // 5 спроб
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(5) // на 5 хвилин
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(ApiResponse<object>.FailureResult("Занадто багато запитів. Спробуйте пізніше.", "TooManyRequests"), token);
    };
});

// 2. Налаштування CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Ваша адреса фронтенду
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

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
        ClockSkew = TimeSpan.Zero // Без затримки при перевірці часу
    };
});

builder.Services.AddAuthorization();

// Кеш пам'яті для швидких GET-запитів каталогу
builder.Services.AddMemoryCache();

// Promotion service
builder.Services.AddScoped<KhduSouvenirShop.API.Services.PromotionService>();

// Payment service
builder.Services.AddScoped<KhduSouvenirShop.API.Services.IPaymentService, KhduSouvenirShop.API.Services.PaymentService>();

// Email service
builder.Services.AddScoped<KhduSouvenirShop.API.Services.IEmailService, KhduSouvenirShop.API.Services.EmailService>();

// Image service
builder.Services.AddScoped<KhduSouvenirShop.API.Services.IImageService, KhduSouvenirShop.API.Services.ImageService>();

// Nova Poshta service
builder.Services.AddHttpClient<KhduSouvenirShop.API.Services.INovaPoshtaService, KhduSouvenirShop.API.Services.NovaPoshtaService>();

// University service
builder.Services.AddHttpClient<KhduSouvenirShop.API.Services.IUniversityService, KhduSouvenirShop.API.Services.UniversityService>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Додавання контролерів з обробкою циклічних посилань та кастомною обробкою помилок валідації
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

// Додавання Swagger для документації API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "KhduSouvenirShop API",
        Version = "v1",
        Description = "API для інтернет-магазину сувенірної продукції ХДУ"
    });

    // Додавання JWT авторизації в Swagger
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

// Додавання CORS (для frontend)
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAll", policy =>
//     {
//         policy.AllowAnyOrigin()
//               .AllowAnyMethod()
//               .AllowAnyHeader();
//     });
// });

var app = builder.Build();

// Глобальна обробка помилок
app.UseMiddleware<ExceptionMiddleware>();

// Middleware для розробки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Налаштування HSTS для продакшн (Stage 12: Security)
    app.UseHsts();
}

app.UseHttpsRedirection();

// Додавання Security Headers (Stage 12: Security)
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

// Обслуговування статичних файлів (зображення, тощо)
// Доступні за адресою: http://localhost:5000/images/products/image.jpg
app.UseStaticFiles();

app.UseAuthentication();  // Спочатку аутентифікація
app.UseAuthorization();   // Потім авторизація

app.MapControllers();

Log.Information("Сервер запущено!");
app.Run();
