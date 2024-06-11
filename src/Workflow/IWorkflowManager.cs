namespace InMemBus.Workflow;

internal interface IWorkflowManager
{
    WorkflowCreationResult<TStartingMessage> TryCreateNewWorkflow<TStartingMessage>(IServiceProvider currentScopeServiceProvider, object message, WorkflowStep step)
        where TStartingMessage : class;

    WorkflowAccessResult<TMessage> TryGetWorkflowForStep<TMessage>(object message, WorkflowStep step)
        where TMessage : class;

    Task ProcessOutstandingTimeoutsAsync();
}