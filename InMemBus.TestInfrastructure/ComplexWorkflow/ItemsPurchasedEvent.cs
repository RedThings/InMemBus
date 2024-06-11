namespace InMemBus.TestInfrastructure.ComplexWorkflow;

public record ItemsPurchasedEvent(Guid PurchaseId, string TestInstruction = "", string TestValue = "");