using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace KhduSouvenirShop.API.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // DbSet для кожної таблиці
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<OrderHistory> OrderHistories { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Shipping> Shippings { get; set; } = null!;
        public DbSet<Promotion> Promotions { get; set; } = null!;
        public DbSet<UserPromotion> UserPromotions { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<IncomingDocument> IncomingDocuments { get; set; } = null!;
        public DbSet<OutgoingDocument> OutgoingDocuments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var seedDate = new DateTime(2026, 1, 1);

            // Налаштування унікальних індексів
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            modelBuilder.Entity<Promotion>()
                .HasIndex(p => p.PromoCode)
                .IsUnique();

            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Email)
                .IsUnique();

            // Налаштування унікальної пари для CartItems
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.ProductId })
                .IsUnique();

            // Налаштування унікальної пари для UserPromotions
            modelBuilder.Entity<UserPromotion>()
                .HasIndex(up => new { up.UserId, up.PromotionId })
                .IsUnique();

            // Налаштування каскадного видалення
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Налаштування рекурсивного зв'язку для категорій
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Налаштування зв'язків Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Shipping)
                .WithOne(s => s.Order)
                .HasForeignKey<Shipping>(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderHistory>()
                .HasOne(oh => oh.Order)
                .WithMany(o => o.OrderHistories)
                .HasForeignKey(oh => oh.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Налаштування зв'язків Promotion
            modelBuilder.Entity<Promotion>()
                .HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Налаштування зв'язків UserPromotion (many-to-many)
            modelBuilder.Entity<UserPromotion>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPromotions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPromotion>()
                .HasOne(up => up.Promotion)
                .WithMany()
                .HasForeignKey(up => up.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Налаштування зв'язків IncomingDocuments
            modelBuilder.Entity<IncomingDocument>()
                .HasOne(id => id.Product)
                .WithMany()
                .HasForeignKey(id => id.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IncomingDocument>()
                .HasOne(id => id.Company)
                .WithMany(c => c.IncomingDocuments)
                .HasForeignKey(id => id.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IncomingDocument>()
                .HasOne(id => id.CreatedByUser)
                .WithMany()
                .HasForeignKey(id => id.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Налаштування зв'язків OutgoingDocuments
            modelBuilder.Entity<OutgoingDocument>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OutgoingDocument>()
                .HasOne(od => od.Order)
                .WithMany()
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OutgoingDocument>()
                .HasOne(od => od.Company)
                .WithMany(c => c.OutgoingDocuments)
                .HasForeignKey(od => od.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OutgoingDocument>()
                .HasOne(od => od.AppliedPromotion)
                .WithMany()
                .HasForeignKey(od => od.AppliedPromotionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OutgoingDocument>()
                .HasOne(od => od.CreatedByUser)
                .WithMany()
                .HasForeignKey(od => od.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Початкові категорії
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Футболки", NameEn = "T-Shirts", DisplayOrder = 1, CreatedAt = seedDate },
                new Category { CategoryId = 2, Name = "Худі", NameEn = "Hoodies", DisplayOrder = 2, CreatedAt = seedDate },
                new Category { CategoryId = 3, Name = "Гуртки", NameEn = "Mugs", DisplayOrder = 3, CreatedAt = seedDate },
                new Category { CategoryId = 4, Name = "Канцелярія", NameEn = "Stationery", DisplayOrder = 4, CreatedAt = seedDate }
            );

            // Початкові товари
            modelBuilder.Entity<Product>().HasData(
                new Product { ProductId = 1, Name = "Худі KSU Black", NameEn = "KSU Hoodie Black", Description = "Якісне чорне худі з логотипом університету", DescriptionEn = "High-quality black hoodie with university logo", Price = 850, Weight = 0.600m, CategoryId = 2, Stock = 25, CreatedAt = seedDate },
                new Product { ProductId = 2, Name = "Худі KSU Grey", NameEn = "KSU Hoodie Grey", Description = "Комфортне сіре худі", DescriptionEn = "Comfortable grey hoodie", Price = 850, Weight = 0.600m, CategoryId = 2, Stock = 15, CreatedAt = seedDate },
                new Product { ProductId = 3, Name = "Футболка KSU White", NameEn = "KSU T-Shirt White", Description = "Базова біла футболка", DescriptionEn = "Basic white t-shirt", Price = 350, Weight = 0.200m, CategoryId = 1, Stock = 50, CreatedAt = seedDate },
                new Product { ProductId = 4, Name = "Футболка KSU Blue", NameEn = "KSU T-Shirt Blue", Description = "Синя патріотична футболка", DescriptionEn = "Blue patriotic t-shirt", Price = 350, Weight = 0.200m, CategoryId = 1, Stock = 40, CreatedAt = seedDate },
                new Product { ProductId = 5, Name = "Гурток керамічний", NameEn = "Ceramic Mug", Description = "Біла керамічна кружка 330мл", DescriptionEn = "White ceramic mug 330ml", Price = 150, Weight = 0.350m, CategoryId = 3, Stock = 100, CreatedAt = seedDate },
                new Product { ProductId = 6, Name = "Гурток-термос", NameEn = "Thermo Mug", Description = "Металевий гурток-термос", DescriptionEn = "Metal thermo mug", Price = 450, Weight = 0.400m, CategoryId = 3, Stock = 30, CreatedAt = seedDate },
                new Product { ProductId = 7, Name = "Блокнот А5", NameEn = "Notebook A5", Description = "Блокнот у лінійку на 96 аркушів", DescriptionEn = "96-page lined notebook", Price = 120, Weight = 0.250m, CategoryId = 4, Stock = 200, CreatedAt = seedDate },
                new Product { ProductId = 8, Name = "Ручка металева", NameEn = "Metal Pen", Description = "Стильна металева ручка", DescriptionEn = "Stylish metal pen", Price = 80, Weight = 0.050m, CategoryId = 4, Stock = 500, CreatedAt = seedDate }
            );

            // Додавання фільтрів для Soft Delete
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(ConvertFilterExpression(entityType.ClrType));
                }
            }

            // Зображення товарів
            modelBuilder.Entity<ProductImage>().HasData(
                // Футболки
                new ProductImage { ImageId = 1, ProductId = 1, ImageURL = "/images/products/tshirt-white-front.jpg", IsPrimary = true, DisplayOrder = 1 },
                new ProductImage { ImageId = 2, ProductId = 1, ImageURL = "/images/products/tshirt-white-back.jpg", IsPrimary = false, DisplayOrder = 2 },
                new ProductImage { ImageId = 3, ProductId = 2, ImageURL = "/images/products/tshirt-blue-front.jpg", IsPrimary = true, DisplayOrder = 1 },
                new ProductImage { ImageId = 4, ProductId = 2, ImageURL = "/images/products/tshirt-blue-back.jpg", IsPrimary = false, DisplayOrder = 2 },
                // Худі
                new ProductImage { ImageId = 5, ProductId = 3, ImageURL = "/images/products/hoodie-black-front.jpg", IsPrimary = true, DisplayOrder = 1 },
                new ProductImage { ImageId = 6, ProductId = 3, ImageURL = "/images/products/hoodie-black-back.jpg", IsPrimary = false, DisplayOrder = 2 },
                new ProductImage { ImageId = 7, ProductId = 4, ImageURL = "/images/products/hoodie-grey-front.jpg", IsPrimary = true, DisplayOrder = 1 },
                new ProductImage { ImageId = 8, ProductId = 4, ImageURL = "/images/products/hoodie-grey-back.jpg", IsPrimary = false, DisplayOrder = 2 },
                // Гуртки
                new ProductImage { ImageId = 9, ProductId = 5, ImageURL = "/images/products/mug-ceramic-white.jpg", IsPrimary = true, DisplayOrder = 1 },
                new ProductImage { ImageId = 10, ProductId = 6, ImageURL = "/images/products/mug-thermo-steel.jpg", IsPrimary = true, DisplayOrder = 1 },
                // Канцелярія
                new ProductImage { ImageId = 11, ProductId = 7, ImageURL = "/images/products/pen-metal.jpg", IsPrimary = true, DisplayOrder = 1 },
                new ProductImage { ImageId = 12, ProductId = 8, ImageURL = "/images/products/notebook-a5.jpg", IsPrimary = true, DisplayOrder = 1 }
            );
        }

        private static System.Linq.Expressions.LambdaExpression ConvertFilterExpression(Type type)
        { 
            var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
            var falseConstant = System.Linq.Expressions.Expression.Constant(false);
            var propertyMethod = typeof(EF).GetMethod("Property")!.MakeGenericMethod(typeof(bool));
            var isDeletedProperty = System.Linq.Expressions.Expression.Call(propertyMethod, parameter, System.Linq.Expressions.Expression.Constant("IsDeleted"));
            var compareExpression = System.Linq.Expressions.Expression.Equal(isDeletedProperty, falseConstant);
            return System.Linq.Expressions.Expression.Lambda(compareExpression, parameter);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        { 
            var userId = GetCurrentUserId();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is IAuditable auditable)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditable.CreatedAt = DateTime.UtcNow;
                            auditable.UpdatedAt = auditable.CreatedAt; // Set UpdatedAt to match CreatedAt on creation
                            auditable.CreatedBy = userId;
                            break;
                        case EntityState.Modified:
                            auditable.UpdatedAt = DateTime.UtcNow;
                            auditable.UpdatedBy = userId;
                            break;
                    }
                }

                if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    softDeletable.IsDeleted = true;
                    softDeletable.DeletedAt = DateTime.UtcNow;
                    softDeletable.DeletedBy = userId;

                    // Якщо сутність також підтримує аудит, оновимо поле UpdatedAt
                    if (entry.Entity is IAuditable auditableEntity)
                    {
                        auditableEntity.UpdatedAt = softDeletable.DeletedAt;
                        auditableEntity.UpdatedBy = userId;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }
    }
}
