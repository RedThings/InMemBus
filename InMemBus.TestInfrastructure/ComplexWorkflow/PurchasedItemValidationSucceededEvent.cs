namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public record PurchasedItemValidationSucceededEvent(Guid PurchaseId, Guid PurchasedItemId);