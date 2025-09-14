using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddAiReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiInstructions",
                table: "Surveys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AiQuestionsApproved",
                table: "Surveys",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AiRetryCount",
                table: "Surveys",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiInstructions",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "AiQuestionsApproved",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "AiRetryCount",
                table: "Surveys");
        }
    }
}
