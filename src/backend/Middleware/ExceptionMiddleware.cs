using System.Net;
using System.Text.Json;
using KhduSouvenirShop.API.Models.Common;

namespace KhduSouvenirShop.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";

                var (statusCode, errorCode, message) = ex switch
                {
                    UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized", "Доступ заборонено"),
                    KeyNotFoundException => (HttpStatusCode.NotFound, "NotFound", "Ресурс не знайдено"),
                    ArgumentException => (HttpStatusCode.BadRequest, "BadRequest", ex.Message),
                    _ => (HttpStatusCode.InternalServerError, "InternalServerError", _env.IsDevelopment() ? ex.Message : "Сталася непередбачена помилка")
                };

                context.Response.StatusCode = (int)statusCode;

                var response = ApiResponse<object>.FailureResult(message, errorCode);

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(response, options);

                await context.Response.WriteAsync(json);
            }
        }
    }
}
