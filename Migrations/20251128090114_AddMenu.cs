using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QwenHT.Migrations
{
    /// <inheritdoc />
    public partial class AddMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 9, DateTimeKind.Utc).AddTicks(7473),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(5438));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 9, DateTimeKind.Utc).AddTicks(7249),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(5258));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 8, DateTimeKind.Utc).AddTicks(9453),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(318));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 8, DateTimeKind.Utc).AddTicks(9245),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(123));

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FootMins = table.Column<int>(type: "int", nullable: false),
                    BodyMins = table.Column<int>(type: "int", nullable: false),
                    StaffCommissionPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ExcommCommissionPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Category",
                table: "Menus",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Code",
                table: "Menus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Status",
                table: "Menus",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(5438),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 9, DateTimeKind.Utc).AddTicks(7473));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(5258),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 9, DateTimeKind.Utc).AddTicks(7249));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(318),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 8, DateTimeKind.Utc).AddTicks(9453));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(123),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 8, DateTimeKind.Utc).AddTicks(9245));
        }
    }
}
