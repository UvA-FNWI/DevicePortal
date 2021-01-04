using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DevicePortal.Migrations
{
    public partial class Device_StatusEffectiveDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StatusEffectiveDate",
                table: "Devices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusEffectiveDate",
                table: "Devices");
        }
    }
}
