using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addActivityEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_ManagerId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_UserId",
                table: "Tasks");

            migrationBuilder.CreateTable(
                name: "Approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserRequestingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Approvals_AspNetUsers_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Approvals_AspNetUsers_UserRequestingId",
                        column: x => x.UserRequestingId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Approvals_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggeredById = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Approvals_TriggeredById",
                        column: x => x.TriggeredById,
                        principalTable: "Approvals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Activities_AspNetUsers_TriggeredById",
                        column: x => x.TriggeredById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Activities_Tasks_TriggeredById",
                        column: x => x.TriggeredById,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TriggeredById",
                table: "Activities",
                column: "TriggeredById");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_ManagerId",
                table: "Approvals",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_TaskId",
                table: "Approvals",
                column: "TaskId",
                unique: true,
                filter: "[TaskId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Approvals_UserRequestingId",
                table: "Approvals",
                column: "UserRequestingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_ManagerId",
                table: "Tasks",
                column: "ManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_UserId",
                table: "Tasks",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_ManagerId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_AspNetUsers_UserId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "Approvals");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_ManagerId",
                table: "Tasks",
                column: "ManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_AspNetUsers_UserId",
                table: "Tasks",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
