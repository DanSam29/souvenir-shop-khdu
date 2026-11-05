using AutoMapper;
using KhduSouvenirShop.API.DTOs.Admin;
using KhduSouvenirShop.API.DTOs.Requests;
using KhduSouvenirShop.API.DTOs.Responses;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;

namespace KhduSouvenirShop.API.DTOs.Mappings
{
    /// <summary>
    /// Профіль конфігурації AutoMapper: маппінг між сутностями (Models)
    /// та DTO (Requests / Responses / Admin).
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            MapUsers();
            MapCatalog();
            MapCart();
            MapOrders();
            MapPromotions();
            MapCompanies();
            MapWarehouse();
        }

        // Користувачі та автентифікація
        private void MapUsers()
        {
            CreateMap<User, UserResponse>();
            CreateMap<User, UserSummaryResponse>();
            CreateMap<User, AdminUserListItemResponse>();
            CreateMap<User, AuthResponse>()
                .ForMember(d => d.Token, o => o.Ignore()) // JWT токен генерується окремо
                .ForMember(d => d.RefreshToken, o => o.Ignore()); // refresh-токен генерується окремо

            CreateMap<RegisterRequest, User>()
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.Password, o => o.Ignore()) // хеш паролю формується окремо (BCrypt)
                .ForMember(d => d.Role, o => o.Ignore())
                .ForMember(d => d.StudentStatus, o => o.Ignore())
                .ForMember(d => d.GPA, o => o.Ignore())
                .ForMember(d => d.StudentVerifiedAt, o => o.Ignore())
                .ForMember(d => d.StudentExpiresAt, o => o.Ignore())
                .ForMember(d => d.Cart, o => o.Ignore())
                .ForMember(d => d.Orders, o => o.Ignore())
                .ForMember(d => d.UserPromotions, o => o.Ignore())
                .IgnoreAuditFields();

            CreateMap<UpdateProfileRequest, User>()
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.Email, o => o.Ignore())
                .ForMember(d => d.Password, o => o.Ignore())
                .ForMember(d => d.Role, o => o.Ignore())
                .ForMember(d => d.StudentStatus, o => o.Ignore())
                .ForMember(d => d.GPA, o => o.Ignore())
                .ForMember(d => d.StudentVerifiedAt, o => o.Ignore())
                .ForMember(d => d.StudentExpiresAt, o => o.Ignore())
                .ForMember(d => d.Cart, o => o.Ignore())
                .ForMember(d => d.Orders, o => o.Ignore())
                .ForMember(d => d.UserPromotions, o => o.Ignore())
                .IgnoreAuditFields();
        }

        // Каталог: товари та категорії
        private void MapCatalog()
        {
            CreateMap<Product, ProductResponse>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : null));
            CreateMap<Product, ProductSummaryResponse>()
                .ForMember(d => d.PrimaryImageUrl, o => o.MapFrom(s =>
                    s.Images.Where(i => i.IsPrimary).Select(i => i.ImageURL).FirstOrDefault()
                    ?? s.Images.Select(i => i.ImageURL).FirstOrDefault()));
            CreateMap<ProductImage, ProductImageResponse>();

            CreateMap<Category, CategoryResponse>();
            CreateMap<Category, CategoryTreeResponse>();

            CreateMap<CreateProductRequest, Product>()
                .ForMember(d => d.ProductId, o => o.Ignore())
                .ForMember(d => d.Category, o => o.Ignore())
                .ForMember(d => d.Images, o => o.Ignore())
                .ForMember(d => d.CartItems, o => o.Ignore())
                .ForMember(d => d.OrderItems, o => o.Ignore())
                .IgnoreAuditFields();
            CreateMap<UpdateProductRequest, Product>()
                .ForMember(d => d.ProductId, o => o.Ignore())
                .ForMember(d => d.Category, o => o.Ignore())
                .ForMember(d => d.Images, o => o.Ignore())
                .ForMember(d => d.CartItems, o => o.Ignore())
                .ForMember(d => d.OrderItems, o => o.Ignore())
                .IgnoreAuditFields();

            CreateMap<CreateCategoryRequest, Category>()
                .ForMember(d => d.CategoryId, o => o.Ignore())
                .ForMember(d => d.ParentCategory, o => o.Ignore())
                .ForMember(d => d.SubCategories, o => o.Ignore())
                .ForMember(d => d.Products, o => o.Ignore())
                .IgnoreAuditFields();
            CreateMap<UpdateCategoryRequest, Category>()
                .ForMember(d => d.CategoryId, o => o.Ignore())
                .ForMember(d => d.ParentCategory, o => o.Ignore())
                .ForMember(d => d.SubCategories, o => o.Ignore())
                .ForMember(d => d.Products, o => o.Ignore())
                .IgnoreAuditFields();
        }

        // Кошик
        private void MapCart()
        {
            CreateMap<Cart, CartResponse>()
                .ForMember(d => d.Items, o => o.MapFrom(s => s.CartItems))
                .ForMember(d => d.ItemCount, o => o.MapFrom(s => s.CartItems.Count))
                .ForMember(d => d.TotalAmount, o => o.Ignore()); // обчислюється з урахуванням акцій

            CreateMap<CartItem, CartItemResponse>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
                .ForMember(d => d.ProductNameEn, o => o.MapFrom(s => s.Product != null ? s.Product.NameEn : null))
                .ForMember(d => d.OriginalPrice, o => o.MapFrom(s => s.Product != null ? s.Product.Price : 0))
                .ForMember(d => d.Weight, o => o.MapFrom(s => s.Product != null ? s.Product.Weight : 0))
                .ForMember(d => d.ProductImage, o => o.MapFrom(s => s.Product != null
                    ? (s.Product.Images.Where(i => i.IsPrimary).Select(i => i.ImageURL).FirstOrDefault()
                       ?? s.Product.Images.Select(i => i.ImageURL).FirstOrDefault())
                    : null))
                .ForMember(d => d.ProductPrice, o => o.Ignore()) // ціна з урахуванням акцій
                .ForMember(d => d.Subtotal, o => o.Ignore());    // обчислюється разом із ціною
        }

        // Замовлення, платежі, доставка
        private void MapOrders()
        {
            CreateMap<Order, OrderResponse>()
                .ForMember(d => d.Items, o => o.MapFrom(s => s.OrderItems));
            CreateMap<Order, OrderSummaryResponse>();
            CreateMap<Order, AdminOrderListItemResponse>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null
                    ? s.User.FirstName + " " + s.User.LastName
                    : string.Empty));
            CreateMap<OrderItem, OrderItemResponse>();
            CreateMap<OrderHistory, OrderHistoryResponse>();
            CreateMap<Payment, PaymentResponse>();
            CreateMap<Shipping, ShippingResponse>();
        }

        // Акції та промокоди
        private void MapPromotions()
        {
            CreateMap<Promotion, PromotionResponse>();
            CreateMap<CreatePromotionRequest, Promotion>()
                .ForMember(d => d.PromotionId, o => o.Ignore())
                .ForMember(d => d.CurrentUsage, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .IgnoreAuditFields();
            CreateMap<UpdatePromotionRequest, Promotion>()
                .ForMember(d => d.PromotionId, o => o.Ignore())
                .ForMember(d => d.CurrentUsage, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .IgnoreAuditFields();
        }

        // Компанії-постачальники
        private void MapCompanies()
        {
            CreateMap<Company, CompanyResponse>();
            CreateMap<CreateCompanyRequest, Company>()
                .ForMember(d => d.CompanyId, o => o.Ignore())
                .ForMember(d => d.IncomingDocuments, o => o.Ignore())
                .ForMember(d => d.OutgoingDocuments, o => o.Ignore())
                .IgnoreAuditFields();
            CreateMap<UpdateCompanyRequest, Company>()
                .ForMember(d => d.CompanyId, o => o.Ignore())
                .ForMember(d => d.IncomingDocuments, o => o.Ignore())
                .ForMember(d => d.OutgoingDocuments, o => o.Ignore())
                .IgnoreAuditFields();
        }

        // Склад (прибуткові та видаткові накладні)
        private void MapWarehouse()
        {
            CreateMap<IncomingDocument, IncomingDocumentResponse>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : null))
                .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company != null ? s.Company.Name : null));
            CreateMap<OutgoingDocument, OutgoingDocumentResponse>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : null))
                .ForMember(d => d.CompanyName, o => o.MapFrom(s => s.Company != null ? s.Company.Name : null));

            CreateMap<CreateIncomingDocumentRequest, IncomingDocument>()
                .ForMember(d => d.DocumentId, o => o.Ignore())
                .ForMember(d => d.Product, o => o.Ignore())
                .ForMember(d => d.Company, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .IgnoreAuditFields();
            CreateMap<CreateOutgoingDocumentRequest, OutgoingDocument>()
                .ForMember(d => d.DocumentId, o => o.Ignore())
                .ForMember(d => d.Product, o => o.Ignore())
                .ForMember(d => d.Order, o => o.Ignore())
                .ForMember(d => d.Company, o => o.Ignore())
                .ForMember(d => d.AppliedPromotion, o => o.Ignore())
                .ForMember(d => d.CreatedByUser, o => o.Ignore())
                .ForMember(d => d.OrderId, o => o.Ignore())
                .ForMember(d => d.OriginalPrice, o => o.Ignore())
                .ForMember(d => d.AppliedPromotionId, o => o.Ignore())
                .ForMember(d => d.DiscountAmount, o => o.Ignore())
                .ForMember(d => d.FinalPrice, o => o.Ignore())
                .IgnoreAuditFields();
        }
    }

    /// <summary>
    /// Допоміжні розширення для налаштування маппінгу.
    /// </summary>
    internal static class MappingExpressionExtensions
    {
        /// <summary>
        /// Ігнорує службові поля аудиту та soft-delete, які встановлюються
        /// автоматично в AppDbContext.SaveChangesAsync і не приходять із DTO.
        /// </summary>
        public static IMappingExpression<TSource, TDestination> IgnoreAuditFields<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression)
            where TDestination : BaseEntity
        {
            expression.ForMember(d => d.CreatedAt, o => o.Ignore());
            expression.ForMember(d => d.CreatedBy, o => o.Ignore());
            expression.ForMember(d => d.UpdatedAt, o => o.Ignore());
            expression.ForMember(d => d.UpdatedBy, o => o.Ignore());
            expression.ForMember(d => d.IsDeleted, o => o.Ignore());
            expression.ForMember(d => d.DeletedAt, o => o.Ignore());
            expression.ForMember(d => d.DeletedBy, o => o.Ignore());
            return expression;
        }
    }
}
