using Microsoft.AspNetCore.Http;
using System.IO;

namespace KhduSouvenirShop.API.Services
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile file, string subFolder = "products");
        Task DeleteImageAsync(string imageURL);
    }
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ImageService> _logger;
        private const long _maxFileSize = 5 * 1024 * 1024; // 5MB
        // Дозволені розширення та відповідні їм MIME-типи (ключ - розширення)
        private static readonly Dictionary<string, string> _allowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".webp", "image/webp" }
        };
        public ImageService(IWebHostEnvironment env, ILogger<ImageService> logger)
        {
            _env = env;
            _logger = logger;
        }
        public async Task<string> UploadImageAsync(IFormFile file, string subFolder = "products")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Файл порожній");
            if (file.Length > _maxFileSize)
                throw new ArgumentException("Розмір файлу перевищує 5МБ");
            // Перевірка розширення
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !_allowedTypes.TryGetValue(extension, out var expectedMime))
                throw new ArgumentException("Непідтримуваний формат файлу. Дозволені: jpg, jpeg, png, webp");
            // Перевірка MIME-типу (Content-Type), який передав клієнт
            if (string.IsNullOrWhiteSpace(file.ContentType) ||
                !string.Equals(file.ContentType.Trim(), expectedMime, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"MIME-тип файлу не відповідає розширенню. Очікується: {expectedMime}");
            // Перевірка реального вмісту файлу за магічними байтами
            if (!IsValidImageContent(file, extension))
                throw new ArgumentException("Вміст файлу не є коректним зображенням підтримуваного формату");
            // Шлях збереження: wwwroot/images/products/YYYY/MM/
            var now = DateTime.UtcNow;
            var uploadsFolder = Path.Combine(
                _env.WebRootPath,
                "images",
                subFolder,
                now.ToString("yyyy"),
                now.ToString("MM"));
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            // Генерація унікального імені файлу (GUID)
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            // Повернення публічного URL для збереження в БД
            return $"/images/{subFolder}/{now:yyyy}/{now:MM}/{fileName}";
        }
        public Task DeleteImageAsync(string imageURL)
        {
            if (string.IsNullOrEmpty(imageURL)) return Task.CompletedTask;
            try
            {
                // Видалення початкового слешу, якщо він є
                var relativePath = imageURL.StartsWith("/") ? imageURL.Substring(1) : imageURL;
                var fullPath = Path.Combine(_env.WebRootPath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Image deleted: {Path}", fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {Path}", imageURL);
            }
            return Task.CompletedTask;
        }
        // Перевірка магічних байтів для визначення реального формату зображення
        private static bool IsValidImageContent(IFormFile file, string extension)
        {
            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[12];
            var read = stream.Read(header);
            if (read < 4) return false;
            return extension switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                ".webp" => read >= 12 &&
                           header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                           header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
                _ => false
            };
        }
    }
}
