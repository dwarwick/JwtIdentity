using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddRating1To10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rating1To10Answer_SelectedOptionId",
                table: "Answers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating1To10Answer_SelectedOptionId",
                table: "Answers");
        }
    }
}
