using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InMemBus.Workflow;

internal class WorkflowManager : IWorkflowManager
{
    private readonly ILogger<WorkflowManager> logger;
    private readonly ConcurrentDictionary<Guid, AliveWorkflow> workflows = [];

    public WorkflowManager(ILogger<WorkflowManager> logger)
    {
        this.logger = logger;
        StartCheckingTimeouts();
    }

    public WorkflowCreationResult<TStartingMessage> TryCreateNewWorkflow<TStartingMessage>(IServiceProvider currentScopeServiceProvider, object message, WorkflowStep step)
        where TStartingMessage : class
    {
        var workflowId = step.CompiledFinderExpression.Invoke(message);
        var workflowExists = workflows.TryGetValue(workflowId, out var aliveWorkflow);

        if (workflowExists)
        {
            return new WorkflowCreationResult<TStartingMessage>(
                success: false,
                new DefaultWorkflow<TStartingMessage>(),
                () => { }
            );
        }

        var newWorkflow = currentScopeServiceProvider.GetRequiredService<InMemBusWorkflow<TStartingMessage>>();
        aliveWorkflow = new AliveWorkflow(
            newWorkflow,
            getIsComplete: () => newWorkflow.Complete,
            getOutstandingTimeouts: () => newWorkflow.GetOutstandingTimeouts(),
            removeOutstandingTimeouts: () => newWorkflow.RemoveOutstandingTimeouts());

        var addedOk = workflows.TryAdd(workflowId, aliveWorkflow);

        if (!addedOk)
        {
            return new WorkflowCreationResult<TStartingMessage>(
                success: false,
                new DefaultWorkflow<TStartingMessage>(),
                () => { }
            );
        }

        return new WorkflowCreationResult<TStartingMessage>(
            success: true,
            newWorkflow,
            () =>
            {
                if (newWorkflow.Complete)
                {
                    workflows.Remove(workflowId, out _);
                    return;
                }

                aliveWorkflow.Unlock();
            }
        );
    }

    public WorkflowAccessResult<TMessage> TryGetWorkflowForStep<TMessage>(object message, WorkflowStep step)
        where TMessage : class
    {
        var workflowId = step.CompiledFinderExpression.Invoke(message);
        var workflowExists = workflows.TryGetValue(workflowId, out var aliveWorkflow);

        if (!workflowExists || aliveWorkflow is not { Workflow: IInMemBusWorkflowStep<TMessage> workflowStep } || aliveWorkflow.Locked)
        {
            return new WorkflowAccessResult<TMessage>(
                success: false,
                new DefaultWorkflowStep<TMessage>(),
                () => { }
            );
        }

        aliveWorkflow.Lock();

        return new WorkflowAccessResult<TMessage>(
            success: true,
            workflowStep,
            () =>
            {
                if (aliveWorkflow.GetIsComplete.Invoke())
                {
                    workflows.Remove(workflowId, out _);
                    return;
                }

                aliveWorkflow.Unlock();
            }
        );
    }

    private void StartCheckingTimeouts()
    {
        Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                var workflowsWithTimeouts = workflows.Where(x => x.Value.HasOutstandingTimeouts()).Select(x => x.Value).ToArray();

                if (workflowsWithTimeouts.Length < 1)
                {
                    continue;
                }

                foreach (var aliveWorkflow in workflowsWithTimeouts)
                {
                    try
                    {
                        if (aliveWorkflow.Locked)
                        {
                            continue;
                        }

                        aliveWorkflow.Lock();

                        try
                        {
                            await aliveWorkflow.ProcessOutstandingTimeouts();
                        }
                        catch (Exception innerEx)
                        {
                            logger.LogError(innerEx, "Error occurred when attempting to process a single workflow's outstanding timeouts");
                        }
                        finally
                        {
                            aliveWorkflow.Unlock();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error occurred when attempting to process all outstanding timeouts");
                    }
                }

                await Task.Delay(1);
            }
            // ReSharper disable once FunctionNeverReturns
        }, TaskCreationOptions.LongRunning);
    }
}