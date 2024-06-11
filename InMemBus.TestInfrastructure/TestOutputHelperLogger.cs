using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace InMemBus.TestInfrastructure;

public class TestOutputHelperLogger(ITestOutputHelper testOutputHelper) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        testOutputHelper.WriteLine($"--- {logLevel}: {formatter.Invoke(state, exception)}");
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => default;
}