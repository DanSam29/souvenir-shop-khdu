using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Всі методи потребують авторизації
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CartController> _logger;

        private readonly KhduSouvenirShop.API.Services.PromotionService _promotionService;

        public CartController(AppDbContext context, ILogger<CartController> logger, KhduSouvenirShop.API.Services.PromotionService promotionService)
        {
            _context = context;
            _logger = logger;
            _promotionService = promotionService;
        }

        // GET: api/Cart - отримання кошика поточного користувача
        [HttpGet]
        public async Task<ActionResult> GetCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Шукаємо або створюємо кошик
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Category)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                // Створюємо новий кошик
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Розраховуємо загальну суму з урахуванням промо (якщо є)
            // Отримуємо studentStatus для поточного користувача
            var user = await _context.Users.FindAsync(cart.UserId);
            string studentStatus = user?.StudentStatus ?? "NONE";

            var promos = await _promotionService.GetActivePromotionsForUserAsync(studentStatus);

            var itemsDto = cart.CartItems.Select(ci =>
            {
                var discountedPrice = _promotionService.GetPriceAfterPromotions(ci.Product, promos);
                return new
                {
                    cartItemId = ci.CartItemId,
                    productId = ci.ProductId,
                    productName = ci.Product.Name,
                    productPrice = discountedPrice,
                    originalPrice = ci.Product.Price,
                    productImage = ci.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageURL 
                                   ?? ci.Product.Images.FirstOrDefault()?.ImageURL,
                    quantity = ci.Quantity,
                    subtotal = discountedPrice * ci.Quantity
                };
            }).ToList();

            var totalAmount = itemsDto.Sum(i => (decimal)i.subtotal);

            return Ok(new
            {
                cartId = cart.CartId,
                items = itemsDto,
                totalAmount = totalAmount,
                itemCount = cart.CartItems.Count
            });
        }

        // POST: api/Cart/add - додавання товару до кошика
        [HttpPost("add")]
        public async Task<ActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (dto.Quantity <= 0)
            {
                return BadRequest(new { error = "Кількість має бути більше 0" });
            }

            // Перевірка існування товару
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return NotFound(new { error = "Товар не знайдено" });
            }

            // Перевірка наявності на складі
            if (product.Stock < dto.Quantity)
            {
                return BadRequest(new { error = $"Недостатньо товару на складі. Доступно: {product.Stock}" });
            }

            // Шукаємо або створюємо кошик
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Перевіряємо чи вже є цей товар у кошику
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + dto.Quantity;
                if (newQuantity > product.Stock)
                {
                    return BadRequest(new { error = $"Недостатньо товару на складі. Доступно: {product.Stock}" });
                }
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    AddedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Товар {ProductId} додано до кошика користувача {UserId}", dto.ProductId, userId);

            return Ok(new { message = "Товар додано до кошика" });
        }

        // PUT: api/Cart/update/{cartItemId} - оновлення кількості товару
        [HttpPut("update/{cartItemId}")]
        public async Task<ActionResult> UpdateQuantity(int cartItemId, [FromBody] UpdateQuantityDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(new { error = "Товар не знайдено у кошику" });
            }

            if (dto.Quantity <= 0)
            {
                return BadRequest(new { error = "Кількість має бути більше 0" });
            }

            // Перевірка наявності
            if (cartItem.Product.Stock < dto.Quantity)
            {
                return BadRequest(new { error = $"Недостатньо товару на складі. Доступно: {cartItem.Product.Stock}" });
            }

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Кількість оновлено" });
        }

        // DELETE: api/Cart/remove/{cartItemId} - видалення товару з кошика
        [HttpDelete("remove/{cartItemId}")]
        public async Task<ActionResult> RemoveFromCart(int cartItemId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(new { error = "Товар не знайдено у кошику" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Товар видалено з кошика" });
        }

        // DELETE: api/Cart/clear - очищення кошика
        [HttpDelete("clear")]
        public async Task<ActionResult> ClearCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return NotFound(new { error = "Кошик не знайдено" });
            }

            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Кошик очищено" });
        }
    }

    // DTO для додавання до кошика
    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    // DTO для оновлення кількості
    public class UpdateQuantityDto
    {
        public int Quantity { get; set; }
    }
}
