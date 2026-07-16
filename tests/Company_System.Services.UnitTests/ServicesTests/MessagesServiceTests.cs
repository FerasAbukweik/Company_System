using AutoFixture;
   using FluentAssertions;
   using HR_System.Core.common;
   using HR_System.Core.Domain.Entities;
   using HR_System.Core.DTO.LazyLoading;
   using HR_System.Core.DTO.Message;
   using HR_System.Core.Interfaces.RepositoryContracts;
   using HR_System.Core.Interfaces.ServiceContracts;
   using HR_System.Infrastructure.Services;
   using Microsoft.Extensions.Logging.Abstractions;
   using Moq;
   using Xunit.Abstractions;
   
   namespace TestProject1.ServicesTests;
   
   public class MessagesServiceTests
   {
       private readonly IMessagesService _messagesService;
       private readonly Mock<IMessageRepository> _messageRepositoryMock;
       private readonly ITestOutputHelper _output;
       private readonly IFixture _fixture;
   
       public MessagesServiceTests(ITestOutputHelper output)
       {
           _output = output;
   
           _fixture = new Fixture();
           _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
               .ForEach(b => _fixture.Behaviors.Remove(b));
           _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
   
           _messageRepositoryMock = new Mock<IMessageRepository>();
   
           _messagesService = new MessagesService(
               _messageRepositoryMock.Object,
               NullLogger<MessagesService>.Instance);
       }
   
       private MessageAddDTO CreateMessageAddDto(Guid? receiverId = null)
       {
           return _fixture.Build<MessageAddDTO>()
               .With(m => m.ReceiverId, receiverId ?? Guid.NewGuid())
               .Create();
       }
   
       private Message CreateMessage()
       {
           return _fixture.Build<Message>()
               // adjust/add .Without(...) here if Message has navigation properties
               // (e.g. Sender, Receiver) that would cause AutoFixture recursion issues
               .Create();
       }
   
       private LazyDTO CreateLazyDto()
       {
           return _fixture.Create<LazyDTO>();
       }
   
       #region AddAsync
   
       [Fact]
       public async Task AddAsync_ShouldReturnFailure_WhenSaveChangesFails()
       {
           // Arrange
           var dto = CreateMessageAddDto();
           var userId = Guid.NewGuid();
   
           _messageRepositoryMock
               .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
   
           // Act
           var result = await _messagesService.AddAsync(dto, userId);
   
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
   
           _messageRepositoryMock
               .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);
   
           // Act
           await _messagesService.AddAsync(dto, userId);
   
           // Assert
           _messageRepositoryMock.Verify(
               r => r.Add(It.Is<Message>(m =>
                   m.Content == dto.Content &&
                   m.ReceiverId == dto.ReceiverId &&
                   m.SenderId == userId)),
               Times.Once);
       }
       [Fact]
       public async Task AddAsync_ShouldReturnSuccessWithMappedMessage_WhenSaveChangesSucceeds()
       {
           // Arrange
           var dto = CreateMessageAddDto();
           var userId = Guid.NewGuid();

           _messageRepositoryMock
               .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

           // Act
           var result = await _messagesService.AddAsync(dto, userId);

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
   
           _messageRepositoryMock
               .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
   
           // Act
           var result = await _messagesService.AddAsync(dto, userId);
   
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
           var messages = new List<Message> { CreateMessage(), CreateMessage() };
   
           _messageRepositoryMock
               .Setup(r => r.LazyGetMessages(userId, otherUserId, lazyData, It.IsAny<CancellationToken>()))
               .ReturnsAsync(messages);
   
           // Act
           var result = await _messagesService.LazyGetMessages(userId, otherUserId, lazyData);
   
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
   
           _messageRepositoryMock
               .Setup(r => r.LazyGetMessages(userId, otherUserId, lazyData, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Message>());
   
           // Act
           var result = await _messagesService.LazyGetMessages(userId, otherUserId, lazyData);
   
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
   
           _messageRepositoryMock
               .Setup(r => r.LazyGetMessages(userId, otherUserId, lazyData, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Message>());
   
           // Act
           await _messagesService.LazyGetMessages(userId, otherUserId, lazyData);
   
           // Assert
           _messageRepositoryMock.Verify(
               r => r.LazyGetMessages(userId, otherUserId, lazyData, It.IsAny<CancellationToken>()),
               Times.Once);
       }
   
       #endregion
   }