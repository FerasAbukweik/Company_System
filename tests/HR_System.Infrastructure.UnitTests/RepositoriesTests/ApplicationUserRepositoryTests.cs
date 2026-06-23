using AutoFixture;
using EntityFrameworkCore.Testing.Moq;
using FluentAssertions;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Domain.RepositoryContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class ApplicationUserRepositoryTests
{
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public ApplicationUserRepositoryTests(ITestOutputHelper output)
    {
        _output = output;
        
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(e => _fixture.Behaviors.Remove(e));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        // generateing Dbcontext options
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            // add use in memory to fix async-await issues 
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = Create.MockedDbContextFor<ApplicationDbContext>(dbContextOptions);
        
        _applicationUserRepository = new ApplicationUserRepository(_dbContext);
    }
    
    [Fact]
    public async Task FilterAsyncTest_ValidInput_ShouldSucceed()
    {
        // Arrange
        var initalData = _fixture.CreateMany<ApplicationUser>(10).ToArray();
        initalData[0].FullName = "Test Name";
        
        // add inital data to DB
        _dbContext.Users.AddRange(initalData);
        _dbContext.SaveChanges();
        
        var expected = initalData[0];
        _output.WriteLine($"Expected Value:\n{expected.ToString()}");

        // Act
        var actual = await _applicationUserRepository.FilterAsync(u => u.FullName == "Test Name");
        _output.WriteLine($"Actual Value:\n{actual.ToString()}");
        
        // Assert
        actual.Should().NotBeNull();
        actual.IsSuccess.Should().BeTrue();
        actual.Value.Should().NotBeNull();
        actual.Value.Length.Should().Be(1);
        actual.Value[0].Id.Should().Be(expected.Id);
    }
}