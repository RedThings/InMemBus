namespace InMemBus.Saga;

internal record SagaStep(bool IsStarting, Type MessageBeingHandledType, Func<object, Guid> CompiledFinderExpression);