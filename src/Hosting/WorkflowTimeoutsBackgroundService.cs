using InMemBus.Workflow;
using Microsoft.Extensions.Hosting;

namespace InMemBus.Hosting;

internal class WorkflowTimeoutsBackgroundService(WorkflowTimeoutsObserver workflowTimeoutsObserver) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) =>
        await workflowTimeoutsObserver.ExecuteAsync(stoppingToken);
}