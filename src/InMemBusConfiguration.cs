using System.Linq.Expressions;
using InMemBus.MessageHandling;
using InMemBus.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace InMemBus;

public class InMemBusConfiguration
{
    private readonly IServiceCollection services;
    private readonly WorkflowsConfiguration workflowsConfiguration;
    private readonly int[] defaultRetryPattern;

    public InMemBusConfiguration(IServiceCollection services)
    {
        this.services = services;

        workflowsConfiguration = new WorkflowsConfiguration();

        services.AddSingleton(workflowsConfiguration);

        defaultRetryPattern =
        [
            100, 100, 100, 100, 1000,
            100, 100, 100, 100, 1000,
            100, 100, 100, 100, 5000,
            100, 100, 100, 100, 1000,
            100, 100, 100, 100
        ];
    }

    public InMemBusConfiguration SetMaximumHandlingConcurrency(int max)
    {
        MaximumHandlingConcurrency = max;
        return this;
    }

    public InMemBusConfiguration UseDebugMode()
    {
        DebugMode = true;
        return this;
    }

    public InMemBusConfiguration AddMessageHandler<TMessage, TMessageHandler>()
        where TMessage : class
        where TMessageHandler : IInMemBusMessageHandler<TMessage> =>
        ConfigureHandler<TMessage, TMessageHandler>();

    public InMemBusConfiguration AddWorkflow<TStartingMessage, TWorkflow>(
        Expression<Func<TStartingMessage, Guid>> workflowIdFinderExpression,
        Action<InMemBusWorkflowStepsConfiguration<TStartingMessage, TWorkflow>> stepConfiguration)
        where TStartingMessage : class
        where TWorkflow : InMemBusWorkflow<TStartingMessage> =>
        ConfigureWorkflow(workflowIdFinderExpression, stepConfiguration);

    internal int MaximumHandlingConcurrency = 2000;
    internal bool DebugMode;

    private InMemBusConfiguration ConfigureHandler<TMessage, TMessageHandler>()
        where TMessage : class
        where TMessageHandler : IInMemBusMessageHandler<TMessage>
    {
        services.AddScoped(typeof(IInMemBusMessageHandler<TMessage>), typeof(TMessageHandler));

        var messageHandlerType = typeof(MessageHandler<TMessage>);
        var messageHandlerRegistrationExists = services.Any(x => x.ServiceType == messageHandlerType);

        if (!messageHandlerRegistrationExists)
        {
            services.AddScoped(messageHandlerType);
        }

        return this;
    }

    private InMemBusConfiguration ConfigureWorkflow<TStartingMessage, TWorkflow>(
        Expression<Func<TStartingMessage, Guid>> workflowIdFinderExpression, 
        Action<InMemBusWorkflowStepsConfiguration<TStartingMessage, TWorkflow>> stepConfigurationAction)
        where TStartingMessage : class
        where TWorkflow : InMemBusWorkflow<TStartingMessage>
    {
        services.AddScoped(typeof(InMemBusWorkflow<TStartingMessage>), typeof(TWorkflow));

        var messageHandlerType = typeof(MessageHandler<TStartingMessage>);
        var messageHandlerRegistrationExists = services.Any(x => x.ServiceType == messageHandlerType);

        if (!messageHandlerRegistrationExists)
        {
            services.AddScoped(messageHandlerType);
        }

        var stepConfiguration = new InMemBusWorkflowStepsConfiguration<TStartingMessage, TWorkflow>(services);
        stepConfigurationAction.Invoke(stepConfiguration);

        workflowsConfiguration.AddWorkflow(workflowIdFinderExpression, stepConfiguration);

        return this;
    }

    // TODO: custom patterns
    internal int[] GetRetryPattern(Type messageType) => defaultRetryPattern;
}