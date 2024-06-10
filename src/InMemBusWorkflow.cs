namespace InMemBus;

public abstract class InMemBusWorkflow<TStartingMessage>
    where TStartingMessage : class
{
    public abstract Task HandleStartAsync(TStartingMessage message, CancellationToken cancellationToken);

    protected void CompleteWorkflow()
    {
        Complete = true;
    }

    internal bool Complete;
}

