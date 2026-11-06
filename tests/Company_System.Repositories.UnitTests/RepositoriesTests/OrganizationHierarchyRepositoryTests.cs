using AutoFixture;
using FluentAssertions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.LazyLoading;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class OrganizationHierarchyRepositoryTests : RepositoryTestBase, IDisposable
{
    private readonly IOrganizationHierarchyRepository _hierarchyRepository;
    private readonly ApplicationDbContext _dbContext;

    public OrganizationHierarchyRepositoryTests(ITestOutputHelper output) : base(output)
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ApplicationDbContext(dbOptions);
        _hierarchyRepository = new OrganizationHierarchyRepository(_dbContext);
    }

    private ApplicationUser CreateUser(string? userName = null)
    {
        return Fixture.Build<ApplicationUser>()
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
        return Fixture.Build<OrganizationHierarchy>()
            .With(o => o.UserId, userId)
            .With(o => o.IsActive, true)
            .With(
            o => o.ParentId,
            parentId
        )
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
        LogArrange("Scenario", nameof(Add_ShouldTrackEntityAsAdded));

        var user = CreateUser();
        var node = CreateNode(user.Id);

        // Act
        _hierarchyRepository.Add(node);
        LogAct("Operation completed");

        // Assert
        _dbContext.Entry(node).State.Should().Be(EntityState.Added);
        _dbContext.OrganizationHierarchies.Local.Should().Contain(node);
    }

    [Fact]
    public void Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled()
    {
        // Arrange
        LogArrange("Scenario", nameof(Add_ShouldNotPersistToDatabase_BeforeSaveChangesIsCalled));

        var user = CreateUser();
        var node = CreateNode(user.Id);

        // Act
        _hierarchyRepository.Add(node);
        LogAct("Operation completed");

        // Assert
        _dbContext.OrganizationHierarchies.AsNoTracking().Any(o => o.Id == node.Id).Should().BeFalse();
    }

    #endregion
    #region GetChildrenAsync
    [Fact]
    public async Task GetChildrenAsync_ShouldReturnRootNodes_WhenParentsIsNullOrEmpty()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetChildrenAsync_ShouldReturnRootNodes_WhenParentsIsNullOrEmpty));

        var rootUser = CreateUser();
        var childUser = CreateUser();
        _dbContext.Users.AddRange(rootUser, childUser);
        var root = CreateNode(rootUser.Id);
        var child = CreateNode(childUser.Id, parentId: root.Id);
        _dbContext.OrganizationHierarchies.AddRange(root, child);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _hierarchyRepository.FilterAsync(
            node => node.IsActive && node.ParentId == null,
            include: [node => node.User!, node => node.Children]
        );
        LogAct(result);

        // Assert
        result.Should().ContainSingle(o => o.Id == root.Id);
        result.Should().NotContain(o => o.Id == child.Id);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnNodesWithMatchingParentIds_WhenParentsProvided()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetChildrenAsync_ShouldReturnNodesWithMatchingParentIds_WhenParentsProvided));

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
        var result = await _hierarchyRepository.FilterAsync(
            node => node.IsActive && node.ParentId != null && (new Guid[] { parent.Id }).Contains(node.ParentId.Value),
            include: [node => node.User!, node => node.Children]
        );
        LogAct(result);

        // Assert
        result.Should().ContainSingle(o => o.Id == child.Id);
        result.Should().NotContain(o => o.Id == parent.Id);
        result.Should().NotContain(o => o.Id == unrelated.Id);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnEmptyList_WhenNoNodesMatchGivenParents()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetChildrenAsync_ShouldReturnEmptyList_WhenNoNodesMatchGivenParents));

        var user = CreateUser();
        _dbContext.Users.Add(user);
        _dbContext.OrganizationHierarchies.Add(CreateNode(user.Id));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _hierarchyRepository.FilterAsync(
            node => node.IsActive && node.ParentId != null && (new Guid[] { Guid.NewGuid() }).Contains(node.ParentId.Value),
            include: [node => node.User!, node => node.Children]
        );
        LogAct(result);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_ShouldLoadOnlyTheRequestedNavigationProperties()
    {
        // Arrange
        LogArrange("Scenario", nameof(FilterAsync_ShouldLoadOnlyTheRequestedNavigationProperties));

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
        var result = await _hierarchyRepository.FilterAsync(
            node => node.IsActive && node.ParentId == null,
            include: [node => node.User!, node => node.Children]
        );
        var fetchedRoot = result.Single(o => o.Id == root.Id);
        LogAct(result);

        // Assert
        fetchedRoot.User.Should().NotBeNull();
        fetchedRoot.User!.UserName.Should().Be("root_user");
        var fetchedChild = fetchedRoot.Children.Single();
        fetchedChild.Id.Should().Be(child.Id);
        fetchedChild.User.Should().BeNull();
    }

    #endregion
    #region DeactivateBranchAsync
    [Fact]
    public async Task DeactivateBranchAsync_ShouldRetainAndHideDescendantsButKeepRootProtected()
    {
        // Arrange
        LogArrange("Scenario", nameof(DeactivateBranchAsync_ShouldRetainAndHideDescendantsButKeepRootProtected));

        var rootUser = CreateUser();
        var managerUser = CreateUser();
        var employeeUser = CreateUser();
        _dbContext.Users.AddRange(rootUser, managerUser, employeeUser);
        var root = CreateNode(rootUser.Id);
        var manager = CreateNode(managerUser.Id, root.Id);
        var employee = CreateNode(employeeUser.Id, manager.Id);
        _dbContext.OrganizationHierarchies.AddRange(root, manager, employee);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        await _hierarchyRepository.DeactivateBranchAsync(root.Id);
        LogAct("Operation completed");

        // Assert
        root.IsActive.Should().BeTrue();
        _dbContext.ChangeTracker.Clear();
        await _hierarchyRepository.DeactivateBranchAsync(manager.Id);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        (await _hierarchyRepository.FilterAsync(
            node => node.IsActive && node.ParentId != null && new[] { root.Id }.Contains(node.ParentId.Value),
            include: [node => node.User!, node => node.Children]
        )).Should().BeEmpty();
        (await _hierarchyRepository.GetByUserIdAsync(employee.UserId)).Should().BeNull();
        var archived = await _dbContext.OrganizationHierarchies.IgnoreQueryFilters().ToListAsync();
        archived.Should().HaveCount(3);
        archived.Single(n => n.Id == employee.Id).ParentId.Should().Be(manager.Id);
        archived.Single(n => n.Id == employee.Id).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeactivateEntity_WhenNodeExists()
    {
        // Arrange
        LogArrange("Scenario", nameof(RemoveAsync_ShouldDeactivateEntity_WhenNodeExists));

        var user = CreateUser();
        _dbContext.Users.Add(user);
        var node = CreateNode(user.Id, Guid.NewGuid());
        _dbContext.OrganizationHierarchies.Add(node);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _hierarchyRepository.DeactivateBranchAsync(node.Id);
        LogAct(result);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(node.Id);
        _dbContext.Entry(result).State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public async Task RemoveAsync_ShouldPersistRemoval_AfterSaveChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(RemoveAsync_ShouldPersistRemoval_AfterSaveChanges));

        var user = CreateUser();
        _dbContext.Users.Add(user);
        var node = CreateNode(user.Id, Guid.NewGuid());
        _dbContext.OrganizationHierarchies.Add(node);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        _dbContext.ChangeTracker.Clear();

        // Act
        await _hierarchyRepository.DeactivateBranchAsync(node.Id);
        await _dbContext.SaveChangesAsync();
        LogAct("Operation completed");

        // Assert
        (await _dbContext.OrganizationHierarchies.AnyAsync(o => o.Id == node.Id && o.IsActive)).Should().BeFalse();
        (await _dbContext.OrganizationHierarchies.IgnoreQueryFilters().SingleAsync(o => o.Id == node.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_ShouldReturnNull_WhenNodeDoesNotExist()
    {
        // Arrange
        LogArrange("Scenario", nameof(RemoveAsync_ShouldReturnNull_WhenNodeDoesNotExist));

        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _hierarchyRepository.DeactivateBranchAsync(Guid.NewGuid());
        LogAct(result);

        // Assert
        result.Should().BeNull();
    }

    #endregion
    #region GetByUserIdAsync
    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnMatchingNode_WhenItExists()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetByUserIdAsync_ShouldReturnMatchingNode_WhenItExists));

        var user = CreateUser();
        _dbContext.Users.Add(user);
        var node = CreateNode(user.Id);
        _dbContext.OrganizationHierarchies.Add(node);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _hierarchyRepository.GetByUserIdAsync(user.Id);
        LogAct(result);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(node.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenNoNodeMatchesUserId()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetByUserIdAsync_ShouldReturnNull_WhenNoNodeMatchesUserId));

        // Act
        var result = await _hierarchyRepository.GetByUserIdAsync(Guid.NewGuid());
        LogAct(result);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldIncludeParentNode()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetByUserIdAsync_ShouldIncludeParentNode));

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
        LogAct(result);

        // Assert
        result.Should().NotBeNull();
        result!.Parent.Should().NotBeNull();
        result.Parent!.Id.Should().Be(parent.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUntrackedEntity()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetByUserIdAsync_ShouldReturnUntrackedEntity));

        var user = CreateUser();
        _dbContext.Users.Add(user);
        var node = CreateNode(user.Id);
        _dbContext.OrganizationHierarchies.Add(node);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _hierarchyRepository.GetByUserIdAsync(user.Id);
        LogAct(result);

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
        // Arrange
        LogArrange("Scenario", nameof(GetParentUserIds_RequiresIntegrationTestAgainstRealSqlServer));

        // Act
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
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(lazyData), lazyData);

        // Act
        var result = await _hierarchyRepository.GetUserNames(lazyData);
        var entry = result.Single(r => r.TreeId == node.Id);
        LogAct(result);

        // Assert
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
        var lazyData = new LazyDTO
        {
            Taken = 1,
            SectionSize = 2
        };
        LogArrange(nameof(lazyData), lazyData);

        // Act
        var result = await _hierarchyRepository.GetUserNames(lazyData);
        LogAct(result);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserNames_ShouldReturnEmptyList_WhenNoNodesExist()
    {
        // Arrange
        var lazyData = new LazyDTO
        {
            Taken = 0,
            SectionSize = 10
        };
        LogArrange(nameof(lazyData), lazyData);

        // Act
        var result = await _hierarchyRepository.GetUserNames(lazyData);
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

        var user = CreateUser();
        _dbContext.Users.Add(user);
        _dbContext.OrganizationHierarchies.Add(CreateNode(user.Id));

        // Act
        var result = await _hierarchyRepository.SaveChangesAsync();
        LogAct(result);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges()
    {
        // Arrange
        LogArrange("Scenario", nameof(SaveChangesAsync_ShouldReturnFalse_WhenThereAreNoPendingChanges));

        // Act
        var result = await _hierarchyRepository.SaveChangesAsync();
        LogAct(result);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
    public void Dispose()
    {
        _dbContext.Dispose();
    }
    #region ManagerChanges

    [Fact]
    public async Task SetParent_ShouldOnlyChangeManager_AfterSaving()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var node = CreateNode(userId, Guid.NewGuid());
        var previousParent = node.ParentId;
        var managerId = Guid.NewGuid();
        _dbContext.OrganizationHierarchies.Add(node);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        LogArrange("Employee hierarchy", node);

        // Act
        _hierarchyRepository.SetParent(node.Id, managerId);
        var beforeSave = await _hierarchyRepository.GetByIdAsync(node.Id);
        await _hierarchyRepository.SaveChangesAsync();
        var afterSave = await _hierarchyRepository.GetByIdAsync(node.Id);
        LogAct(afterSave);

        // Assert
        beforeSave!.ParentId.Should().Be(previousParent);
        afterSave!.ParentId.Should().Be(managerId);
        afterSave.UserId.Should().Be(userId);
        afterSave.IsActive.Should().BeTrue();
    }

    #endregion
}
