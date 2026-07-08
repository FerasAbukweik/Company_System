using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.extensionMethods;

public static class DataBaseExtensionMethods
{
    public static IQueryable<Guid> GetSubTreeUserIds(this ApplicationDbContext dbContext, Guid userId)
    {
        return dbContext.Database
            .SqlQueryRaw<Guid>("EXEC GetSubTreeUserIds {0}", userId);
        
    }
    
    public static IQueryable<Guid> GetFatherUserIds(this ApplicationDbContext dbContext, Guid userId)
    {
        return dbContext.Database
            .SqlQueryRaw<Guid>("EXEC getParentUserIds {0}", userId);
    }
}