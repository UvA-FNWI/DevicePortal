using Microsoft.EntityFrameworkCore.Migrations;

namespace DevicePortal.Migrations
{
    public partial class AddDeviceUserEditId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserEditId",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserEditId",
                table: "DeviceHistories",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserEditId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "UserEditId",
                table: "DeviceHistories");
        }
    }
}
