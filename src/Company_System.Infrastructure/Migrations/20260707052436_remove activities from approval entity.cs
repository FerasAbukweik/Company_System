using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removeactivitiesfromapprovalentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Approvals_ApprovalId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals");

            migrationBuilder.DropIndex(
                name: "IX_Activities_ApprovalId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "Activities");

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

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalId",
                table: "Activities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ApprovalId",
                table: "Activities",
                column: "ApprovalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Approvals_ApprovalId",
                table: "Activities",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Approvals_Tasks_TaskId",
                table: "Approvals",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
