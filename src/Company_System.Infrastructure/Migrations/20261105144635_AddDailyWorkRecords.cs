using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyWorkRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyWorkRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MinutesWorked = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyWorkRecords", x => x.Id);
                    table.CheckConstraint("CK_DailyWorkRecords_MinutesWorked", "[MinutesWorked] >= 0 AND [MinutesWorked] <= 1440");
                    table.CheckConstraint("CK_DailyWorkRecords_State", "[State] IN (0, 1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_DailyWorkRecords_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkRecords_UserId_Date",
                table: "DailyWorkRecords",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyWorkRecords");
        }
    }
}
