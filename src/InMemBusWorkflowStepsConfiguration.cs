using System.Linq.Expressions;
using InMemBus.MessageHandling;
using InMemBus.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace InMemBus;

public class InMemBusWorkflowStepsConfiguration<TStartingMessage, TWorkflow>(IServiceCollection services)
    where TStartingMessage : class
    where TWorkflow : InMemBusWorkflow<TStartingMessage>
{
    private readonly List<WorkflowStep> steps = [];

    public InMemBusWorkflowStepsConfiguration<TStartingMessage, TWorkflow> AddStep<TMessage>(Expression<Func<TMessage, Guid>> workflowFinderExpression)
        where TMessage : class
    {
        var stepMessageType = typeof(TMessage);
        var workflowType = typeof(TWorkflow);

        if (stepMessageType == typeof(TStartingMessage))
        {
            throw new Exception($"A workflow cannot handle the same message type twice - offending type: {stepMessageType}. Offending workflow type = {workflowType}");
        }

        services.AddScoped(typeof(IInMemBusWorkflowStep<TMessage>), workflowType);

        var messageHandlerType = typeof(MessageHandler<TMessage>);
        var messageHandlerRegistrationExists = services.Any(x => x.ServiceType == messageHandlerType);

        if (!messageHandlerRegistrationExists)
        {
            services.AddScoped(messageHandlerType);
        }

        var compiledFinderExpression = workflowFinderExpression.Compile();

        steps.Add(new WorkflowStep(IsStarting: false, stepMessageType, CastCompiledFinderExpression));

        return this;

        Guid CastCompiledFinderExpression(object obj) => compiledFinderExpression((TMessage) obj);
    }

    internal IReadOnlyCollection<WorkflowStep> BuildSteps() => steps;
}