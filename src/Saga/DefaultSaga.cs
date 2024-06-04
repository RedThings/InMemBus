namespace InMemBus.Saga;

public class DefaultSaga<TStartingMessage> : InMemBusSaga<TStartingMessage>
    where TStartingMessage : class
{
    public override Task HandleStartAsync(TStartingMessage message, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}