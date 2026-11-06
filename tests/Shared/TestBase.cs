using System.Text.Json;
using System.Text.Json.Serialization;
using AutoFixture;
using Xunit.Abstractions;

namespace HR_System.Tests.Shared;

public abstract class TestBase
{
    protected IFixture Fixture { get; } = new Fixture();

    protected ITestOutputHelper Output { get; }

    protected TestBase(ITestOutputHelper output)
    {
        Output = output;

        foreach (var behavior in Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList())
            Fixture.Behaviors.Remove(behavior);

        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    protected void LogArrange(string name, object? value) => Write("Arrange", name, value);

    protected void LogAct(object? result) => Write("Act", "Result", result);

    private void Write(string stage, string name, object? value)
    {
        if (value is Microsoft.EntityFrameworkCore.DbContext)
        {
            Output.WriteLine($"{stage} — {name}: {value.GetType().Name}");
            return;
        }

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        string details;
        try
        {
            details = JsonSerializer.Serialize(value, options);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            details = value?.GetType().Name ?? "null";
        }

        Output.WriteLine($"{stage} — {name}: {details}");
    }
}

public abstract class ServiceTestBase(ITestOutputHelper output) : TestBase(output);

public abstract class RepositoryTestBase(ITestOutputHelper output) : TestBase(output);
