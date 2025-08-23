using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JwtIdentity.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedDate", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "Theme", "TwoFactorEnabled", "UpdatedDate", "UserName" },
                values: new object[] { 4, 0, "be6fc596-979b-42b1-906e-d6d5a59d6fce", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "DemoUser@surveyshark.site", true, false, null, "DEMOUSER@SURVEYSHARK.SITE", "DEMOUSER@SURVEYSHARK.SITE", "AQAAAAIAAYagAAAAEDaaeD+y1I6b06Mfnm/tKqk8uIC+IIyCC5XMjODRg0PAJuxDcmPh6iihBkSLhMoyJQ==", null, false, "", "light", false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "DemoUser@surveyshark.site" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { 2, 4 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 4 });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
