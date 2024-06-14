using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace InMemBus.MemoryBus;

internal class InMemoryBus(ILogger<InMemoryBus> logger, InMemBusConfiguration configuration) : IInMemBus
{
    private readonly ConcurrentQueue<Message> queue = [];
    private readonly ConcurrentBag<Message> receivedMessages = [];
    private readonly ConcurrentBag<Message> processedMessages = [];

    public async Task SendAsync<TMessage>(TMessage message)
        where TMessage : class
    {
        await Task.Run(() =>
        {
            if (configuration.DebugMode)
            {
                var id = Guid.NewGuid();

                if (receivedMessages.Any(x => x.Id == id))
                {
                    logger.LogError("Message with the ID {id} has already been received - nothing will happen, this is just for information", id);
                }

                if (processedMessages.Any(x => x.Id == id))
                {
                    logger.LogError("Message with the ID {id} has already been processed - nothing will happen, this is just for information", id);
                }

                receivedMessages.Add(new Message(message).WithId(id));
            }

            queue.Enqueue(new Message(message));
        }).ConfigureAwait(false);
    }

    public Task PublishAsync<TEvent>(TEvent @event)
        where TEvent : class =>
        SendAsync(@event);

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

    public async Task RequeueAsync(Message message)
    {
        await Task.Run(() =>
        {
            message.AddRequeueAttempt();

            if (message.RequeueAttempts > 1000) // arbitrary but will do for now
            {
                logger.LogError("Message is poison. Will not requeue");
                return;
            }

            queue.Enqueue(message);
        }).ConfigureAwait(false);
    }

    public void AddProcessedMessage(Message message)
    {
        processedMessages.Add(message);
    }
}