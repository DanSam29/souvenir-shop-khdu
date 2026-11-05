using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhduSouvenirShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAddressesToShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "shipping",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "shipping",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "shipping",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipping_UserId",
                table: "shipping",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_shipping_users_UserId",
                table: "shipping",
                column: "UserId",
                principalTable: "users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shipping_users_UserId",
                table: "shipping");

            migrationBuilder.DropIndex(
                name: "IX_shipping_UserId",
                table: "shipping");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "shipping");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "shipping");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "shipping",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
