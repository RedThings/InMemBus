namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public record ShipItemsCommand(Guid PurchaseId, Guid ShippingId, IReadOnlyCollection<PurchasedItem> Items);