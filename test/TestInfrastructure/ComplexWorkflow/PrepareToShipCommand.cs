namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record PrepareToShipCommand(Guid PurchaseId, Guid ShippingId, PurchasedItem Item);