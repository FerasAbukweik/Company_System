using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class changetaskapprovalrelationfrom11to1many : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals");

            migrationBuilder.DropIndex(
                name: "IX_Approvals_TaskId",
                table: "Approvals");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_TaskId",
                table: "Approvals",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals");

            migrationBuilder.DropIndex(
                name: "IX_Approvals_TaskId",
                table: "Approvals");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_TaskId",
                table: "Approvals",
                column: "TaskId",
                unique: true,
                filter: "[TaskId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
