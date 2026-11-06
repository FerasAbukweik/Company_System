using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class ActivitiesServiceTests : ServiceTestBase
{
    private readonly IActivitiesService _activitiesService;
    private readonly Mock<IActivityRepository> _activityRepositoryMock;

    public ActivitiesServiceTests(ITestOutputHelper output) : base(output)
    {
        _activityRepositoryMock = new Mock<IActivityRepository>();
        _activitiesService = new ActivitiesService(_activityRepositoryMock.Object, Mock.Of<IOrganizationHierarchyRepository>());
    }

    private ActivityAddDto CreateActivityAddDto()
    {
        return Fixture.Create<ActivityAddDto>();
    }

    #region AddAsync
    [Fact]
    public async Task AddAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var dto = CreateActivityAddDto();
        var triggeredById = Guid.NewGuid();
        _activityRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _activitiesService.AddAsync(dto, triggeredById);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ShouldCallRepositoryAddWithCorrectMappedActivity()
    {
        // Arrange
        var dto = CreateActivityAddDto();
        var triggeredById = Guid.NewGuid();
        _activityRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        LogArrange(nameof(dto), dto);

        // Act
        await _activitiesService.AddAsync(dto, triggeredById);
        LogAct("Operation completed");

        // Assert
        _activityRepositoryMock.Verify(
            r => r.Add(It.Is<Activity>(a => a.Type == dto.Type && a.TaskId == dto.TaskId && a.ApprovalId == dto.ApprovalId && a.Title == null && a.Description == null && a.TriggeredById == triggeredById)),
            Times.Once
        );
    }

    [Fact]
    public async Task AddAsync_ShouldReturnSuccessWithMappedDto_WhenSaveChangesSucceeds()
    {
        // Arrange
        var dto = CreateActivityAddDto();
        var triggeredById = Guid.NewGuid();
        _activityRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _activitiesService.AddAsync(dto, triggeredById);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(dto.Type);
        result.Value.Title.Should().Be("Activity");
        result.Value.Description.Should().BeEmpty();
    }

    #endregion
    #region LazyGetAllSortedAsync
    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldReturnMappedDtos_WhenRepositoryReturnsActivities()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        var activity = Fixture.Build<Activity>()
            .Without(a => a.TriggeredBy)
            .Create();
        _activityRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Activity, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Activity, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Activity, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Activity> { activity });
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _activitiesService.LazyGetAllSortedAsync(lazyData, userId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Id == activity.Id && a.Title == activity.ToDTO().Title);
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldReturnEmptyList_WhenRepositoryReturnsNoActivities()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        _activityRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Activity, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Activity, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Activity, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Activity>());
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _activitiesService.LazyGetAllSortedAsync(lazyData, userId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldPassGivenLazyDataAndUserIdToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO
        {
            Taken = 5,
            SectionSize = 20
        };
        _activityRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Activity, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Activity, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Activity, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Activity>());
        LogArrange(nameof(userId), userId);

        // Act
        await _activitiesService.LazyGetAllSortedAsync(lazyData, userId);
        LogAct("Operation completed");

        // Assert
        _activityRepositoryMock.Verify(
            r => r.FilterAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Activity, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Activity, object?>>[]?>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Activity, object?>>?>(),
                true,
                lazyData.Taken,
                lazyData.SectionSize,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
    #endregion
}
