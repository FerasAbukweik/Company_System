using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Message;
using HR_System.Core.Enums;
using HR_System.Infrastructure.Services;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class RequestValidationTests : ServiceTestBase
{
    public RequestValidationTests(ITestOutputHelper output) : base(output)
    {
    }

    // ---------- InvalidTaskStatus ----------
    #region InvalidTaskStatus
    [Fact]
    public async Task InvalidTaskStatus_DoesNotReadOrChangeTask()
    {
        // Arrange
        LogArrange("Scenario", nameof(InvalidTaskStatus_DoesNotReadOrChangeTask));

        var repository = new Mock<ITasksRepository>(MockBehavior.Strict);
        var service = new TasksService(repository.Object, Mock.Of<IActivitiesService>(), Mock.Of<IOrganizationHierarchyService>());

        // Act
        var result = await service.UpdateStatusAsync(Guid.NewGuid(), (TaskStatusEnum)100, Guid.NewGuid());
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        repository.VerifyNoOtherCalls();
    }

    #endregion
    // ---------- InvalidApprovalStatus ----------
    #region InvalidApprovalStatus
    [Fact]
    public async Task InvalidApprovalStatus_DoesNotReadOrChangeApproval()
    {
        // Arrange
        LogArrange("Scenario", nameof(InvalidApprovalStatus_DoesNotReadOrChangeApproval));

        var repository = new Mock<IApprovalRepository>(MockBehavior.Strict);
        var service = new TasksApprovalsService(Mock.Of<ITasksService>(), Mock.Of<IApprovalService>(), Mock.Of<IActivitiesService>(), repository.Object);

        // Act
        var result = await service.UpdateApprovalStatus(Guid.NewGuid(), (ApprovalStatusEnum)100, Guid.NewGuid());
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        repository.VerifyNoOtherCalls();
    }

    #endregion
    // ---------- InvalidMessageLength ----------
    #region InvalidMessageLength
    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task InvalidMessageLength_DoesNotPersistMessage(int length)
    {
        // Arrange
        var repository = new Mock<IMessageRepository>(MockBehavior.Strict);
        var service = new MessagesService(repository.Object);
        var message = new MessageAddDTO
        {
            Content = new string ('a', length),
            ReceiverId = Guid.NewGuid(),
        };
        LogArrange(nameof(length), length);

        // Act
        var result = await service.AddAsync(message, Guid.NewGuid());
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        repository.VerifyNoOtherCalls();
    }
    #endregion
}
