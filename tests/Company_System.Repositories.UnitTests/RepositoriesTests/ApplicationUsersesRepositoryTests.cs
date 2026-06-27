using AutoFixture;
using EntityFrameworkCore.Testing.Moq;
using FluentAssertions;
using HR_System.Core.Domain.Idnetity;
using HR_System.Core.Interfaces.RepositoryContracts;
using HR_System.Infrastructure;
using HR_System.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestProject1.RepositoriesTests;

public class ApplicationUsersesRepositoryTests
{
    private readonly IApplicationUsersRepository _applicationUsersRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ITestOutputHelper _output;
    private readonly IFixture _fixture;

    public ApplicationUsersesRepositoryTests(ITestOutputHelper output)
    {
        _output = output;
        
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(e => _fixture.Behaviors.Remove(e));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        // generate DB context options
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            // add use in memory to fix async-await issues 
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = Create.MockedDbContextFor<ApplicationDbContext>(dbContextOptions);
        
        _applicationUsersRepository = new ApplicationUsersesRepository(_dbContext);
    }
    
    [Fact]
    public async Task FilterAsyncTest_ValidInput_ShouldBeSuccess()
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
        var actual = await _applicationUsersRepository.FilterAsync(u => u.FullName == "Test Name");
        _output.WriteLine($"Actual Value:\n{actual.ToString()}");
        
        // Assert
        actual.Should().NotBeNull();
        actual.Should().NotBeNull();
        actual.Count.Should().Be(1);
        actual[0].Id.Should().Be(expected.Id);
    }
}