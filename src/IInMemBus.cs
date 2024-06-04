namespace InMemBus;

public interface IInMemBus
{
    public void Send<TMessage>(TMessage message) where TMessage : class;
    public void Publish<TEvent>(TEvent @event) where TEvent : class;
    internal IEnumerable<object> GetNextMessagesToProcess();
    internal void AddToHeadOfQueue(object message);
    internal void ReleaseHandlingSlot(object message);
}