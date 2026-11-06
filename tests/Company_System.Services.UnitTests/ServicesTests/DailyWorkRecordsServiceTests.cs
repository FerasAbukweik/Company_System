using System.Linq.Expressions;
using System.Net;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Attendance;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Decorators.Loggers;
using HR_System.Infrastructure.Services;
using HR_System.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class DailyWorkRecordsServiceTests : ServiceTestBase
{
    private readonly Mock<IDailyWorkRecordsRepository> _recordsRepositoryMock;
    private readonly Mock<IApplicationUsersRepository> _usersRepositoryMock;

    private readonly IDailyWorkRecordsService _dailyWorkRecordsService;

    public DailyWorkRecordsServiceTests(ITestOutputHelper output) : base(output)
    {
        _recordsRepositoryMock = new();
        _usersRepositoryMock = new();
        _dailyWorkRecordsService = new DailyWorkRecordsService(_recordsRepositoryMock.Object, _usersRepositoryMock.Object);
    }

    private static DailyWorkRecordSaveDTO CreateData() => new()
    {
        UserId = Guid.NewGuid(),
        Date = new DateOnly(2026, 11, 5),
        MinutesWorked = 480,
        State = WorkDayStateEnum.Worked
    };
    private void SetupRecords(params DailyWorkRecord[] records)
    {
        _recordsRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<Expression<Func<DailyWorkRecord, bool>>>(),
            It.IsAny<Expression<Func<DailyWorkRecord, object?>>[]?>(),
            It.IsAny<Expression<Func<DailyWorkRecord, object?>>?>(),
            It.IsAny<bool>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(records);
    }

    // ---------- Add ----------
    #region Add
    [Theory]
    [InlineData(-1, WorkDayStateEnum.Worked)]
    [InlineData(1441, WorkDayStateEnum.Worked)]
    [InlineData(480, (WorkDayStateEnum)99)]
    public async Task Add_ShouldRejectInvalidMinutesAndStatesBeforeWriting(int minutes, WorkDayStateEnum state)
    {
        // Arrange
        var data = CreateData();
        data.MinutesWorked = minutes;
        data.State = state;
        LogArrange(nameof(data), data);

        // Act
        var result = await _dailyWorkRecordsService.AddAsync(data);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _recordsRepositoryMock.VerifyNoOtherCalls();
        _usersRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Add_ShouldReturnNotFoundForMissingEmployee()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldReturnNotFoundForMissingEmployee));

        // Act
        var result = await _dailyWorkRecordsService.AddAsync(CreateData());
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _recordsRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Add_ShouldRejectDuplicateEmployeeDate()
    {
        // Arrange
        var data = CreateData();
        _usersRepositoryMock.Setup(u => u.GetByIdAsync(data.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { FullName = "Employee", Position = default });
        SetupRecords(new DailyWorkRecord { UserId = data.UserId, Date = data.Date, MinutesWorked = 480, State = WorkDayStateEnum.Worked });
        LogArrange(nameof(data), data);

        // Act
        var result = await _dailyWorkRecordsService.AddAsync(data);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _recordsRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(WorkDayStateEnum.Worked, 480)]
    [InlineData(WorkDayStateEnum.PaidLeave, 0)]
    [InlineData(WorkDayStateEnum.Holiday, 0)]
    [InlineData(WorkDayStateEnum.Weekend, 0)]
    public async Task Add_ShouldPersistEachDefinedState(WorkDayStateEnum state, int minutes)
    {
        // Arrange
        var data = CreateData();
        data.State = state;
        data.MinutesWorked = minutes;
        _usersRepositoryMock.Setup(u => u.GetByIdAsync(data.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { FullName = "Employee", Position = default });
        SetupRecords();
        _recordsRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        LogArrange(nameof(data), data);

        // Act
        var result = await _dailyWorkRecordsService.AddAsync(data);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Value!.State.Should().Be(state);
        result.Value.MinutesWorked.Should().Be(minutes);
        _recordsRepositoryMock.Verify(
            r => r.Add(
                It.Is<DailyWorkRecord>(record => record.UserId == data.UserId && record.Date == data.Date),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    #endregion
    // ---------- Update ----------
    #region Update
    [Fact]
    public async Task Update_ShouldReturnNotFoundWithoutSavingMissingRecord()
    {
        // Arrange
        LogArrange("Scenario", nameof(Update_ShouldReturnNotFoundWithoutSavingMissingRecord));

        // Act
        var result = await _dailyWorkRecordsService.UpdateAsync(Guid.NewGuid(), 480, WorkDayStateEnum.Worked);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _recordsRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
    // ---------- Get ----------
    #region Get
    [Fact]
    public async Task Get_ShouldRejectReversedDateRanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(Get_ShouldRejectReversedDateRanges));

        // Act
        var result = await _dailyWorkRecordsService.GetUserRecordsAsync(Guid.NewGuid(), new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 5));
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _recordsRepositoryMock.VerifyNoOtherCalls();
    }

    #endregion
    // ---------- Logger ----------
    #region Logger
    [Fact]
    public async Task Logger_ShouldForwardArgumentsAndReturnTheInnerResult()
    {
        // Arrange
        var inner = new Mock<IDailyWorkRecordsService>();
        var expected = Result<DailyWorkRecordDTO>.Failure("Employee not found", HttpStatusCode.NotFound);
        var data = CreateData();
        using var cancellation = new CancellationTokenSource();
        inner.Setup(s => s.AddAsync(data, cancellation.Token))
            .ReturnsAsync(expected);
        LogArrange(nameof(data), data);

        // Act
        var logger = new LoggingDailyWorkRecordsService(inner.Object, NullLogger<LoggingDailyWorkRecordsService>.Instance);
        LogAct(logger);

        // Assert
        (await logger.AddAsync(data, cancellation.Token)).Should().BeSameAs(expected);
        inner.Verify(s => s.AddAsync(data, cancellation.Token), Times.Once);
    }

    #endregion
    // ---------- Controller ----------
    #region Controller
    [Theory]
    [InlineData(nameof(DailyWorkRecordsController.Add))]
    [InlineData(nameof(DailyWorkRecordsController.Update))]
    [InlineData(nameof(DailyWorkRecordsController.GetUserRecords))]
    public void Controller_ShouldRestrictEmployeeManagementToAdmins(string action)
    {
        // Arrange
        var method = typeof(DailyWorkRecordsController).GetMethod(action)!;
        LogArrange(nameof(action), action);

        // Act
        var authorization = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Should().ContainSingle().Which;
        LogAct(authorization);

        // Assert
        authorization.Roles.Should().Be(nameof(RolesEnum.Admin));
    }
    #endregion
}
