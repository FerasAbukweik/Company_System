using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Notification;
using HR_System.Core.Enums;
using HR_System.ExtensionMethods;
using HR_System.Filters;
using HR_System.Infrastructure;
using HR_System.Services;
using HR_System.SignalR.App;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace TestProject1.Integration;

public class TransactionalNotificationTests : RepositoryTestBase
{
    public TransactionalNotificationTests(ITestOutputHelper output) : base(output) { }

    [Theory]
    [InlineData("success")]
    [InlineData("failure-result")]
    [InlineData("exception")]
    [InlineData("executed-exception")]
    [InlineData("aborted-request")]
    [InlineData("delivery-failure")]
    public async Task Transaction_ShouldOnlyPublishAfterCommitAndDiscardOnFailure(string outcome)
    {
        // Arrange
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        await context.Database.OpenConnectionAsync();
        var client = new Mock<IAppClient>();
        if (outcome == "delivery-failure")
            client.Setup(value => value.ReceiveNotification(It.IsAny<NotificationDTO>()))
                .ThrowsAsync(new InvalidOperationException("SignalR unavailable"));

        var clients = new Mock<IHubClients<IAppClient>>();
        clients.Setup(value => value.User(It.IsAny<string>())).Returns(client.Object);
        var hub = new Mock<IHubContext<AppHub, IAppClient>>();
        hub.SetupGet(value => value.Clients).Returns(clients.Object);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiServices();
        services.AddScoped(_ => context);
        services.AddSingleton(hub.Object);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<NotificationPublisher>();
        var abstraction = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();
        abstraction.Should().BeSameAs(publisher);

        using var cancellation = new CancellationTokenSource();
        var http = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            RequestAborted = cancellation.Token
        };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var filters = new List<IFilterMetadata>();
        var executing = new ActionExecutingContext(action, filters, new Dictionary<string, object?>(), new object());
        var notification = new NotificationDTO
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Title = "New task", Content = "Assigned",
            TargetPath = "/dashboard", CreatedAt = DateTime.UtcNow, IsRead = false, Type = NotificationTypeEnum.TaskAssigned
        };
        LogArrange(nameof(outcome), outcome);

        // Act
        var execute = () => new TransactionalAttribute().OnActionExecutionAsync(executing, async () =>
        {
            await abstraction.PublishAsync(notification);
            client.VerifyNoOtherCalls();
            if (outcome == "exception") throw new InvalidOperationException("Action failed");
            if (outcome == "aborted-request") cancellation.Cancel();

            return new ActionExecutedContext(action, filters, executing.Controller)
            {
                Result = outcome is "failure-result" or "aborted-request" ? new BadRequestResult() : new OkResult(),
                Exception = outcome == "executed-exception" ? new InvalidOperationException("Action failed") : null
            };
        });
        if (outcome == "exception") await execute.Should().ThrowAsync<InvalidOperationException>();
        else await execute();
        await publisher.PublishPendingAsync();
        LogAct("Transaction completed and remaining queue checked");

        // Assert
        var committed = outcome is "success" or "delivery-failure";
        client.Verify(value => value.ReceiveNotification(notification), committed ? Times.Once : Times.Never);
        context.Database.CurrentTransaction.Should().BeNull();
    }
}
