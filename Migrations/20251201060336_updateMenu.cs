using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QwenHT.Migrations
{
    /// <inheritdoc />
    public partial class updateMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StaffCommissionPrice",
                table: "Menus",
                newName: "StaffCommission");

            migrationBuilder.RenameColumn(
                name: "ExcommCommissionPrice",
                table: "Menus",
                newName: "ExtraCommission");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 12, 1, 6, 3, 34, 902, DateTimeKind.Utc).AddTicks(1746),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 9, DateTimeKind.Utc).AddTicks(7473));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 12, 1, 6, 3, 34, 902, DateTimeKind.Utc).AddTicks(1606),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 9, DateTimeKind.Utc).AddTicks(7249));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 12, 1, 6, 3, 34, 901, DateTimeKind.Utc).AddTicks(7292),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 8, DateTimeKind.Utc).AddTicks(9453));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 12, 1, 6, 3, 34, 901, DateTimeKind.Utc).AddTicks(7130),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 8, DateTimeKind.Utc).AddTicks(9245));

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Outlet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Request = table.Column<bool>(type: "bit", nullable: true),
                    FootCream = table.Column<bool>(type: "bit", nullable: true),
                    Oil = table.Column<bool>(type: "bit", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ExtraCommission = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    StaffCommission = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sales_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sales_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_MenuId",
                table: "Sales",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Outlet",
                table: "Sales",
                column: "Outlet");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SalesDate",
                table: "Sales",
                column: "SalesDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_StaffId",
                table: "Sales",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status",
                table: "Sales",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.RenameColumn(
                name: "StaffCommission",
                table: "Menus",
                newName: "StaffCommissionPrice");

            migrationBuilder.RenameColumn(
                name: "ExtraCommission",
                table: "Menus",
                newName: "ExcommCommissionPrice");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 9, DateTimeKind.Utc).AddTicks(7473),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 12, 1, 6, 3, 34, 902, DateTimeKind.Utc).AddTicks(1746));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "OptionValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 9, DateTimeKind.Utc).AddTicks(7249),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 12, 1, 6, 3, 34, 902, DateTimeKind.Utc).AddTicks(1606));

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdated",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 8, DateTimeKind.Utc).AddTicks(9453),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 12, 1, 6, 3, 34, 901, DateTimeKind.Utc).AddTicks(7292));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "NavigationItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 11, 28, 9, 1, 14, 8, DateTimeKind.Utc).AddTicks(9245),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 12, 1, 6, 3, 34, 901, DateTimeKind.Utc).AddTicks(7130));
        }
    }
}
