using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addemployeerole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { new Guid("3f2c9d11-7a6b-4c8e-9d5f-1b2a9c8e7d3f"), "3f2c9d11-7a6b-4c8e-9d5f-1b2a9c8e7d3f", "Employee", "EMPLOYEE" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("3f2c9d11-7a6b-4c8e-9d5f-1b2a9c8e7d3f"));
        }
    }
}
