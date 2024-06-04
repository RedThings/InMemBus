namespace InMemBus;

public interface IInMemBusMessageHandler<in TMessage>
    where TMessage : class
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}