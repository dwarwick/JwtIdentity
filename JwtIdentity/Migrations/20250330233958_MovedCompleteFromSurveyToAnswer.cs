using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class MovedCompleteFromSurveyToAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropColumn(
                name: "Complete",
                table: "Surveys");

            _ = migrationBuilder.AddColumn<bool>(
                name: "Complete",
                table: "Answers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropColumn(
                name: "Complete",
                table: "Answers");
        }
    }
}
