using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace KhduSouvenirShop.API.Services
{
    /// <summary>Рендеринг HTML-листів із файлових шаблонів (папка EmailTemplates) у спільному базовому layout.</summary>
    public interface IEmailTemplateRenderer
    {
        string Render(string templateName, IReadOnlyDictionary<string, string> tokens, string lang = "ua");
    }

    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        // Мова шаблонів за замовчуванням (мова EN додається на кроці 10.4)
        private const string DefaultLang = "ua";
        private const string LayoutName = "_Layout";
        // Токени виду {{TokenName}}
        private static readonly Regex TokenRegex = new(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled);
        private readonly string _templatesRoot;
        private readonly string _clientUrl;
        private readonly ILogger<EmailTemplateRenderer> _logger;
        // Кеш вмісту шаблонів, щоб не читати файли на кожен лист
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public EmailTemplateRenderer(IConfiguration configuration, IWebHostEnvironment environment, ILogger<EmailTemplateRenderer> logger)
        {
            _templatesRoot = Path.Combine(environment.ContentRootPath, "EmailTemplates");
            _clientUrl = configuration.GetSection("AllowedOrigins").Get<string[]>()?.FirstOrDefault()?.TrimEnd('/') ?? "http://localhost:3000";
            _logger = logger;
        }

        public string Render(string templateName, IReadOnlyDictionary<string, string> tokens, string lang = DefaultLang)
        {
            var body = LoadTemplate(lang, templateName);
            var layout = LoadTemplate(lang, LayoutName);
            // Системні токени (посилання, логотип, рік) доступні в усіх шаблонах
            var values = new Dictionary<string, string>(tokens)
            {
                ["ClientUrl"] = _clientUrl,
                ["LogoUrl"] = $"{_clientUrl}/logo512.png",
                ["Year"] = DateTime.UtcNow.Year.ToString(),
            };
            values.TryAdd("Title", "ХДУ Souvenir Shop");
            // Спочатку тіло шаблону, потім вставка у базовий layout
            values["Content"] = ReplaceTokens(body, values);
            return ReplaceTokens(layout, values);
        }

        // Завантажує шаблон із диска (з кешуванням) та відкочується на мову за замовчуванням, якщо файлу немає
        private string LoadTemplate(string lang, string name)
        {
            var cacheKey = $"{lang}/{name}";
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
            var path = Path.Combine(_templatesRoot, lang, name + ".html");
            if (!File.Exists(path))
            {
                _logger.LogWarning("Email template '{Name}' for language '{Lang}' not found, falling back to '{Default}'", name, lang, DefaultLang);
                path = Path.Combine(_templatesRoot, DefaultLang, name + ".html");
            }
            if (!File.Exists(path))
            {
                _logger.LogError("Email template not found: {Path}", path);
                throw new FileNotFoundException($"Email template '{name}' not found", path);
            }
            var content = File.ReadAllText(path);
            _cache[cacheKey] = content;
            return content;
        }

        // Замінює всі {{Token}} на передані значення (невідомі токени стають порожніми)
        private static string ReplaceTokens(string content, IReadOnlyDictionary<string, string> values)
        {
            return TokenRegex.Replace(content, match => values.TryGetValue(match.Groups[1].Value, out var value) ? value : string.Empty);
        }
    }
}
