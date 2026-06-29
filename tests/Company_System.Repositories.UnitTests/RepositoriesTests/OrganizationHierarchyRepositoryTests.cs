using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class OrganizationHierarchyRepositoryTests : IDisposable
{
    private readonly IOrganizationHierarchyRepository _repository;
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
        _repository = new OrganizationHierarchyRepository(_dbContext);
    }

    #region Add

    [Fact]
    public async Task Add_ValidNode_ShouldPersistAfterSave()
    {
        // Arrange
        var node = CreateNode();
        _output.WriteLine($"Adding Node: {node.Id} | {node.Position}");

        // Act
        _repository.Add(node);
        await _repository.SaveChangesAsync();
        var actual = await _repository.GetChildrenAsync(null);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().ContainSingle(n => n.Id == node.Id);
    }

    [Fact]
    public async Task Add_MultipleNodes_ShouldPersistAll()
    {
        // Arrange
        var nodes = CreateMany(3, parentId: null);
        _output.WriteLine($"Expected Count: {nodes.Count}");
        nodes.ForEach(n => _output.WriteLine($"  Node: {n.Id} | {n.Position}"));

        // Act
        foreach (var node in nodes)
            _repository.Add(node);
        await _repository.SaveChangesAsync();
        var actual = await _repository.GetChildrenAsync(null);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().HaveCount(nodes.Count);
    }

    #endregion

    #region GetChildrenAsync

    [Fact]
    public async Task GetChildrenAsync_NullParents_ShouldReturnRootNodes()
    {
        // Arrange
        var roots = CreateMany(3, parentId: null);
        var nonRoots = CreateMany(3, parentId: Guid.NewGuid());
        await SeedAsync([.. roots, .. nonRoots]);

        _output.WriteLine($"Expected Root Count: {roots.Count}");
        roots.ForEach(n => _output.WriteLine($"  Root: {n.Id} | ParentId: {n.ParentId}"));

        // Act
        var actual = await _repository.GetChildrenAsync(null);
        _output.WriteLine($"Actual Count: {actual.Count}");
        actual.ToList().ForEach(n => _output.WriteLine($"  Actual: {n.Id} | ParentId: {n.ParentId}"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().HaveCount(roots.Count);
        actual.Should().OnlyContain(n => n.ParentId == null);
    }

    [Fact]
    public async Task GetChildrenAsync_WithParentIds_ShouldReturnOnlyChildren()
    {
        // Arrange
        var parentId1 = Guid.NewGuid();
        var parentId2 = Guid.NewGuid();
        var children1 = CreateMany(2, parentId: parentId1);
        var children2 = CreateMany(2, parentId: parentId2);
        var otherChildren = CreateMany(2, parentId: Guid.NewGuid());
        await SeedAsync([.. children1, .. children2, .. otherChildren]);

        var parentIds = new List<Guid> { parentId1, parentId2 };
        _output.WriteLine($"ParentIds     : {string.Join(", ", parentIds)}");
        _output.WriteLine($"Expected Count: {children1.Count + children2.Count}");

        // Act
        var actual = await _repository.GetChildrenAsync(parentIds);
        _output.WriteLine($"Actual Count: {actual.Count}");
        actual.ToList().ForEach(n => _output.WriteLine($"  Actual: {n.Id} | ParentId: {n.ParentId}"));

        // Assert
        actual.Should().NotBeNull();
        actual.Should().HaveCount(children1.Count + children2.Count);
        actual.Should().OnlyContain(n => parentIds.Contains(n.ParentId!.Value));
    }

    [Fact]
    public async Task GetChildrenAsync_EmptyParentIds_ShouldReturnRootNodes()
    {
        // Arrange
        var roots = CreateMany(2, parentId: null);
        var nonRoots = CreateMany(2, parentId: Guid.NewGuid());
        await SeedAsync([.. roots, .. nonRoots]);

        _output.WriteLine($"Expected Root Count: {roots.Count}");

        // Act
        var actual = await _repository.GetChildrenAsync([]);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().HaveCount(roots.Count);
        actual.Should().OnlyContain(n => n.ParentId == null);
    }

    [Fact]
    public async Task GetChildrenAsync_NoMatchingChildren_ShouldReturnEmpty()
    {
        // Arrange
        var nodes = CreateMany(3, parentId: Guid.NewGuid());
        await SeedAsync([.. nodes]);

        var nonExistentParentIds = new List<Guid> { Guid.NewGuid() };
        _output.WriteLine("No children for given parentIds");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _repository.GetChildrenAsync(nonExistentParentIds);
        _output.WriteLine($"Actual Count: {actual.Count}");

        // Assert
        actual.Should().NotBeNull();
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldIncludeChildrenUpTo5Layers()
    {
        // Arrange — build a 3-layer tree
        var root = CreateNode(parentId: null);
        var child = CreateNode(parentId: root.Id);
        var grandChild = CreateNode(parentId: child.Id);
        await SeedAsync(root, child, grandChild);

        _output.WriteLine($"Root      : {root.Id}");
        _output.WriteLine($"Child     : {child.Id} | ParentId: {child.ParentId}");
        _output.WriteLine($"GrandChild: {grandChild.Id} | ParentId: {grandChild.ParentId}");

        // Act
        var actual = await _repository.GetChildrenAsync(null);
        _output.WriteLine($"Actual Root Count   : {actual.Count}");
        _output.WriteLine($"Actual Children     : {actual.FirstOrDefault()?.Children.Count}");
        _output.WriteLine($"Actual GrandChildren: {actual.FirstOrDefault()?.Children.FirstOrDefault()?.Children.Count}");

        // Assert
        actual.Should().ContainSingle();
        actual[0].Id.Should().Be(root.Id);
        actual[0].Children.Should().ContainSingle(c => c.Id == child.Id);
        actual[0].Children[0].Children.Should().ContainSingle(c => c.Id == grandChild.Id);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnReadOnlyList()
    {
        // Arrange
        var root = CreateNode(parentId: null);
        await SeedAsync(root);

        // Act
        var actual = await _repository.GetChildrenAsync(null);
        _output.WriteLine($"Actual Type: {actual.GetType().Name}");

        // Assert
        actual.Should().BeAssignableTo<IReadOnlyList<OrganizationHierarchy>>();
    }

    #endregion

    #region RemoveAsync
    [Fact]
    public async Task RemoveAsync_NonExistentNode_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _output.WriteLine($"Non-existent Node Id: {nonExistentId}");

        // Act
        var actual = await _repository.RemoveAsync(nonExistentId);
        _output.WriteLine($"Actual: {actual?.ToString() ?? "null"}");

        // Assert
        actual.Should().BeNull();
    }

    #endregion

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithPendingChanges_ShouldReturnTrue()
    {
        // Arrange
        var node = CreateNode();
        _repository.Add(node);
        _output.WriteLine($"Added Node: {node.Id}");

        // Act
        var actual = await _repository.SaveChangesAsync();
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
        var actual = await _repository.SaveChangesAsync();
        _output.WriteLine($"Expected: false | Actual: {actual}");

        // Assert
        actual.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private OrganizationHierarchy CreateNode(Guid? parentId = null) =>
        _fixture.Build<OrganizationHierarchy>()
            .With(o => o.ParentId, parentId)
            .With(o => o.User, null as ApplicationUser)
            .With(o => o.Parent, null as OrganizationHierarchy)
            .With(o => o.Children, [])
            .Create();

    private List<OrganizationHierarchy> CreateMany(int count, Guid? parentId = null) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateNode(parentId))
            .ToList();

    private async Task SeedAsync(params OrganizationHierarchy[] nodes)
    {
        await _dbContext.OrganizationHierarchies.AddRangeAsync(nodes);
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();

    #endregion
}