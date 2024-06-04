namespace InMemBus.Saga;

public class AliveSaga(
    object saga,
    Func<bool> getIsComplete)
{
    public object Saga { get; } = saga;
    public Func<bool> GetIsComplete { get; } = getIsComplete;
    public bool Locked { get; private set; } = true;
    public void Lock() => Locked = true;
    public void Unlock() => Locked = false;
}