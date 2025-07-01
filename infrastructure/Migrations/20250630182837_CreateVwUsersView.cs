using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateVwUsersView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the view if it exists
            migrationBuilder.Sql(@"
      IF OBJECT_ID('dbo.VwUsers', 'V') IS NOT NULL
          DROP VIEW dbo.VwUsers;
    ");

            // Re-create it from scratch
            migrationBuilder.Sql(@"
      CREATE VIEW dbo.VwUsers
      AS
      SELECT
          u.Id,
          u.FullName    AS Name,
          u.Email       AS Email,
          u.AvatarUrl   AS ImageUser,
          u.AcceptTerms AS ActiveUser,
          ISNULL(r.Name, '') AS Role
      FROM AspNetUsers u
      LEFT JOIN AspNetUserRoles ur
        ON ur.UserId = u.Id
      LEFT JOIN AspNetRoles r
        ON r.Id = ur.RoleId;
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.VwUsers;");
        }

    }
}
