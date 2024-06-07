using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace InMemBus.Saga;

internal class SagasConfiguration
{
    private readonly List<SagaConfiguration> sagaConfigurations = [];
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<SagaConfiguration>> cachedConfigurations = [];

    public void AddSaga<TStartingMessage, TSaga>(
        Expression<Func<TStartingMessage, Guid>> sagaIdFinderExpression, 
        InMemBusSagaStepsConfiguration<TStartingMessage, TSaga> stepConfiguration)
        where TStartingMessage : class 
        where TSaga : InMemBusSaga<TStartingMessage>
    {
        var compiledFinderExpression = sagaIdFinderExpression.Compile();

        var startingStep = new SagaStep(IsStarting: true, typeof(TStartingMessage), CastCompiledFinderExpression);

        sagaConfigurations.Add(new SagaConfiguration(startingStep, stepConfiguration.BuildSteps()));
        
        return;

        Guid CastCompiledFinderExpression(object obj) => compiledFinderExpression((TStartingMessage) obj);
    }

    public IReadOnlyCollection<SagaConfiguration> GetSagaConfigurations<TMessage>()
    {
        var messageType = typeof(TMessage);
        var cachedConfigExists = cachedConfigurations.TryGetValue(messageType, out var configurations);

        if (cachedConfigExists && configurations != null)
        {
            return configurations;
        }

        configurations = sagaConfigurations.Where(x => x.HandlesMessage(messageType)).ToArray();

        cachedConfigurations.TryAdd(messageType, configurations);

        return configurations;
    }
}