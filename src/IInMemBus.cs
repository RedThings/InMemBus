using InMemBus.MemoryBus;

namespace InMemBus;

public interface IInMemBus
{
    public Task SendAsync<TMessage>(TMessage message) where TMessage : class;
    public Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
    internal IReadOnlyCollection<Message> GetNextMessagesToProcess(int maxMessagesToDequeue);
    internal Task RequeueAsync(Message message);
    internal void AddProcessedMessage(Message message);
}