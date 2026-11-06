using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Result;

namespace HR_System.Core.Abstraction.ServiceContracts;

public interface IActivitiesService
{
    /// <summary>
    /// Records an activity triggered by a user and saves it.
    /// </summary>
    /// <param name="toAdd">The type of the activity and the task or approval it is about.</param>
    /// <param name="triggeredById">The id of the user who triggered the activity.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the saved activity, with its related task/approval data when available.</returns>
    Task<Result<ActivityDto>> AddAsync(ActivityAddDto toAdd, Guid triggeredById, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the activities triggered by a user and everyone under him in the hierarchy, newest first, one section at a time.
    /// </summary>
    /// <param name="lazyData">How many activities were already taken and how many to return now.</param>
    /// <param name="userId">The id of the user whose subtree's activities are returned.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result containing the requested section of activities.</returns>
    Task<Result<IReadOnlyList<ActivityDto>>> LazyGetAllSortedAsync(LazyDTO lazyData, Guid userId, CancellationToken cancellationToken = default);
}
