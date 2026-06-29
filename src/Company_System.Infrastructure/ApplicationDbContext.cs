using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure;

public class ApplicationDbContext(DbContextOptions options) : IdentityDbContext<ApplicationUser , ApplicationRole , Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationRole>().HasData(
            new ApplicationRole(){
                Name = nameof(RolesEnum.Admin),
                NormalizedName = nameof(RolesEnum.Admin).ToUpper(),
                Id = Guid.Parse("7b3c2d49-a1b8-4c5e-9f82-3d6a1b2c4d5e"),
                ConcurrencyStamp = "e2d5c4b1-6a3d-4e9f-829f-3d6a1b2c4d5e"
            }
        );
        
        // refresh token relations -----------------------------------------------------
        builder.Entity<RefreshToken>()
            .HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        // Tasks relations ---------------------------------------------------------------------
        builder.Entity<AppTask>()
            .HasOne(t => t.User)
            .WithMany(u => u.Tasks)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.Entity<AppTask>()
            .HasOne(t => t.Manager)
            .WithMany(u => u.CreatedTasks)
            .HasForeignKey(t => t.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);
        
        
        
        // Approvals relations ------------------------------------------------------------------
        builder.Entity<Approval>()
            .HasOne(a => a.Task)
            .WithOne(t => t.Approval)
            .HasForeignKey<Approval>(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Approval>()
            .HasOne(a => a.UserRequesting)
            .WithMany(u => u.Approvals)
            .HasForeignKey(a => a.UserRequestingId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<Approval>()
            .HasOne(a => a.Manager)
            .WithMany(u => u.ToApprove)
            .HasForeignKey(a => a.ManagerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        
        
        // Activity relations --------------------------------------------------------------------------------
        builder.Entity<Activity>()
            .HasOne(a => a.TriggeredBy)
            .WithMany(u => u.Activities)
            .HasForeignKey(a => a.TriggeredById)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.Entity<Activity>()
            .HasOne(a => a.Task)
            .WithMany(u => u.Activities)
            .HasForeignKey(a => a.TriggeredById)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.Entity<Activity>()
            .HasOne(a => a.Approval)
            .WithMany(u => u.Activities)
            .HasForeignKey(a => a.TriggeredById)
            .OnDelete(DeleteBehavior.SetNull);

        
        
        // OrganizationHierarchy -----------------------------------------------------------------
        builder.Entity<OrganizationHierarchy>()
            .HasOne(a => a.User)
            .WithOne(u => u.OrganizationHierarchy)
            .HasForeignKey<OrganizationHierarchy>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Entity<OrganizationHierarchy>()
            .HasOne(o => o.Parent)
            .WithMany(o => o.Children)
            .HasForeignKey(o => o.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        
    }
    
    
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<AppTask> Tasks { get; set; }
    public virtual DbSet<Approval> Approvals { get; set; }
    public virtual DbSet<Activity> Activities { get; set; }
    public virtual DbSet<OrganizationHierarchy> OrganizationHierarchies { get; set; }
}