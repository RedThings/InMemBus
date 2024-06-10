using InMemBus.Tests.TestInfrastructure;
using InMemBus.Tests.TestInfrastructure.ComplexWorkflow;
using Xunit;

namespace InMemBus.Tests;

public class BasicHandlingTests : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public void Dispose()
    {
        cancellationTokenSource.Cancel();

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GivenIHaveSentAMessage_ItShouldBeHandled()
    {
        // Arrange
        var (inMemBus, testDataAsserter) = InMemBusTester.Instance.Setup(c =>
        {
            c.AddMessageHandler<DoSomethingCommand, DoSomethingCommandHandler>();
        }, cancellationTokenSource.Token);

        var id = Guid.NewGuid();

        // Act
        inMemBus.Send(new DoSomethingCommand(id));

        // Assert
        Assert.True(testDataAsserter.Poll(t => t.Assert(id)));
    }

    [Fact]
    public void GivenIHavePublishedAMessage_ItShouldBeHandledInMultipleHandlers()
    {
        // Arrange
        var (inMemBus, testDataAsserter) = InMemBusTester.Instance.Setup(config =>
        {
            config
                .AddMessageHandler<SomethingHappenedEvent, SomethingHappenedEventHandler1>()
                .AddMessageHandler<SomethingHappenedEvent, SomethingHappenedEventHandler2>();
        }, cancellationTokenSource.Token);

        var id = Guid.NewGuid();

        // Act
        inMemBus.Publish(new SomethingHappenedEvent(id));

        // Assert
        Assert.True(testDataAsserter.Poll(t => t.AssertMultiple(id, 2)));
    }

    [Fact]
    public void GivenIHaveAComplexWorkflow_ThatWorkflowShouldBeProcessed_AndTheWorkflowCannotBeReStartedByANonStartingStep()
    {
        // Arrange
        var (inMemBus, testDataAsserter) = SetupComplexWorkflow();

        var purchaseId = Guid.NewGuid();
        const int maxPollingSeconds = 30;

        // Act
        inMemBus.Publish(new ItemsPurchasedEvent(purchaseId));

        // Assert (that original workflow completes, and new one cannot be started on step 2)
        Assert.True(testDataAsserter.Poll(t => t.Assert(purchaseId), maxPollingSeconds));

        testDataAsserter.Remove(purchaseId);

        inMemBus.Publish(new PurchasedItemValidationSucceededEvent(purchaseId, Guid.NewGuid()));

        Assert.False(testDataAsserter.Poll(t => t.Assert(purchaseId), maxPollingSeconds));
    }

    [Fact]
    public void GivenIHaveAComplexWorkflowThatTimesOut_ThatWorkflowShouldTimeOut()
    {
        // Arrange
        var (inMemBus, testDataAsserter) = SetupComplexWorkflow();

        var purchaseId = Guid.NewGuid();
        const string testInstruction = "add-timeout";
        var testValue = Guid.NewGuid().ToString();

        // Act
        inMemBus.Publish(new ItemsPurchasedEvent(purchaseId, testInstruction, testValue));

        // Assert
        Assert.True(testDataAsserter.Poll(t => t.Assert(testValue)));
    }

    private (IInMemBus inMemBus, TestDataAsserter testDataAsserter) SetupComplexWorkflow()
    {
        return InMemBusTester.Instance.Setup(config =>
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
        }, cancellationTokenSource.Token);
    }
}

