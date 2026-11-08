using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhduSouvenirShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingDeliveryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryCost",
                table: "shipping",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethod",
                table: "shipping",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NovaPoshta")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDays",
                table: "shipping",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseAddress",
                table: "shipping",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryCost",
                table: "shipping");

            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "shipping");

            migrationBuilder.DropColumn(
                name: "EstimatedDays",
                table: "shipping");

            migrationBuilder.DropColumn(
                name: "WarehouseAddress",
                table: "shipping");
        }
    }
}
