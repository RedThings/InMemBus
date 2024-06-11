namespace InMemBus.TestInfrastructure;

public class DoSomethingCommandHandler(TestDataAsserter testDataAsserter) : IInMemBusMessageHandler<DoSomethingCommand>
{
    public Task HandleAsync(DoSomethingCommand message, CancellationToken cancellationToken)
    {
        testDataAsserter.Add(message.Id);

        return Task.CompletedTask;
    }
}