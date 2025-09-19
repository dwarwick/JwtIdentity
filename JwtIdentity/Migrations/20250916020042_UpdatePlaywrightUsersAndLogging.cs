using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlaywrightUsersAndLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaywrightLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailedElement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Browser = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaywrightLogs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedDate", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "Theme", "TwoFactorEnabled", "UpdatedDate", "UserName" },
                values: new object[,]
                {
                    { -3, 0, "c5ab9ce3-a09f-4d77-b332-98cc44396f44", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "playwrightadmin@example.com", true, false, null, "PLAYWRIGHTADMIN@EXAMPLE.COM", "PLAYWRIGHTADMIN@EXAMPLE.COM", "AQAAAAIAAYagAAAAEDaaeD+y1I6b06Mfnm/tKqk8uIC+IIyCC5XMjODRg0PAJuxDcmPh6iihBkSLhMoyJQ==", null, false, "", "dark", false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "playwrightadmin@example.com" },
                    { -2, 0, "9b8d1f30-4bd3-4f1f-b83b-5677f49a434e", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "playwrightuser@example.com", true, false, null, "PLAYWRIGHTUSER@EXAMPLE.COM", "PLAYWRIGHTUSER@EXAMPLE.COM", "AQAAAAIAAYagAAAAEDaaeD+y1I6b06Mfnm/tKqk8uIC+IIyCC5XMjODRg0PAJuxDcmPh6iihBkSLhMoyJQ==", null, false, "", "light", false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "playwrightuser@example.com" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { 1, -3 },
                    { 2, -2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaywrightLogs_ExecutedAt",
                table: "PlaywrightLogs",
                column: "ExecutedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaywrightLogs");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 1, -3 });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, -2 });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: -3);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: -2);
        }
    }
}
