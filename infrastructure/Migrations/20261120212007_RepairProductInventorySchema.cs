using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairProductInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The prior migration is present in __EFMigrationsHistory on the production
            // database, but these columns are absent. Add only what is missing.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Products', N'OldPrice') IS NULL
                    ALTER TABLE dbo.Products ADD OldPrice decimal(18,2) NULL;

                IF COL_LENGTH(N'dbo.Products', N'SKU') IS NULL
                    ALTER TABLE dbo.Products ADD SKU nvarchar(50) NULL;

                IF COL_LENGTH(N'dbo.Products', N'StockQuantity') IS NULL
                    ALTER TABLE dbo.Products ADD StockQuantity int NOT NULL CONSTRAINT DF_Products_StockQuantity DEFAULT (0);

                IF COL_LENGTH(N'dbo.Products', N'LowStockThreshold') IS NULL
                    ALTER TABLE dbo.Products ADD LowStockThreshold int NOT NULL CONSTRAINT DF_Products_LowStockThreshold DEFAULT (0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately left empty: these columns may have existed before this repair.
        }
    }
}
