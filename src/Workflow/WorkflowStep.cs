namespace InMemBus.Workflow;

internal record WorkflowStep(bool IsStarting, Type MessageBeingHandledType, Func<object, Guid> CompiledFinderExpression);