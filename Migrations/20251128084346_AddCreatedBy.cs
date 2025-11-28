using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QwenHT.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(5438),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(3409));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(5258),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(3223));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "OptionValues",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "Migration");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(318),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(1218));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(123),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(978));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "NavigationItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "Migration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "OptionValues");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "NavigationItems");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(3409),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(5438));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(3223),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(5258));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(1218),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(318));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 8, 34, 16, 91, DateTimeKind.Utc).AddTicks(978),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 8, 43, 41, 457, DateTimeKind.Utc).AddTicks(123));
        }
    }
}
