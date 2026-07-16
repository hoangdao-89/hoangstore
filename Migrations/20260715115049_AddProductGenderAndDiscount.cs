using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hoangstore.Migrations
{
    /// <inheritdoc />
    public partial class AddProductGenderAndDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Products");
        }
    }
}
