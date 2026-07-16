using AutoFixture;
using FluentAssertions;
using HR_System.Core.common;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Activity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit.Abstractions;

namespace TestProject1.ServicesTests;

public class ActivitiesServiceTests
{
    private readonly IActivitiesService _activitiesService;
    private readonly Mock<IActivityRepository> _activityRepositoryMock;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public ActivitiesServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _activityRepositoryMock = new Mock<IActivityRepository>();

        _activitiesService = new ActivitiesService(
            _activityRepositoryMock.Object,
            NullLogger<ActivitiesService>.Instance);
    }

    private ActivityAddDTO CreateActivityAddDto()
    {
        return _fixture.Create<ActivityAddDTO>();
    }

    #region AddAsync

    [Fact]
    public async Task AddAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var dto = CreateActivityAddDto();
        var triggeredById = Guid.NewGuid();

        _activityRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _activitiesService.AddAsync(dto, triggeredById);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ShouldCallRepositoryAddWithCorrectMappedActivity()
    {
        // Arrange
        var dto = CreateActivityAddDto();
        var triggeredById = Guid.NewGuid();

        _activityRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _activitiesService.AddAsync(dto, triggeredById);

        // Assert
        _activityRepositoryMock.Verify(
            r => r.Add(It.Is<Activity>(a =>
                a.Type == dto.Type &&
                a.Title == dto.Title &&
                a.Description == dto.Description &&
                a.TriggeredById == triggeredById)),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnSuccessWithMappedDto_WhenSaveChangesSucceeds()
    {
        // Arrange
        var dto = CreateActivityAddDto();
        var triggeredById = Guid.NewGuid();

        _activityRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _activitiesService.AddAsync(dto, triggeredById);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(dto.Type);
        result.Value.Title.Should().Be(dto.Title);
        result.Value.Description.Should().Be(dto.Description);
    }

    #endregion

    #region LazyGetAllSortedAsync

    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldReturnMappedDtos_WhenRepositoryReturnsActivities()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        var activity = _fixture.Build<Activity>()
            .Without(a => a.TriggeredBy)
            .Create();

        _activityRepositoryMock
            .Setup(r => r.LazyGetAllSortedAsync(lazyData, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity> { activity });

        // Act
        var result = await _activitiesService.LazyGetAllSortedAsync(lazyData, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Id == activity.Id && a.Title == activity.Title);
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldReturnEmptyList_WhenRepositoryReturnsNoActivities()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        _activityRepositoryMock
            .Setup(r => r.LazyGetAllSortedAsync(lazyData, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity>());

        // Act
        var result = await _activitiesService.LazyGetAllSortedAsync(lazyData, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetAllSortedAsync_ShouldPassGivenLazyDataAndUserIdToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = new LazyDTO { Taken = 5, SectionSize = 20 };

        _activityRepositoryMock
            .Setup(r => r.LazyGetAllSortedAsync(lazyData, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity>());

        // Act
        await _activitiesService.LazyGetAllSortedAsync(lazyData, userId);

        // Assert
        _activityRepositoryMock.Verify(
            r => r.LazyGetAllSortedAsync(lazyData, userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}