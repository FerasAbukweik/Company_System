using HR_System.Core.Domain.Entities;
using HR_System.Core.Domain.Identity;
using HR_System.Core.Enums;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TestProject1.RepositoriesTests;

public class RefreshTokensRepositoryTests : RepositoryTestBase, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;
    private readonly RefreshTokensRepository _refreshTokensRepository;
    private readonly Guid _userId;

    public RefreshTokensRepositoryTests(ITestOutputHelper output) : base(output)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.EnsureCreated();
        var user = new ApplicationUser
        {
            FullName = "Employee",
            Position = PositionsEnum.Employee
        };
        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        _userId = user.Id;
        _dbContext.ChangeTracker.Clear();
        _refreshTokensRepository = new RefreshTokensRepository(_dbContext);
    }

    // ---------- helpers ----------
    private RefreshToken CreateToken(DateTime? expires = null) => new()
    {
        UserId = _userId,
        Token = Guid.NewGuid().ToString(),
        Expires = expires ?? DateTime.UtcNow.AddDays(1)
    };
    #region AddAsync
    [Fact]
    public async Task AddAsync_ShouldDeferPersistenceUntilSaveChanges()
    {
        var token = CreateToken();
        LogArrange(nameof(token.Id), token.Id);

        // Act
        _refreshTokensRepository.AddAsync(token);
        var beforeSave = await _dbContext.RefreshTokens.AsNoTracking().CountAsync();
        await _refreshTokensRepository.SaveChangesAsync();
        LogAct(beforeSave);

        // Assert
        beforeSave.Should().Be(0);
        (await _dbContext.RefreshTokens.AsNoTracking().CountAsync()).Should().Be(1);
    }

    #endregion
    #region GetByTokenAsync
    [Fact]
    public async Task GetByTokenAsync_ShouldReadWithoutTracking()
    {
        var token = CreateToken();
        _dbContext.Add(token);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        LogArrange(nameof(token.Id), token.Id);

        // Act
        var result = await _refreshTokensRepository.GetByTokenAsync(token.Token);
        LogAct(result);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(token.Id);
        _dbContext.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldReturnNull_WhenTokenDoesNotExist()
    {
        // Arrange
        LogArrange("Scenario", nameof(GetByTokenAsync_ShouldReturnNull_WhenTokenDoesNotExist));

        var token = Guid.NewGuid().ToString();

        // Act
        var result = await _refreshTokensRepository.GetByTokenAsync(token);
        LogAct(result);

        // Assert
        result.Should().BeNull();
    }

    #endregion
    #region RemoveAsync
    [Fact]
    public async Task RemoveAsync_ShouldDeleteImmediatelyAndRejectSecondConsumption()
    {
        var token = CreateToken();
        _dbContext.Add(token);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        LogArrange(nameof(token.Id), token.Id);

        // Act
        var first = await _refreshTokensRepository.RemoveAsync(token);
        var second = await _refreshTokensRepository.RemoveAsync(token);
        LogAct(new { first, second });

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
        (await _dbContext.RefreshTokens.CountAsync()).Should().Be(0);
    }

    #endregion
    #region RemoveExpiredRefreshTokensAsync
    [Fact]
    public async Task RemoveExpiredRefreshTokensAsync_ShouldDeleteOnlyExpiredTokens()
    {
        // Arrange
        LogArrange("Scenario", nameof(RemoveExpiredRefreshTokensAsync_ShouldDeleteOnlyExpiredTokens));

        var expired = CreateToken(DateTime.UtcNow.AddDays(-1));
        var valid = CreateToken();
        _dbContext.AddRange(expired, valid);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _refreshTokensRepository.RemoveExpiredRefreshTokensAsync();
        LogAct(result);

        // Assert
        result.Should().ContainSingle().Which.Id.Should().Be(expired.Id);
        (await _dbContext.RefreshTokens.SingleAsync()).Id.Should().Be(valid.Id);
    }

    [Fact]
    public async Task RemoveExpiredRefreshTokensAsync_ShouldReturnEmpty_WhenNothingExpired()
    {
        // Arrange
        LogArrange("Scenario", nameof(RemoveExpiredRefreshTokensAsync_ShouldReturnEmpty_WhenNothingExpired));

        // Act
        var result = await _refreshTokensRepository.RemoveExpiredRefreshTokensAsync();
        LogAct(result);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
