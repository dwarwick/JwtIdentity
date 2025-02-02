using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "Theme" },
                values: new object[] { "975c6e69-81c0-463e-bc0f-212c970f34d4", "AQAAAAIAAYagAAAAENYeHtZjzzSCzot7QF9qVAC25mKeyiv6v/kdBakqJiTW7Jt5TCt/9tSVdTSABsJGtQ==", "dark" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "Theme" },
                values: new object[] { "be6fc596-979b-42b1-906e-d6d5a59d6fce", "AQAAAAIAAYagAAAAEDaaeD+y1I6b06Mfnm/tKqk8uIC+IIyCC5XMjODRg0PAJuxDcmPh6iihBkSLhMoyJQ==", "light" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Theme",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "97270c0d-00bc-45bc-a7bd-fef6805db588", "AQAAAAIAAYagAAAAEF/fbS48/SWf2yV/FNlZTFwXU//m9QUXHFZH4KHg8mwl4xVpbvF204XWi0ICxGY9tw==" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "2b896488-e96b-4700-9c93-4600c281893b", "AQAAAAIAAYagAAAAEAqabsr/tMnb7tgJwpvktcIiE/WHiPol3LL9iSRNX9AsiGrbBPA3pLbTFqAf62jGgg==" });
        }
    }
}
