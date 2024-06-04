namespace InMemBus.Saga;

public class SagaConfiguration(
    SagaStep startingStep,
    IReadOnlyCollection<SagaStep> otherSteps
)
{
    public bool HandlesMessage(Type messageType) =>
        startingStep.MessageBeingHandledType == messageType ||
        otherSteps.Select(x => x.MessageBeingHandledType).Contains(messageType);

    public SagaStep GetStepForMessage(Type type) =>
        startingStep.MessageBeingHandledType == type 
            ? startingStep 
            : otherSteps.Single(x => x.MessageBeingHandledType == type);
}