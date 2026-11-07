using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HR_System.Infrastructure;

public class ApplicationDbContext(DbContextOptions options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OrganizationHierarchy>(oh =>
        {
            oh.Property(o => o.IsActive).HasDefaultValue(true);

            oh
                .HasOne(o => o.User)
                .WithOne(u => u.OrganizationHierarchy)
                .HasForeignKey<OrganizationHierarchy>(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        
        builder.Entity<Activity>(a =>
        {
            a
                .HasOne(a => a.Task)
                .WithMany()
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Restrict);
            
            a
                .HasOne(a => a.Approval)
                .WithMany()
                .HasForeignKey(a => a.ApprovalId)
                .OnDelete(DeleteBehavior.Restrict);
            
            a
                .HasOne(a => a.TriggeredBy)
                .WithMany(u => u.Activities)
                .HasForeignKey(a => a.TriggeredById)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        
        builder.Entity<ApplicationRole>(ar =>
        {
            ar.HasData(
                new ApplicationRole()
                {
                    Id = Guid.Parse("7b3c2d49-a1b8-4c5e-9f82-3d6a1b2c4d5e"),
                    Name = nameof(RolesEnum.Admin),
                    NormalizedName = nameof(RolesEnum.Admin).ToUpper(),
                    ConcurrencyStamp = "e2d5c4b1-6a3d-4e9f-829f-3d6a1b2c4d5e"
                },
                new ApplicationRole()
                {
                    Id = Guid.Parse("3f2c9d11-7a6b-4c8e-9d5f-1b2a9c8e7d3f"),
                    Name = nameof(RolesEnum.Employee),
                    NormalizedName = nameof(RolesEnum.Employee).ToUpper(),
                    ConcurrencyStamp = "3f2c9d11-7a6b-4c8e-9d5f-1b2a9c8e7d3f"
                });
        });

        
        // Image relations (one to one) ---------------------------------------------------
        builder.Entity<Image>(i =>
        {
            i
                .HasOne(i => i.User)
                .WithOne(u => u.Image)
                .HasForeignKey<Image>(i => i.UserId)
                .OnDelete(DeleteBehavior.SetNull); // user deleted => image becomes orphaned => background service removes it
        });


        // Refresh Token relations --------------------------------------------------------
        builder.Entity<RefreshToken>(rt =>
        {
            rt
                .HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
            

        // Employee payments -------------------------------------------------------------
        builder.Entity<Payment>(p =>
        {
            p.HasIndex(p => new { p.UserId, p.Year, p.Month }).IsUnique();

            p.Property(p => p.State).HasDefaultValue(PaymentStateEnum.Pending).IsConcurrencyToken();

            p.ToTable("Payments", table =>
            {
                table.HasCheckConstraint("CK_Payments_State", "[State] IN (0, 1, 2)");
                table.HasCheckConstraint("CK_Payments_PaidAmount", "[PaidAmount] > 0");
                table.HasCheckConstraint("CK_Payments_Year", "[Year] >= 1 AND [Year] <= 9999");
                table.HasCheckConstraint("CK_Payments_Month", "[Month] >= 1 AND [Month] <= 12");
            });

            p.HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        builder.Entity<Notification>(notification =>
        {
            notification.HasIndex(item => new { item.UserId, item.SourceKey }).IsUnique();
            notification.HasIndex(item => new { item.UserId, item.IsRead, item.CreatedAt });
            notification.HasOne(item => item.User).WithMany(user => user.Notifications)
                .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        });


        // Daily work records ------------------------------------------------------------
        builder.Entity<DailyWorkRecord>(r =>
        {
            r.HasIndex(r => new { r.UserId, r.Date }).IsUnique();

            r.ToTable("DailyWorkRecords", table =>
            {
                table.HasCheckConstraint("CK_DailyWorkRecords_State", "[State] IN (0, 1, 2, 3)");
            });

            r.HasOne(r => r.User)
                .WithMany(u => u.DailyWorkRecords)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // Tasks relations ------------------------------------------------------------------------
        builder.Entity<AppTask>(t =>
        {
            t
                .HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            t
                .HasOne(t => t.Manager)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        

        // Approvals relations ----------------------------------------------------------------
        builder.Entity<Approval>(a =>
        {
            a
                .HasOne(a => a.Task)
                .WithMany(t => t.Approvals)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            a
                .HasOne(a => a.UserRequesting)
                .WithMany(u => u.Approvals)
                .HasForeignKey(a => a.UserRequestingId)
                .OnDelete(DeleteBehavior.Restrict);

            a
                .HasOne(a => a.Manager)
                .WithMany(u => u.ToApprove)
                .HasForeignKey(a => a.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
            
        
        // Message relations ------------------------------------------------------------
        builder.Entity<Message>(m =>
        {
            m
                .HasOne(m => m.Sender)
                .WithMany(s => s.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            
            m
                .HasOne(m => m.Receiver)
                .WithMany(r => r.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        
        // return date in utc time
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(
                        new ValueConverter<DateTime, DateTime>(
                            v => v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
            }
        }
    }


    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<AppTask> Tasks { get; set; }
    public virtual DbSet<Approval> Approvals { get; set; }
    public virtual DbSet<Activity> Activities { get; set; }
    public virtual DbSet<OrganizationHierarchy> OrganizationHierarchies { get; set; }
    public virtual DbSet<Message> Messages { get; set; }
    public virtual DbSet<Image> Images { get; set; }
    public virtual DbSet<DailyWorkRecord> DailyWorkRecords { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
}