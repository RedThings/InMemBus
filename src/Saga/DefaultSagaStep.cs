namespace InMemBus.Saga;

public class DefaultSagaStep<TMessage> : IInMemBusSagaStep<TMessage>
    where TMessage : class
{
    public Task HandleStepAsync(TMessage message, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}