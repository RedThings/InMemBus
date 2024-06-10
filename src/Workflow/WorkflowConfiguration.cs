namespace InMemBus.Workflow;

internal class WorkflowConfiguration(
    WorkflowStep startingStep,
    IReadOnlyCollection<WorkflowStep> otherSteps
)
{
    public bool HandlesMessage(Type messageType) =>
        startingStep.MessageBeingHandledType == messageType ||
        otherSteps.Select(x => x.MessageBeingHandledType).Contains(messageType);

    public WorkflowStep GetStepForMessage(Type type) =>
        startingStep.MessageBeingHandledType == type 
            ? startingStep 
            : otherSteps.Single(x => x.MessageBeingHandledType == type);
}