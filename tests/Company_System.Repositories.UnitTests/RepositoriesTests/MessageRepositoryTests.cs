using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class MessageRepositoryTests : IDisposable
{
    private readonly IMessageRepository _messageRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public MessageRepositoryTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(dbOptions);
        _messageRepository = new MessageRepository(_dbContext);
    }

    #region Add

    [Fact]
    public async Task Add_ValidMessage_ShouldPersistAfterSave()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var message = CreateMessage(senderId: senderId);
        _output.WriteLine($"Adding Message: {message.Id} | SenderId: {message.SenderId}");

        // Act
        _messageRepository.Add(message);
        await _messageRepository.SaveChangesAsync();

        // Assert
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
        var actual = await _messageRepository.LazyGetMessages(senderId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");
        _output.WriteLine($"Actual Id   : {actual.FirstOrDefault()?.Id}");

        actual.Should().ContainSingle(m => m.Id == message.Id);
    }

    [Fact]
    public async Task Add_WithoutSaving_ShouldNotPersist()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var message = CreateMessage(senderId: senderId);
        _output.WriteLine($"Adding Message without saving: {message.Id}");

        // Act — skip SaveChangesAsync intentionally
        _messageRepository.Add(message);

        // Assert
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
        var actual = await _messageRepository.LazyGetMessages(senderId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_MultipleMessages_ShouldPersistAll()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var messages = CreateMany(3, senderId: senderId);
        _output.WriteLine($"Expected Count: {messages.Count}");
        messages.ForEach(m => _output.WriteLine($"  Message: {m.Id} | SenderId: {m.SenderId}"));

        // Act
        foreach (var message in messages)
            _messageRepository.Add(message);
        await _messageRepository.SaveChangesAsync();

        // Assert
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
        var actual = await _messageRepository.LazyGetMessages(senderId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        actual.Should().HaveCount(messages.Count);
    }

    #endregion

    #region LazyGetMessages

    [Fact]
    public async Task LazyGetMessages_UserIsSender_ShouldReturnMessages()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var message = CreateMessage(senderId: senderId);
        await SeedAsync(message);
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        _output.WriteLine($"SenderId      : {senderId}");
        _output.WriteLine($"Expected Id   : {message.Id}");

        // Act
        var actual = await _messageRepository.LazyGetMessages(senderId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");
        _output.WriteLine($"Actual Id   : {actual.FirstOrDefault()?.Id}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().ContainSingle(m => m.Id == message.Id);
    }

    [Fact]
    public async Task LazyGetMessages_UserIsReceiver_ShouldReturnMessages()
    {
        // Arrange
        var receiverId = Guid.NewGuid();
        var message = CreateMessage(receiverId: receiverId);
        await SeedAsync(message);
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        _output.WriteLine($"ReceiverId    : {receiverId}");
        _output.WriteLine($"Expected Id   : {message.Id}");

        // Act
        var actual = await _messageRepository.LazyGetMessages(receiverId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().ContainSingle(m => m.Id == message.Id);
    }

    [Fact]
    public async Task LazyGetMessages_UserHasNoMessages_ShouldReturnEmpty()
    {
        // Arrange
        var unrelatedUser = Guid.NewGuid();
        var otherMessages = CreateMany(3); // belong to random sender/receiver ids
        await SeedAsync([.. otherMessages]);
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        _output.WriteLine($"UserId        : {unrelatedUser}");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _messageRepository.LazyGetMessages(unrelatedUser, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetMessages_ShouldExcludeUnrelatedMessages()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userMessages = CreateMany(2, senderId: userId);
        var otherMessages = CreateMany(3);
        await SeedAsync([.. userMessages, .. otherMessages]);
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        _output.WriteLine($"UserId        : {userId}");
        _output.WriteLine($"Expected Count: {userMessages.Count}");

        // Act
        var actual = await _messageRepository.LazyGetMessages(userId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().HaveCount(userMessages.Count);
        actual.Should().OnlyContain(m => m.SenderId == userId || m.ReceiverId == userId);
    }

    [Fact]
    public async Task LazyGetMessages_ShouldReturnMessagesOrderedByCreatedAtDescending()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var oldest = CreateMessage(senderId: senderId, createdAt: DateTime.Now.AddHours(-2));
        var middle = CreateMessage(senderId: senderId, createdAt: DateTime.Now.AddHours(-1));
        var newest = CreateMessage(senderId: senderId, createdAt: DateTime.Now);
        await SeedAsync(oldest, middle, newest);
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        _output.WriteLine($"Oldest : {oldest.Id} | {oldest.CreatedAt}");
        _output.WriteLine($"Middle : {middle.Id} | {middle.CreatedAt}");
        _output.WriteLine($"Newest : {newest.Id} | {newest.CreatedAt}");

        // Act
        var actual = await _messageRepository.LazyGetMessages(senderId, lazyData);
        actual.ToList().ForEach(m => _output.WriteLine($"  Actual: {m.Id} | {m.CreatedAt}"));

        // Assert
        actual.Select(m => m.Id).Should().ContainInOrder(newest.Id, middle.Id, oldest.Id);
    }

    [Fact]
    public async Task LazyGetMessages_PaginationSkip_ShouldSkipTakenMessages()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var oldest = CreateMessage(senderId: senderId, createdAt: DateTime.Now.AddHours(-2));
        var middle = CreateMessage(senderId: senderId, createdAt: DateTime.Now.AddHours(-1));
        var newest = CreateMessage(senderId: senderId, createdAt: DateTime.Now);
        await SeedAsync(oldest, middle, newest);
        var lazyData = new LazyDTO { Taken = 2, SectionSize = 10 };

        _output.WriteLine($"Taken         : {lazyData.Taken}");
        _output.WriteLine($"Expected Id   : {oldest.Id}");

        // Act
        var actual = await _messageRepository.LazyGetMessages(senderId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");
        _output.WriteLine($"Actual Id   : {actual.FirstOrDefault()?.Id}");

        // Assert
        actual.Should().ContainSingle(m => m.Id == oldest.Id);
    }

    [Fact]
    public async Task LazyGetMessages_PaginationTake_ShouldLimitBySectionSize()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var messages = CreateMany(5, senderId: senderId);
        await SeedAsync([.. messages]);
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 3 };

        _output.WriteLine($"Total Seeded  : {messages.Count}");
        _output.WriteLine($"SectionSize   : {lazyData.SectionSize}");
        _output.WriteLine("Expected Count: 3");

        // Act
        var actual = await _messageRepository.LazyGetMessages(senderId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().HaveCount(lazyData.SectionSize);
    }

    [Fact]
    public async Task LazyGetMessages_TakenBeyondTotal_ShouldReturnEmpty()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        await SeedAsync(CreateMessage(senderId: senderId));
        var lazyData = new LazyDTO { Taken = 100, SectionSize = 10 };

        _output.WriteLine($"Taken         : {lazyData.Taken}");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _messageRepository.LazyGetMessages(senderId, lazyData);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task LazyGetMessages_ShouldReturnReadOnlyList()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        await SeedAsync(CreateMessage(senderId: senderId));
        var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };

        // Act
        var actual = await _messageRepository.LazyGetMessages(senderId, lazyData);
        _output.WriteLine($"Actual Type: {actual.GetType().Name}");

        // Assert
        actual.Should().BeAssignableTo<IReadOnlyList<Message>>();
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ShouldReturnTrue()
    {
        // Arrange
        var message = CreateMessage();
        _messageRepository.Add(message);
        _output.WriteLine($"Added Message: {message.Id}");

        // Act
        var actual = await _messageRepository.SaveChangesAsync();
        _output.WriteLine($"Expected: true | Actual: {actual}");

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ShouldReturnFalse()
    {
        // Arrange
        _output.WriteLine("No changes made");

        // Act
        var actual = await _messageRepository.SaveChangesAsync();
        _output.WriteLine($"Expected: false | Actual: {actual}");

        // Assert
        actual.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private Message CreateMessage(Guid? senderId = null, Guid? receiverId = null, DateTime? createdAt = null) =>
        _fixture.Build<Message>()
            .With(m => m.SenderId, senderId ?? Guid.NewGuid())
            .With(m => m.ReceiverId, receiverId ?? Guid.NewGuid())
            .With(m => m.CreatedAt, createdAt ?? DateTime.Now)
            .With(m => m.Sender, null as ApplicationUser)
            .With(m => m.Receiver, null as ApplicationUser)
            .Create();

    private List<Message> CreateMany(int count, Guid? senderId = null, Guid? receiverId = null) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateMessage(senderId, receiverId))
            .ToList();

    private async Task SeedAsync(params Message[] messages)
    {
        await _dbContext.Messages.AddRangeAsync(messages);
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();

    #endregion
}