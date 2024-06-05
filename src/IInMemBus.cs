using InMemBus.MemoryBus;

namespace InMemBus;

public interface IInMemBus
{
    public void Send<TMessage>(TMessage message) where TMessage : class;
    public void Publish<TEvent>(TEvent @event) where TEvent : class;
    internal IReadOnlyCollection<Message> GetNextMessagesToProcess(int maxMessagesToDequeue);
    internal void Requeue(Message message);
}