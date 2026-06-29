using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Company_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationHierarchies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationHierarchies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationHierarchies_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationHierarchies_OrganizationHierarchies_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OrganizationHierarchies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationHierarchies_ParentId",
                table: "OrganizationHierarchies",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationHierarchies_UserId",
                table: "OrganizationHierarchies",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationHierarchies");
        }
    }
}
