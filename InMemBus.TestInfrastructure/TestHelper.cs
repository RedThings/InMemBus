using InMemBus.MemoryBus;
using InMemBus.TestInfrastructure.ComplexWorkflow;
using InMemBus.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace InMemBus.TestInfrastructure;

public class TestHelper
{
    private TestHelper()
    {
    }

    static TestHelper()
    {
    }

    public static TestHelper Instance { get; } = new();

    public (IInMemBus inMemBus, TestDataAsserter testDataAsserter) Setup(
        Action<InMemBusConfiguration> configurationAction,
        ITestOutputHelper testOutputHelper,
        CancellationToken cancellationToken)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(configure =>
        {
            configure.AddProvider(new TestOutputHelperLoggerProvider(testOutputHelper));
        });

        serviceCollection.UseInMemBus(configurationAction);

        var testDataAsserter = new TestDataAsserter();
        serviceCollection.AddSingleton(testDataAsserter);

        var rootServiceProvider = serviceCollection.BuildServiceProvider();

        var messageObserver = rootServiceProvider.GetRequiredService<InMemBusObserver>();
        var timeoutObserver = rootServiceProvider.GetRequiredService<WorkflowTimeoutsObserver>();

        var inMemBus = rootServiceProvider.GetRequiredService<IInMemBus>();

        _ = messageObserver.ExecuteAsync(cancellationToken);
        _ = timeoutObserver.ExecuteAsync(cancellationToken);

        return (inMemBus, testDataAsserter);
    }

    public (IInMemBus inMemBus, TestDataAsserter testDataAsserter) SetupComplexWorkflow(ITestOutputHelper testOutputHelper, CancellationToken token) =>
        Setup(ConfigureComplexWorkflow, testOutputHelper, token);

    public void ConfigureComplexWorkflow(InMemBusConfiguration config)
    {
        config
            .AddMessageHandler<GetPurchasedItemsQuery, GetPurchasedItemsQueryHandler>()
            .AddMessageHandler<PrepareToShipCommand, PrepareToShipCommandHandler>()
            .AddMessageHandler<ValidatePurchasedItemCommand, ValidatePurchasedItemCommandHandler>()
            .AddWorkflow<ItemsPurchasedEvent, PurchaseWorkflow>(msg => msg.PurchaseId, workflow =>
            {
                workflow
                    .AddStep<PurchasedItemsQueryResult>(msg => msg.PurchaseId)
                    .AddStep<PurchasedItemValidationSucceededEvent>(msg => msg.PurchaseId)
                    .AddStep<PurchasedItemValidationFailedEvent>(msg => msg.PurchaseId)
                    .AddStep<ItemsShippedEvent>(msg => msg.PurchaseId);
            })
            .AddWorkflow<ShipItemsCommand, ShippingWorkflow>(msg => msg.ShippingId, workflow =>
            {
                workflow
                    .AddStep<ItemShippingPreparedEvent>(msg => msg.ShippingId);
            });
    }
}