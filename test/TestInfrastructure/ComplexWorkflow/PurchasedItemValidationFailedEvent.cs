namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record PurchasedItemValidationFailedEvent(Guid PurchaseId, Guid PurchasedItemId, string Reason);