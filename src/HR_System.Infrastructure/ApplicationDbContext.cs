using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Idnetity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser , ApplicationRole , Guid>
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<RefreshToken>()
            .HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
}