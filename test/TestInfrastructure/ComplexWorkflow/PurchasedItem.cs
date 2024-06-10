namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record PurchasedItem(Guid PurchasedItemId, Guid ProductId, decimal PurchasedAtPrice);