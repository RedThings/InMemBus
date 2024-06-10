namespace InMemBus.Tests.TestInfrastructure.ComplexWorkflow;

public record ItemsPurchasedEvent(Guid PurchaseId, string TestInstruction = "", string TestValue = "");