using AutoFixture;
using CloudinaryDotNet.Actions;
using FluentAssertions;
using HR_System.Core.common;
using HR_System.Core.Domain.Identity;
using HR_System.Core.DTO.Account;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.Enums;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit.Abstractions;

namespace TestProject1.ServicesTests;

public class AccountOrgHierarchyServiceTests
{
    private readonly IAccountOrgHierarchyService _service;
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<IOrganizationHierarchyService> _hierarchyServiceMock;
    private readonly Mock<IApplicationUsersRepository> _usersRepositoryMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public AccountOrgHierarchyServiceTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _accountServiceMock = new Mock<IAccountService>();
        _hierarchyServiceMock = new Mock<IOrganizationHierarchyService>();
        _usersRepositoryMock = new Mock<IApplicationUsersRepository>();
        _imageServiceMock = new Mock<IImageService>();

        _service = new AccountOrgHierarchyService(
            _accountServiceMock.Object,
            _hierarchyServiceMock.Object,
            _usersRepositoryMock.Object,
            _imageServiceMock.Object);
    }

    private AddEmployeeDTO CreateAddEmployeeDto(Guid? parentId = null)
    {
        var formFileMock = new Mock<IFormFile>();

        return _fixture.Build<AddEmployeeDTO>()
            .With(d => d.ParentId, parentId ?? Guid.NewGuid())
            .With(d => d.Image, formFileMock.Object)
            .With(d => d.Position, PositionsEnum.Employee) // adjust if enum member name differs
            .Create();
    }

    private ApplicationUser CreateUser()
    {
        return _fixture.Build<ApplicationUser>()
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

    private static ImageUploadResult CreateSuccessfulUploadResult()
    {
        return new ImageUploadResult
        {
            SecureUrl = new Uri("https://res.cloudinary.com/demo/image/upload/sample.jpg"),
            PublicId = "sample-public-id"
        };
    }

    private static ImageUploadResult CreateFailedUploadResult(string message)
    {
        return new ImageUploadResult
        {
            Error = new Error { Message = message }
        };
    }

    #region AddEmployee - Image Upload Failure

    [Fact]
    public async Task AddEmployee_ShouldReturnFailure_WhenImageUploadFails()
    {
        // Arrange
        var dto = CreateAddEmployeeDto();
        var failedUpload = CreateFailedUploadResult("upload failed");

        _imageServiceMock
            .Setup(s => s.Upload(dto.Image))
            .ReturnsAsync(failedUpload);

        // Act
        var result = await _service.AddEmployee(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("upload failed");

        _accountServiceMock.Verify(
            s => s.CreateAccountAsync(It.IsAny<UserCreateDTO>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _hierarchyServiceMock.Verify(
            s => s.AddAsync(It.IsAny<OrganizationHierarchyAddDTO>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region AddEmployee - Account Creation Failure

    [Fact]
    public async Task AddEmployee_ShouldReturnFailure_WhenAccountCreationFails()
    {
        // Arrange
        var dto = CreateAddEmployeeDto();
        var successfulUpload = CreateSuccessfulUploadResult();

        _imageServiceMock
            .Setup(s => s.Upload(dto.Image))
            .ReturnsAsync(successfulUpload);

        _accountServiceMock
            .Setup(s => s.CreateAccountAsync(dto, successfulUpload.SecureUrl.AbsoluteUri, successfulUpload.PublicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApplicationUser>.Failure("account creation failed"));

        // Act
        var result = await _service.AddEmployee(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("account creation failed");

        _hierarchyServiceMock.Verify(
            s => s.AddAsync(It.IsAny<OrganizationHierarchyAddDTO>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region AddEmployee - Hierarchy Add Failure

    [Fact]
    public async Task AddEmployee_ShouldReturnFailure_WhenAddingToHierarchyFails()
    {
        // Arrange
        var dto = CreateAddEmployeeDto();
        var successfulUpload = CreateSuccessfulUploadResult();
        var createdUser = CreateUser();

        _imageServiceMock
            .Setup(s => s.Upload(dto.Image))
            .ReturnsAsync(successfulUpload);

        _accountServiceMock
            .Setup(s => s.CreateAccountAsync(dto, successfulUpload.SecureUrl.AbsoluteUri, successfulUpload.PublicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApplicationUser>.Success(createdUser));

        _hierarchyServiceMock
            .Setup(s => s.AddAsync(
                It.Is<OrganizationHierarchyAddDTO>(h => h.UserId == createdUser.Id && h.ParentId == dto.ParentId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrganizationHierarchyDTO>.Failure("hierarchy add failed"));

        // Act
        var result = await _service.AddEmployee(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("hierarchy add failed");
    }

    #endregion

    #region AddEmployee - Success

    [Fact]
    public async Task AddEmployee_ShouldReturnSuccessWithCreatedUser_WhenAllStepsSucceed()
    {
        // Arrange
        var dto = CreateAddEmployeeDto();
        var successfulUpload = CreateSuccessfulUploadResult();
        var createdUser = CreateUser();

        _imageServiceMock
            .Setup(s => s.Upload(dto.Image))
            .ReturnsAsync(successfulUpload);

        _accountServiceMock
            .Setup(s => s.CreateAccountAsync(dto, successfulUpload.SecureUrl.AbsoluteUri, successfulUpload.PublicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApplicationUser>.Success(createdUser));

        _hierarchyServiceMock
            .Setup(s => s.AddAsync(It.IsAny<OrganizationHierarchyAddDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrganizationHierarchyDTO>.Success(new OrganizationHierarchyDTO
            {
                Id = Guid.NewGuid(),
                UserId = createdUser.Id,
                Children = [],
                IsCurrUser = false,
                UserName = createdUser.UserName!,
                Position = createdUser.Position,
                UserImageUrl = createdUser.ImageUrl ?? "unknown"
            }));

        // Act
        var result = await _service.AddEmployee(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(createdUser);
    }

    [Fact]
    public async Task AddEmployee_ShouldPassCorrectParentIdAndUserIdToHierarchyService()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var dto = CreateAddEmployeeDto(parentId);
        var successfulUpload = CreateSuccessfulUploadResult();
        var createdUser = CreateUser();

        _imageServiceMock
            .Setup(s => s.Upload(dto.Image))
            .ReturnsAsync(successfulUpload);

        _accountServiceMock
            .Setup(s => s.CreateAccountAsync(dto, successfulUpload.SecureUrl.AbsoluteUri, successfulUpload.PublicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApplicationUser>.Success(createdUser));

        _hierarchyServiceMock
            .Setup(s => s.AddAsync(It.IsAny<OrganizationHierarchyAddDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrganizationHierarchyDTO>.Success(new OrganizationHierarchyDTO
            {
                Id = Guid.NewGuid(),
                UserId = createdUser.Id,
                Children = [],
                IsCurrUser = false,
                UserName = createdUser.UserName!,
                Position = createdUser.Position,
                UserImageUrl = createdUser.ImageUrl ?? "unknown"
            }));

        // Act
        await _service.AddEmployee(dto);

        // Assert
        _hierarchyServiceMock.Verify(
            s => s.AddAsync(
                It.Is<OrganizationHierarchyAddDTO>(h => h.UserId == createdUser.Id && h.ParentId == parentId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}