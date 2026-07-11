using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR_System.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addgetparentids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"create procedure [dbo].[getParentUserIds]
                                @TargetUserId UNIQUEIDENTIFIER
                                    as
                                    begin 
                                        with rec as (
                                            select ParentId, UserId from [dbo].[OrganizationHierarchies] where UserId = @TargetUserId
                                            
                                            UNION ALL
                                            
                                            select  h.ParentId, h.UserId from [dbo].[OrganizationHierarchies] h
                                                join rec r on r.ParentId = h.id
                                        )
                                                 
                                        select UserId from rec where UserId <> @TargetUserId and UserId is not null
                                        
                                    end 
                                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP PROCEDURE IF EXISTS [dbo].[getParentUserIds]");
        }
    }
}
