using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectAllThatApplyQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectAllThatApplyQuestionId",
                table: "ChoiceOptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedOptionIds",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChoiceOptions_SelectAllThatApplyQuestionId",
                table: "ChoiceOptions",
                column: "SelectAllThatApplyQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChoiceOptions_Questions_SelectAllThatApplyQuestionId",
                table: "ChoiceOptions",
                column: "SelectAllThatApplyQuestionId",
                principalTable: "Questions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChoiceOptions_Questions_SelectAllThatApplyQuestionId",
                table: "ChoiceOptions");

            migrationBuilder.DropIndex(
                name: "IX_ChoiceOptions_SelectAllThatApplyQuestionId",
                table: "ChoiceOptions");

            migrationBuilder.DropColumn(
                name: "SelectAllThatApplyQuestionId",
                table: "ChoiceOptions");

            migrationBuilder.DropColumn(
                name: "SelectedOptionIds",
                table: "Answers");
        }
    }
}
