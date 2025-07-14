using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nafath.Migrations
{
    /// <inheritdoc />
    public partial class addOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                 name: "TotalPrice",
                 table: "OrderItems",
                 type: "decimal(18,2)",
                 nullable: false,
                 computedColumnSql: "[Quantity] * [UnitPrice]",
                 stored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldComputedColumnSql: "[Quantity] * [UnitPrice]");
        }
    }
}
