namespace InMemBus.Saga;

public class SagaAccessResult<TMessage>(
    bool success, 
    IInMemBusSagaStep<TMessage> sagaStep, 
    Action postSuccess
) where TMessage : class
{
    public bool Success { get; } = success;
    public IInMemBusSagaStep<TMessage> SagaStep { get; } = sagaStep;

    public void ProcessSuccessfullyHandledMessage()
    {
        postSuccess.Invoke();
    }
}