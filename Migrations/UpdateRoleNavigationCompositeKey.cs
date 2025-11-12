using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QwenHT.Migrations
{
    public partial class UpdateRoleNavigationCompositeKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old index
            migrationBuilder.DropIndex(
                name: "IX_RoleNavigations_RoleId",
                table: "RoleNavigations");

            // Drop and recreate the primary key
            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleNavigations",
                table: "RoleNavigations");

            // Recreate the primary key with the correct columns
            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleNavigations",
                table: "RoleNavigations",
                columns: new[] { "RoleName", "NavigationItemId" });
                
            // Add the correct index
            migrationBuilder.CreateIndex(
                name: "IX_RoleNavigations_NavigationItemId",
                table: "RoleNavigations",
                column: "NavigationItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the changes
            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleNavigations",
                table: "RoleNavigations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleNavigations",
                table: "RoleNavigations",
                columns: new[] { "RoleId", "NavigationItemId" });
                
            migrationBuilder.CreateIndex(
                name: "IX_RoleNavigations_RoleId",
                table: "RoleNavigations",
                column: "RoleId");
        }
    }
}