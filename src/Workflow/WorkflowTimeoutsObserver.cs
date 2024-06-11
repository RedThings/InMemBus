namespace InMemBus.Workflow;

internal class WorkflowTimeoutsObserver(IWorkflowManager workflowManager)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await workflowManager.ProcessOutstandingTimeoutsAsync();

            await Task.Delay(1000, cancellationToken);
        }
    }
}