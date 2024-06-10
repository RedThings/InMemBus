namespace InMemBus.Workflow;

internal class DefaultWorkflow<TStartingMessage> : InMemBusWorkflow<TStartingMessage>
    where TStartingMessage : class
{
    public override Task HandleStartAsync(TStartingMessage message, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}