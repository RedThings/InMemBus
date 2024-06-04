using System.Linq.Expressions;
using InMemBus.MessageHandling;
using InMemBus.Saga;
using Microsoft.Extensions.DependencyInjection;

namespace InMemBus;

public class InMemBusConfiguration
{
    private readonly IServiceCollection services;
    private readonly SagasConfiguration sagasConfiguration;
    private readonly int[] defaultRetryPattern;

    public InMemBusConfiguration(IServiceCollection services)
    {
        this.services = services;

        sagasConfiguration = new SagasConfiguration();

        services.AddSingleton(sagasConfiguration);

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

    public InMemBusConfiguration AddMessageHandler<TMessage, TMessageHandler>()
        where TMessage : class
        where TMessageHandler : IInMemBusMessageHandler<TMessage> =>
        ConfigureHandler<TMessage, TMessageHandler>();

    public InMemBusConfiguration AddSaga<TStartingMessage, TSaga>(
        Expression<Func<TStartingMessage, Guid>> sagaIdFinderExpression,
        Action<InMemBusSagaStepsConfiguration<TStartingMessage, TSaga>> stepConfiguration)
        where TStartingMessage : class
        where TSaga : InMemBusSaga<TStartingMessage> =>
        ConfigureSaga(sagaIdFinderExpression, stepConfiguration);

    internal int MaximumHandlingConcurrency = 200;

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

    private InMemBusConfiguration ConfigureSaga<TStartingMessage, TSaga>(
        Expression<Func<TStartingMessage, Guid>> sagaIdFinderExpression, 
        Action<InMemBusSagaStepsConfiguration<TStartingMessage, TSaga>> stepConfigurationAction)
        where TStartingMessage : class
        where TSaga : InMemBusSaga<TStartingMessage>
    {
        services.AddScoped(typeof(InMemBusSaga<TStartingMessage>), typeof(TSaga));

        var messageHandlerType = typeof(MessageHandler<TStartingMessage>);
        var messageHandlerRegistrationExists = services.Any(x => x.ServiceType == messageHandlerType);

        if (!messageHandlerRegistrationExists)
        {
            services.AddScoped(messageHandlerType);
        }

        var stepConfiguration = new InMemBusSagaStepsConfiguration<TStartingMessage, TSaga>(services);
        stepConfigurationAction.Invoke(stepConfiguration);

        sagasConfiguration.AddSaga(sagaIdFinderExpression, stepConfiguration);

        return this;
    }

    // TODO: custom patterns
    internal int[] GetRetryPattern(Type messageType) => defaultRetryPattern;
}