namespace InMemBus.Saga;

internal class DefaultSagaStep<TMessage> : IInMemBusSagaStep<TMessage>
    where TMessage : class
{
    public Task HandleStepAsync(TMessage message, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}