using System.Linq.Expressions;
using InMemBus.MessageHandling;
using InMemBus.Saga;
using Microsoft.Extensions.DependencyInjection;

namespace InMemBus;

public class InMemBusSagaStepsConfiguration<TStartingMessage, TSaga>(IServiceCollection services)
    where TStartingMessage : class
    where TSaga : InMemBusSaga<TStartingMessage>
{
    private readonly List<SagaStep> steps = [];

    public InMemBusSagaStepsConfiguration<TStartingMessage, TSaga> AddStep<TMessage>(Expression<Func<TMessage, Guid>> sagaFinderExpression)
        where TMessage : class
    {
        var stepMessageType = typeof(TMessage);
        var sagaType = typeof(TSaga);

        if (stepMessageType == typeof(TStartingMessage))
        {
            throw new Exception($"A saga cannot handle the same message type twice - offending type: {stepMessageType}. Offending saga type = {sagaType}");
        }

        services.AddScoped(typeof(IInMemBusSagaStep<TMessage>), sagaType);

        var messageHandlerType = typeof(MessageHandler<TMessage>);
        var messageHandlerRegistrationExists = services.Any(x => x.ServiceType == messageHandlerType);

        if (!messageHandlerRegistrationExists)
        {
            services.AddScoped(messageHandlerType);
        }

        var compiledFinderExpression = sagaFinderExpression.Compile();

        steps.Add(new SagaStep(IsStarting: false, stepMessageType, CastCompiledFinderExpression));

        return this;

        Guid CastCompiledFinderExpression(object obj) => compiledFinderExpression((TMessage) obj);
    }

    internal IReadOnlyCollection<SagaStep> BuildSteps() => steps;
}