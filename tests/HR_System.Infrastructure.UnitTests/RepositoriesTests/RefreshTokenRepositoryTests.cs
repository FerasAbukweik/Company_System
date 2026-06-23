using AutoFixture;
using EntityFrameworkCore.Testing.Moq;
using FluentAssertions;
using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.RepositoryContracts;
using HR_System.Core.Helpers;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class RefreshTokenRepositoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IFixture _fixture;
    private readonly ApplicationDbContext _dbContext;

    public RefreshTokenRepositoryTests(ITestOutputHelper output)
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
        
        _refreshTokenRepository = new RefreshTokenRepository(_dbContext);
    }
    
    [Fact]
    public async Task AddTest_validInput_ShouldSucceed()
    {
        // Arrang
        var toAdd = _fixture.Create<RefreshToken>();
        _output.WriteLine($"Expected Valud: {toAdd.ToString()}");
        
        // Act
        Result<RefreshToken> actualResult = await _refreshTokenRepository.AddAsync(toAdd);
        _output.WriteLine($"Expected Valud: {actualResult?.Value?.ToString() ?? "null"}");

        // Assert
        actualResult.Should().NotBeNull();
        actualResult.Value.Should().NotBeNull();
        actualResult.IsSuccess.Should().BeTrue();
        actualResult.Value.Should().Be(toAdd);
    }

    [Fact]
    public async Task RemoveExpiredRefreshTokensTest_validInput_ShouldSucceed()
    {
        // Arrange
        var initalData = _fixture.CreateMany<RefreshToken>(10).ToArray();
        initalData[0].Expires = DateTime.UtcNow.AddDays(-1);
        
        // add inital data to DB
        _dbContext.RefreshTokens.AddRange(initalData);
        _dbContext.SaveChanges();

        var expected = initalData[0];
        
        _output.WriteLine($"Expected:\n{expected.ToString()}");
        
        // Act
        var actual = await _refreshTokenRepository.RemoveExpiredRefreshTokensAsync();
        _output.WriteLine($"Actual:\n{actual.ToString()}");

        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Length.Should().BeGreaterThanOrEqualTo(1);
        actual.Value.Should().Contain(expected);
    }
}