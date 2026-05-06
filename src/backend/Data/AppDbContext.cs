using Microsoft.EntityFrameworkCore;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
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
                .HasForeignKey(p => p.CreatedByUserId)
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
                .HasForeignKey(id => id.CreatedByUserId)
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
                .HasForeignKey(od => od.CreatedByUserId)
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
                new Product { ProductId = 1, Name = "Футболка біла", NameEn = "White T-Shirt", Description = "Біла футболка з логотипом ХДУ", DescriptionEn = "White t-shirt with KSU logo", Price = 399.00m, Weight = 0.200m, CategoryId = 1, Stock = 100, CreatedAt = seedDate },
                new Product { ProductId = 2, Name = "Футболка синя", NameEn = "Blue T-Shirt", Description = "Синя футболка з логотипом ХДУ", DescriptionEn = "Blue t-shirt with KSU logo", Price = 399.00m, Weight = 0.200m, CategoryId = 1, Stock = 100, CreatedAt = seedDate },
                new Product { ProductId = 3, Name = "Худі чорний", NameEn = "Black Hoodie", Description = "Чорне худі з емблемою ХДУ", DescriptionEn = "Black hoodie with KSU emblem", Price = 899.00m, Weight = 0.800m, CategoryId = 2, Stock = 50, CreatedAt = seedDate },
                new Product { ProductId = 4, Name = "Худі сірий", NameEn = "Grey Hoodie", Description = "Сіре худі з емблемою ХДУ", DescriptionEn = "Grey hoodie with KSU emblem", Price = 899.00m, Weight = 0.800m, CategoryId = 2, Stock = 50, CreatedAt = seedDate },
                new Product { ProductId = 5, Name = "Гуртка керамічна", NameEn = "Ceramic Mug", Description = "Біла керамічна гуртка з логотипом ХДУ", DescriptionEn = "White ceramic mug with KSU logo", Price = 199.00m, Weight = 0.350m, CategoryId = 3, Stock = 200, CreatedAt = seedDate },
                new Product { ProductId = 6, Name = "Термогорнятко сталь", NameEn = "Steel Thermo Mug", Description = "Термогорнятко зі сталі з логотипом ХДУ", DescriptionEn = "Steel thermo mug with KSU logo", Price = 499.00m, Weight = 0.450m, CategoryId = 3, Stock = 120, CreatedAt = seedDate },
                new Product { ProductId = 7, Name = "Ручка металева", NameEn = "Metal Pen", Description = "Металева ручка з гравіюванням ХДУ", DescriptionEn = "Metal pen with KSU engraving", Price = 129.00m, Weight = 0.050m, CategoryId = 4, Stock = 300, CreatedAt = seedDate },
                new Product { ProductId = 8, Name = "Блокнот A5", NameEn = "A5 Notebook", Description = "Блокнот формату A5 з логотипом ХДУ", DescriptionEn = "A5 notebook with KSU logo", Price = 149.00m, Weight = 0.250m, CategoryId = 4, Stock = 180, CreatedAt = seedDate }
            );

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
    }
}
