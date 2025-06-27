using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nafath.Data.Migrations
{
    /// <inheritdoc />
    public partial class add : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "f5cc3b4f-0d0a-4704-8643-32ed5dc6083d");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ff193553-2861-4109-bd83-b4fe4c7d713c");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "117af781-a39d-4d33-86bb-3e0e6746ded7", "2330d643-eefc-4f16-8c8b-4cb26723b363", "Admin", "admin" },
                    { "499cb10d-cdab-45b4-91fc-7a3bf90e0edf", "7376f771-4753-4c13-9ac0-977095a13911", "User", "user" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "117af781-a39d-4d33-86bb-3e0e6746ded7");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "499cb10d-cdab-45b4-91fc-7a3bf90e0edf");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "f5cc3b4f-0d0a-4704-8643-32ed5dc6083d", "d7310f5b-5ae2-4b18-b31a-dbbe1f3d8003", "Admin", "admin" },
                    { "ff193553-2861-4109-bd83-b4fe4c7d713c", "d0a6a347-132c-4643-b848-f804a60a19c8", "User", "user" }
                });
        }
    }
}
