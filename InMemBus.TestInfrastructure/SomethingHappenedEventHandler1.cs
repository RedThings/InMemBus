namespace InMemBus.TestInfrastructure;

public class SomethingHappenedEventHandler1(TestDataAsserter testDataAsserter) : IInMemBusMessageHandler<SomethingHappenedEvent>
{
    public Task HandleAsync(SomethingHappenedEvent message, CancellationToken cancellationToken)
    {
        testDataAsserter.Add(message.Id);

        return Task.CompletedTask;
    }
}