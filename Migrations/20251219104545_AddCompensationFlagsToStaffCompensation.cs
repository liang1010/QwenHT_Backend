using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QwenHT.Migrations
{
    /// <inheritdoc />
    public partial class AddCompensationFlagsToStaffCompensation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCommissionPercentage",
                table: "StaffCompensations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsGuaranteeIncome",
                table: "StaffCompensations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRate",
                table: "StaffCompensations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCommissionPercentage",
                table: "StaffCompensations");

            migrationBuilder.DropColumn(
                name: "IsGuaranteeIncome",
                table: "StaffCompensations");

            migrationBuilder.DropColumn(
                name: "IsRate",
                table: "StaffCompensations");
        }
    }
}
