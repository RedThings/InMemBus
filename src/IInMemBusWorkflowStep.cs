namespace InMemBus;

public interface IInMemBusWorkflowStep<in TMessage> where TMessage : class
{
    Task HandleStepAsync(TMessage message, CancellationToken cancellationToken);
}