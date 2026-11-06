using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class MessageRepositoryTests : RepositoryTestBase, IDisposable
{
    private readonly IMessageRepository _messageRepository;
    private readonly ApplicationDbContext _dbContext;

    public MessageRepositoryTests(ITestOutputHelper output) : base(output)
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(dbOptions);
        _messageRepository = new MessageRepository(_dbContext);
    }

    private Message CreateMessage(Guid senderId, Guid receiverId, DateTime? createdAt = null)
    {
        return Fixture.Build<Message>()
            .With(m => m.SenderId, senderId)
            .With(m => m.ReceiverId, receiverId)
            .With(
            m => m.CreatedAt,
            createdAt ?? DateTime.UtcNow
        )
            .Without(m => m.Sender)
            .Without(m => m.Receiver)
            .Create();
    }

    #region Add
    [Fact]
    public void Add_ShouldTrackEntityAsAdded()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldTrackEntityAsAdded));

        var message = CreateMessage(Guid.NewGuid(), Guid.NewGuid());

        // Act
        _messageRepository.Add(message);
        LogAct("Operation completed");

        // Assert
        _dbContext.Entry(message).State.Should().Be(EntityState.Added);
        _dbContext.Messages.Local.Should().Contain(message);
    }

    [Fact]
    public void Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled));

        var message = CreateMessage(Guid.NewGuid(), Guid.NewGuid());

        // Act
        _messageRepository.Add(message);
        LogAct("Operation completed");

        // Assert
        _dbContext.Messages.AsNoTracking().Any(m => m.Id == message.Id).Should().BeFalse();
    }

    #endregion
    #region LazyGetMessages
    [Fact]
    public async Task LazyGetMessages_ShouldReturnMessagesBetweenTheTwoGivenUsers_RegardlessOfDirection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var userToOther = CreateMessage(userId, otherUserId);
        var otherToUser = CreateMessage(otherUserId, userId);
        _dbContext.Messages.AddRange(userToOther, otherToUser);
        await _dbContext.SaveChangesAsync();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _messageRepository.FilterAsync(
            message => (message.SenderId == userId && message.ReceiverId == otherUserId) || (message.SenderId == otherUserId && message.ReceiverId == userId),
            orderBy: message => message.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert
        result.Should().HaveCount(2);
        result.Select(m => m.Id).Should().Contain([userToOther.Id, otherToUser.Id]);
    }

    [Fact]
    public async Task LazyGetMessages_ShouldExcludeMessagesInvolvingAThirdParty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var thirdPartyId = Guid.NewGuid();
        var conversationMessage = CreateMessage(userId, otherUserId);
        var unrelatedMessage = CreateMessage(userId, thirdPartyId);
        _dbContext.Messages.AddRange(
            conversationMessage,
            unrelatedMessage,
            CreateMessage(userId, userId),
            CreateMessage(otherUserId, otherUserId)
        );
        await _dbContext.SaveChangesAsync();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _messageRepository.FilterAsync(
            message => (message.SenderId == userId && message.ReceiverId == otherUserId) || (message.SenderId == otherUserId && message.ReceiverId == userId),
            orderBy: message => message.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert
        result.Should().ContainSingle(m => m.Id == conversationMessage.Id);
        result.Should().NotContain(m => m.Id == unrelatedMessage.Id);
    }

    [Fact]
    public async Task LazyGetMessages_ShouldReturnMessagesOrderedByCreatedAtDescending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var oldest = CreateMessage(userId, otherUserId, DateTime.UtcNow.AddDays(-2));
        var middle = CreateMessage(otherUserId, userId, DateTime.UtcNow.AddDays(-1));
        var newest = CreateMessage(userId, otherUserId, DateTime.UtcNow);
        _dbContext.Messages.AddRange(oldest, middle, newest);
        await _dbContext.SaveChangesAsync();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _messageRepository.FilterAsync(
            message => (message.SenderId == userId && message.ReceiverId == otherUserId) || (message.SenderId == otherUserId && message.ReceiverId == userId),
            orderBy: message => message.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert
        result.Select(m => m.Id).Should().ContainInOrder(newest.Id, middle.Id, oldest.Id);
    }

    [Fact]
    public async Task LazyGetMessages_ShouldRespectSkipAndTake()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var messages = Enumerable.Range(0, 5).Select(i => CreateMessage(
            i % 2 == 0 ? userId : otherUserId,
            i % 2 == 0 ? otherUserId : userId,
            DateTime.UtcNow.AddMinutes(-i)
        )).ToList();
        _dbContext.Messages.AddRange(messages);
        await _dbContext.SaveChangesAsync();
        var lazyData = new LazyDTO
        {
            Taken = 1,
            SectionSize = 2
        };
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _messageRepository.FilterAsync(
            message => (message.SenderId == userId && message.ReceiverId == otherUserId) || (message.SenderId == otherUserId && message.ReceiverId == userId),
            orderBy: message => message.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert — sorted desc by CreatedAt: [0,1,2,3,4] -> skip 1, take 2 -> [1,2]

        // Assert
        result.Should().HaveCount(2);
        result.Select(m => m.Id).Should().ContainInOrder(messages[1].Id, messages[2].Id);
    }

    [Fact]
    public async Task LazyGetMessages_ShouldReturnEmptyList_WhenNoConversationExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _dbContext.Messages.Add(CreateMessage(Guid.NewGuid(), Guid.NewGuid()));
        await _dbContext.SaveChangesAsync();
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(userId), userId);

        // Act
        var result = await _messageRepository.FilterAsync(
            message => (message.SenderId == userId && message.ReceiverId == otherUserId) || (message.SenderId == otherUserId && message.ReceiverId == userId),
            orderBy: message => message.CreatedAt,
            orderByDesc: true,
            skip: lazyData.Taken,
            take: lazyData.SectionSize
        );
        LogAct(result);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
    #region SaveChangesAsync
    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges));

        _dbContext.Messages.Add(CreateMessage(Guid.NewGuid(), Guid.NewGuid()));

        // Act
        var result = await _messageRepository.SaveChangesAsync();
        LogAct(result);

        // Assert
        result.Should().BeTrue();
        (await _dbContext.Messages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges));

        // Act
        var result = await _messageRepository.SaveChangesAsync();
        LogAct(result);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
    public void Dispose() => _dbContext.Dispose();
}
