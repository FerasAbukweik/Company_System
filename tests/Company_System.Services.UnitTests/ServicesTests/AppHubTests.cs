using System.Security.Claims;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Infrastructure;
using HR_System.SignalR.App;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class AppHubTests : ServiceTestBase, IDisposable
{
    private readonly Mock<IApplicationUsersRepository> _users = new();
    private readonly Mock<IMessagesService> _messages = new();
    private readonly Mock<IGroupManager> _groups = new();
    private readonly Guid _currentUser = Guid.NewGuid();
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly AppHub _hub;

    public AppHubTests(ITestOutputHelper output) : base(output)
    {
        var caller = new Mock<HubCallerContext>();
        caller.SetupGet(context => context.User).Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, _currentUser.ToString())], "test")));
        caller.SetupGet(context => context.ConnectionId).Returns("connection");
        caller.SetupGet(context => context.Items).Returns(new Dictionary<object, object?>());
        _hub = new AppHub(_messages.Object, _users.Object, Mock.Of<INotificationsService>(), Mock.Of<INotificationPublisher>(), _context)
        {
            Context = caller.Object,
            Groups = _groups.Object
        };
    }

    [Fact]
    public async Task Join_ShouldOnlyUseTheAuthenticatedUsersConversation()
    {
        // Arrange
        var other = Guid.NewGuid();
        _users.Setup(repository => repository.GetByIdAsync(other, It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, object?>>[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { FullName = "Recipient", Position = default });
        LogArrange(nameof(other), other);

        // Act
        await _hub.AddToGroup(other);
        LogAct("Joined conversation");

        // Assert
        _groups.Verify(groups => groups.AddToGroupAsync("connection", It.Is<string>(name => name.Contains(_currentUser.ToString()) && name.Contains(other.ToString())), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_ShouldRejectAConversationTheConnectionHasNotJoined()
    {
        // Arrange
        var recipient = Guid.NewGuid();
        LogArrange(nameof(recipient), recipient);

        // Act
        var action = () => _hub.SendMessage(recipient, "Private message");
        LogAct("Attempted unjoined conversation");

        // Assert
        await action.Should().ThrowAsync<HubException>();
        _messages.VerifyNoOtherCalls();
    }

    public void Dispose() => _context.Dispose();
}
