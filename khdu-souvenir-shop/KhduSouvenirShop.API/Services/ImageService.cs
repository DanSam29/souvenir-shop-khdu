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
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long _maxFileSize = 5 * 1024 * 1024; // 5MB

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

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                throw new ArgumentException("Непідтримуваний формат файлу. Дозволені: jpg, jpeg, png, webp");

            // Шлях до папки wwwroot/uploads
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Генерація унікального імені файлу
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Повертаємо відносний шлях для збереження в БД
            return $"/uploads/{subFolder}/{fileName}";
        }

        public Task DeleteImageAsync(string imageURL)
        {
            if (string.IsNullOrEmpty(imageURL)) return Task.CompletedTask;

            try
            {
                // Видаляємо початковий слеш, якщо він є
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
    }
}