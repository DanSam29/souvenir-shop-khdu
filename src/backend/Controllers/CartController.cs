using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCart()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

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
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

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
                    productNameEn = ci.Product.NameEn,
                    productPrice = discountedPrice,
                    originalPrice = ci.Product.Price,
                    productImage = ci.Product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageURL 
                                   ?? ci.Product.Images.FirstOrDefault()?.ImageURL,
                    quantity = ci.Quantity,
                    weight = ci.Product.Weight,
                    subtotal = discountedPrice * ci.Quantity
                };
            }).ToList();

            var totalAmount = itemsDto.Sum(i => (decimal)i.subtotal);

            var result = new
            {
                cartId = cart.CartId,
                items = itemsDto,
                totalAmount = totalAmount,
                itemCount = cart.CartItems.Count
            };

            return Ok(ApiResponse<object>.SuccessResult(result));
        }

        // POST: api/Cart/add - додавання товару до кошика
        [HttpPost("add")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

            if (dto.Quantity <= 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Кількість має бути більше 0", "BadRequest"));
            }

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено", "NotFound"));
            }

            if (product.Stock < dto.Quantity)
            {
                return BadRequest(ApiResponse<object>.FailureResult($"Недостатньо товару на складі. Доступно: {product.Stock}", "BadRequest"));
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + dto.Quantity;
                if (newQuantity > product.Stock)
                {
                    return BadRequest(ApiResponse<object>.FailureResult($"Недостатньо товару на складі. Доступно: {product.Stock}", "BadRequest"));
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

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар додано до кошика"));
        }

        // PUT: api/Cart/update/{cartItemId} - оновлення кількості товару
        [HttpPut("update/{cartItemId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateQuantity(int cartItemId, [FromBody] UpdateQuantityDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено у кошику", "NotFound"));
            }

            if (dto.Quantity <= 0)
            {
                return BadRequest(ApiResponse<object>.FailureResult("Кількість має бути більше 0", "BadRequest"));
            }

            if (cartItem.Product.Stock < dto.Quantity)
            {
                return BadRequest(ApiResponse<object>.FailureResult($"Недостатньо товару на складі. Доступно: {cartItem.Product.Stock}", "BadRequest"));
            }

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Кількість оновлено"));
        }

        // DELETE: api/Cart/remove/{cartItemId} - видалення товару з кошика
        [HttpDelete("remove/{cartItemId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RemoveFromCart(int cartItemId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Товар не знайдено у кошику", "NotFound"));
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Товар видалено з кошика"));
        }

        // DELETE: api/Cart/clear - очищення кошика
        [HttpDelete("clear")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ClearCart()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<object>.FailureResult("Не авторизовано", "Unauthorized"));
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Кошик не знайдено", "NotFound"));
            }

            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "Кошик очищено"));
        }
    }

    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQuantityDto
    {
        public int Quantity { get; set; }
    }
}
