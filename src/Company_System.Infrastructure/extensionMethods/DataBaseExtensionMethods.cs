using Microsoft.EntityFrameworkCore;

namespace HR_System.Infrastructure.extensionMethods;

public static class DataBaseExtensionMethods
{
    public static IQueryable<Guid> GetSubTreeUserIds(this ApplicationDbContext dbContext, Guid userId)
    {
        return dbContext.Database
            .SqlQueryRaw<Guid>("EXEC GetSubTreeUserIds {0}", userId);
        
    }
}