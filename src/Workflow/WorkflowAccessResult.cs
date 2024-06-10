namespace InMemBus.Workflow;

internal class WorkflowAccessResult<TMessage>(
    bool success, 
    IInMemBusWorkflowStep<TMessage> workflowStep, 
    Action postSuccess
) where TMessage : class
{
    public bool Success { get; } = success;
    public IInMemBusWorkflowStep<TMessage> WorkflowStep { get; } = workflowStep;

    public void ProcessSuccessfullyHandledMessage()
    {
        postSuccess.Invoke();
    }
}