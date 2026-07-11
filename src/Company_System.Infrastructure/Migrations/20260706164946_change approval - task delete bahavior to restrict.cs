using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changeapprovaltaskdeletebahaviortorestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
