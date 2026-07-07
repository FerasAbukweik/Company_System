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
            Id = Guid.Parse("7b3c2d49-a1b8-4c5e-9f82-3d6a1b2c4d5e"),
            Name = nameof(RolesEnum.Admin),
            NormalizedName = nameof(RolesEnum.Admin).ToUpper(),
            ConcurrencyStamp = "e2d5c4b1-6a3d-4e9f-829f-3d6a1b2c4d5e"
        }
    );
    builder.Entity<ApplicationRole>().HasData(
        new ApplicationRole(){
            Id = Guid.Parse("3f2c9d11-7a6b-4c8e-9d5f-1b2a9c8e7d3f"),
            Name = nameof(RolesEnum.Employee),
            NormalizedName = nameof(RolesEnum.Employee).ToUpper(),
            ConcurrencyStamp = "3f2c9d11-7a6b-4c8e-9d5f-1b2a9c8e7d3f"
        }
    );
    
    // Refresh Token relations --------------------------------------------------------
    builder.Entity<RefreshToken>()
        .HasOne(r => r.User)
        .WithMany(u => u.RefreshTokens)
        .HasForeignKey(r => r.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Tasks relations ------------------------------------------------------------------------
    builder.Entity<AppTask>()
        .HasOne(t => t.User)
        .WithMany(u => u.Tasks)
        .HasForeignKey(t => t.UserId)
        .OnDelete(DeleteBehavior.Restrict); 
    
    builder.Entity<AppTask>()
        .HasOne(t => t.Manager)
        .WithMany(u => u.CreatedTasks)
        .HasForeignKey(t => t.ManagerId)
        .OnDelete(DeleteBehavior.Restrict);
    
    // Approvals relations ----------------------------------------------------------------
    builder.Entity<Approval>()
        .HasOne(a => a.Task)
        .WithMany(t => t.Approvals)
        .HasForeignKey(a => a.TaskId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Entity<Approval>()
        .HasOne(a => a.UserRequesting)
        .WithMany(u => u.Approvals)
        .HasForeignKey(a => a.UserRequestingId)
        .OnDelete(DeleteBehavior.Restrict);
    
    builder.Entity<Approval>()
        .HasOne(a => a.Manager)
        .WithMany(u => u.ToApprove)
        .HasForeignKey(a => a.ManagerId)
        .OnDelete(DeleteBehavior.Restrict);
        
    
    // Activity relations ------------------------------------------------------------
    builder.Entity<Activity>()
        .HasOne(a => a.TriggeredBy)
        .WithMany(u => u.Activities)
        .HasForeignKey(a => a.TriggeredById)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Message relations ------------------------------------------------------------
    builder.Entity<Message>()
        .HasOne(m => m.Sender)
        .WithMany(s => s.SentMessages)
        .HasForeignKey(m => m.SenderId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Entity<Message>()
        .HasOne(m => m.Receiver)
        .WithMany(r => r.ReceivedMessages)
        .HasForeignKey(m => m.ReceiverId)
        .OnDelete(DeleteBehavior.Restrict);
}
    
    
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<AppTask> Tasks { get; set; }
    public virtual DbSet<Approval> Approvals { get; set; }
    public virtual DbSet<Activity> Activities { get; set; }
    public virtual DbSet<OrganizationHierarchy> OrganizationHierarchies { get; set; }
    public virtual DbSet<Message> Messages { get; set; }
}