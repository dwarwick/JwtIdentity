using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class FixMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "Questions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Controller",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExceptionMessage",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExceptionType",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Parameters",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestMethod",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestPath",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StackTrace",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "LogEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoleClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "RoleId" },
                values: new object[,]
                {
                    { 4, "permission", "LeaveFeedback", 1 },
                    { 5, "permission", "ManageSettings", 1 },
                    { 6, "permission", "UseHangfire", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "Controller",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "ExceptionMessage",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "ExceptionType",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "Parameters",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "RequestMethod",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "RequestPath",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "StackTrace",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "LogEntries");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "LogEntries");
        }
    }
}
