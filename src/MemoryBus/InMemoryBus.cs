using Microsoft.Extensions.Logging;

namespace InMemBus.MemoryBus;

internal class InMemoryBus(
    ILogger<InMemoryBus> logger,
    InMemBusConfiguration configuration) : IInMemBus
{
    private readonly PriorityQueue<object, int> queue = new();
    private readonly List<object> handlingSlots = [];

    public void Send<TMessage>(TMessage message)
        where TMessage : class
    {
        queue.Enqueue(message, queue.Count);
    }

    public void Publish<TEvent>(TEvent @event)
        where TEvent : class => 
        Send(@event);

    public IEnumerable<object> GetNextMessagesToProcess()
    {
        var maxConcurrent = configuration.MaximumHandlingConcurrency;
        var totalInQueue = queue.Count;
        var availableHandlingSlots = maxConcurrent - handlingSlots.Count;
        var messagesToGetCount = Math.Min(maxConcurrent, totalInQueue);

        if (messagesToGetCount > availableHandlingSlots)
        {
            messagesToGetCount = availableHandlingSlots;
        }

        var messagesToProcess = new List<object>(messagesToGetCount);

        for (var i = 0; i < messagesToGetCount; i++)
        {
            var ok = queue.TryDequeue(out var message, out _);

            if (!ok || message == null)
            {
                continue;
            }

            messagesToProcess.Add(message);
            handlingSlots.Add(message);
        }

        return messagesToProcess;
    }

    public void AddToHeadOfQueue(object message)
    {
        queue.Enqueue(message, int.MinValue);
    }

    public void ReleaseHandlingSlot(object message)
    {
        try
        {
            handlingSlots.Remove(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Attempt to remove message from handling slot failed");
        }
    }
}