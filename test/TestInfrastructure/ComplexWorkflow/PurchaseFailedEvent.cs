namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record PurchaseFailedEvent(Guid PurchaseId, string Reason);