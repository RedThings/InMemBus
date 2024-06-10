namespace InMemBus.Workflow;

internal class DefaultWorkflowStep<TMessage> : IInMemBusWorkflowStep<TMessage>
    where TMessage : class
{
    public Task HandleStepAsync(TMessage message, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}