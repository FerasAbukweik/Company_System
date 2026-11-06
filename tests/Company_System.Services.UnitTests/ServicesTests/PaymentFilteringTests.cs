using System.Linq.Expressions;
using System.Net;
using HR_System.Controllers;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.Payments;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Decorators.Loggers;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class PaymentFilteringTests : ServiceTestBase
{
    public PaymentFilteringTests(ITestOutputHelper output) : base(output)
    {
    }

    // ---------- Get ----------
    #region Get
    [Theory]
    [InlineData(null)]
    [InlineData(PaymentStateEnum.Pending)]
    [InlineData(PaymentStateEnum.Approved)]
    [InlineData(PaymentStateEnum.Rejected)]
    public async Task Get_ShouldFilterByStateWhileKeepingEmployeeAndYearScope(PaymentStateEnum? state)
    {
        // Arrange
        var repository = new Mock<IPaymentsRepository>();
        var userId = Guid.NewGuid();
        var records = Enum.GetValues<PaymentStateEnum>().Select(value => CreatePayment(userId, 2026, value)).Append(CreatePayment(Guid.NewGuid(), 2026, PaymentStateEnum.Pending)).Append(CreatePayment(userId, 2025, PaymentStateEnum.Pending)).ToList();
        repository.Setup(r => r.FilterAsync(
            It.IsAny<Expression<Func<Payment, bool>>>(),
            It.IsAny<Expression<Func<Payment, object?>>[]?>(),
            It.IsAny<Expression<Func<Payment, object?>>?>(),
            It.IsAny<bool>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()
        ))
            .Returns((Expression<Func<Payment, bool>> predicate, Expression<Func<Payment, object?>>[]? include, Expression<Func<Payment, object?>>? order, bool descending, int? skip, int? take, CancellationToken token) => Task.FromResult<IReadOnlyList<Payment>>(records.Where(predicate.Compile()).ToList()));
        var service = new PaymentsService(repository.Object, Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());
        LogArrange(nameof(userId), userId);

        // Act
        var result = await service.GetUserPaymentsAsync(userId, 2026, state);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().Be(state.HasValue ? 1 : 3);
        result.Value.Should().AllSatisfy(payment =>
        {
            payment.UserId.Should().Be(userId);
            payment.Year.Should().Be(2026);
            if (state.HasValue)
                payment.State.Should().Be(state.Value);
        });
    }

    [Fact]
    public async Task Get_ShouldRejectUndefinedStatesWithoutQuerying()
    {
        // Arrange
        LogArrange("Scenario", nameof(Get_ShouldRejectUndefinedStatesWithoutQuerying));

        var repository = new Mock<IPaymentsRepository>();
        var service = new PaymentsService(repository.Object, Mock.Of<IApplicationUsersRepository>(), Mock.Of<IDailyWorkRecordsRepository>());

        // Act
        var result = await service.GetUserPaymentsAsync(Guid.NewGuid(), 2026, (PaymentStateEnum)99);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        repository.VerifyNoOtherCalls();
    }

    #endregion
    // ---------- Logger ----------
    #region Logger
    [Theory]
    [InlineData(null)]
    [InlineData(PaymentStateEnum.Approved)]
    public async Task Logger_ShouldForwardStateAndCancellation(PaymentStateEnum? state)
    {
        // Arrange
        var inner = new Mock<IPaymentsService>();
        var userId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var expected = Result<IReadOnlyList<PaymentDTO>>.Success([]);
        inner.Setup(s => s.GetUserPaymentsAsync(userId, 2026, state, cancellation.Token))
            .ReturnsAsync(expected);
        LogArrange(nameof(userId), userId);

        // Act
        var service = new LoggingPaymentsService(inner.Object, NullLogger<LoggingPaymentsService>.Instance);
        LogAct(service);

        // Assert
        (await service.GetUserPaymentsAsync(userId, 2026, state, cancellation.Token)).Should().BeSameAs(expected);
        inner.Verify(s => s.GetUserPaymentsAsync(userId, 2026, state, cancellation.Token), Times.Once);
    }

    #endregion
    // ---------- AdminEndpoint ----------
    #region AdminEndpoint
    [Theory]
    [InlineData(null)]
    [InlineData(PaymentStateEnum.Rejected)]
    public async Task AdminEndpoint_ShouldForwardTheOptionalFilter(PaymentStateEnum? state)
    {
        // Arrange
        var service = new Mock<IPaymentsService>();
        var userId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        service.Setup(s => s.GetUserPaymentsAsync(userId, 2026, state, cancellation.Token))
            .ReturnsAsync(Result<IReadOnlyList<PaymentDTO>>.Success([]));
        var controller = new PaymentsController(service.Object);
        LogArrange(nameof(userId), userId);

        // Act
        var result = await controller.GetUserPayments(userId, 2026, state, cancellation.Token);
        LogAct(result);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        service.Verify(s => s.GetUserPaymentsAsync(userId, 2026, state, cancellation.Token), Times.Once);
    }

    private static Payment CreatePayment(Guid userId, int year, PaymentStateEnum state) => new()
    {
        UserId = userId,
        Year = year,
        Month = 11,
        PaidAmount = 100,
        PaymentDate = new DateOnly(year, 11, 5),
        State = state
    };
    #endregion
}
