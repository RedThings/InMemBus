namespace InMemBus.Saga;

public class SagaCreationResult<TStartingMessage>(
    bool success, 
    InMemBusSaga<TStartingMessage> saga, 
    Action postSuccess
) where TStartingMessage : class
{
    public bool Success { get; } = success;
    public InMemBusSaga<TStartingMessage> Saga { get; } = saga;

    public void ProcessSuccessfullyHandledMessage()
    {
        postSuccess.Invoke();
    }
}