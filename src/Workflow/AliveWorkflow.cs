namespace InMemBus.Workflow;

internal class AliveWorkflow(
    object workflow,
    Func<bool> getIsComplete,
    Func<IReadOnlyCollection<Func<Task>>> getOutstandingTimeouts,
    Action removeOutstandingTimeouts)
{
    public object Workflow { get; } = workflow;
    public Func<bool> GetIsComplete { get; } = getIsComplete;
    public bool Locked { get; private set; } = true;
    public void Lock() => Locked = true;
    public void Unlock() => Locked = false;

    public bool HasOutstandingTimeouts() => getOutstandingTimeouts.Invoke().Count > 0;

    public async Task ProcessOutstandingTimeouts()
    {
        var outstandingTimeouts = getOutstandingTimeouts.Invoke();

        foreach (var outstandingTimeout in outstandingTimeouts)
        {
            await outstandingTimeout.Invoke();
        }

        removeOutstandingTimeouts.Invoke();
    }
}