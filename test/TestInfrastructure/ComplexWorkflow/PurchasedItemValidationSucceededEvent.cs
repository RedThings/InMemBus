namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record PurchasedItemValidationSucceededEvent(Guid PurchaseId, Guid PurchasedItemId);