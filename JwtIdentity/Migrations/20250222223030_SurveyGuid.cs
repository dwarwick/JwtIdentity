using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class SurveyGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropForeignKey(
                name: "FK_Answers_AspNetUsers_CreatedById",
                table: "Answers");

            _ = migrationBuilder.DropForeignKey(
                name: "FK_Questions_AspNetUsers_CreatedById",
                table: "Questions");

            _ = migrationBuilder.DropForeignKey(
                name: "FK_Questions_Surveys_SurveyId",
                table: "Questions");

            _ = migrationBuilder.DropForeignKey(
                name: "FK_Surveys_AspNetUsers_CreatedById",
                table: "Surveys");

            _ = migrationBuilder.AlterColumn<int>(
                name: "CreatedById",
                table: "Surveys",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            _ = migrationBuilder.AddColumn<string>(
                name: "Guid",
                table: "Surveys",
                type: "nvarchar(max)",
                nullable: true);

            _ = migrationBuilder.AlterColumn<int>(
                name: "SurveyId",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            _ = migrationBuilder.AlterColumn<int>(
                name: "CreatedById",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            _ = migrationBuilder.AlterColumn<int>(
                name: "CreatedById",
                table: "Answers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            _ = migrationBuilder.InsertData(
                table: "AspNetRoleClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "RoleId" },
                values: new object[] { 2, "permission", "CreateSurvey", 1 });

            _ = migrationBuilder.AddForeignKey(
                name: "FK_Answers_AspNetUsers_CreatedById",
                table: "Answers",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            _ = migrationBuilder.AddForeignKey(
                name: "FK_Questions_AspNetUsers_CreatedById",
                table: "Questions",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            _ = migrationBuilder.AddForeignKey(
                name: "FK_Questions_Surveys_SurveyId",
                table: "Questions",
                column: "SurveyId",
                principalTable: "Surveys",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            _ = migrationBuilder.AddForeignKey(
                name: "FK_Surveys_AspNetUsers_CreatedById",
                table: "Surveys",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropForeignKey(
                name: "FK_Answers_AspNetUsers_CreatedById",
                table: "Answers");

            _ = migrationBuilder.DropForeignKey(
                name: "FK_Questions_AspNetUsers_CreatedById",
                table: "Questions");

            _ = migrationBuilder.DropForeignKey(
                name: "FK_Questions_Surveys_SurveyId",
                table: "Questions");

            _ = migrationBuilder.DropForeignKey(
                name: "FK_Surveys_AspNetUsers_CreatedById",
                table: "Surveys");

            _ = migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 2);

            _ = migrationBuilder.DropColumn(
                name: "Guid",
                table: "Surveys");

            _ = migrationBuilder.AlterColumn<int>(
                name: "CreatedById",
                table: "Surveys",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            _ = migrationBuilder.AlterColumn<int>(
                name: "SurveyId",
                table: "Questions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            _ = migrationBuilder.AlterColumn<int>(
                name: "CreatedById",
                table: "Questions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            _ = migrationBuilder.AlterColumn<int>(
                name: "CreatedById",
                table: "Answers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            _ = migrationBuilder.AddForeignKey(
                name: "FK_Answers_AspNetUsers_CreatedById",
                table: "Answers",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            _ = migrationBuilder.AddForeignKey(
                name: "FK_Questions_AspNetUsers_CreatedById",
                table: "Questions",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            _ = migrationBuilder.AddForeignKey(
                name: "FK_Questions_Surveys_SurveyId",
                table: "Questions",
                column: "SurveyId",
                principalTable: "Surveys",
                principalColumn: "Id");

            _ = migrationBuilder.AddForeignKey(
                name: "FK_Surveys_AspNetUsers_CreatedById",
                table: "Surveys",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
