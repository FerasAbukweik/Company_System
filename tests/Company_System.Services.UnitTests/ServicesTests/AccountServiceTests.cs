using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Message;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Tests.Services;

public class MessageServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;
    private readonly Mock<IMessageRepository> _repositoryMock;
    private readonly MessageService _messageService;

    public MessageServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _repositoryMock = new Mock<IMessageRepository>();
        _messageService = new MessageService(_repositoryMock.Object);
    }

    #region AddAsync

    [Fact]
    public async Task AddAsync_ValidInput_ShouldReturnSuccessWithCorrectDTO()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var toAdd = _fixture.Create<MessageAddDTO>();

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Message>()));
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _output.WriteLine($"Expected:\nUserId : {userId}\n{toAdd.ToString()}");

        // Act
        var actual = await _messageService.AddAsync(toAdd, userId);
        _output.WriteLine($"Actual:\n{actual.Value?.ToString() ?? "null"}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value!.Content.Should().Be(toAdd.Content);
        actual.Value!.IsCurrUserSender.Should().BeTrue();

        _repositoryMock.Verify(r => r.Add(It.IsAny<Message>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_SaveChangesFails_ShouldReturnFailureWithNullValue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var toAdd = _fixture.Create<MessageAddDTO>();

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<Message>()));
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _output.WriteLine($"Expected:\nUserId : {userId}\n{toAdd.ToString()}");
        _output.WriteLine("SaveChanges returns false — expecting failure and null Value");

        // Act
        var actual = await _messageService.AddAsync(toAdd, userId);
        _output.WriteLine($"Actual:\nIsSuccess    : {actual.IsSuccess}\nErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorMessage.Should().NotBeNullOrEmpty();
        actual.Value.Should().BeNull();

        _repositoryMock.Verify(r => r.Add(It.IsAny<Message>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region LazyGetMessages

    [Fact]
    public async Task LazyGetMessages_RepositoryReturnsMessages_ShouldReturnSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = CreateLazyDTO();
        var messages = CreateMessages(3, senderId: userId);

        _repositoryMock
            .Setup(r => r.LazyGetMessages(userId, lazyData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        _output.WriteLine($"Expected:\nUserId : {userId}");
        messages.ForEach(m => _output.WriteLine(m.ToString()));

        // Act
        var actual = await _messageService.LazyGetMessages(userId, lazyData);
        _output.WriteLine($"Actual:\nIsSuccess : {actual.IsSuccess}\nCount     : {actual.Value?.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Should().HaveCount(messages.Count);
        actual.Value.Should().BeAssignableTo<IReadOnlyList<MessageDTO>>();

        _repositoryMock.Verify(r =>
            r.LazyGetMessages(userId, lazyData, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LazyGetMessages_SenderMessages_ShouldMarkIsCurrUserSenderAsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = CreateLazyDTO();
        var messages = CreateMessages(3, senderId: userId);

        _repositoryMock
            .Setup(r => r.LazyGetMessages(userId, lazyData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        _output.WriteLine($"Expected:\nUserId : {userId}");
        messages.ForEach(m => _output.WriteLine(m.ToString()));

        // Act
        var actual = await _messageService.LazyGetMessages(userId, lazyData);
        _output.WriteLine($"Actual:");
        actual.Value?.ToList().ForEach(m => _output.WriteLine(m.ToString()));

        // Assert
        actual.Value.Should().OnlyContain(m => m.IsCurrUserSender);
    }

    [Fact]
    public async Task LazyGetMessages_ReceiverMessages_ShouldMarkIsCurrUserSenderAsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = CreateLazyDTO();
        var messages = CreateMessages(3, receiverId: userId);

        _repositoryMock
            .Setup(r => r.LazyGetMessages(userId, lazyData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        _output.WriteLine($"Expected:\nUserId : {userId}");
        messages.ForEach(m => _output.WriteLine(m.ToString()));

        // Act
        var actual = await _messageService.LazyGetMessages(userId, lazyData);
        _output.WriteLine($"Actual:");
        actual.Value?.ToList().ForEach(m => _output.WriteLine(m.ToString()));

        // Assert
        actual.Value.Should().OnlyContain(m => !m.IsCurrUserSender);
    }

    [Fact]
    public async Task LazyGetMessages_RepositoryReturnsEmpty_ShouldReturnSuccessWithEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lazyData = CreateLazyDTO();

        _repositoryMock
            .Setup(r => r.LazyGetMessages(userId, lazyData, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _output.WriteLine($"Expected:\nUserId : {userId}\nCount  : 0");

        // Act
        var actual = await _messageService.LazyGetMessages(userId, lazyData);
        _output.WriteLine($"Actual:\nIsSuccess : {actual.IsSuccess}\nCount     : {actual.Value?.Count}");

        // Assert
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Should().BeEmpty();

        _repositoryMock.Verify(r =>
            r.LazyGetMessages(userId, lazyData, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helpers

    private LazyDTO CreateLazyDTO() =>
        _fixture.Build<LazyDTO>()
            .With(l => l.Taken, 0)
            .With(l => l.SectionSize, 10)
            .Create();

    private Message CreateMessage(Guid? senderId = null, Guid? receiverId = null) =>
        _fixture.Build<Message>()
            .With(m => m.SenderId, senderId ?? Guid.NewGuid())
            .With(m => m.ReceiverId, receiverId ?? Guid.NewGuid())
            .With(m => m.Sender, null as HR_System.Core.Domain.Identity.ApplicationUser)
            .With(m => m.Receiver, null as HR_System.Core.Domain.Identity.ApplicationUser)
            .Create();

    private List<Message> CreateMessages(int count, Guid? senderId = null, Guid? receiverId = null) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateMessage(senderId, receiverId))
            .ToList();

    #endregion
}