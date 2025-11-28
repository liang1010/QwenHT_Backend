using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QwenHT.Migrations
{
    /// <inheritdoc />
    public partial class AddAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(3409),
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(3223),
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "OptionValues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(978));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "NavigationItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(1218));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "OptionValues");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "NavigationItems");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "NavigationItems");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "NavigationItems");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(3409));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(3223));
        }
    }
}
