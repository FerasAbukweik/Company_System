using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixactivityrealtions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Approvals_TriggeredById",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Tasks_TriggeredById",
                table: "Activities");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ApprovalId",
                table: "Activities",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TaskId",
                table: "Activities",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Approvals_ApprovalId",
                table: "Activities",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Tasks_TaskId",
                table: "Activities",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Approvals_ApprovalId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Tasks_TaskId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_ApprovalId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_TaskId",
                table: "Activities");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Approvals_TriggeredById",
                table: "Activities",
                column: "TriggeredById",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Tasks_TriggeredById",
                table: "Activities",
                column: "TriggeredById",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
