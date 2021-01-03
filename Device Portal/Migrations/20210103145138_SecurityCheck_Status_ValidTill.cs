using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DevicePortal.Migrations
{
    public partial class SecurityCheck_Status_ValidTill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "SecurityChecks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusEffectiveDate",
                table: "SecurityChecks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidTill",
                table: "SecurityChecks",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "SecurityChecks");

            migrationBuilder.DropColumn(
                name: "StatusEffectiveDate",
                table: "SecurityChecks");

            migrationBuilder.DropColumn(
                name: "ValidTill",
                table: "SecurityChecks");
        }
    }
}
