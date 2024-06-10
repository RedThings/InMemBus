namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record ShipItemsCommand(Guid PurchaseId, Guid ShippingId, IReadOnlyCollection<PurchasedItem> Items);