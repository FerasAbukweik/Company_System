using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.ENUM;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

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
        
        builder.Entity<RefreshToken>()
            .HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.Entity<AppTask>()
            .HasOne(t => t.User)
            .WithMany(u => u.Tasks)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.Entity<AppTask>()
            .HasOne(t => t.Manager)
            .WithMany(u => u.CreatedTasks)
            .HasForeignKey(t => t.ManagerId)
            .OnDelete(DeleteBehavior.NoAction);
    }
    
    
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<AppTask> Tasks { get; set; }
}