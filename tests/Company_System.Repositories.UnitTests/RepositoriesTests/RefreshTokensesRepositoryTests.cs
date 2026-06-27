using AutoFixture;
using EntityFrameworkCore.Testing.Moq;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Core.Interfaces.ServiceContracts.IRedisService;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace TestProject1.RepositoriesTests;

public class RefreshTokensesRepositoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly IRefreshTokensRepository _refreshTokensRepository;
    private readonly IFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IRedisService> _redisMock;

    public RefreshTokensesRepositoryTests(ITestOutputHelper output)
    {
        _output = output;
        
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = Create.MockedDbContextFor<ApplicationDbContext>(dbOptions);

        _redisMock = new Mock<IRedisService>();
        
        _refreshTokensRepository = new RefreshTokensesRepository(_dbContext, _redisMock.Object);
    }


    #region FindRefreshTokenByRefreshTokenStringTests

    [Fact]
    public async Task FindRefreshTokenByRefreshTokenString_validData_ShouldSucceed()
    {
        // Arrange
        var initalData = _fixture.CreateMany<RefreshToken>(10).ToArray();
        _dbContext.RefreshTokens.AddRange(initalData);
        _dbContext.SaveChanges();

        _redisMock.Setup(t => t.Get<RefreshToken>(It.IsAny<string>()))
            .ReturnsAsync(null as RefreshToken);

        _redisMock.Setup(t => t.Set(It.IsAny<string>(), It.IsAny<object>()));

        var expected = initalData[0];
        
        _output.WriteLine($"Expected: {expected}");
        
        // Act
        var actual = await _refreshTokensRepository.FindRefreshTokenByRefreshTokenStringAsync(expected.Token);
        _output.WriteLine($"Actual: {actual}");
        
        // Assert
        actual.Should().NotBeNull();
        actual.Should().Be(expected);
    }
    
    [Fact]
    public async Task FindRefreshTokenByRefreshTokenString_MissingToken_ShouldFail()
    {
        // Arrange
        _redisMock.Setup(t => t.Get<RefreshToken>(It.IsAny<string>()))
            .ReturnsAsync(null as RefreshToken);
        
        _redisMock.Setup(t => t.Set(It.IsAny<string>(), It.IsAny<object>()));
        
        
        // Act
        var actual = await _refreshTokensRepository.FindRefreshTokenByRefreshTokenStringAsync(_fixture.Create<string>());
        _output.WriteLine($"Actual: {actual}");
        
        // Assert
        actual.Should().BeNull();
    }
    #endregion
}