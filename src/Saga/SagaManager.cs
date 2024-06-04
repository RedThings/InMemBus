using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace InMemBus.Saga;

public class SagaManager : ISagaManager
{
    private readonly ConcurrentDictionary<Guid, AliveSaga> sagas = [];

    public SagaCreationResult<TStartingMessage> TryCreateNewSaga<TStartingMessage>(IServiceProvider currentScopeServiceProvider, object message, SagaStep step)
        where TStartingMessage : class
    {
        var sagaId = step.CompiledFinderExpression.Invoke(message);
        var sagaExists = sagas.TryGetValue(sagaId, out var aliveSaga);

        if (sagaExists)
        {
            return new SagaCreationResult<TStartingMessage>(
                success: false,
                new DefaultSaga<TStartingMessage>(),
                () => { }
            );
        }

        var newSaga = currentScopeServiceProvider.GetRequiredService<InMemBusSaga<TStartingMessage>>();
        aliveSaga = new AliveSaga(newSaga, () => newSaga.Complete);

        var addedOk = sagas.TryAdd(sagaId, aliveSaga);

        if (!addedOk)
        {
            return new SagaCreationResult<TStartingMessage>(
                success: false,
                new DefaultSaga<TStartingMessage>(),
                () => { }
            );
        }

        return new SagaCreationResult<TStartingMessage>(
            success: true,
            newSaga,
            () =>
            {
                if (newSaga.Complete)
                {
                    sagas.Remove(sagaId, out _);
                    return;
                }

                aliveSaga.Unlock();
            }
        );
    }

    public SagaAccessResult<TMessage> TryGetSagaForStep<TMessage>(object message, SagaStep step)
        where TMessage : class
    {
        var sagaId = step.CompiledFinderExpression.Invoke(message);
        var sagaExists = sagas.TryGetValue(sagaId, out var aliveSaga);

        if (!sagaExists || aliveSaga is not { Saga: IInMemBusSagaStep<TMessage> sagaStep } || aliveSaga.Locked)
        {
            return new SagaAccessResult<TMessage>(
                success: false,
                new DefaultSagaStep<TMessage>(),
                () => { }
            );
        }

        aliveSaga.Lock();

        return new SagaAccessResult<TMessage>(
            success: true,
            sagaStep,
            () =>
            {
                if (aliveSaga.GetIsComplete.Invoke())
                {
                    sagas.Remove(sagaId, out _);
                    return;
                }

                aliveSaga.Unlock();
            }
        );
    }
}