using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removetaskidandapprovalidfromactivityentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Approvals_ApprovalId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_AspNetUsers_TriggeredById",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Tasks_TaskId",
                table: "Activities");

            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "Activities",
                newName: "AppTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Activities_TaskId",
                table: "Activities",
                newName: "IX_Activities_AppTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Approvals_ApprovalId",
                table: "Activities",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_AspNetUsers_TriggeredById",
                table: "Activities",
                column: "TriggeredById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Tasks_AppTaskId",
                table: "Activities",
                column: "AppTaskId",
                principalTable: "Tasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Approvals_ApprovalId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_AspNetUsers_TriggeredById",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Tasks_AppTaskId",
                table: "Activities");

            migrationBuilder.RenameColumn(
                name: "AppTaskId",
                table: "Activities",
                newName: "TaskId");

            migrationBuilder.RenameIndex(
                name: "IX_Activities_AppTaskId",
                table: "Activities",
                newName: "IX_Activities_TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Approvals_ApprovalId",
                table: "Activities",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_AspNetUsers_TriggeredById",
                table: "Activities",
                column: "TriggeredById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Tasks_TaskId",
                table: "Activities",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
