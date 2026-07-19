using System.Net;
   using AutoFixture;
   using FluentAssertions;
   using HR_System.Core.common;
   using HR_System.Core.Domain.Entities;
   using HR_System.Core.Domain.Identity;
   using HR_System.Core.DTO.LazyLoading;
   using HR_System.Core.DTO.OrganizationHierarchy;
   using HR_System.Core.Enums;
   using HR_System.Core.Interfaces.RepositoryContracts;
   using HR_System.Core.Interfaces.ServiceContracts;
   using HR_System.Infrastructure.Services;
   using Microsoft.Extensions.Logging.Abstractions;
   using Moq;
   using Xunit.Abstractions;
   
   namespace TestProject1.ServicesTests;
   
   public class OrganizationHierarchyServiceTests
   {
       private readonly IOrganizationHierarchyService _hierarchyService;
       private readonly Mock<IOrganizationHierarchyRepository> _hierarchyRepositoryMock;
       private readonly ITestOutputHelper _output;
       private readonly IFixture _fixture;
   
       public OrganizationHierarchyServiceTests(ITestOutputHelper output)
       {
           _output = output;
   
           _fixture = new Fixture();
           _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
               .ForEach(b => _fixture.Behaviors.Remove(b));
           _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
   
           _hierarchyRepositoryMock = new Mock<IOrganizationHierarchyRepository>();
   
           _hierarchyService = new OrganizationHierarchyService(
               _hierarchyRepositoryMock.Object,
               NullLogger<OrganizationHierarchyService>.Instance);
       }
   
       private OrganizationHierarchyAddDTO CreateAddDto(Guid? userId = null, Guid? parentId = null)
       {
           return _fixture.Build<OrganizationHierarchyAddDTO>()
               .With(h => h.UserId, userId ?? Guid.NewGuid())
               .With(h => h.ParentId, parentId ?? Guid.NewGuid())
               .Create();
       }
   
       private ApplicationUser CreateUser(string? userName = null, string? imageUrl = null, PositionsEnum? position = null)
       {
           return _fixture.Build<ApplicationUser>()
               .With(u => u.UserName, userName ?? _fixture.Create<string>())
               .With(u => u.ImageUrl, imageUrl ?? _fixture.Create<string>())
               .With(u => u.Position, position ?? PositionsEnum.Employee)
               .Without(u => u.RefreshTokens)
               .Without(u => u.Tasks)
               .Without(u => u.CreatedTasks)
               .Without(u => u.Approvals)
               .Without(u => u.ToApprove)
               .Without(u => u.Activities)
               .Without(u => u.OrganizationHierarchy)
               .Without(u => u.SentMessages)
               .Without(u => u.ReceivedMessages)
               .Create();
       }
   
       private OrganizationHierarchy CreateHierarchy(
           Guid? userId = null,
           Guid? parentId = null,
           ApplicationUser? user = null,
           List<OrganizationHierarchy>? children = null)
       {
           return _fixture.Build<OrganizationHierarchy>()
               .With(h => h.UserId, userId ?? Guid.NewGuid())
               .With(h => h.ParentId, parentId)
               .With(h => h.User, user)
               .With(h => h.Children, children ?? new List<OrganizationHierarchy>())
               .Without(h => h.Parent)
               .Create();
       }
   
       #region AddAsync
   
       [Fact]
       public async Task AddAsync_ShouldReturnFailure_WhenSaveChangesFails()
       {
           // Arrange
           var dto = CreateAddDto();
   
           _hierarchyRepositoryMock
               .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
   
           // Act
           var result = await _hierarchyService.AddAsync(dto);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("Failed to add organization hierarchy");
       }
   
       [Fact]
       public async Task AddAsync_ShouldCallRepositoryAdd_WithCorrectData()
       {
           // Arrange
           var dto = CreateAddDto();
   
           _hierarchyRepositoryMock
               .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);
   
           // Act
           await _hierarchyService.AddAsync(dto);
   
           // Assert
           _hierarchyRepositoryMock.Verify(
               r => r.Add(It.Is<OrganizationHierarchy>(h =>
                   h.UserId == dto.UserId &&
                   h.ParentId == dto.ParentId)),
               Times.Once);
       }
   
       [Fact]
       public async Task AddAsync_ShouldReturnSuccessWithMappedDto_WhenSaveChangesSucceeds()
       {
           // Arrange
           var dto = CreateAddDto();
   
           _hierarchyRepositoryMock
               .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);
   
           // Act
           var result = await _hierarchyService.AddAsync(dto);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value.Should().NotBeNull();
           result.Value!.UserId.Should().Be(dto.UserId);
           result.Value.Children.Should().BeEmpty();
           // Newly created entity has no User navigation loaded, so ToDTO falls back to defaults:
           result.Value.UserName.Should().Be("unknown");
           result.Value.UserImageUrl.Should().Be("Missing Photo");
           result.Value.Position.Should().Be(PositionsEnum.unknown);
       }
   
       #endregion
   
       #region GetChildrenAsync
   
       [Fact]
       public async Task GetChildrenAsync_ShouldGroupUnderEmptyGuid_WhenParentsIsNull()
       {
           // Arrange
           var children = new List<OrganizationHierarchy> { CreateHierarchy(), CreateHierarchy() };
   
           _hierarchyRepositoryMock
               .Setup(r => r.GetChildrenAsync(null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(children);
   
           // Act
           var result = await _hierarchyService.GetChildrenAsync( null);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value.Should().ContainKey(Guid.Empty);
           result.Value![Guid.Empty].Count.Should().Be(children.Count);
       }
   
       [Fact]
       public async Task GetChildrenAsync_ShouldGroupUnderEmptyGuid_WhenParentsIsEmpty()
       {
           // Arrange
           Guid[] parents = [];
           var children = new List<OrganizationHierarchy> { CreateHierarchy() };
   
           _hierarchyRepositoryMock
               .Setup(r => r.GetChildrenAsync(parents, It.IsAny<CancellationToken>()))
               .ReturnsAsync(children);
   
           // Act
           var result = await _hierarchyService.GetChildrenAsync(parents);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value.Should().ContainKey(Guid.Empty);
           result.Value![Guid.Empty].Count.Should().Be(children.Count);
       }
   
       [Fact]
       public async Task GetChildrenAsync_ShouldGroupChildrenByParentId_WhenParentsProvided()
       {
           // Arrange
           var parent1 = Guid.NewGuid();
           var parent2 = Guid.NewGuid();
           var parents = new List<Guid> { parent1, parent2 };
   
           var child1 = CreateHierarchy(parentId: parent1);
           var child2 = CreateHierarchy(parentId: parent1);
           var child3 = CreateHierarchy(parentId: parent2);
           var children = new List<OrganizationHierarchy> { child1, child2, child3 };
   
           _hierarchyRepositoryMock
               .Setup(r => r.GetChildrenAsync(parents, It.IsAny<CancellationToken>()))
               .ReturnsAsync(children);
   
           // Act
           var result = await _hierarchyService.GetChildrenAsync(parents);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value![parent1].Count.Should().Be(2);
           result.Value[parent2].Count.Should().Be(1);
       }
   
       [Fact]
       public async Task GetChildrenAsync_ShouldSkipChildrenWithNullParentId_WhenParentsProvided()
       {
           // Arrange
           var parent1 = Guid.NewGuid();
           var parents = new List<Guid> { parent1 };
   
           var childWithParent = CreateHierarchy(parentId: parent1);
           var childWithoutParent = CreateHierarchy(parentId: null);
           var children = new List<OrganizationHierarchy> { childWithParent, childWithoutParent };
   
           _hierarchyRepositoryMock
               .Setup(r => r.GetChildrenAsync(parents, It.IsAny<CancellationToken>()))
               .ReturnsAsync(children);
   
           // Act
           var result = await _hierarchyService.GetChildrenAsync(parents);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value![parent1].Count.Should().Be(1);
       }
   
       [Fact]
       public async Task GetChildrenAsync_ShouldCallRepository_WithCorrectParents()
       {
           // Arrange
           var parents = new List<Guid> { Guid.NewGuid() };
   
           _hierarchyRepositoryMock
               .Setup(r => r.GetChildrenAsync(parents, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<OrganizationHierarchy>());
   
           // Act
           await _hierarchyService.GetChildrenAsync(parents);
   
           // Assert
           _hierarchyRepositoryMock.Verify(
               r => r.GetChildrenAsync(parents, It.IsAny<CancellationToken>()),
               Times.Once);
       }
   
       #endregion
   
       #region RemoveAsync
   
       [Fact]
       public async Task RemoveAsync_ShouldReturnFailure_WhenHierarchyNotFound()
       {
           // Arrange
           var toRemoveId = Guid.NewGuid();
           var currUserId = Guid.NewGuid();
   
           _hierarchyRepositoryMock
               .Setup(r => r.RemoveAsync(toRemoveId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((OrganizationHierarchy?)null);
   
           // Act
           var result = await _hierarchyService.RemoveAsync(toRemoveId, currUserId);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("hierarchy not found");
           result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
   
           _hierarchyRepositoryMock.Verify(
               r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
               Times.Never);
       }
   
       [Fact]
       public async Task RemoveAsync_ShouldReturnFailure_WhenRemovingRootEmployee()
       {
           // Arrange
           var toRemoveId = Guid.NewGuid();
           var currUserId = Guid.NewGuid();
           var removed = CreateHierarchy(parentId: null);
   
           _hierarchyRepositoryMock
               .Setup(r => r.RemoveAsync(toRemoveId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(removed);
   
           // Act
           var result = await _hierarchyService.RemoveAsync(toRemoveId, currUserId);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("cannt remove root employee");
           result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
   
           _hierarchyRepositoryMock.Verify(
               r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
               Times.Never);
       }
   
       [Fact]
       public async Task RemoveAsync_ShouldReturnFailure_WhenSaveChangesFails()
       {
           // Arrange
           var toRemoveId = Guid.NewGuid();
           var currUserId = Guid.NewGuid();
           var removed = CreateHierarchy(parentId: Guid.NewGuid());
   
           _hierarchyRepositoryMock
               .Setup(r => r.RemoveAsync(toRemoveId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(removed);
   
           _hierarchyRepositoryMock
               .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
   
           // Act
           var result = await _hierarchyService.RemoveAsync(toRemoveId, currUserId);
   
           // Assert
           result.IsSuccess.Should().BeFalse();
           result.ErrorMessage.Should().Be("failed saving changes to DB");
       }
   
       [Fact]
       public async Task RemoveAsync_ShouldReturnSuccessWithMappedDto_WhenAllStepsSucceed()
       {
           // Arrange
           var toRemoveId = Guid.NewGuid();
           var currUserId = Guid.NewGuid();
           var user = CreateUser();
           var removed = CreateHierarchy(userId: currUserId, parentId: Guid.NewGuid(), user: user);
   
           _hierarchyRepositoryMock
               .Setup(r => r.RemoveAsync(toRemoveId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(removed);
   
           _hierarchyRepositoryMock
               .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);
   
           // Act
           var result = await _hierarchyService.RemoveAsync(toRemoveId, currUserId);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value.Should().NotBeNull();
           result.Value!.UserId.Should().Be(currUserId);
           result.Value.UserName.Should().Be(user.UserName);
       }
   
       #endregion
   
       #region GetParentUserIds
   
       [Fact]
       public async Task GetParentUserIds_ShouldReturnRepositoryResult()
       {
           // Arrange
           var userId = Guid.NewGuid();
           var parentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
   
           _hierarchyRepositoryMock
               .Setup(r => r.GetParentUserIds(userId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(parentIds);
   
           // Act
           var result = await _hierarchyService.GetParentUserIds(userId);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value.Should().BeEquivalentTo(parentIds);
       }
   
       #endregion
   
       #region GetUserNames
   
       [Fact]
       public async Task GetUserNames_ShouldReturnRepositoryResult()
       {
           // Arrange
           var lazyData = _fixture.Create<LazyDTO>();
           var userNames = new List<UserNameDTO>
           {
               new() { UserName = "John", TreeId = Guid.NewGuid() },
               new() { UserName = "Jane", TreeId = Guid.NewGuid() }
           };
   
           _hierarchyRepositoryMock
               .Setup(r => r.GetUserNames(lazyData, It.IsAny<CancellationToken>()))
               .ReturnsAsync(userNames);
   
           // Act
           var result = await _hierarchyService.GetUserNames(lazyData);
   
           // Assert
           result.IsSuccess.Should().BeTrue();
           result.Value.Should().BeEquivalentTo(userNames);
   
           _hierarchyRepositoryMock.Verify(
               r => r.GetUserNames(lazyData, It.IsAny<CancellationToken>()),
               Times.Once);
       }
   
       #endregion
   }