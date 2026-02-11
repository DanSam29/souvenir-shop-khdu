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
        }
    }
}
