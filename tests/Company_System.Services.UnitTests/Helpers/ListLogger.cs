using Microsoft.Extensions.Logging;

namespace HR_System.Core.UnitTests.Helpers;

public sealed record LogEntry(LogLevel Level, string Message);

/// <summary>
/// Minimal in-memory logger that records every entry so tests can assert on what a decorator logged.
/// </summary>
public sealed class ListLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
}
