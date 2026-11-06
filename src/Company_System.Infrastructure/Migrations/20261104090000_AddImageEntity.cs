using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(150)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_UserId",
                table: "Images",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            // keep the images users already have: move them from AspNetUsers into Images
            migrationBuilder.Sql(@"
                INSERT INTO Images (Id, PublicId, Url, CreatedAt, UserId)
                SELECT NEWID(), PublicImageId, ImageUrl, SYSUTCDATETIME(), Id
                FROM AspNetUsers
                WHERE PublicImageId IS NOT NULL AND ImageUrl IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PublicImageId",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "AspNetUsers",
                type: "nvarchar(150)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicImageId",
                table: "AspNetUsers",
                type: "nvarchar(150)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE u
                SET u.ImageUrl = i.Url, u.PublicImageId = i.PublicId
                FROM AspNetUsers u
                INNER JOIN Images i ON i.UserId = u.Id;");

            migrationBuilder.DropTable(
                name: "Images");
        }
    }
}
