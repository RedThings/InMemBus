namespace InMemBus.Workflow;

internal class AliveWorkflow(
    object workflow,
    Func<bool> getIsComplete)
{
    public object Workflow { get; } = workflow;
    public Func<bool> GetIsComplete { get; } = getIsComplete;
    public bool Locked { get; private set; } = true;
    public void Lock() => Locked = true;
    public void Unlock() => Locked = false;
}