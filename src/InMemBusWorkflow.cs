namespace InMemBus;

public abstract class InMemBusWorkflow<TStartingMessage>
    where TStartingMessage : class
{
    private readonly List<(Func<Task> Action, DateTime DoNotProcessBefore)> timeouts = [];

    public abstract Task HandleStartAsync(TStartingMessage message, CancellationToken cancellationToken);

    protected void CompleteWorkflow()
    {
        Complete = true;
    }

    protected void AddTimeout(Func<Task> action, TimeSpan timeSpan)
    {
        timeouts.Add((action, DateTime.UtcNow.Add(timeSpan)));
    }

    internal bool Complete;

    internal IReadOnlyCollection<Func<Task>> GetOutstandingTimeouts()
    {
        var now = DateTime.UtcNow;

        return timeouts.Where(x => x.DoNotProcessBefore <= now).Select(x => x.Action).ToArray();
    }

    internal void RemoveOutstandingTimeouts()
    {
        var now = DateTime.UtcNow;
        var toRemove = timeouts.Where(x => x.DoNotProcessBefore <= now);

        foreach (var timeout in toRemove)
        {
            timeouts.Remove(timeout);
        }
    }
}

