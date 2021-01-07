using Microsoft.EntityFrameworkCore.Migrations;

namespace DevicePortal.Migrations
{
    public partial class AddRecommendation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "SecurityQuestions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "SecurityQuestions");
        }
    }
}
