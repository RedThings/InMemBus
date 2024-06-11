namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public record ValidatePurchasedItemCommand(Guid PurchaseId, PurchasedItem PurchasedItem);