using System.Linq.Expressions;
using System.Net;
using HR_System.Controllers;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Payments;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Decorators.Loggers;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class PaymentsServiceTests : ServiceTestBase
{
    private readonly Mock<IPaymentsRepository> _paymentsRepositoryMock;
    private readonly Mock<IApplicationUsersRepository> _usersRepositoryMock;

    private readonly IPaymentsService _paymentsService;

    public PaymentsServiceTests(ITestOutputHelper output) : base(output)
    {
        _paymentsRepositoryMock = new();
        _usersRepositoryMock = new();
        _paymentsService = new PaymentsService(_paymentsRepositoryMock.Object, _usersRepositoryMock.Object, Mock.Of<IDailyWorkRecordsRepository>());
    }

    private void SetupPayments(params Payment[] payments)
    {
        _paymentsRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<Expression<Func<Payment, bool>>>(),
            It.IsAny<Expression<Func<Payment, object?>>[]?>(),
            It.IsAny<Expression<Func<Payment, object?>>?>(),
            It.IsAny<bool>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(payments);
    }

    // ---------- Get ----------
    #region Get
    [Fact]
    public async Task Get_ShouldScopePaymentsToTheRequestedEmployeeAndYear()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupPayments();
        using var cancellation = new CancellationTokenSource();
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _paymentsService.GetUserPaymentsAsync(userId, 2026, cancellationToken: cancellation.Token);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _paymentsRepositoryMock.Verify(
            r => r.FilterAsync(
                It.Is<Expression<Func<Payment, bool>>>(predicate => predicate.Compile()(new Payment { UserId = userId, Year = 2026, Month = 1, PaidAmount = 100, PaymentDate = new DateOnly(2026, 1, 5) }) && !predicate.Compile()(new Payment { UserId = Guid.NewGuid(), Year = 2026, Month = 1, PaidAmount = 100, PaymentDate = new DateOnly(2026, 1, 5) }) && !predicate.Compile()(new Payment { UserId = userId, Year = 2025, Month = 1, PaidAmount = 100, PaymentDate = new DateOnly(2026, 1, 5) })),
                null,
                It.IsAny<Expression<Func<Payment, object?>>>(),
                true,
                null,
                null,
                cancellation.Token
            ),
            Times.Once
        );
    }

    #endregion
    // ---------- UpdateState ----------
    #region UpdateState
    [Fact]
    public async Task UpdateState_ShouldRejectUndefinedStateWithoutWriting()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateState_ShouldRejectUndefinedStateWithoutWriting));

        // Act
        var result = await _paymentsService.UpdateStateAsync(Guid.NewGuid(), (PaymentStateEnum)99);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _paymentsRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateState_ShouldNotSaveWhenPaymentIsMissing()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateState_ShouldNotSaveWhenPaymentIsMissing));

        // Act
        var result = await _paymentsService.UpdateStateAsync(Guid.NewGuid(), PaymentStateEnum.Approved);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _paymentsRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(PaymentStateEnum.Approved)]
    [InlineData(PaymentStateEnum.Pending)]
    [InlineData(PaymentStateEnum.Rejected)]
    public async Task UpdateState_ShouldSaveEachDefinedStateAndReturnIt(PaymentStateEnum state)
    {
        // Arrange
        var payment = new Payment
        {
            UserId = Guid.NewGuid(),
            Year = 2026,
            Month = 11,
            PaidAmount = 1250.75m,
            PaymentDate = new DateOnly(2026, 11, 5),
            State = state
        };
        using var cancellation = new CancellationTokenSource();
        _paymentsRepositoryMock.Setup(r => r.UpdateStateAsync(payment.Id, state, cancellation.Token))
            .ReturnsAsync(payment);
        _paymentsRepositoryMock.Setup(r => r.SaveChangesAsync(cancellation.Token))
            .ReturnsAsync(true);
        LogArrange(nameof(state), state);

        // Act
        var result = await _paymentsService.UpdateStateAsync(payment.Id, state, cancellation.Token);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.State.Should().Be(state);
        _paymentsRepositoryMock.Verify(r => r.SaveChangesAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task UpdateState_ShouldReportSaveFailure()
    {
        // Arrange
        LogArrange("Scenario", nameof(UpdateState_ShouldReportSaveFailure));

        var payment = new Payment
        {
            UserId = Guid.NewGuid(),
            Year = 2026,
            Month = 11,
            PaidAmount = 1250.75m,
            PaymentDate = new DateOnly(2026, 11, 5)
        };
        _paymentsRepositoryMock.Setup(r => r.UpdateStateAsync(payment.Id, PaymentStateEnum.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act
        var result = await _paymentsService.UpdateStateAsync(payment.Id, PaymentStateEnum.Approved);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion
    // ---------- Logger ----------
    #region Logger
    [Fact]
    public async Task Logger_ShouldForwardStateChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(Logger_ShouldForwardStateChanges));

        var inner = new Mock<IPaymentsService>();
        var paymentId = Guid.NewGuid();
        var expected = Result<PaymentDTO>.Failure("Payment not found", HttpStatusCode.NotFound);
        using var cancellation = new CancellationTokenSource();
        inner.Setup(s => s.UpdateStateAsync(paymentId, PaymentStateEnum.Rejected, cancellation.Token))
            .ReturnsAsync(expected);

        // Act
        var service = new LoggingPaymentsService(inner.Object, NullLogger<LoggingPaymentsService>.Instance);
        LogAct(service);

        // Assert
        (await service.UpdateStateAsync(paymentId, PaymentStateEnum.Rejected, cancellation.Token)).Should().BeSameAs(expected);
        inner.Verify(s => s.UpdateStateAsync(paymentId, PaymentStateEnum.Rejected, cancellation.Token), Times.Once);
    }

    #endregion
    // ---------- Controller ----------
    #region Controller
    [Theory]
    [InlineData(nameof(PaymentsController.UpdateState))]
    [InlineData(nameof(PaymentsController.GetUserPayments))]
    public void Controller_ShouldRestrictPaymentManagementToAdmins(string action)
    {
        // Arrange
        var method = typeof(PaymentsController).GetMethod(action)!;
        LogArrange(nameof(action), action);

        // Act
        var authorization = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Should().ContainSingle().Which;
        LogAct(authorization);

        // Assert
        authorization.Roles.Should().Be("Admin");
    }
    #endregion
}
