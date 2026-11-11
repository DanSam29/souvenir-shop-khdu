using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhduSouvenirShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedAt_Status",
                table: "orders",
                columns: new[] { "CreatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_orderitems_ProductId_AppliedPromotionId",
                table: "orderitems",
                columns: new[] { "ProductId", "AppliedPromotionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_CreatedAt_Status",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orderitems_ProductId_AppliedPromotionId",
                table: "orderitems");
        }
    }
}
