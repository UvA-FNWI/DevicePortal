using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DevicePortal.Migrations
{
    public partial class AddDeviceDisownedAndHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Disowned",
                table: "Devices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DeviceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalDeviceId = table.Column<int>(type: "int", nullable: false),
                    DateHistory = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OS_Type = table.Column<int>(type: "int", nullable: false),
                    OS_Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CostCentre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSeenDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItracsBuilding = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItracsRoom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItracsOutlet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Macadres = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusEffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Disowned = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceHistories_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceHistories_Devices_OriginalDeviceId",
                        column: x => x.OriginalDeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceHistories_Users_UserName",
                        column: x => x.UserName,
                        principalTable: "Users",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHistories_DepartmentId",
                table: "DeviceHistories",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHistories_OriginalDeviceId",
                table: "DeviceHistories",
                column: "OriginalDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHistories_UserName",
                table: "DeviceHistories",
                column: "UserName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceHistories");

            migrationBuilder.DropColumn(
                name: "Disowned",
                table: "Devices");
        }
    }
}
