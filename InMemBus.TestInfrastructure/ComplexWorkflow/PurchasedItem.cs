namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public record PurchasedItem(Guid PurchasedItemId, Guid ProductId, decimal PurchasedAtPrice);