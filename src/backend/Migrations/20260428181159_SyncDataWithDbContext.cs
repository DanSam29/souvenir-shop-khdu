using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KhduSouvenirShop.API.Migrations
{
    /// <inheritdoc />
    public partial class SyncDataWithDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Брендований одяг ХДУ", "Одяг" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Аксесуари та сувеніри", "Аксесуари" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Канцелярські товари з символікою", "Канцелярія" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                columns: new[] { "Description", "DisplayOrder", "Name", "ParentCategoryId" },
                values: new object[] { "Футболки з логотипом ХДУ", 1, "Футболки", 1 });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CreatedAt", "Description", "DisplayOrder", "Name", "ParentCategoryId", "UpdatedAt" },
                values: new object[,]
                {
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Толстовки та худі", 2, "Худі", 1, null },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Кружки з символікою", 1, "Кружки", 2, null }
                });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "CompanyId", "Address", "ContactPerson", "CreatedAt", "Email", "IsActive", "Name", "Notes", "Phone", "UpdatedAt" },
                values: new object[] { 1, "м. Київ, вул. Хрещатик, 1", "Іванов Іван Іванович", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "textile@example.com", true, "ТОВ \"Текстиль Плюс\"", null, "+380501234567", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "ProductImages",
                keyColumn: "ImageId",
                keyValue: 11,
                column: "ImageURL",
                value: "/images/products/notebook-a5.jpg");

            migrationBuilder.UpdateData(
                table: "ProductImages",
                keyColumn: "ImageId",
                keyValue: 12,
                column: "ImageURL",
                value: "/images/products/pen-metal.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock" },
                values: new object[] { 4, "Класична біла футболка з логотипом Херсонського державного університету. Матеріал: 100% бавовна. Розміри: S, M, L, XL, XXL. Висока якість друку, стійкий до прання.", "Футболка ХДУ класична біла", 450.00m, 15 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 4, "Синя футболка преміум-якості з великим гербом університету на грудях. Матеріал: бавовна з еластаном для зручної посадки. Ідеальна для студентських заходів.", "Футболка ХДУ синя з гербом", 450.00m, 11, 0.220m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 5, "Тепле чорне худі з капюшоном та кишенею-кенгуру. Логотип університету вишитий на грудях. Матеріал: 80% бавовна, 20% поліестер. Утеплений флісом всередині.", "Худі ХДУ чорне унісекс", 800.00m, 0, 0.650m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 4,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 5, "Стильне сіре худі oversize з великим принтом \"KHERSON STATE UNIVERSITY\" на спині. Комфортний крій, м'яка тканина. Ідеальне для прохолодної погоди.", "Худі ХДУ сіре з великим принтом", 800.00m, 8, 0.700m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 5,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock" },
                values: new object[] { 6, "Біла керамічна кружка об'ємом 350 мл з логотипом ХДУ. Можна мити в посудомийній машині та використовувати в мікрохвильовці. Якісний друк не стирається.", "Кружка ХДУ керамічна класична", 200.00m, 22 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 6,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 6, "Термокружка з нержавіючої сталі об'ємом 450 мл. Зберігає температуру до 6 годин. Герметична кришка, зручна для подорожей. Лазерне гравірування логотипу.", "Термокружка ХДУ з кришкою", 450.00m, 12, 0.280m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 7,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 3, "Блокнот формату А5 на 96 аркушів в клітинку. Тверда обкладинка з тисненням логотипу. Зручна закладка-ляссе. Папір 80 г/м². Ідеальний для конспектів.", "Блокнот ХДУ А5 в клітинку", 95.00m, 3, 0.250m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 8,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 3, "Преміальна металева ручка з гравіруванням \"KHERSON STATE UNIVERSITY\". Поставляється в подарунковому футлярі. Синє чорнило, змінний стрижень. Відмінний подарунок.", "Ручка ХДУ металева в футлярі", 30.00m, 0, 0.080m });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "Email", "FirstName", "GPA", "IsActive", "Language", "LastName", "Password", "Phone", "Role", "StudentExpiresAt", "StudentStatus", "StudentVerifiedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@university.ks.ua", "Адміністратор", null, true, "ua", "Системи", "$2b$12$FIGyzdX5j3cGSfZQdY5zjeGR40lSqx51Q4G7aUd.KGuwbW6WGXMZC", null, "Administrator", null, "NONE", null, new DateTime(2026, 4, 28, 18, 11, 58, 797, DateTimeKind.Utc).AddTicks(6439) },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "manager@university.ks.ua", "Менеджер", null, true, "ua", "Магазину", "$2b$12$eaTf0o/lZJtggqx6TDuNp.ymtaoUgPOGJHjsiAB42tApObYofacze", null, "Manager", null, "NONE", null, new DateTime(2026, 4, 28, 18, 11, 58, 797, DateTimeKind.Utc).AddTicks(6452) },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "petrenko@university.ks.ua", "Олександр", 3.50m, true, "ua", "Петренко", "$2b$12$UDinljSJfa4vx5VwAa47U.XU5U2WP1jh9gKc54ekG4lIWNrHLFReO", null, "Customer", new DateTime(2026, 6, 11, 4, 50, 12, 0, DateTimeKind.Unspecified), "REGULAR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 28, 18, 11, 58, 797, DateTimeKind.Utc).AddTicks(6458) },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "kovalenko@university.ks.ua", "Марія", 4.20m, true, "ua", "Коваленко", "$2b$12$UDinljSJfa4vx5VwAa47U.XU5U2WP1jh9gKc54ekG4lIWNrHLFReO", null, "Customer", new DateTime(2026, 6, 11, 4, 50, 12, 0, DateTimeKind.Unspecified), "SCHOLARSHIP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 28, 18, 11, 58, 797, DateTimeKind.Utc).AddTicks(6477) },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "shevchenko@university.ks.ua", "Дмитро", 4.80m, true, "ua", "Шевченко", "$2b$12$UDinljSJfa4vx5VwAa47U.XU5U2WP1jh9gKc54ekG4lIWNrHLFReO", null, "Customer", new DateTime(2026, 6, 11, 4, 50, 12, 0, DateTimeKind.Unspecified), "HIGH_ACHIEVER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 28, 18, 11, 58, 797, DateTimeKind.Utc).AddTicks(6483) },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "melnyk@gmail.com", "Іван", null, true, "ua", "Мельник", "$2b$12$UDinljSJfa4vx5VwAa47U.XU5U2WP1jh9gKc54ekG4lIWNrHLFReO", null, "Customer", null, "NONE", null, new DateTime(2026, 4, 28, 18, 11, 58, 797, DateTimeKind.Utc).AddTicks(6489) },
                    { 7, new DateTime(2026, 2, 11, 8, 35, 36, 0, DateTimeKind.Unspecified), "autotest@university.ks.ua", "AutoTest", null, true, "ua", "Student", "$2a$12$sVi3uWM4IGApkUS/d9nmEe4dzNWb8oR3eBRJmgIskyFzr5yjoW8Xu", "000", "Customer", new DateTime(2027, 2, 11, 8, 35, 36, 0, DateTimeKind.Unspecified), "REGULAR", new DateTime(2026, 2, 11, 8, 35, 36, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 28, 18, 11, 58, 797, DateTimeKind.Utc).AddTicks(6493) }
                });

            migrationBuilder.InsertData(
                table: "Promotions",
                columns: new[] { "PromotionId", "AudienceType", "CreatedAt", "CreatedBy", "CurrentUsage", "Description", "EndDate", "IsActive", "MinOrderAmount", "MinQuantity", "Name", "Priority", "PromoCode", "StartDate", "TargetId", "TargetType", "Type", "UpdatedAt", "UsageLimit", "Value" },
                values: new object[,]
                {
                    { 2, "REGULAR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 0, "Постійна знижка 5% для студентів зі статусом REGULAR на весь асортимент", null, true, null, null, "Знижка для студентів REGULAR", 2, null, null, null, "CART", "PERCENTAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 5.00m },
                    { 3, "SCHOLARSHIP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 0, "Постійна знижка 10% для студентів зі статусом SCHOLARSHIP на весь асортимент", null, true, null, null, "Знижка для студентів SCHOLARSHIP", 2, null, null, null, "CART", "PERCENTAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 10.00m },
                    { 4, "HIGH_ACHIEVER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 0, "Постійна знижка 15% для студентів зі статусом HIGH_ACHIEVER на весь асортимент", null, true, null, null, "Знижка для студентів HIGH_ACHIEVER", 2, null, null, null, "CART", "PERCENTAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 15.00m },
                    { 5, "ALL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 0, "Спеціальна знижка 5% за промокодом KSU2026 на весь асортимент", null, true, null, null, "Промокод KSU2026", 2, "KSU2026", null, null, "CART", "PERCENTAGE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 5.00m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "CompanyId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "PromotionId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "PromotionId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "PromotionId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "PromotionId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { null, "Футболки" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { null, "Худі" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { null, "Гуртки" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                columns: new[] { "Description", "DisplayOrder", "Name", "ParentCategoryId" },
                values: new object[] { null, 4, "Канцелярія", null });

            migrationBuilder.UpdateData(
                table: "ProductImages",
                keyColumn: "ImageId",
                keyValue: 11,
                column: "ImageURL",
                value: "/images/products/pen-metal.jpg");

            migrationBuilder.UpdateData(
                table: "ProductImages",
                keyColumn: "ImageId",
                keyValue: 12,
                column: "ImageURL",
                value: "/images/products/notebook-a5.jpg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock" },
                values: new object[] { 1, "Біла футболка з логотипом ХДУ", "Футболка біла", 399.00m, 100 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 1, "Синя футболка з логотипом ХДУ", "Футболка синя", 399.00m, 100, 0.200m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 2, "Чорне худі з емблемою ХДУ", "Худі чорний", 899.00m, 50, 0.800m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 4,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 2, "Сіре худі з емблемою ХДУ", "Худі сірий", 899.00m, 50, 0.800m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 5,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock" },
                values: new object[] { 3, "Біла керамічна гуртка з логотипом ХДУ", "Гуртка керамічна", 199.00m, 200 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 6,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 3, "Термогорнятко зі сталі з логотипом ХДУ", "Термогорнятко сталь", 499.00m, 120, 0.450m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 7,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 4, "Металева ручка з гравіюванням ХДУ", "Ручка металева", 129.00m, 300, 0.050m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 8,
                columns: new[] { "CategoryId", "Description", "Name", "Price", "Stock", "Weight" },
                values: new object[] { 4, "Блокнот формату A5 з логотипом ХДУ", "Блокнот A5", 149.00m, 180, 0.250m });
        }
    }
}
