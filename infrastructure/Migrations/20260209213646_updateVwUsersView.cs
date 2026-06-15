using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateVwUsersView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop the old view if it exists so we can recreate it with the new columns
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.VwUsers");

            // 2. Create the new view matching your VwUser class
            migrationBuilder.Sql(@"
        EXEC('
            CREATE VIEW dbo.VwUsers
            AS
            SELECT 
                u.Id,
                u.FullName AS Name,            -- Maps to public string? Name
                u.Email,                       -- Maps to public string? Email
                u.AvatarUrl AS ImageUser,      -- Maps to public String? ImageUser
                u.EmailConfirmed AS ActiveUser,-- Maps to public bool? ActiveUser
                r.Name AS Role                 -- Maps to public string? Role
            FROM ApplicationUsers u
            JOIN AspNetUserRoles ur ON ur.UserId = u.Id
            JOIN AspNetRoles r      ON r.Id      = ur.RoleId
        ')
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert logic (Optional: drops the view)
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.VwUsers");
        }
    }
}
