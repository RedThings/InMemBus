namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record ItemShippingPreparedEvent(Guid PurchaseId, Guid ShippingId, Guid PurchasedItemId);