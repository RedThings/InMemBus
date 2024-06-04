namespace InMemBus.Saga;

public record SagaStep(bool IsStarting, Type MessageBeingHandledType, Func<object, Guid> CompiledFinderExpression);