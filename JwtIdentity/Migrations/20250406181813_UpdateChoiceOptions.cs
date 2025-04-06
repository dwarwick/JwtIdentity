using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChoiceOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropForeignKey(
                name: "FK_ChoiceOptions_Questions_SelectAllThatApplyQuestionId",
                table: "ChoiceOptions");

            _ = migrationBuilder.AlterColumn<int>(
                name: "MultipleChoiceQuestionId",
                table: "ChoiceOptions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            _ = migrationBuilder.AddForeignKey(
                name: "FK_ChoiceOptions_Questions_SelectAllThatApplyQuestionId",
                table: "ChoiceOptions",
                column: "SelectAllThatApplyQuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropForeignKey(
                name: "FK_ChoiceOptions_Questions_SelectAllThatApplyQuestionId",
                table: "ChoiceOptions");

            _ = migrationBuilder.AlterColumn<int>(
                name: "MultipleChoiceQuestionId",
                table: "ChoiceOptions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            _ = migrationBuilder.AddForeignKey(
                name: "FK_ChoiceOptions_Questions_SelectAllThatApplyQuestionId",
                table: "ChoiceOptions",
                column: "SelectAllThatApplyQuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
