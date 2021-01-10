using Microsoft.EntityFrameworkCore.Migrations;

namespace DevicePortal.Migrations
{
    public partial class AddSecurityRecommendation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "SecurityQuestions");

            migrationBuilder.CreateTable(
                name: "SecurityRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OS_Type = table.Column<int>(type: "int", nullable: false),
                    SecurityQuestionsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityRecommendations_SecurityQuestions_SecurityQuestionsId",
                        column: x => x.SecurityQuestionsId,
                        principalTable: "SecurityQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityRecommendations_SecurityQuestionsId",
                table: "SecurityRecommendations",
                column: "SecurityQuestionsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityRecommendations");

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "SecurityQuestions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
