using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DevicePortal.Migrations
{
    public partial class Device_AdditionalFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostCentre",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItracsBuilding",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItracsOutlet",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItracsRoom",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenDate",
                table: "Devices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Macadres",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Devices",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostCentre",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ItracsBuilding",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ItracsOutlet",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ItracsRoom",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LastSeenDate",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Macadres",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Devices");
        }
    }
}
