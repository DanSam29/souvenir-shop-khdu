using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Services
{
    // Сервіс складу: єдине джерело істини для залишків - документи (прибуткові/видаткові),
    // а Product.Stock - похідне поле для швидкого відображення
    public class WarehouseService
    {
        private readonly IProductRepository _productRepository;
        private readonly IRepository<IncomingDocument> _incomingRepository;
        private readonly IRepository<OutgoingDocument> _outgoingRepository;

        // Поріг низького залишку (налаштовується, типово 5)
        public int LowStockThreshold { get; }

        public WarehouseService(
            IProductRepository productRepository,
            IRepository<IncomingDocument> incomingRepository,
            IRepository<OutgoingDocument> outgoingRepository,
            IConfiguration configuration)
        {
            _productRepository = productRepository;
            _incomingRepository = incomingRepository;
            _outgoingRepository = outgoingRepository;
            LowStockThreshold = configuration.GetValue<int?>("Warehouse:LowStockThreshold") ?? 5;
        }

        // Перерахунок залишку товару: SUM(прибуткові) - SUM(видаткові).
        // Викликається після кожної зміни документів, щоб Product.Stock відповідав документам.
        public async Task<int> RecalculateStockAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return 0;
            var totalIncoming = await _incomingRepository.Query()
                .Where(d => d.ProductId == productId)
                .SumAsync(d => (int?)d.Quantity) ?? 0;
            var totalOutgoing = await _outgoingRepository.Query()
                .Where(d => d.ProductId == productId)
                .SumAsync(d => (int?)d.Quantity) ?? 0;
            product.Stock = totalIncoming - totalOutgoing;
            await _productRepository.SaveChangesAsync();
            return product.Stock;
        }

        // Ознака низького залишку за поточним порогом
        public bool IsLowStock(int stock) => stock <= LowStockThreshold;

        // Список товарів із низьким залишком (для відмітки в адмін-панелі)
        public async Task<IReadOnlyList<Product>> GetLowStockProductsAsync() =>
            await _productRepository.GetLowStockAsync(LowStockThreshold);
    }
}
