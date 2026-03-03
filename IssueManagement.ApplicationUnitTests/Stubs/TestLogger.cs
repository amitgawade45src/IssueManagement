using Microsoft.Extensions.Logging;

namespace IssueManagement.ApplicationUnitTests.Stubs;

public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {       
    }

    private class NullScope : IDisposable { public void Dispose() { } public static readonly NullScope Instance = new(); }
}