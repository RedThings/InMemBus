using InMemBus.MemoryBus;
using Microsoft.Extensions.DependencyInjection;

namespace InMemBus.Tests.TestInfrastructure;

public class InMemBusTester
{
    private InMemBusTester()
    {
    }

    static InMemBusTester()
    {
    }

    public static InMemBusTester Instance { get; } = new();

    public (IInMemBus inMemBus, TestDataAsserter testDataAsserter) Setup(
        Action<InMemBusConfiguration> configurationAction,
        CancellationToken cancellationToken)
    {
        var serviceCollection = new ServiceCollection();
        
        serviceCollection.AddLogging();
        
        serviceCollection.UseInMemBus(configurationAction);

        var testDataAsserter = new TestDataAsserter();
        serviceCollection.AddSingleton(testDataAsserter);

        var rootServiceProvider = serviceCollection.BuildServiceProvider();

        var observer = new InMemBusObserver(rootServiceProvider);

        var inMemBus = rootServiceProvider.GetRequiredService<IInMemBus>();

        _ = observer.ExecuteAsync(cancellationToken);

        return (inMemBus, testDataAsserter);
    }
}