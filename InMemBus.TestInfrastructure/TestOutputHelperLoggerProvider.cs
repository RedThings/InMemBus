using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace InMemBus.TestInfrastructure;

public class TestOutputHelperLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    private TestOutputHelperLogger? logger;

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public ILogger CreateLogger(string categoryName) => logger ??= new TestOutputHelperLogger(testOutputHelper);
}