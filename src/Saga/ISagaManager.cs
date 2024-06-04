namespace InMemBus.Saga;

public interface ISagaManager
{
    SagaCreationResult<TStartingMessage> TryCreateNewSaga<TStartingMessage>(IServiceProvider currentScopeServiceProvider, object message, SagaStep step)
        where TStartingMessage : class;

    SagaAccessResult<TMessage> TryGetSagaForStep<TMessage>(object message, SagaStep step)
        where TMessage : class;
}