using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE VIEW dbo.VwUsers
            AS
            SELECT 
                u.Id,
                u.UserName,
                u.Email,
                r.Name AS RoleName
            FROM AspNetUsers u
            JOIN AspNetUserRoles ur ON ur.UserId = u.Id
            JOIN AspNetRoles r       ON r.Id      = ur.RoleId;
        ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.VwUsers;");
        }
    }
}
