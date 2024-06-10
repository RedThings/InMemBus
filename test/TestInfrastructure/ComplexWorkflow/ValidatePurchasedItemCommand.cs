namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record ValidatePurchasedItemCommand(Guid PurchaseId, PurchasedItem PurchasedItem);