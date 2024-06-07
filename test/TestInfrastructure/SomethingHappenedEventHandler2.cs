namespace InMemBus.Tests.TestInfrastructure;

public class SomethingHappenedEventHandler2(TestDataAsserter testDataAsserter) : IInMemBusMessageHandler<SomethingHappenedEvent>
{
    public Task HandleAsync(SomethingHappenedEvent message, CancellationToken cancellationToken)
    {
        testDataAsserter.Add(message.Id);

        return Task.CompletedTask;
    }
}