using System.Net;
using AutoFixture;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure.Services;
using Moq;
using Xunit.Abstractions;

namespace HR_System.Core.UnitTests.ServicesTests;

public class OrganizationHierarchyServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;
    private readonly Mock<IOrganizationHierarchyRepository> _repositoryMock;
    private readonly OrganizationHierarchyService _service;

    public OrganizationHierarchyServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _repositoryMock = new Mock<IOrganizationHierarchyRepository>();
        _service = new OrganizationHierarchyService(_repositoryMock.Object);
    }

    #region AddAsync

    [Fact]
    public async Task AddAsync_ValidData_ShouldSucceed()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var toAdd = _fixture.Create<OrganizationHierarchyAddDTO>();

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<OrganizationHierarchy>()));
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _output.WriteLine($"currUserId: {currUserId}");
        _output.WriteLine($"toAdd     : {toAdd.ToString()}");

        // Act
        var actual = await _service.AddAsync(toAdd, currUserId);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value?.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value!.Position.Should().Be(toAdd.Position);
        actual.Value!.UserId.Should().Be(currUserId);

        _repositoryMock.Verify(r =>
            r.Add(It.IsAny<OrganizationHierarchy>()), Times.Once);
        _repositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_SaveChangesFails_ShouldReturnFailure()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var toAdd = _fixture.Create<OrganizationHierarchyAddDTO>();

        _repositoryMock
            .Setup(r => r.Add(It.IsAny<OrganizationHierarchy>()));
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _output.WriteLine($"currUserId: {currUserId}");
        _output.WriteLine($"toAdd     : {toAdd.ToString()}");
        _output.WriteLine("SaveChanges returns false — expecting failure");

        // Act
        var actual = await _service.AddAsync(toAdd, currUserId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorMessage.Should().Be("Failed to add organization hierarchy");

        _repositoryMock.Verify(r =>
            r.Add(It.IsAny<OrganizationHierarchy>()), Times.Once);
        _repositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetChildrenAsync

    [Fact]
    public async Task GetChildrenAsync_WithChildren_ShouldReturnMappedDTOs()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var parentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var children = CreateMany(3);

        _repositoryMock
            .Setup(r => r.GetChildrenAsync(parentIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(children);

        _output.WriteLine($"currUserId    : {currUserId}");
        _output.WriteLine($"Expected Count: {children.Count}");
        children.ForEach(c => _output.WriteLine($"  {c.ToString()}"));

        // Act
        var actual = await _service.GetChildrenAsync(currUserId, parentIds);
        _output.WriteLine($"IsSuccess   : {actual.IsSuccess}");
        _output.WriteLine($"Actual Count: {actual.Value?.Count ?? -1}");
        actual.Value?.ToList().ForEach(c => _output.WriteLine($"  {c.ToString()}"));

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Should().HaveCount(children.Count);
        actual.Value.Should().BeAssignableTo<IReadOnlyList<OrganizationHierarchyDTO>>();

        _repositoryMock.Verify(r =>
            r.GetChildrenAsync(parentIds, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChildrenAsync_NoChildren_ShouldReturnEmpty()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var parentIds = new List<Guid> { Guid.NewGuid() };

        _repositoryMock
            .Setup(r => r.GetChildrenAsync(parentIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _output.WriteLine($"currUserId    : {currUserId}");
        _output.WriteLine("Expected Count: 0");

        // Act
        var actual = await _service.GetChildrenAsync(currUserId, parentIds);
        _output.WriteLine($"IsSuccess   : {actual.IsSuccess}");
        _output.WriteLine($"Actual Count: {actual.Value?.Count ?? -1}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().BeEmpty();

        _repositoryMock.Verify(r =>
            r.GetChildrenAsync(parentIds, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChildrenAsync_NullParents_ShouldReturnRoots()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var roots = CreateMany(2);

        _repositoryMock
            .Setup(r => r.GetChildrenAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roots);

        _output.WriteLine($"currUserId    : {currUserId}");
        _output.WriteLine($"Expected Count: {roots.Count}");
        roots.ForEach(r => _output.WriteLine($"  {r.ToString()}"));

        // Act
        var actual = await _service.GetChildrenAsync(currUserId, null);
        _output.WriteLine($"IsSuccess   : {actual.IsSuccess}");
        _output.WriteLine($"Actual Count: {actual.Value?.Count ?? -1}");
        actual.Value?.ToList().ForEach(r => _output.WriteLine($"  {r.ToString()}"));

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().HaveCount(roots.Count);

        _repositoryMock.Verify(r =>
            r.GetChildrenAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RemoveAsync

    [Fact]
    public async Task RemoveAsync_ValidNonRootNode_ShouldSucceed()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var node = CreateNode(parentId: Guid.NewGuid()); // non-root

        _repositoryMock
            .Setup(r => r.RemoveAsync(node.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _output.WriteLine($"currUserId: {currUserId}");
        _output.WriteLine($"node      : {node.ToString()}");

        // Act
        var actual = await _service.RemoveAsync(node.Id, currUserId);
        _output.WriteLine($"IsSuccess : {actual.IsSuccess}");
        _output.WriteLine($"Value     : {actual.Value?.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value!.Id.Should().Be(node.Id);

        _repositoryMock.Verify(r =>
            r.RemoveAsync(node.Id, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_NodeNotFound_ShouldReturnFailure()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.RemoveAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as OrganizationHierarchy);

        _output.WriteLine($"currUserId    : {currUserId}");
        _output.WriteLine($"nonExistentId : {nonExistentId}");
        _output.WriteLine("RemoveAsync returns null — expecting failure");

        // Act
        var actual = await _service.RemoveAsync(nonExistentId, currUserId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"StatusCode   : {actual.StatusCode}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        actual.ErrorMessage.Should().Be("hierarchy not found");

        _repositoryMock.Verify(r =>
            r.RemoveAsync(nonExistentId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_RootNode_ShouldReturnFailure()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var rootNode = CreateNode(parentId: null); // root

        _repositoryMock
            .Setup(r => r.RemoveAsync(rootNode.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rootNode);

        _output.WriteLine($"currUserId: {currUserId}");
        _output.WriteLine($"rootNode  : {rootNode.ToString()} — root node, expecting failure");

        // Act
        var actual = await _service.RemoveAsync(rootNode.Id, currUserId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"StatusCode   : {actual.StatusCode}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        actual.ErrorMessage.Should().Be("cannt remove root employee");

        _repositoryMock.Verify(r =>
            r.RemoveAsync(rootNode.Id, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_SaveChangesFails_ShouldReturnFailure()
    {
        // Arrange
        var currUserId = Guid.NewGuid();
        var node = CreateNode(parentId: Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.RemoveAsync(node.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _output.WriteLine($"currUserId: {currUserId}");
        _output.WriteLine($"node      : {node.ToString()}");
        _output.WriteLine("SaveChanges returns false — expecting failure");

        // Act
        var actual = await _service.RemoveAsync(node.Id, currUserId);
        _output.WriteLine($"IsSuccess    : {actual.IsSuccess}");
        _output.WriteLine($"ErrorMessage : {actual.ErrorMessage}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeFalse();
        actual.ErrorMessage.Should().Be("failed saving changes to DB");

        _repositoryMock.Verify(r =>
            r.RemoveAsync(node.Id, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r =>
            r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helpers

    private OrganizationHierarchy CreateNode(Guid? parentId = null) =>
        _fixture.Build<OrganizationHierarchy>()
            .With(o => o.ParentId, parentId)
            .With(o => o.User, null as HR_System.Core.Domain.Identity.ApplicationUser)
            .With(o => o.Parent, null as OrganizationHierarchy)
            .With(o => o.Children, [])
            .Create();

    private List<OrganizationHierarchy> CreateMany(int count, Guid? parentId = null) =>
        Enumerable.Range(0, count)
            .Select(_ => CreateNode(parentId))
            .ToList();

    #endregion
}