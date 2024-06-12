namespace InMemBus.MemoryBus;

internal class Message(object payload)
{
    public object Payload { get; } = payload;
    public int RequeueAttempts { get; private set; }
    public Guid Id { get; private set; } = Guid.NewGuid();

    public void AddRequeueAttempt()
    {
        RequeueAttempts++;
    }

    public Message WithId(Guid id)
    {
        Id = id;
        return this;
    }
}