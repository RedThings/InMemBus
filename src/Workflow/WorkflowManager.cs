using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace InMemBus.Workflow;

internal class WorkflowManager : IWorkflowManager
{
    private readonly ConcurrentDictionary<Guid, AliveWorkflow> workflows = [];

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
        aliveWorkflow = new AliveWorkflow(newWorkflow, () => newWorkflow.Complete);

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
}