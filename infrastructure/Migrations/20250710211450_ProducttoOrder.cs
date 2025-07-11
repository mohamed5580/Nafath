using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProducttoOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 0) نظّف أولاً أية OrderItems تشير إلى Products غير موجودة
            migrationBuilder.Sql(@"
            DELETE O
            FROM OrderItems O
            LEFT JOIN Products P ON O.ProductId = P.Id
            WHERE P.Id IS NULL;
        ");

            // 1) ازل القيود والأعمدة القديمة
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Chairs_ChairId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ChairId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ChairId",
                table: "OrderItems");

            // 2) إجبار ProductId أن يكون NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // 3) أعد إنشاء FK نظيف
            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "OrderItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ChairId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ChairId",
                table: "OrderItems",
                column: "ChairId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Chairs_ChairId",
                table: "OrderItems",
                column: "ChairId",
                principalTable: "Chairs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
