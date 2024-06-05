using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace InMemBus.MemoryBus;

internal class InMemoryBus(ILogger<InMemoryBus> logger) : IInMemBus
{
    private readonly ConcurrentQueue<Message> queue = [];

    public void Send<TMessage>(TMessage message)
        where TMessage : class
    {
        queue.Enqueue(new Message(message));
    }

    public void Publish<TEvent>(TEvent @event)
        where TEvent : class =>
        Send(@event);

    public IReadOnlyCollection<Message> GetNextMessagesToProcess(int maxMessagesToDequeue)
    {
        maxMessagesToDequeue = Math.Min(maxMessagesToDequeue, queue.Count);
        var messagesToProcess = new List<Message>(maxMessagesToDequeue);

        for (var i = 0; i < maxMessagesToDequeue; i++)
        {
            var ok = queue.TryDequeue(out var message);

            if (!ok || message == null)
            {
                continue;
            }

            messagesToProcess.Add(message);
        }

        return messagesToProcess;
    }

    public void Requeue(Message message)
    {
        message.AddRequeueAttempt();

        if (message.RequeueAttempts > 1000) // arbitrary but will do for now
        {
            logger.LogError("Message is poison. Will not requeue");
            return;
        }

        queue.Enqueue(message);
    }
}