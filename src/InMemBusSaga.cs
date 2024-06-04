namespace InMemBus;

public abstract class InMemBusSaga<TStartingMessage>
    where TStartingMessage : class
{
    public abstract Task HandleStartAsync(TStartingMessage message, CancellationToken cancellationToken);

    protected void CompleteSaga()
    {
        Complete = true;
    }

    internal bool Complete;
}

