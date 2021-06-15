using Microsoft.EntityFrameworkCore.Migrations;

namespace DevicePortal.Migrations
{
    public partial class AddLabnet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ipv4",
                table: "Devices",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ipv6",
                table: "Devices",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LabnetId",
                table: "Devices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ipv4",
                table: "DeviceHistories",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ipv6",
                table: "DeviceHistories",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LabnetId",
                table: "DeviceHistories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Labnets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labnets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Labnets_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_LabnetId",
                table: "Devices",
                column: "LabnetId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHistories_LabnetId",
                table: "DeviceHistories",
                column: "LabnetId");

            migrationBuilder.CreateIndex(
                name: "IX_Labnets_DepartmentId",
                table: "Labnets",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceHistories_Labnets_LabnetId",
                table: "DeviceHistories",
                column: "LabnetId",
                principalTable: "Labnets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Labnets_LabnetId",
                table: "Devices",
                column: "LabnetId",
                principalTable: "Labnets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceHistories_Labnets_LabnetId",
                table: "DeviceHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Labnets_LabnetId",
                table: "Devices");

            migrationBuilder.DropTable(
                name: "Labnets");

            migrationBuilder.DropIndex(
                name: "IX_Devices_LabnetId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_DeviceHistories_LabnetId",
                table: "DeviceHistories");

            migrationBuilder.DropColumn(
                name: "Ipv4",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Ipv6",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LabnetId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Ipv4",
                table: "DeviceHistories");

            migrationBuilder.DropColumn(
                name: "Ipv6",
                table: "DeviceHistories");

            migrationBuilder.DropColumn(
                name: "LabnetId",
                table: "DeviceHistories");
        }
    }
}
