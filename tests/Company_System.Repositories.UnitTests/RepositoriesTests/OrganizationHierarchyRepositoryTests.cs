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
   
   public class OrganizationHierarchyRepositoryTests : IDisposable
   {
       private readonly IOrganizationHierarchyRepository _hierarchyRepository;
       private readonly ApplicationDbContext _dbContext;
       private readonly ITestOutputHelper _output;
       private readonly IFixture _fixture;
   
       public OrganizationHierarchyRepositoryTests(ITestOutputHelper output)
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
           _hierarchyRepository = new OrganizationHierarchyRepository(_dbContext);
       }
   
       private ApplicationUser CreateUser(string? userName = null)
       {
           return _fixture.Build<ApplicationUser>()
               .With(u => u.UserName, userName ?? $"user_{Guid.NewGuid():N}")
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
   
       private OrganizationHierarchy CreateNode(Guid userId, Guid? parentId = null)
       {
           return _fixture.Build<OrganizationHierarchy>()
               .With(o => o.UserId, userId)
               .With(o => o.ParentId, parentId)
               .Without(o => o.User)
               .Without(o => o.Parent)
               .Without(o => o.Children)
               .Create();
       }
   
       #region Add
   
       [Fact]
       public void Add_ShouldTrackEntityAsAdded()
       {
           // Arrange
           var user = CreateUser();
           var node = CreateNode(user.Id);
   
           // Act
           _hierarchyRepository.Add(node);
   
           // Assert
           _dbContext.Entry(node).State.Should().Be(EntityState.Added);
           _dbContext.OrganizationHierarchies.Local.Should().Contain(node);
       }
   
       [Fact]
       public void Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled()
       {
           // Arrange
           var user = CreateUser();
           var node = CreateNode(user.Id);
   
           // Act
           _hierarchyRepository.Add(node);
   
           // Assert
           _dbContext.OrganizationHierarchies.AsNoTracking().Any(o => o.Id == node.Id).Should().BeFalse();
       }
   
       #endregion
   
       #region GetChildrenAsync
   
       [Fact]
       public async Task GetChildrenAsync_ShouldReturnRootNodes_WhenParentsIsNullOrEmpty()
       {
           // Arrange
           var rootUser = CreateUser();
           var childUser = CreateUser();
           _dbContext.Users.AddRange(rootUser, childUser);
   
           var root = CreateNode(rootUser.Id);
           var child = CreateNode(childUser.Id, parentId: root.Id);
           _dbContext.OrganizationHierarchies.AddRange(root, child);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           var result = await _hierarchyRepository.GetChildrenAsync(null);
   
           // Assert
           result.Should().ContainSingle(o => o.Id == root.Id);
           result.Should().NotContain(o => o.Id == child.Id);
       }
   
       [Fact]
       public async Task GetChildrenAsync_ShouldReturnNodesWithMatchingParentIds_WhenParentsProvided()
       {
           // Arrange
           var parentUser = CreateUser();
           var childUser = CreateUser();
           var unrelatedUser = CreateUser();
           _dbContext.Users.AddRange(parentUser, childUser, unrelatedUser);
   
           var parent = CreateNode(parentUser.Id);
           var child = CreateNode(childUser.Id, parentId: parent.Id);
           var unrelated = CreateNode(unrelatedUser.Id);
           _dbContext.OrganizationHierarchies.AddRange(parent, child, unrelated);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           var result = await _hierarchyRepository.GetChildrenAsync([parent.Id]);
   
           // Assert
           result.Should().ContainSingle(o => o.Id == child.Id);
           result.Should().NotContain(o => o.Id == parent.Id);
           result.Should().NotContain(o => o.Id == unrelated.Id);
       }
   
       [Fact]
       public async Task GetChildrenAsync_ShouldReturnEmptyList_WhenNoNodesMatchGivenParents()
       {
           // Arrange
           var user = CreateUser();
           _dbContext.Users.Add(user);
           _dbContext.OrganizationHierarchies.Add(CreateNode(user.Id));
           await _dbContext.SaveChangesAsync();
   
           // Act
           var result = await _hierarchyRepository.GetChildrenAsync([Guid.NewGuid()]);
   
           // Assert
           result.Should().BeEmpty();
       }
   
       [Fact]
       public async Task GetChildrenAsync_ShouldIncludeUserAndNestedChildren()
       {
           // Arrange
           var rootUser = CreateUser("root_user");
           var childUser = CreateUser("child_user");
           var grandChildUser = CreateUser("grandchild_user");
           _dbContext.Users.AddRange(rootUser, childUser, grandChildUser);
   
           var root = CreateNode(rootUser.Id);
           var child = CreateNode(childUser.Id, parentId: root.Id);
           var grandChild = CreateNode(grandChildUser.Id, parentId: child.Id);
           _dbContext.OrganizationHierarchies.AddRange(root, child, grandChild);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           var result = await _hierarchyRepository.GetChildrenAsync(null);
   
           // Assert
           var fetchedRoot = result.Single(o => o.Id == root.Id);
           fetchedRoot.User.Should().NotBeNull();
           fetchedRoot.User!.UserName.Should().Be("root_user");
   
           var fetchedChild = fetchedRoot.Children.Single(c => c.Id == child.Id);
           fetchedChild.User!.UserName.Should().Be("child_user");
   
           var fetchedGrandChild = fetchedChild.Children.Single(c => c.Id == grandChild.Id);
           fetchedGrandChild.User!.UserName.Should().Be("grandchild_user");
       }
   
       #endregion
   
       #region RemoveAsync
   
       [Fact]
       public async Task RemoveAsync_ShouldMarkEntityAsRemoved_WhenNodeExists()
       {
           // Arrange
           var user = CreateUser();
           _dbContext.Users.Add(user);
           var node = CreateNode(user.Id);
           _dbContext.OrganizationHierarchies.Add(node);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           var result = await _hierarchyRepository.RemoveAsync(node.Id);
   
           // Assert
           result.Should().NotBeNull();
           result!.Id.Should().Be(node.Id);
           _dbContext.Entry(result).State.Should().Be(EntityState.Deleted);
       }
   
       [Fact]
       public async Task RemoveAsync_ShouldPersistRemoval_AfterSaveChanges()
       {
           // Arrange
           var user = CreateUser();
           _dbContext.Users.Add(user);
           var node = CreateNode(user.Id);
           _dbContext.OrganizationHierarchies.Add(node);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           await _hierarchyRepository.RemoveAsync(node.Id);
           await _dbContext.SaveChangesAsync();
   
           // Assert
           (await _dbContext.OrganizationHierarchies.AnyAsync(o => o.Id == node.Id)).Should().BeFalse();
       }
   
       [Fact]
       public async Task RemoveAsync_ShouldReturnNull_WhenNodeDoesNotExist()
       {
           // Act
           var result = await _hierarchyRepository.RemoveAsync(Guid.NewGuid());
   
           // Assert
           result.Should().BeNull();
       }
   
       #endregion
   
       #region GetByUserIdAsync
   
       [Fact]
       public async Task GetByUserIdAsync_ShouldReturnMatchingNode_WhenItExists()
       {
           // Arrange
           var user = CreateUser();
           _dbContext.Users.Add(user);
           var node = CreateNode(user.Id);
           _dbContext.OrganizationHierarchies.Add(node);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           var result = await _hierarchyRepository.GetByUserIdAsync(user.Id);
   
           // Assert
           result.Should().NotBeNull();
           result!.Id.Should().Be(node.Id);
       }
   
       [Fact]
       public async Task GetByUserIdAsync_ShouldReturnNull_WhenNoNodeMatchesUserId()
       {
           // Act
           var result = await _hierarchyRepository.GetByUserIdAsync(Guid.NewGuid());
   
           // Assert
           result.Should().BeNull();
       }
   
       [Fact]
       public async Task GetByUserIdAsync_ShouldIncludeParentNode()
       {
           // Arrange
           var parentUser = CreateUser();
           var childUser = CreateUser();
           _dbContext.Users.AddRange(parentUser, childUser);
   
           var parent = CreateNode(parentUser.Id);
           var child = CreateNode(childUser.Id, parentId: parent.Id);
           _dbContext.OrganizationHierarchies.AddRange(parent, child);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           var result = await _hierarchyRepository.GetByUserIdAsync(childUser.Id);
   
           // Assert
           result.Should().NotBeNull();
           result!.Parent.Should().NotBeNull();
           result.Parent!.Id.Should().Be(parent.Id);
       }
   
       [Fact]
       public async Task GetByUserIdAsync_ShouldReturnUntrackedEntity()
       {
           // Arrange
           var user = CreateUser();
           _dbContext.Users.Add(user);
           var node = CreateNode(user.Id);
           _dbContext.OrganizationHierarchies.Add(node);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           // Act
           var result = await _hierarchyRepository.GetByUserIdAsync(user.Id);
   
           // Assert
           _dbContext.Entry(result!).State.Should().Be(EntityState.Detached);
       }
   
       #endregion
   
       #region GetParentUserIds
   
       // Not unit-testable as written: calls dbContext.GetFatherUserIds(), which executes
       // "EXEC getParentUserIds @userId" — a raw SQL Server stored procedure call.
       // UseInMemoryDatabase has no SQL engine and cannot run SqlQueryRaw/EXEC statements,
       // so any call to this method against InMemory throws at runtime. Covering this
       // requires an integration test against a real SQL Server instance with the
       // getParentUserIds proc deployed.
       [Fact(Skip = "Calls a real SQL Server stored procedure via SqlQueryRaw; not executable against UseInMemoryDatabase. Needs an integration test against a live SQL Server.")]
       public Task GetParentUserIds_RequiresIntegrationTestAgainstRealSqlServer()
       {
           return Task.CompletedTask;
       }
   
       #endregion
   
       #region GetUserNames
   
       [Fact]
       public async Task GetUserNames_ShouldReturnTreeIdAndUserNameForEachNode()
       {
           // Arrange
           var user = CreateUser("some_user");
           _dbContext.Users.Add(user);
           var node = CreateNode(user.Id);
           _dbContext.OrganizationHierarchies.Add(node);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
   
           // Act
           var result = await _hierarchyRepository.GetUserNames(lazyData);
   
           // Assert
           var entry = result.Single(r => r.TreeId == node.Id);
           entry.UserName.Should().Be("some_user");
       }
   
       [Fact]
       public async Task GetUserNames_ShouldRespectSkipAndTake()
       {
           // Arrange
           var users = Enumerable.Range(0, 5).Select(i => CreateUser($"user_{i}")).ToList();
           _dbContext.Users.AddRange(users);
   
           var nodes = users.Select(u => CreateNode(u.Id)).ToList();
           _dbContext.OrganizationHierarchies.AddRange(nodes);
           await _dbContext.SaveChangesAsync();
           _dbContext.ChangeTracker.Clear();
   
           var lazyData = new LazyDTO { Taken = 1, SectionSize = 2 };
   
           // Act
           var result = await _hierarchyRepository.GetUserNames(lazyData);
   
           // Assert
           result.Should().HaveCount(2);
       }
   
       [Fact]
       public async Task GetUserNames_ShouldReturnEmptyList_WhenNoNodesExist()
       {
           // Arrange
           var lazyData = new LazyDTO { Taken = 0, SectionSize = 10 };
   
           // Act
           var result = await _hierarchyRepository.GetUserNames(lazyData);
   
           // Assert
           result.Should().BeEmpty();
       }
   
       #endregion
   
       #region SaveChangesAsync
   
       [Fact]
       public async Task SaveChangesAsync_ShouldReturnTrue_WhenThereArePendingChanges()
       {
           // Arrange
           var user = CreateUser();
           _dbContext.Users.Add(user);
           _dbContext.OrganizationHierarchies.Add(CreateNode(user.Id));
   
           // Act
           var result = await _hierarchyRepository.SaveChangesAsync();
   
           // Assert
           result.Should().BeTrue();
       }
   
       [Fact]
       public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
       {
           // Act
           var result = await _hierarchyRepository.SaveChangesAsync();
   
           // Assert
           result.Should().BeFalse();
       }
   
       #endregion
   
       public void Dispose() => _dbContext.Dispose();
   }