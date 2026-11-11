using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhduSouvenirShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredLanguageToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "users",
                type: "varchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "ua")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "users");
        }
    }
}
