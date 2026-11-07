using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceClockTimesAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DailyWorkRecords_MinutesWorked",
                table: "DailyWorkRecords");

            migrationBuilder.DropColumn(
                name: "MinutesWorked",
                table: "DailyWorkRecords");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ClockIn",
                table: "DailyWorkRecords",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ClockOut",
                table: "DailyWorkRecords",
                type: "time",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TargetPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_SourceKey",
                table: "Notifications",
                columns: new[] { "UserId", "SourceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "ClockIn",
                table: "DailyWorkRecords");

            migrationBuilder.DropColumn(
                name: "ClockOut",
                table: "DailyWorkRecords");

            migrationBuilder.AddColumn<int>(
                name: "MinutesWorked",
                table: "DailyWorkRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DailyWorkRecords_MinutesWorked",
                table: "DailyWorkRecords",
                sql: "[MinutesWorked] >= 0 AND [MinutesWorked] <= 1440");
        }
    }
}
