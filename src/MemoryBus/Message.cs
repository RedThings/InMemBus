namespace InMemBus.MemoryBus;

internal class Message(object payload)
{
    public object Payload { get; } = payload;
    public int RequeueAttempts { get; private set; }

    public void AddRequeueAttempt()
    {
        RequeueAttempts++;
    }
}