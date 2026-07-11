using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class getsubtreeusersid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE PROCEDURE GetSubTreeUserIds
                    @UserId UNIQUEIDENTIFIER
                AS
                BEGIN
                    WITH SubTree AS (
                        SELECT Id, UserId FROM OrganizationHierarchies WHERE UserId = @UserId
                        UNION ALL
                        SELECT h.Id, h.UserId FROM OrganizationHierarchies h
                        JOIN SubTree s ON h.ParentId = s.Id
                    )
                    SELECT UserId as Value FROM SubTree WHERE UserId IS NOT NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS GetSubTreeUserIds");
        }
    }
}
