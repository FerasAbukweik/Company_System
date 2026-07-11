using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removeactivitiesfromAppTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Tasks_AppTaskId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_AppTaskId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "AppTaskId",
                table: "Activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppTaskId",
                table: "Activities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_AppTaskId",
                table: "Activities",
                column: "AppTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Tasks_AppTaskId",
                table: "Activities",
                column: "AppTaskId",
                principalTable: "Tasks",
                principalColumn: "Id");
        }
    }
}
