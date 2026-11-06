using System.Linq.Expressions;
using System.Net;
using AutoFixture;
using CloudinaryDotNet.Actions;
using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Domain.Entities;
using HR_System.Core.DTO.OrganizationHierarchy;
using HR_System.Core.DTO.Users;
using HR_System.Core.Enums;
using HR_System.Core.Result;
using HR_System.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace HR_System.Core.UnitTests.ServicesTests;

public class UsersServiceTests : ServiceTestBase
{
    private readonly IUsersService _usersService;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IApplicationUsersRepository> _usersRepositoryMock;
    private readonly Mock<IOrganizationHierarchyService> _hierarchyServiceMock;
    private readonly Mock<IImageService> _imageServiceMock;

    public UsersServiceTests(ITestOutputHelper output) : base(output)
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null !, null !, null !, null !, null !, null !, null !, null !);
        _usersRepositoryMock = new Mock<IApplicationUsersRepository>();
        _hierarchyServiceMock = new Mock<IOrganizationHierarchyService>();
        _imageServiceMock = new Mock<IImageService>();
        _usersService = new UsersService(_userManagerMock.Object, _usersRepositoryMock.Object, _hierarchyServiceMock.Object, _imageServiceMock.Object, Mock.Of<IOrganizationHierarchyRepository>());
    }

    // ---------- helpers ----------
    private AddEmployeeDTO CreateData(PositionsEnum position = PositionsEnum.Employee) => Fixture.Build<AddEmployeeDTO>()
        .With(data => data.Image, Mock.Of<IFormFile>())
        .With(
                data => data.Position,
                position
            )
        .With(
            data => data.HourlyRate,
            25m
        )
        .With(
        data => data.AnnualHolidayAllowance,
        20
    )
        .Create();
    private void SetupCreation()
    {
        _imageServiceMock.Setup(service => service.Upload(It.IsAny<IFormFile>()))
            .ReturnsAsync(new ImageUploadResult { PublicId = "test-image", SecureUrl = new Uri("https://example.com/image.png") });
        _usersRepositoryMock.Setup(repository => repository.FilterAsync(
            It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
            It.IsAny<Expression<Func<ApplicationUser, object?>>[]?>(),
            It.IsAny<Expression<Func<ApplicationUser, object?>>?>(),
            It.IsAny<bool>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync([]);
        _userManagerMock.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _hierarchyServiceMock.Setup(service => service.AddAsync(It.IsAny<OrganizationHierarchyAddDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrganizationHierarchyDTO>.Success(Fixture.Create<OrganizationHierarchyDTO>()));
    }

    #region AddEmployeeAsync
    [Theory]
    [InlineData(PositionsEnum.CEO, RolesEnum.Admin)]
    [InlineData(PositionsEnum.Employee, RolesEnum.Employee)]
    public async Task AddEmployeeAsync_ShouldAssignTheRoleAndMapProfileAndHourlyRate(PositionsEnum position, RolesEnum role)
    {
        // Arrange
        var data = CreateData(position);
        SetupCreation();
        LogArrange(nameof(data.UserId), data.UserId);

        // Act
        var result = await _usersService.AddEmployeeAsync(data);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be(data.FullName);
        result.Value.HourlyRate.Should().Be(25m);
        result.Value.CreatedAt.Should().NotBeNull();
        _userManagerMock.Verify(
            manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), role.ToString()),
            Times.Once
        );
        _hierarchyServiceMock.Verify(
            service => service.AddAsync(
                It.Is<OrganizationHierarchyAddDTO>(node => node.ParentId == data.ParentId && node.UserId == data.UserId),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task AddEmployeeAsync_ShouldRejectDuplicatesAndCleanUpUploadedImage()
    {
        // Arrange
        var data = CreateData();
        SetupCreation();
        var existing = new ApplicationUser
        {
            FullName = data.FullName,
            Position = data.Position,
            UserName = data.UserName,
            Email = data.Email,
            PhoneNumber = data.PhoneNumber
        };
        _usersRepositoryMock.Setup(repository => repository.FilterAsync(
            It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
            It.IsAny<Expression<Func<ApplicationUser, object?>>[]?>(),
            It.IsAny<Expression<Func<ApplicationUser, object?>>?>(),
            It.IsAny<bool>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()
        ))
            .ReturnsAsync([existing]);
        LogArrange(nameof(data.UserId), data.UserId);

        // Act
        var result = await _usersService.AddEmployeeAsync(data);
        LogAct(result);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _userManagerMock.Verify(
            manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never
        );
        _imageServiceMock.Verify(service => service.Delete("test-image"), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddEmployeeAsync_ShouldPropagateIdentityFailuresAndCleanUpTheImage(bool creationFails)
    {
        // Arrange
        var data = CreateData();
        SetupCreation();
        var failure = IdentityResult.Failed(new IdentityError { Description = "Identity failure" });
        if (creationFails)
            _userManagerMock.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(failure);
        else
            _userManagerMock.Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(failure);
        LogArrange(nameof(creationFails), creationFails);

        // Act
        var result = await _usersService.AddEmployeeAsync(data);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Identity failure");
        _imageServiceMock.Verify(service => service.Delete("test-image"), Times.Once);
        _hierarchyServiceMock.Verify(
            service => service.AddAsync(It.IsAny<OrganizationHierarchyAddDTO>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AddEmployeeAsync_ShouldPropagateHierarchyFailureAndCleanUpTheImage()
    {
        // Arrange
        var data = CreateData();
        SetupCreation();
        _hierarchyServiceMock.Setup(service => service.AddAsync(It.IsAny<OrganizationHierarchyAddDTO>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<OrganizationHierarchyDTO>.Failure("Hierarchy failure"));
        LogArrange(nameof(data.UserId), data.UserId);

        // Act
        var result = await _usersService.AddEmployeeAsync(data);
        LogAct(result);

        // Assert
        result.ErrorMessage.Should().Be("Hierarchy failure");
        _imageServiceMock.Verify(service => service.Delete("test-image"), Times.Once);
    }
    #endregion
    #region ManagerChanges

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateEmployee_ShouldRejectCircularManagers_AndSaveValidManagers(bool circular)
    {
        // Arrange
        LogArrange("Circular manager", circular);
        var node = new OrganizationHierarchy { UserId = Guid.NewGuid(), ParentId = Guid.NewGuid() };
        var manager = new OrganizationHierarchy { UserId = Guid.NewGuid(), ParentId = circular ? node.Id : null };
        var user = new ApplicationUser { Id = node.UserId, FullName = "Employee", Position = PositionsEnum.Employee, OrganizationHierarchy = node };
        var hierarchy = new Mock<IOrganizationHierarchyRepository>();
        hierarchy.Setup(repository => repository.GetByIdAsync(manager.Id, It.IsAny<CancellationToken>())).ReturnsAsync(manager);
        hierarchy.Setup(repository => repository.GetByIdAsync(node.Id, It.IsAny<CancellationToken>())).ReturnsAsync(node);
        hierarchy.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _usersRepositoryMock.Setup(repository => repository.GetByIdAsync(user.Id,
            It.IsAny<Expression<Func<ApplicationUser, object?>>[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usersRepositoryMock.Setup(repository => repository.FilterAsync(
            It.IsAny<Expression<Func<ApplicationUser, bool>>>(), null, null, false, null, 1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _userManagerMock.Setup(manager => manager.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var service = new UsersService(_userManagerMock.Object, _usersRepositoryMock.Object,
            _hierarchyServiceMock.Object, _imageServiceMock.Object, hierarchy.Object);
        var data = new EmployeeUpdateDTO
        {
            FullName = "Employee", UserName = "employee", Email = "employee@example.com",
            PhoneNumber = "12345678", Position = PositionsEnum.Employee, ParentId = manager.Id
        };

        // Act
        var result = await service.UpdateEmployeeAsync(user.Id, data);
        LogAct(result);

        // Assert
        result.IsSuccess.Should().Be(!circular);
        hierarchy.Verify(repository => repository.SetParent(node.Id, manager.Id), circular ? Times.Never() : Times.Once());
        hierarchy.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), circular ? Times.Never() : Times.Once());
        if (!circular) result.Value!.ParentId.Should().Be(manager.Id);
    }

    #endregion
}
