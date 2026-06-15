using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

public partial class CreateOrUpdateVwUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID('VwUsers', 'V') IS NOT NULL
    DROP VIEW VwUsers;
");

        migrationBuilder.Sql(@"
CREATE VIEW VwUsers
AS
SELECT 
    u.Id,
    u.FullName,
    u.Email,
    r.Name AS Role
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID('VwUsers', 'V') IS NOT NULL
    DROP VIEW VwUsers;
");
    }
}
