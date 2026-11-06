using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.DTO.Message;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class MessagesServiceTests : ServiceTestBase
{
    private readonly IMessagesService _messagesService;
    private readonly Mock<IMessageRepository> _messageRepositoryMock;

    public MessagesServiceTests(ITestOutputHelper output) : base(output)
    {
        _messageRepositoryMock = new Mock<IMessageRepository>();
        _messagesService = new MessagesService(_messageRepositoryMock.Object);
    }

    private MessageAddDTO CreateMessageAddDto(Guid? receiverId = null)
    {
        return Fixture.Build<MessageAddDTO>()
            .With(m => m.ReceiverId, receiverId ?? Guid.NewGuid())
            .Create();
    }

    private Message CreateMessage()
    {
        return Fixture.Build<Message>() // adjust/add .Without(...) here if Message has navigation properties
        // (e.g. Sender, Receiver) that would cause AutoFixture recursion issues
        .Create();
    }

    private LazyDTO CreateLazyDto()
    {
        return Fixture.Create<LazyDTO>();
    }

    #region AddAsync
    [Fact]
    public async Task AddAsync_ShouldReturnFailure_WhenSaveChangesFails()
    {
        // Arrange
        var dto = CreateMessageAddDto();
        var userId = Guid.NewGuid();
        _messageRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _messagesService.AddAsync(dto, userId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to save changes to DB");
    }

    [Fact]
    public async Task AddAsync_ShouldCallRepositoryAdd_WithCorrectMessageData()
    {
        // Arrange
        var dto = CreateMessageAddDto();
        var userId = Guid.NewGuid();
        _messageRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        LogArrange(nameof(dto), dto);

        // Act
        await _messagesService.AddAsync(dto, userId);
        LogAct("Operation completed");

        // Assert
        _messageRepositoryMock.Verify(
            r => r.Add(It.Is<Message>(m => m.Content == dto.Content && m.ReceiverId == dto.ReceiverId && m.SenderId == userId)),
            Times.Once
        );
    }

    [Fact]
    public async Task AddAsync_ShouldReturnSuccessWithMappedMessage_WhenSaveChangesSucceeds()
    {
        // Arrange
        var dto = CreateMessageAddDto();
        var userId = Guid.NewGuid();
        _messageRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _messagesService.AddAsync(dto, userId);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Content.Should().Be(dto.Content);
        result.Value.IsCurrUserSender.Should().BeTrue(); // sender == userId, waiting to confirm ToDTO logic
    // result.Value.GroupName.Should().Be(???); // need Message entity / ToDTO to know source of GroupName
    }

    [Fact]
    public async Task AddAsync_ShouldNotThrow_WhenSaveChangesFails_AndShouldNotReturnValue()
    {
        // Arrange
        var dto = CreateMessageAddDto();
        var userId = Guid.NewGuid();
        _messageRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        LogArrange(nameof(dto), dto);

        // Act
        var result = await _messagesService.AddAsync(dto, userId);
        LogAct(result);

        // Assert
        result.Value.Should().BeNull();
    }

    #endregion
    #region LazyGetMessages
    [Fact]
    public async Task LazyGetMessages_ShouldReturnMappedMessages_WhenMessagesExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var lazyData = CreateLazyDto();
        var messages = new List<Message>
        {
            CreateMessage(),
            CreateMessage()
        };
        _messageRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Message, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Message, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Message, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(messages);
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _messagesService.LazyGetMessages(userId, otherUserId, lazyData);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Count.Should().Be(messages.Count);
    }

    [Fact]
    public async Task LazyGetMessages_ShouldReturnEmptyList_WhenNoMessagesExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var lazyData = CreateLazyDto();
        _messageRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Message, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Message, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Message, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Message>());
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _messagesService.LazyGetMessages(userId, otherUserId, lazyData);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetMessages_ShouldCallRepository_WithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var lazyData = CreateLazyDto();
        _messageRepositoryMock.Setup(r => r.FilterAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Message, bool>>>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Message, object?>>[]?>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Message, object?>>?>(),
            true,
            lazyData.Taken,
            lazyData.SectionSize,
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync(new List<Message>());
        LogArrange(nameof(userId), userId);

        // Act
        await _messagesService.LazyGetMessages(userId, otherUserId, lazyData);
        LogAct("Operation completed");

        // Assert
        _messageRepositoryMock.Verify(
            r => r.FilterAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Message, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Message, object?>>[]?>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Message, object?>>?>(),
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
