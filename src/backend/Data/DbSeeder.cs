using KhduSouvenirShop.API.Models;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Data
{
    /// <summary>
    /// Початкове наповнення БД корисними тестовими даними: облікові записи, акції з промокодами,
    /// компанії-постачальники та складські документи.
    /// Методи ідемпотентні - повторний запуск застосунку не створює дублікатів.
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>Єдиний пароль для всіх тестових облікових записів.</summary>
        public const string TestPassword = "Password123!";
        private const string SeedMarker = "[seed]";

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            var created = 0;
            created += await SeedUsersAsync(context);
            created += await SeedCompaniesAsync(context);
            created += await SeedPromotionsAsync(context);
            created += await SeedWarehouseDocumentsAsync(context);

            if (created > 0)
            {
                logger.LogInformation("Початкове наповнення БД: додано {Count} тестових записів.", created);
            }
        }

        // Тестові користувачі з усіма ролями системи (пароль однаковий для зручності перевірки)
        private static async Task<int> SeedUsersAsync(AppDbContext context)
        {
            var testUsers = new[]
            {
                new { Email = "superadmin@ksu.edu.ua", FirstName = "Олег", LastName = "Сидоренко", Role = "SuperAdmin", StudentStatus = "NONE" },
                new { Email = "admin@ksu.edu.ua", FirstName = "Ірина", LastName = "Коваленко", Role = "Administrator", StudentStatus = "NONE" },
                new { Email = "manager@ksu.edu.ua", FirstName = "Максим", LastName = "Петренко", Role = "Manager", StudentStatus = "NONE" },
                new { Email = "customer@ksu.edu.ua", FirstName = "Анна", LastName = "Шевченко", Role = "Customer", StudentStatus = "NONE" },
                new { Email = "student@ksu.edu.ua", FirstName = "Богдан", LastName = "Мельник", Role = "Customer", StudentStatus = "REGULAR" }
            };

            var existingEmails = await context.Users.IgnoreQueryFilters().Select(u => u.Email).ToListAsync();
            var added = 0;
            foreach (var test in testUsers)
            {
                if (existingEmails.Contains(test.Email)) continue;

                var isStudent = test.StudentStatus != "NONE";
                context.Users.Add(new User
                {
                    FirstName = test.FirstName,
                    LastName = test.LastName,
                    Email = test.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(TestPassword, workFactor: 12),
                    Role = test.Role,
                    StudentStatus = test.StudentStatus,
                    GPA = isStudent ? 4.5m : null,
                    StudentVerifiedAt = isStudent ? DateTime.UtcNow : null,
                    StudentExpiresAt = isStudent ? DateTime.UtcNow.AddYears(1) : null
                });
                added++;
            }
            await context.SaveChangesAsync();
            return added;
        }

        // Компанії-постачальники сувенірної продукції
        private static async Task<int> SeedCompaniesAsync(AppDbContext context)
        {
            var companies = new[]
            {
                new Company { Name = "ТОВ \"Сувенір Гурт\"", ContactPerson = "Ігор Бондаренко", Phone = "+380442223344", Email = "sales@souvenir-gurt.ua", Address = "м. Херсон, вул. Університетська, 27", Notes = SeedMarker + " основний постачальник сувенірів" },
                new Company { Name = "ФОП \"Керамік Студіо\"", ContactPerson = "Марія Гриценко", Phone = "+380672221100", Email = "order@ceramic-studio.ua", Address = "м. Київ, вул. Гончарна, 5", Notes = SeedMarker + " керамічний посуд" },
                new Company { Name = "ТОВ \"Текстиль Пром\"", ContactPerson = "Сергій Литвин", Phone = "+380503334455", Email = "info@textile-prom.ua", Address = "м. Львів, вул. Шевченка, 14", Notes = SeedMarker + " одяг та текстиль" }
            };

            var existingNames = await context.Companies.IgnoreQueryFilters().Select(c => c.Name).ToListAsync();
            var added = 0;
            foreach (var company in companies)
            {
                if (existingNames.Contains(company.Name)) continue;
                context.Companies.Add(company);
                added++;
            }
            await context.SaveChangesAsync();
            return added;
        }

        // Акції з промокодами: студентська знижка, вітальний бонус та сезонна пропозиція
        private static async Task<int> SeedPromotionsAsync(AppDbContext context)
        {
            var creatorId = await context.Users.Where(u => u.Role == "SuperAdmin").Select(u => (int?)u.UserId).FirstOrDefaultAsync();
            if (creatorId is null) return 0;

            var now = DateTime.UtcNow;
            var promotions = new[]
            {
                new Promotion { Name = "Студентська знижка 10%", NameEn = "Student discount 10%", Description = "Знижка для верифікованих студентів на весь кошик", DescriptionEn = "Discount for verified students on the whole cart", Type = "PERCENTAGE", Value = 10, TargetType = "CART", AudienceType = "STUDENTS", PromoCode = "STUDENT10", IsActive = true, Priority = 10, StartDate = now.AddDays(-7), EndDate = now.AddDays(60) },
                new Promotion { Name = "Вітальний бонус 50 грн", NameEn = "Welcome bonus 50 UAH", Description = "Знижка 50 грн на перше замовлення від 300 грн", DescriptionEn = "50 UAH off the first order over 300 UAH", Type = "FIXED_AMOUNT", Value = 50, TargetType = "CART", AudienceType = "ALL", PromoCode = "WELCOME50", MinOrderAmount = 300, IsActive = true, Priority = 5, StartDate = now.AddDays(-7), EndDate = now.AddDays(60) },
                new Promotion { Name = "Сезонна акція 15%", NameEn = "Seasonal sale 15%", Description = "Сезонна знижка 15% на весь кошик", DescriptionEn = "Seasonal 15% off the whole cart", Type = "PERCENTAGE", Value = 15, TargetType = "CART", AudienceType = "ALL", PromoCode = "SPRING15", IsActive = true, Priority = 1, StartDate = now.AddDays(-7), EndDate = now.AddDays(30) }
            };

            var existingCodes = await context.Promotions.IgnoreQueryFilters().Where(p => p.PromoCode != null).Select(p => p.PromoCode!).ToListAsync();
            var added = 0;
            foreach (var promotion in promotions)
            {
                if (existingCodes.Contains(promotion.PromoCode!)) continue;
                promotion.CreatedBy = creatorId;
                context.Promotions.Add(promotion);
                added++;
            }
            await context.SaveChangesAsync();
            return added;
        }

        // Тестові складські документи: надходження товару та списання
        private static async Task<int> SeedWarehouseDocumentsAsync(AppDbContext context)
        {
            var creatorId = await context.Users.Where(u => u.Role == "Manager").Select(u => (int?)u.UserId).FirstOrDefaultAsync();
            if (creatorId is null) return 0;

            var products = await context.Products.OrderBy(p => p.ProductId).Take(3).ToListAsync();
            var companies = await context.Companies.OrderBy(c => c.CompanyId).Take(2).ToListAsync();
            if (products.Count == 0 || companies.Count == 0) return 0;

            var added = 0;
            var hasIncoming = await context.IncomingDocuments.IgnoreQueryFilters().AnyAsync(d => d.Notes != null && d.Notes.StartsWith(SeedMarker));
            if (!hasIncoming)
            {
                for (var i = 0; i < products.Count && i < companies.Count; i++)
                {
                    context.IncomingDocuments.Add(new IncomingDocument
                    {
                        ProductId = products[i].ProductId,
                        Quantity = 50 - i * 20,
                        PurchasePrice = products[i].Price * 0.6m,
                        CompanyId = companies[i].CompanyId,
                        DocumentDate = DateTime.UtcNow.AddDays(-10 - i).Date,
                        Notes = SeedMarker + " початкове надходження партії товару",
                        CreatedBy = creatorId
                    });
                    added++;
                }
            }

            var hasOutgoing = await context.OutgoingDocuments.IgnoreQueryFilters().AnyAsync(d => d.Notes != null && d.Notes.StartsWith(SeedMarker));
            if (!hasOutgoing)
            {
                context.OutgoingDocuments.Add(new OutgoingDocument
                {
                    ProductId = products[0].ProductId,
                    Quantity = 5,
                    Reason = "Inventory",
                    DocumentDate = DateTime.UtcNow.AddDays(-5).Date,
                    Notes = SeedMarker + " списання за результатами інвентаризації",
                    CreatedBy = creatorId
                });
                context.OutgoingDocuments.Add(new OutgoingDocument
                {
                    ProductId = products[^1].ProductId,
                    Quantity = 3,
                    Reason = "Damaged",
                    DocumentDate = DateTime.UtcNow.AddDays(-3).Date,
                    Notes = SeedMarker + " списання пошкодженого товару",
                    CreatedBy = creatorId
                });
                added += 2;
            }

            await context.SaveChangesAsync();
            return added;
        }
    }
}
