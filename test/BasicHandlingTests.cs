using InMemBus.Tests.TestInfrastructure;
using Xunit;

namespace InMemBus.Tests;

public class BasicHandlingTests : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public void Dispose()
    {
        cancellationTokenSource.Cancel();
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
}

