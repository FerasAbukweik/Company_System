using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;

namespace HR_System.Core.Interfaces.RepositoryContracts;

public interface IActivityRepository
{
    void Add(Activity toAdd);
    Task<IReadOnlyList<Activity>> LazyGetAllSortedAsync(LazyDTO lazyData,CancellationToken cancellationToken = default);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}