namespace InMemBus.Workflow;

internal class WorkflowCreationResult<TStartingMessage>(
    bool success, 
    InMemBusWorkflow<TStartingMessage> workflow, 
    Action postSuccess
) where TStartingMessage : class
{
    public bool Success { get; } = success;
    public InMemBusWorkflow<TStartingMessage> Workflow { get; } = workflow;

    public void ProcessSuccessfullyHandledMessage()
    {
        postSuccess.Invoke();
    }
}