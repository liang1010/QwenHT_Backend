using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QwenHT.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoleNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleNavigations",
                table: "RoleNavigations");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "RoleNavigations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleNavigations",
                table: "RoleNavigations",
                columns: new[] { "RoleName", "NavigationItemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleNavigations",
                table: "RoleNavigations");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "RoleNavigations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleNavigations",
                table: "RoleNavigations",
                columns: new[] { "RoleId", "NavigationItemId" });
        }
    }
}
