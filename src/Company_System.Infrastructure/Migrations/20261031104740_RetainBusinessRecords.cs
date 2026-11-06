using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RetainBusinessRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationHierarchies_AspNetUsers_UserId",
                table: "OrganizationHierarchies");
            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationHierarchies_AspNetUsers_UserId",
                table: "OrganizationHierarchies", column: "UserId",
                principalTable: "AspNetUsers", principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_AspNetUsers_TriggeredById",
                table: "Activities");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OrganizationHierarchies",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Activities",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Activities",
                type: "nvarchar(250)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalId",
                table: "Activities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskId",
                table: "Activities",
                type: "uniqueidentifier",
                nullable: true);

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
            // Raw SQL bypasses query filters: inactive branches must not grant delegation rights.
            migrationBuilder.Sql(@"EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[getParentUserIds]
                @TargetUserId UNIQUEIDENTIFIER
                AS BEGIN
                    WITH rec AS (
                        SELECT Id, ParentId, UserId FROM OrganizationHierarchies
                        WHERE UserId = @TargetUserId AND IsActive = 1
                        UNION ALL
                        SELECT h.Id, h.ParentId, h.UserId FROM OrganizationHierarchies h
                        JOIN rec r ON r.ParentId = h.Id WHERE h.IsActive = 1
                    )
                    SELECT UserId AS Value FROM rec WHERE UserId <> @TargetUserId
                END');");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationHierarchies_AspNetUsers_UserId",
                table: "OrganizationHierarchies");
            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationHierarchies_AspNetUsers_UserId",
                table: "OrganizationHierarchies", column: "UserId",
                principalTable: "AspNetUsers", principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(@"EXEC(N'CREATE OR ALTER PROCEDURE [dbo].[getParentUserIds]
                @TargetUserId UNIQUEIDENTIFIER
                AS BEGIN
                    WITH rec AS (
                        SELECT Id, ParentId, UserId FROM OrganizationHierarchies WHERE UserId = @TargetUserId
                        UNION ALL
                        SELECT h.Id, h.ParentId, h.UserId FROM OrganizationHierarchies h
                        JOIN rec r ON r.ParentId = h.Id
                    )
                    SELECT UserId AS Value FROM rec WHERE UserId <> @TargetUserId
                END');");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Approvals_ApprovalId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_AspNetUsers_TriggeredById",
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

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OrganizationHierarchies");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Activities");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Activities",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Activities",
                type: "nvarchar(250)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_AspNetUsers_TriggeredById",
                table: "Activities",
                column: "TriggeredById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
