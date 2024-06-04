namespace InMemBus;

public interface IInMemBusSagaStep<in TMessage> where TMessage : class
{
    Task HandleStepAsync(TMessage message, CancellationToken cancellationToken);
}