namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record PurchasedItemsQueryResult(Guid PurchaseId, IReadOnlyCollection<PurchasedItem> Items);