namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public record PurchaseFailedEvent(Guid PurchaseId, string Reason);