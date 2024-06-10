using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace InMemBus.Workflow;

internal class WorkflowsConfiguration
{
    private readonly List<WorkflowConfiguration> workflowConfigurations = [];
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<WorkflowConfiguration>> cachedConfigurations = [];

    public void AddWorkflow<TStartingMessage, TWorkflow>(
        Expression<Func<TStartingMessage, Guid>> workflowIdFinderExpression, 
        InMemBusWorkflowStepsConfiguration<TStartingMessage, TWorkflow> stepConfiguration)
        where TStartingMessage : class 
        where TWorkflow : InMemBusWorkflow<TStartingMessage>
    {
        var compiledFinderExpression = workflowIdFinderExpression.Compile();

        var startingStep = new WorkflowStep(IsStarting: true, typeof(TStartingMessage), CastCompiledFinderExpression);

        workflowConfigurations.Add(new WorkflowConfiguration(startingStep, stepConfiguration.BuildSteps()));
        
        return;

        Guid CastCompiledFinderExpression(object obj) => compiledFinderExpression((TStartingMessage) obj);
    }

    public IReadOnlyCollection<WorkflowConfiguration> GetWorkflowConfigurations<TMessage>()
    {
        var messageType = typeof(TMessage);
        var cachedConfigExists = cachedConfigurations.TryGetValue(messageType, out var configurations);

        if (cachedConfigExists && configurations != null)
        {
            return configurations;
        }

        configurations = workflowConfigurations.Where(x => x.HandlesMessage(messageType)).ToArray();

        cachedConfigurations.TryAdd(messageType, configurations);

        return configurations;
    }
}