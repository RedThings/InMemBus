namespace InMemBus.MemoryBus;

public class Message(object payload)
{
    public object Payload { get; } = payload;
    public int RequeueAttempts { get; private set; }

    public void AddRequeueAttempt()
    {
        RequeueAttempts++;
    }
}