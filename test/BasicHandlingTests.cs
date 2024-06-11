using InMemBus.TestInfrastructure;
using InMemBus.TestInfrastructure.ComplexWorkflow;
using Xunit;
using Xunit.Abstractions;

namespace InMemBus.Tests;

public class BasicHandlingTests(ITestOutputHelper testOutputHelper) : IDisposable
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
        var (inMemBus, testDataAsserter) = TestHelper.Instance.Setup(c =>
        {
            c.AddMessageHandler<DoSomethingCommand, DoSomethingCommandHandler>();
        }, testOutputHelper, cancellationTokenSource.Token);

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
        var (inMemBus, testDataAsserter) = TestHelper.Instance.Setup(config =>
        {
            config
                .AddMessageHandler<SomethingHappenedEvent, SomethingHappenedEventHandler1>()
                .AddMessageHandler<SomethingHappenedEvent, SomethingHappenedEventHandler2>();
        }, testOutputHelper, cancellationTokenSource.Token);

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
        var (inMemBus, testDataAsserter) = TestHelper.Instance.SetupComplexWorkflow(testOutputHelper, cancellationTokenSource.Token);

        var purchaseId = Guid.NewGuid();
        const int maxPollingSeconds = 30;

        // Act
        inMemBus.Publish(new ItemsPurchasedEvent(purchaseId));

        // Assert (that original workflow completes, and new one cannot be started on step 2)
        Assert.True(testDataAsserter.Poll(t => t.Assert(purchaseId), maxPollingSeconds));

        var newPurchaseId = Guid.NewGuid();

        inMemBus.Publish(new PurchasedItemValidationSucceededEvent(newPurchaseId, Guid.NewGuid()));

        Assert.False(testDataAsserter.Poll(t => t.Assert(newPurchaseId), maxPollingSeconds));
    }

    [Fact]
    public void GivenIHaveAComplexWorkflowThatTimesOut_ThatWorkflowShouldTimeOut()
    {
        // Arrange
        var (inMemBus, testDataAsserter) = TestHelper.Instance.SetupComplexWorkflow(testOutputHelper, cancellationTokenSource.Token);

        var purchaseId = Guid.NewGuid();
        const string testInstruction = "add-timeout";
        var testValue = Guid.NewGuid().ToString();

        // Act
        inMemBus.Publish(new ItemsPurchasedEvent(purchaseId, testInstruction, testValue));

        // Assert
        Assert.True(testDataAsserter.Poll(t => t.Assert(testValue)));
    }
}

